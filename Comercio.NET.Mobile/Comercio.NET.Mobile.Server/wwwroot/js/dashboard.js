document.addEventListener('DOMContentLoaded', async function () {
    const token = localStorage.getItem('auth_token');

    if (!token) {
        window.location.href = '/login.html';
        return;
    }

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

    const rol = (localStorage.getItem('usuario_rol') || '').toLowerCase();

    // Pizzeria tiene su propio dashboard, nunca llega aquí
    if (rol === 'pizzeria') {
        window.location.href = '/dashboard-pizzeria.html';
        return;
    }

    const nombreCompleto = localStorage.getItem('usuario_completo')
        || localStorage.getItem('usuario_nombre')
        || 'Usuario';
    document.getElementById('nombreUsuario').textContent = `👤 ${nombreCompleto}`;

    // Ocultar card de mesas para cualquier rol que no sea pizzeria (incluido admin)
    const cardMesas = document.getElementById('cardMesas');
    if (cardMesas) cardMesas.style.display = 'none';

    document.getElementById('btnCerrarSesion').addEventListener('click', function () {
        localStorage.clear();
        window.location.href = '/login.html';
    });
});