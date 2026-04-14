'use strict';

document.addEventListener('DOMContentLoaded', async () => {
    const token = localStorage.getItem('auth_token');
    if (!token) { window.location.href = '/login.html'; return; }

    try {
        const res = await fetch('/api/auth/validar', {
            headers: { 'Authorization': `Bearer ${token}` }
        });
        const data = await res.json();
        if (!data.valido) { localStorage.clear(); window.location.href = '/login.html'; return; }
    } catch {
        localStorage.clear(); window.location.href = '/login.html'; return;
    }

    const rol = (localStorage.getItem('usuario_rol') || '').toLowerCase();
    if (rol !== 'pizzeria') { window.location.href = '/dashboard.html'; return; }

    const nombre = localStorage.getItem('usuario_completo') || localStorage.getItem('usuario_nombre') || 'Usuario';
    document.getElementById('nombreUsuario').textContent = `👤 ${nombre}`;

    document.getElementById('btnCerrarSesion').addEventListener('click', () => {
        localStorage.clear();
        window.location.href = '/login.html';
    });
});