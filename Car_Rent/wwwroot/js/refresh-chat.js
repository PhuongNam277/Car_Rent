// Staff

(function () {
    // ====== Config & DOM refs ======
    const LS_KEY = 'cr_unread_v1';
    const myId = parseInt('@User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier)');
    const badge = document.getElementById('supportBadge');   // <span id="supportBadge">…</span> ở navbar (nếu có)

    // ====== Toast helper (gộp từ block 1) ======
    function toast(msg) {
        const t = document.createElement('div');
        t.className = 'position-fixed end-0 top-0 m-3 p-3 bg-dark text-white rounded-3 shadow';
        t.style.zIndex = 1080;
        t.textContent = msg;
        document.body.appendChild(t);
        setTimeout(() => t.remove(), 3000);
    }

    // ====== State (gộp 2 block) ======
    const queueIds = new Set(); // các conv chưa gán (hàng đợi)
    const unreadMap = new Map(); // convId -> số tin chưa đọc (đã gán cho TÔI)
    const myActiveIds = new Set(); // các conv đã gán CHO TÔI có hoạt động (CustomerBack/Assigned)

    function totalCount() {
        let unreadTotal = 0; unreadMap.forEach(v => unreadTotal += v);
        return queueIds.size + unreadTotal + myActiveIds.size; // cộng cả 3 loại để “đầy đủ nhất”
    }
    function updateBadge() {
        if (!badge) {
            console.warn("Support badge element not found."); // Thêm log nếu không tìm thấy badge
            return;
        }

        const n = totalCount();
        console.log("Updating staff badge count:", n); // Log số lượng để debug

        badge.textContent = String(n);

        // Thay vì dùng badge.hidden, hãy dùng style.display
        if (n > 0) {
            badge.style.display = 'block'; // Hoặc 'inline-block' tùy theo layout của Sir
        } else {
            badge.style.display = 'none'; // Ẩn badge đi
        }
    }

    // ====== Snapshot (để đồng bộ giữa các trang/tab) ======
    function getSnapshot() {
        try { return JSON.parse(localStorage.getItem(LS_KEY) || '{ }'); } catch { return {}; }
    }
    function publishSnapshot() {
        const snap = {
            queueIds: [...queueIds],
            unread: Object.fromEntries(unreadMap),
            myActiveIds: [...myActiveIds],
            ts: Date.now()
        };
        localStorage.setItem(LS_KEY, JSON.stringify(snap));
        window.dispatchEvent(new CustomEvent('chatSnapshot', { detail: snap }));
    }
    function hydrateFromStorage() {
        const snap = getSnapshot();
        if (Array.isArray(snap.queueIds)) snap.queueIds.forEach(id => queueIds.add(id));
        if (snap.unread) Object.entries(snap.unread).forEach(([k, v]) => unreadMap.set(parseInt(k), v));
        if (Array.isArray(snap.myActiveIds)) snap.myActiveIds.forEach(id => myActiveIds.add(id));
        updateBadge();
    }

    // Hydrate sớm
    hydrateFromStorage();

    // ====== SignalR connection ======
    const conn = new signalR.HubConnectionBuilder()
        .withUrl('/hubs/chat')
        .withAutomaticReconnect()
        .build();

    async function joinLobbyAndSync() {
        try { await conn.invoke('JoinStaffLobby'); } catch { }
        await syncCounts();
    }
    conn.onreconnected(joinLobbyAndSync);

    // ====== Handlers (gộp + mở rộng) ======
    // Hàng đợi khách mới
    conn.on('NewConversation', i => {
        queueIds.add(i.conversationId);
        updateBadge(); publishSnapshot();
        toast('Khách mới yêu cầu hỗ trợ');
    });

    // Có người nhận cuộc chat
    conn.on('ConversationAssigned', e => {
        queueIds.delete(e.conversationId);
        // Nếu chính TÔI nhận → thêm vào myActiveIds (để tăng hiển thị badge “đang có hoạt động”)
        if (e.staffId && parseInt(e.staffId) === myId) {
            myActiveIds.add(e.conversationId);
        }
        updateBadge(); publishSnapshot();
    });

    // Khách quay lại phòng đã gán cho TÔI
    conn.on('CustomerBack', e => {
        myActiveIds.add(e.conversationId);
        updateBadge(); publishSnapshot();
        toast('Khách vừa quay lại phòng chat của bạn');
    });

    // Chat đóng
    conn.on('ConversationClosed', e => {
        queueIds.delete(e.conversationId);
        unreadMap.delete(e.conversationId);
        myActiveIds.delete(e.conversationId);
        updateBadge(); publishSnapshot();
    });

    // Seed (khi vừa kết nối)
    conn.on('SeedQueue', items => {
        (items || []).forEach(i => queueIds.add(i.conversationId));
        updateBadge(); publishSnapshot();
    });
    conn.on('SeedAssigned', items => {
        // các conv đang open & đã gán cho tôi
        (items || []).forEach(i => myActiveIds.add(i.conversationId));
        updateBadge(); publishSnapshot();
    });

    // Tin nhắn mới
    conn.on('NewMessage', e => {
        // Nếu đang mở đúng phòng, phía room sẽ MarkRead → không cộng vào unread
        if (window.activeConvId && window.activeConvId === e.conversationId) return;
        const cur = unreadMap.get(e.conversationId) || 0;
        unreadMap.set(e.conversationId, cur + 1);
        updateBadge(); publishSnapshot();
    });

    // ====== Đồng bộ định kỳ từ server (gộp từ block 2) ======
    async function syncCounts() {
        try {
            // Khuyến nghị server trả JSON: {queueIds:[int], unread:{convId:count}, assignedIds:[int] }
            const r = await fetch('/Chat/Counts', { cache: 'no-store' });
            if (!r.ok) return;
            const data = await r.json();

            queueIds.clear();
            (data.queueIds || []).forEach(id => queueIds.add(id));

            unreadMap.clear();
            if (data.unread) Object.entries(data.unread).forEach(([k, v]) => unreadMap.set(parseInt(k), v));

            myActiveIds.clear();
            (data.assignedIds || []).forEach(id => myActiveIds.add(id));

            updateBadge(); publishSnapshot();
        } catch { }
    }

    // Bắt sự kiện “đã đọc” từ phòng chat cụ thể (do client room phát ra)
    window.addEventListener('chatMarkedRead', (ev) => {
        const { convId } = ev.detail || {};
        if (typeof convId === 'number') {
            unreadMap.delete(convId);
            myActiveIds.delete(convId); // khi đã đọc/ngó, có thể coi như đã “xử lý”
            updateBadge(); publishSnapshot();
        }
    });

    // Khởi động
    conn.start().then(joinLobbyAndSync).catch(console.error);

    // Dự phòng rớt mạng/đổi tab
    setInterval(syncCounts, 10000);
})();
