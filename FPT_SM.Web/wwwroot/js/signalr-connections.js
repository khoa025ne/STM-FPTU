// SignalR connection manager
const signalRManager = {
    connections: {},

    async connect(hubName, hubPath) {
        const connection = new signalR.HubConnectionBuilder()
            .withUrl(hubPath)
            .withAutomaticReconnect()
            .build();
        this.connections[hubName] = connection;
        await connection.start();
        return connection;
    },

    get(hubName) {
        return this.connections[hubName];
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
