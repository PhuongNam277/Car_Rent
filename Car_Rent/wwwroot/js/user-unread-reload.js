(function () {
    if (window.historyScriptLoaded) return;
    window.historyScriptLoaded = true;
    console.log("Initializing History page script (Unread Count Preview Version).");

    const LS_KEY_USER = 'cr_user_unread_v1';

    // --- Hàm Cập Nhật Giao Diện ---
    // Cập nhật một item dựa trên convId, unreadCount và newTimestamp (nếu có)
    function updateConversationItem(convId, unreadCount = 0, newTimestamp = null) {
        const listItem = document.getElementById(`conv-${convId}`);
        if (!listItem) return;

        console.log(`Updating UI for conv-${convId}. UnreadCount: ${unreadCount}`, newTimestamp);

        // Lấy các phần tử con
        const titleElement = listItem.querySelector('h5');
        const previewElement = listItem.querySelector('p.conversation-preview');
        const dotElement = listItem.querySelector('.unread-dot');
        const timeElement = listItem.querySelector('small.conversation-timestamp');

        // Lấy dữ liệu gốc từ data attributes
        const originalPreview = listItem.dataset.originalPreview || "Chưa có tin nhắn.";
        const originalTimestampStr = listItem.dataset.originalTimestamp;

        const isUnread = unreadCount > 0;

        // 1. Cập nhật Style (đậm/chấm đỏ)
        if (titleElement) {
            titleElement.style.fontWeight = isUnread ? 'bold' : 'normal';
            titleElement.style.color = isUnread ? '#212529' : '';
        }
        if (dotElement) {
            dotElement.style.display = isUnread ? 'inline-block' : 'none';
        }

        // 2. Cập nhật Nội dung Preview
        if (previewElement) {
            if (isUnread) {
                previewElement.textContent = `Có ${unreadCount} tin nhắn mới`;
                previewElement.style.fontWeight = 'bold'; // In đậm cả text mới
                previewElement.style.color = '#212529';    // Màu đậm
            } else {
                // Cắt ngắn preview gốc nếu cần
                const previewText = originalPreview.length > 50 ? originalPreview.substring(0, 50) + "..." : originalPreview;
                previewElement.textContent = previewText;
                previewElement.style.fontWeight = 'normal'; // Reset style
                previewElement.style.color = '';       // Reset style
            }
        }

        // 3. Cập nhật Thời gian
        if (timeElement) {
            let timestampToFormat = isUnread && newTimestamp ? newTimestamp : originalTimestampStr;
            try {
                const date = new Date(timestampToFormat); // Parse ISO string (UTC)
                if (isNaN(date)) throw new Error("Invalid date"); // Kiểm tra nếu parse lỗi
                const formattedTime = date.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' }) + ' ' + date.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' });
                timeElement.textContent = formattedTime;
            } catch (e) {
                console.error("Error formatting timestamp:", e, " Raw:", timestampToFormat);
                // Hiển thị timestamp gốc nếu format lỗi
                timeElement.textContent = originalTimestampStr ? new Date(originalTimestampStr).toLocaleString('vi-VN') : "Không rõ";
            }
        }

        // 4. (Tùy chọn) Di chuyển lên đầu nếu là tin nhắn mới
        if (isUnread && newTimestamp) { // Chỉ di chuyển khi có tin mới thực sự
            const listGroup = document.querySelector('.list-group');
            if (listGroup && listItem !== listGroup.firstElementChild) {
                console.log(`Prepending conv-${convId} to the top.`);
                listGroup.prepend(listItem);
            }
        }

    } // Kết thúc hàm updateConversationItem

    // --- Hàm đọc và áp dụng trạng thái từ LocalStorage ---
    function applyInitialState() {
        console.log("Applying initial state from localStorage.");
        try {
            let snap = JSON.parse(localStorage.getItem(LS_KEY_USER) || '{}');
            const unreadMap = snap.unread || {};

            // Cập nhật style và preview cho tất cả item dựa trên map đọc được
            document.querySelectorAll('.list-group-item[id^="conv-"]').forEach(item => {
                const idStr = item.id.substring(5);
                const convId = parseInt(idStr);
                if (!isNaN(convId)) {
                    const count = unreadMap.hasOwnProperty(convId) ? (unreadMap[convId] || 0) : 0;
                    // Gọi hàm cập nhật với count đọc được, không có timestamp mới
                    updateConversationItem(convId, count, null);
                }
            });

            // Xóa localStorage sau khi áp dụng xong (logic cũ)
            if (Object.keys(unreadMap).length > 0) {
                console.log("Clearing unread data from localStorage after applying initial state.");
                snap.unread = {};
                snap.ts = Date.now();
                localStorage.setItem(LS_KEY_USER, JSON.stringify(snap));
                window.dispatchEvent(new CustomEvent('userChatSnapshot', { detail: snap }));
            }
        } catch (e) {
            console.error('Error applying initial state:', e);
        }
    } // Kết thúc hàm applyInitialState

    // --- Lắng nghe sự kiện ---
    let isProcessingInitialClear = false;
    window.addEventListener('userChatSnapshot', (event) => {
        if (isProcessingInitialClear) {
            isProcessingInitialClear = false;
            console.log("Ignoring self-triggered userChatSnapshot after initial clear.");
            return;
        }
        const snap = event.detail || {};
        console.log("Received userChatSnapshot event (for styling update):", snap);
        const unreadMap = snap.unread || {};

        // Cập nhật lại style và preview cho TẤT CẢ các item dựa trên snapshot mới
        // Quan trọng: Phải reset cả những item không còn unread
        document.querySelectorAll('.list-group-item[id^="conv-"]').forEach(item => {
            const idStr = item.id.substring(5);
            const convId = parseInt(idStr);
            if (!isNaN(convId)) {
                const count = unreadMap.hasOwnProperty(convId) ? (unreadMap[convId] || 0) : 0;
                // Chỉ cập nhật style + preview text (dùng count), KHÔNG CÓ timestamp mới
                updateConversationItem(convId, count, null);
            }
        });
    });

    // Lắng nghe preview mới (chỉ để cập nhật nội dung VÀ style của item đó)
    window.addEventListener('newMessagePreview', (event) => {
        const data = event.detail;
        if (!data) return;
        console.log("Received newMessagePreview event (for content update):", data);

        // Đọc count MỚI NHẤT từ localStorage (do layout vừa cập nhật)
        let currentUnreadCount = 0;
        try {
            const currentSnap = JSON.parse(localStorage.getItem(LS_KEY_USER) || '{}');
            if (currentSnap.unread && currentSnap.unread.hasOwnProperty(data.conversationId)) {
                currentUnreadCount = currentSnap.unread[data.conversationId] || 0;
            }
        } catch (e) { console.error("Error reading count from localStorage in preview handler:", e); }

        // Gọi hàm cập nhật, truyền count mới nhất và timestamp mới
        updateConversationItem(data.conversationId, currentUnreadCount, data.timestamp);
    });

    // --- Xử lý nút ẩn (Giữ nguyên) ---
    document.addEventListener('click', async (e) => {
        // ... (code xử lý nút ẩn không đổi, nhưng đảm bảo có gọi fetchAndUpdateList nếu dùng polling,
        //      hoặc xóa item + cập nhật storage nếu dùng event-based như hiện tại) ...
        if (e.target && e.target.classList.contains('btn-hide-conv')) {
            e.preventDefault();
            e.stopPropagation();
            const button = e.target;
            const convId = button.getAttribute('data-conv-id');
            if (!convId) return;
            if (confirm('Bạn có chắc muốn ẩn cuộc trò chuyện này không?')) {
                button.disabled = true;
                try {
                    const response = await fetch(`/Chat/HideConversation/${convId}`, {
                        method: 'POST',
                        headers: { 'RequestVerificationToken': '@GetAntiXsrfToken()' }
                    });
                    if (response.ok) {
                        document.getElementById(`conv-${convId}`)?.remove();
                        // Xóa khỏi localStorage khi ẩn thành công
                        let currentSnap = JSON.parse(localStorage.getItem(LS_KEY_USER) || '{}');
                        if (currentSnap.unread && currentSnap.unread.hasOwnProperty(convId)) {
                            delete currentSnap.unread[convId];
                            currentSnap.ts = Date.now();
                            localStorage.setItem(LS_KEY_USER, JSON.stringify(currentSnap));
                            window.dispatchEvent(new CustomEvent('userChatSnapshot', { detail: currentSnap }));
                        }
                    } else {
                        alert('Đã xảy ra lỗi khi ẩn cuộc trò chuyện.');
                        button.disabled = false;
                    }
                } catch (error) {
                    console.error('Error hiding conversation:', error);
                    alert('Đã xảy ra lỗi mạng.');
                    button.disabled = false;
                }
            }
        }
    });

    // --- Khởi động ---
    isProcessingInitialClear = true;
    applyInitialState();
    setTimeout(() => { isProcessingInitialClear = false; }, 0);

})(); // Kết thúc IIFE