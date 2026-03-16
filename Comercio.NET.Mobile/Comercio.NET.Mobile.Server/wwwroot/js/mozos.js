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
    if (rol !== 'pizzeria') { window.location.href = '/dashboard.html'; return; }

    const nombre = localStorage.getItem('usuario_completo') || localStorage.getItem('usuario_nombre') || 'Usuario';
    document.getElementById('nombreUsuarioMozos').textContent = `👤 ${nombre}`;

    cargarMozos();

    document.getElementById('btnSalir').addEventListener('click', () => {
        localStorage.clear(); window.location.href = '/login.html';
    });
    document.getElementById('btnNuevoMozo').addEventListener('click', () => abrirModalNuevo());
    document.getElementById('btnCancelarMozo').addEventListener('click', () => cerrarModal('modalMozo'));
    document.getElementById('formMozo').addEventListener('submit', onGuardarMozo);
});

async function cargarMozos() {
    try {
        const res = await fetch('/api/mesas/mozos');
        if (!res.ok) { console.error('Error GET /api/mesas/mozos:', res.status, await res.text()); return; }
        const mozos = await res.json();
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
                <button class="btn-secondary" style="margin-right:4px"
                    onclick='abrirModalEditar(${m.id}, ${JSON.stringify(m.nombre)})'
                    title="Editar">✏️</button>
                <button class="btn-del" onclick="eliminarMozo(${m.id})" title="Eliminar">🗑️</button>
            </td>
        </tr>
    `).join('');
}

function abrirModalNuevo() {
    document.getElementById('tituloModalMozo').textContent = 'Agregar mozo';
    document.getElementById('inputIdMozo').value = '';
    document.getElementById('inputNombreMozo').value = '';
    abrirModal('modalMozo');
}

function abrirModalEditar(id, nombre) {
    document.getElementById('tituloModalMozo').textContent = 'Editar mozo';
    document.getElementById('inputIdMozo').value = id;
    document.getElementById('inputNombreMozo').value = nombre ?? '';
    abrirModal('modalMozo');
}

async function onGuardarMozo(e) {
    e.preventDefault();
    const id     = document.getElementById('inputIdMozo').value;
    const nombre = document.getElementById('inputNombreMozo').value.trim();

    if (!nombre) { alert('Ingresá un nombre.'); return; }

    const esEdicion = !!id;
    const url    = esEdicion ? `/api/mesas/mozos/${id}` : '/api/mesas/mozos';
    const method = esEdicion ? 'PUT' : 'POST';

    try {
        const res = await fetch(url, {
            method,
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ nombre, activo: true })
        });
        if (!res.ok) throw new Error(await res.text());
        cerrarModal('modalMozo');
        document.getElementById('formMozo').reset();
        cargarMozos();
    } catch (err) {
        console.error('Error guardando mozo:', err);
        alert('No se pudo guardar el mozo.');
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