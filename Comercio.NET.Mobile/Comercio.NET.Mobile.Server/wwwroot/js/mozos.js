'use strict';

document.addEventListener('DOMContentLoaded', async () => {
    const token = localStorage.getItem('auth_token');
    if (!token) { window.location.href = '/login.html'; return; }

    try {
        const res = await fetch('/api/auth/validar', { headers: { 'Authorization': `Bearer ${token}` } });
        const data = await res.json();
        if (!data.valido) { localStorage.clear(); window.location.href = '/login.html'; return; }
    } catch {
        localStorage.clear(); window.location.href = '/login.html'; return;
    }

    const rol = (localStorage.getItem('usuario_rol') || '').toLowerCase();
    if (rol !== 'pizzeria' && rol !== 'admin') { window.location.href = '/dashboard.html'; return; }

    const nombre = localStorage.getItem('usuario_completo') || localStorage.getItem('usuario_nombre') || 'Usuario';
    document.getElementById('nombreUsuarioMozos').textContent = `👤 ${nombre}`;

    cargarMozos();

    document.getElementById('btnSalir').addEventListener('click', () => {
        localStorage.clear(); window.location.href = '/login.html';
    });
    document.getElementById('btnNuevoMozo').addEventListener('click', () => abrirModal('modalNuevoMozo'));
    document.getElementById('btnCancelarMozo').addEventListener('click', () => cerrarModal('modalNuevoMozo'));
    document.getElementById('formNuevoMozo').addEventListener('submit', onCrearMozo);
});

async function cargarMozos() {
    try {
        const res = await fetch('/api/mesas/mozos');
        const mozos = res.ok ? await res.json() : [];
        renderMozos(Array.isArray(mozos) ? mozos : []);
    } catch (err) {
        console.error('Error cargando mozos:', err);
        renderMozos([]);
    }
}

function renderMozos(mozos) {
    const body  = document.getElementById('bodyMozos');
    const vacio = document.getElementById('mensajeVacioMozos');

    if (mozos.length === 0) {
        body.innerHTML = '';
        vacio.style.display = 'block';
        return;
    }

    vacio.style.display = 'none';
    body.innerHTML = mozos.map((m, i) => `
        <tr>
            <td>${i + 1}</td>
            <td>${m.nombre}</td>
            <td style="text-align:center">
                <button class="btn-del" onclick="eliminarMozo(${m.id})" title="Eliminar">🗑️</button>
            </td>
        </tr>
    `).join('');
}

async function onCrearMozo(e) {
    e.preventDefault();
    const nombre = document.getElementById('inputNombreMozo').value.trim();
    if (!nombre) return;

    try {
        const res = await fetch('/api/mesas/mozos', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ nombre, activo: true })
        });
        if (!res.ok) throw new Error(await res.text());
        cerrarModal('modalNuevoMozo');
        document.getElementById('formNuevoMozo').reset();
        cargarMozos();
    } catch (err) {
        console.error('Error creando mozo:', err);
        alert('No se pudo crear el mozo.');
    }
}

async function eliminarMozo(id) {
    if (!confirm('¿Eliminar este mozo?')) return;
    try {
        const res = await fetch(`/api/mesas/mozos/${id}`, { method: 'DELETE' });
        if (!res.ok) throw new Error(await res.text());
        cargarMozos();
    } catch (err) {
        console.error('Error eliminando mozo:', err);
        alert('No se pudo eliminar el mozo.');
    }
}

function abrirModal(id) { document.getElementById(id).classList.add('activo'); }
function cerrarModal(id) { document.getElementById(id).classList.remove('activo'); }