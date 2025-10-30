document.addEventListener("DOMContentLoaded", () => {
    // ===== BƯỚC 1: XÓA LOCALSTORAGE CỦA STAFF KHI VÀO INBOX =====
    // Key localStorage của Staff, phải khớp với key trong refresh-chat.js
    const LS_KEY_STAFF = 'cr_unread_v1';

    console.log("Inbox page loaded - Attempting to clear staff unread state."); // Log

    try {
        // Đọc snapshot hiện tại của Staff
        let snap = JSON.parse(localStorage.getItem(LS_KEY_STAFF) || '{}');

        // Kiểm tra xem có dữ liệu cần xóa không
        const hasQueueItems = snap.queueIds && snap.queueIds.length > 0;
        const hasUnreadMessages = snap.unread && Object.keys(snap.unread).length > 0;
        const hasActiveItems = snap.myActiveIds && snap.myActiveIds.length > 0;

        if (hasQueueItems || hasUnreadMessages || hasActiveItems) {

            // Reset các bộ đếm về rỗng
            snap.queueIds = [];
            snap.unread = {};
            snap.myActiveIds = [];
            snap.ts = Date.now();

            // Lưu lại trạng thái rỗng vào localStorage
            localStorage.setItem(LS_KEY_STAFF, JSON.stringify(snap));
            console.log("Cleared staff unread/queue/active state in localStorage."); // Log

            // Phát sự kiện 'chatSnapshot'
            // File refresh-chat.js (ở layout) sẽ bắt sự kiện này và cập nhật badge về 0.
            window.dispatchEvent(new CustomEvent('chatSnapshot', { detail: snap }));
            console.log("Dispatched chatSnapshot event with cleared data."); // Log
        } else {
            console.log("No staff unread/queue/active state found in localStorage to clear."); // Log
        }
    } catch (e) {
        console.error('Error clearing staff chat inbox state:', e);
    }

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/hubs/chat")
        .withAutomaticReconnect()
        .build();

    const qBody = document.querySelector("#queueTbl tbody");

    // Lắng nghe khi có cuộc hội thoại mới
    connection.on("NewConversation", (data) => {
        console.log("EVENT: NewConversation", data); // Dùng để debug

        // Kiểm tra xem hàng đã tồn tại chưa để tránh trùng lặp
        if (document.getElementById(`q-${data.conversationId}`)) return;

        const newRow = qBody.insertRow(0);
        newRow.id = `q-${data.conversationId}`;
        newRow.innerHTML = `
                    <td>${data.conversationId}</td>
                    <td>${data.customerName || `Khách #${data.customerId}`}</td>
                    <td>${new Date(data.createdAt).toLocaleTimeString()}</td>
                    <td>
                        <button class="btn btn-primary btn-sm btn-accept" data-conv-id="${data.conversationId}">Nhận</button>
                    </td>
                `;
    });

    // Lắng nghe khi một cuộc hội thoại đã được người khác nhận
    connection.on("ConversationAssigned", (data) => {
        console.log("EVENT: ConversationAssigned", data);
        const rowToRemove = document.getElementById(`q-${data.conversationId}`);
        if (rowToRemove) {
            rowToRemove.remove();
        }
    });

    // Lắng nghe khi MÌNH nhận thành công và cần mở phòng chat
    connection.on("OpenConversation", (data) => {
        window.location.href = `/Chat/Room?id=${data.conversationId}`;
    });

    // Lắng nghe dữ liệu hàng đợi ban đầu khi mới kết nối
    connection.on("SeedQueue", (queue) => {
        console.log("EVENT: SeedQueue", queue);
        qBody.innerHTML = ''; // Xóa sạch bảng trước khi seed
        queue.forEach(data => {
            const newRow = qBody.insertRow(0); // Thêm vào đầu
            newRow.id = `q-${data.conversationId}`;
            newRow.innerHTML = `
                        <td>${data.conversationId}</td>
                        <td>${data.customerName || `Khách #${data.customerId}`}</td>
                        <td>${new Date(data.createdAt).toLocaleTimeString()}</td>
                        <td>
                            <button class="btn btn-primary btn-sm btn-accept" data-conv-id="${data.conversationId}">Nhận</button>
                        </td>
                    `;
        });
    });

    // Bắt đầu kết nối
    async function startSignalR() {
        try {
            await connection.start();
            console.log("SignalR Connected.");
        } catch (err) {
            console.error(err);
            setTimeout(startSignalR, 5000);
        }
    }
    startSignalR();

    // Xử lý sự kiện click nút "Nhận"
    document.addEventListener('click', function (e) {
        if (e.target && e.target.classList.contains('btn-accept')) {
            const convId = e.target.getAttribute('data-conv-id');
            if (convId) {
                e.target.disabled = true;
                e.target.textContent = 'Đang nhận...';
                connection.invoke("AcceptConversation", parseInt(convId)).catch(err => {
                    console.error(err.toString());
                    e.target.disabled = false;
                    e.target.textContent = 'Nhận';
                    alert('Không thể nhận cuộc trò chuyện này.');
                });
            }
        }
    });

    const aBody = document.querySelector("#assignedTbl tbody");

    function ensureAssignedRow(convId) {
        let row = document.getElementById("a-" + convId);
        if (row) return row;
        // Nếu chưa có hàng, thêm nhanh để hiện Unread
        row = document.createElement("tr");
        row.id = "a-" + convId;
        row.innerHTML = `
                  <td>${convId}</td>
                  <td>Khách</td>
                  <td>${new Date().toLocaleString()}</td>
                  <td class="text-center" style="width:70px">
                    <span class="badge rounded-pill" data-unread="0"></span>
                  </td>
                  <td><a class="btn btn-success btn-sm" href="/Chat/Room?id=${convId}">Vào phòng</a></td>`;
        aBody.prepend(row);
        return row;
    }

    // Hàm set unread:
    // Lấy hàng tương ứng với convId, tìm ô có attribute là data-unread,
    // if count > 0 thêm class và số, else xóa
    function setUnread(convId, count) {
        const row = ensureAssignedRow(convId);
        const cell = row.querySelector('[data-unread]');
        if (!cell) return;
        cell.textContent = count > 0 ? String(count) : '';
        cell.classList.toggle('bg-danger', count > 0);
        cell.dataset.unread = count;
    }

    // Cập nhật cả local storage khi mark as read
    function markAsRead(convId) {
        setUnread(convId, 0);

        // Cập nhật localStorage
        try {
            let snap = JSON.parse(localStorage.getItem('cr_unread_v1') || '{}');
            if (!snap.unread) snap.unread = {};
            snap.unread[convId] = 0;
            localStorage.setItem('cr_unread_v1', JSON.stringify(snap));
            console.log(`Marked conversation ${convId} as read`);
        } catch (e) {
            console.error('Error updating localStorage:', e);
        }
    }


    // reset tất cả các ô unread về 0 trước khi áp snapshot
    function resetAllUnread() {
        document.querySelectorAll('#assignedTbl tbody tr[id^="a-"]').forEach(tr => {
            const idStr = tr.id.substring(2); // "a-123" -> "123"
            const cid = parseInt(idStr);
            if (!isNaN(cid)) setUnread(cid, 0);
        });
    }

    // Áp dụng snapshot ngay khi vào trang (đảm bảo reset trước)
    (function applySnapshotNow() {
        let snap = null;
        try { snap = JSON.parse(localStorage.getItem('cr_unread_v1') || '{}'); } catch { }
        resetAllUnread();
        if (snap && snap.unread) {
            Object.entries(snap.unread).forEach(([k, v]) => setUnread(parseInt(k), v));
        }
    })();

    // Cập nhật khi layout publish snapshot mới (đảm bảo reset trước)
    window.addEventListener('chatSnapshot', (ev) => {
        const snap = ev.detail || {};
        resetAllUnread();
        if (snap.unread) {
            Object.entries(snap.unread).forEach(([k, v]) => setUnread(parseInt(k), v));
        }
    });

    // Khi room báo đã đọc, sử dụng markAsRead
    window.addEventListener('chatMarkedRead', (ev) => {
        const convId = Number(ev.detail?.convId);
        console.log('chatMarkedRead event received:', convId);
        if (convId) markAsRead(convId);
    });

    // Xử lý event staff click vào phòng, event quan trọng để set đã đọc
    document.addEventListener('click', (e) => {
        // Kiểm tra nếu click vào link "Vào phòng"
        if (e.target.matches('a[href*="/Chat/Room"]') ||
            e.target.closest('a[href*="/Chat/Room"]')) {

            const link = e.target.matches('a') ? e.target : e.target.closest('a');
            try {
                const url = new URL(link.href);
                const convId = parseInt(url.searchParams.get('id'));
                if (convId && !isNaN(convId)) {
                    console.log(`Staff clicked to enter room ${convId}`);
                    markAsRead(convId);
                }
            } catch (error) {
                console.error('Error parsing room URL:', error);
            }
        }
    });

    // NEW: Xử lý khi focus trở lại trang (phòng trường hợp mở tab mới)
    window.addEventListener('focus', () => {
        // Kiểm tra nếu có conversation ID trong URL hiện tại
        const urlParams = new URLSearchParams(window.location.search);
        const currentConvId = parseInt(urlParams.get('id'));
        if (currentConvId && !isNaN(currentConvId)) {
            markAsRead(currentConvId);
        }
    });

    // (giữ nguyên phần SignalR nếu đang dùng để seed/nhận bump)
    // ví dụ: khi có bump riêng cho 1 conv
    window.addEventListener('chatUnreadBump', (ev) => {
        const { convId, count } = ev.detail || {};
        console.log('chatUnreadBump event:', convId, count);
        if (convId) setUnread(convId, count);
        const row = document.getElementById('a-' + convId);
        if (row) {
            row.classList.add('table-warning');
            setTimeout(() => row.classList.remove('table-warning'), 1200);
        }
    });

    // DEBUG: Log để kiểm tra events
    console.log('Unread message handler initialized');
});