// SignalR connection manager
const signalRManager = {
    connections: {},
    _isRefreshing: false,

    async connect(hubName, hubPath) {
        const existing = this.connections[hubName];
        if (existing && existing.state === signalR.HubConnectionState.Connected) {
            return existing;
        }

        const connection = existing ?? new signalR.HubConnectionBuilder()
            .withUrl(hubPath)
            .withAutomaticReconnect()
            .build();

        if (existing && existing.state !== signalR.HubConnectionState.Disconnected) {
            return existing;
        }

        this.connections[hubName] = connection;
        await connection.start();
        return connection;
    },

    get(hubName) {
        return this.connections[hubName];
    },

    async joinGroups(connection, groups) {
        if (!connection || !Array.isArray(groups) || groups.length === 0) return;

        for (const group of groups.filter(Boolean)) {
            try {
                await connection.invoke('JoinGroup', group);
            } catch (err) {
                console.warn('JoinGroup failed:', group, err);
            }
        }
    },

    bindAutoRefresh(connection, options = {}) {
        if (!connection) return;

        const {
            events = [],
            message = 'Du lieu vua duoc cap nhat',
            excludePathContains = ['/Reports']
        } = options;

        const shouldSkip = () => excludePathContains.some(x => window.location.pathname.includes(x));
        const handle = async () => {
            showToast(message, 'info');
            if (!shouldSkip()) {
                await this.softRefreshMainContent();
            }
        };

        for (const eventName of events) {
            connection.on(eventName, handle);
        }
    },

    async softRefreshMainContent() {
        if (this._isRefreshing) return;

        const main = document.querySelector('main');
        if (!main) return;

        this._isRefreshing = true;
        try {
            const response = await fetch(window.location.href, {
                method: 'GET',
                headers: {
                    'X-Requested-With': 'XMLHttpRequest',
                    'Cache-Control': 'no-cache'
                }
            });

            if (!response.ok) return;

            const html = await response.text();
            const parser = new DOMParser();
            const doc = parser.parseFromString(html, 'text/html');
            const newMain = doc.querySelector('main');
            if (!newMain) return;

            main.innerHTML = newMain.innerHTML;

            if (window.lucide && typeof window.lucide.createIcons === 'function') {
                window.lucide.createIcons();
            }
            if (window.AOS && typeof window.AOS.refreshHard === 'function') {
                window.AOS.refreshHard();
            }
        } catch (err) {
            console.warn('Soft refresh failed:', err);
        } finally {
            this._isRefreshing = false;
        }
    }
};

function showToast(message, type = 'success') {
    const Toast = Swal.mixin({
        toast: true, position: 'top-end', showConfirmButton: false,
        timer: 3000, timerProgressBar: true
    });
    Toast.fire({ icon: type, title: message });
}

async function confirmAction(title, text, onConfirm) {
    const result = await Swal.fire({
        title, text,
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#FF6B35',
        cancelButtonColor: '#6b7280',
        confirmButtonText: 'Xác nhận',
        cancelButtonText: 'Hủy'
    });
    if (result.isConfirmed) await onConfirm();
}
