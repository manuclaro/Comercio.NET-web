document.addEventListener('DOMContentLoaded', async function () {
    const token = localStorage.getItem('auth_token');

    if (!token) {
        window.location.href = '/login.html';
        return;
    }

    // Validar token
    try {
        const response = await fetch('/api/auth/validar', {
            headers: { 'Authorization': `Bearer ${token}` }
        });
        const data = await response.json();
        if (!data.valido) {
            localStorage.clear();
            window.location.href = '/login.html';
            return;
        }
    } catch {
        localStorage.clear();
        window.location.href = '/login.html';
        return;
    }

    // Mostrar nombre de usuario
    const nombreCompleto = localStorage.getItem('usuario_completo')
        || localStorage.getItem('usuario_nombre')
        || 'Usuario';
    document.getElementById('nombreUsuario').textContent = `👤 ${nombreCompleto}`;

    // La card de mesas SOLO es visible para el rol Pizzeria
    const rol = (localStorage.getItem('usuario_rol') || '').toLowerCase();
    const cardMesas = document.getElementById('cardMesas');
    if (cardMesas && rol !== 'pizzeria') {
        cardMesas.style.display = 'none';
    }

    // Cerrar sesión
    document.getElementById('btnCerrarSesion').addEventListener('click', function () {
        localStorage.clear();
        window.location.href = '/login.html';
    });
});