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
    document.getElementById('nombreUsuarioFP').textContent = `👤 ${nombre}`;

    cargarFormasPago();

    document.getElementById('btnSalir').addEventListener('click', () => {
        localStorage.clear(); window.location.href = '/login.html';
    });
    document.getElementById('btnNuevaFP').addEventListener('click', () => abrirModalNuevo());
    document.getElementById('btnCancelarFP').addEventListener('click', () => cerrarModal('modalFormaPago'));
    document.getElementById('formFormaPago').addEventListener('submit', onGuardarFormaPago);
});

async function cargarFormasPago() {
    try {
        const res = await fetch('/api/mesas/formas-pago');
        if (!res.ok) { console.error('Error GET /api/mesas/formas-pago:', res.status); return; }
        const formas = await res.json();
        renderFormasPago(Array.isArray(formas) ? formas : []);
    } catch (err) {
        console.error('Error cargando formas de pago:', err);
        renderFormasPago([]);
    }
}

function renderFormasPago(formas) {
    const body  = document.getElementById('bodyFormasPago');
    const vacio = document.getElementById('mensajeVacioFP');

    if (formas.length === 0) {
        body.innerHTML = '';
        vacio.style.display = 'block';
        return;
    }

    vacio.style.display = 'none';
    body.innerHTML = formas.map((f, i) => `
        <tr>
            <td>${i + 1}</td>
            <td>${f.descripcion}</td>
            <td style="text-align:center">
                <button class="btn-secondary" style="margin-right:4px"
                    onclick='abrirModalEditar(${f.id}, ${JSON.stringify(f.descripcion)})'
                    title="Editar">✏️</button>
                <button class="btn-del" onclick="eliminarFormaPago(${f.id})" title="Eliminar">🗑️</button>
            </td>
        </tr>
    `).join('');
}

function abrirModalNuevo() {
    document.getElementById('tituloModalFP').textContent = 'Nueva forma de pago';
    document.getElementById('inputIdFP').value = '';
    document.getElementById('inputDescripcionFP').value = '';
    abrirModal('modalFormaPago');
}

function abrirModalEditar(id, descripcion) {
    document.getElementById('tituloModalFP').textContent = 'Editar forma de pago';
    document.getElementById('inputIdFP').value = id;
    document.getElementById('inputDescripcionFP').value = descripcion ?? '';
    abrirModal('modalFormaPago');
}

async function onGuardarFormaPago(e) {
    e.preventDefault();
    const id          = document.getElementById('inputIdFP').value;
    const descripcion = document.getElementById('inputDescripcionFP').value.trim();

    if (!descripcion) { alert('Ingresá una descripción.'); return; }

    const esEdicion = !!id;
    const url    = esEdicion ? `/api/mesas/formas-pago/${id}` : '/api/mesas/formas-pago';
    const method = esEdicion ? 'PUT' : 'POST';

    try {
        const res = await fetch(url, {
            method,
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ descripcion, activo: true })
        });
        if (!res.ok) throw new Error(await res.text());
        cerrarModal('modalFormaPago');
        document.getElementById('formFormaPago').reset();
        cargarFormasPago();
    } catch (err) {
        console.error('Error guardando forma de pago:', err);
        alert('No se pudo guardar la forma de pago.');
    }
}

async function eliminarFormaPago(id) {
    if (!confirm('¿Eliminar esta forma de pago?')) return;
    try {
        const res = await fetch(`/api/mesas/formas-pago/${id}`, { method: 'DELETE' });
        if (!res.ok) throw new Error(await res.text());
        cargarFormasPago();
    } catch (err) {
        console.error('Error eliminando forma de pago:', err);
        alert('No se pudo eliminar la forma de pago.');
    }
}

function abrirModal(id) { document.getElementById(id).classList.add('activo'); }
function cerrarModal(id) { document.getElementById(id).classList.remove('activo'); }