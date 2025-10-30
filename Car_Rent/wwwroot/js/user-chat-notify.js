(function () {
    // ===== Config & DOM refs =====
    // Dùng một key khác với Staff để tránh xung đột
    const LS_KEY_USER = 'cr_user_unread_v1';
    const badge = document.getElementById('userChatBadge');

    // Nếu không phải trang của User (không có badge), thì không cần chạy
    if (!badge) {
        return;
    }

    // ===== State =====
    // Map<convId, count>
    const unreadMap = new Map();

    // Hàm cập nhật số trên Badge
    function updateBadge() {
        let total = 0;
        unreadMap.forEach(count => total += count);

        if (!badge) return;
        badge.textContent = String(total);
        badge.style.display = (total > 0) ? 'block' : 'none'; // 'block' thay vì 'inline'
    }

    // ===== Snapshot (Logic y hệt của Staff) =====
    function publishSnapshot() {
        const snap = {
            unread: Object.fromEntries(unreadMap),
            ts: Date.now()
        };
        localStorage.setItem(LS_KEY_USER, JSON.stringify(snap));

        // Phát sự kiện để đồng bộ các tab khác của CÙNG User
        window.dispatchEvent(new CustomEvent('userChatSnapshot', { detail: snap }));
    }

    function hydrateFromStorage() {
        let snap = null;
        try {
            snap = JSON.parse(localStorage.getItem(LS_KEY_USER) || '{}');
        } catch { }

        unreadMap.clear();
        if (snap && snap.unread) {
            Object.entries(snap.unread).forEach(([k, v]) => unreadMap.set(parseInt(k), v));
        }
        updateBadge();
    }

    // ===== SignalR connection =====
    const conn = new signalR.HubConnectionBuilder()
        .withUrl('/hubs/chat')
        .withAutomaticReconnect()
        .build();

    // ===== Handlers =====

    // 1. Lắng nghe tin nhắn từ Staff (Sự kiện từ Bước 1)
    conn.on('StaffMessage', (data) => {
        console.log("Staff message received:", data);
        // Nếu User đang ở trong phòng chat đó, không làm gì cả
        // (Vì trang Room.cshtml sẽ tự động MarkRead)
        if (window.activeConvId && window.activeConvId === data.conversationId) {
            return;
        }

        // Tăng bộ đếm cho conversationId này
        const currentCount = unreadMap.get(data.conversationId) || 0;
        unreadMap.set(data.conversationId, currentCount + 1);

        updateBadge();
        publishSnapshot();

        window.dispatchEvent(new CustomEvent('newMessagePreview', {
            detail: {
                conversationId: data.conversationId,
                preview: data.preview, // Nội dung xem trước từ Hub
                timestamp: data.at      // Thời gian từ Hub (UTC string)
            }
        }));

        // (Tùy chọn) Hiển thị toast
        toast(`Tin nhắn mới từ Hỗ trợ...`);
    });

    // 2. Lắng nghe sự kiện "đã đọc" (từ Chat/Room.cshtml)
    // File Room.cshtml của Sir (ở lượt trước) đã có logic phát sự kiện này,
    // bây giờ chúng ta chỉ cần lắng nghe nó.
    window.addEventListener('chatMarkedRead', (ev) => {
        const { convId } = ev.detail || {};
        if (typeof convId === 'number') {

            // User đã đọc, xóa khỏi Map
            if (unreadMap.has(convId)) {
                unreadMap.delete(convId);
                updateBadge();
                publishSnapshot();
            }
        }
    });

    // 3. Lắng nghe từ tab khác (đồng bộ localStorage)
    window.addEventListener('userChatSnapshot', (ev) => {
        // Một tab khác đã cập nhật, tải lại state
        hydrateFromStorage();
    });

    // Hàm toast (tùy chọn)
    function toast(msg) {
        const t = document.createElement('div');
        t.className = 'position-fixed end-0 top-0 m-3 p-3 bg-primary text-white rounded-3 shadow'; // Dùng màu primary
        t.style.zIndex = 1080;
        t.textContent = msg;
        document.body.appendChild(t);
        setTimeout(() => t.remove(), 3000);
    }

    // ===== Khởi động =====
    // 1. Tải state từ localStorage ngay lập tức
    hydrateFromStorage();

    // 2. Bắt đầu kết nối SignalR
    conn.start().catch(err => console.error("User SignalR connection failed: ", err));

})();