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
    document.getElementById('nombreUsuarioProd').textContent = `👤 ${nombre}`;

    cargarProductos();

    document.getElementById('btnSalir').addEventListener('click', () => {
        localStorage.clear(); window.location.href = '/login.html';
    });
    document.getElementById('btnNuevoProducto').addEventListener('click', () => abrirModal('modalNuevoProducto'));
    document.getElementById('btnCancelarProducto').addEventListener('click', () => cerrarModal('modalNuevoProducto'));
    document.getElementById('formNuevoProducto').addEventListener('submit', onCrearProducto);
});

function formatCurrency(value) {
    return new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS' }).format(value);
}

async function cargarProductos() {
    try {
        const res = await fetch('/api/mesas/productos-bar');
        const productos = res.ok ? await res.json() : [];
        renderProductos(Array.isArray(productos) ? productos : []);
    } catch (err) {
        console.error('Error cargando productos:', err);
        renderProductos([]);
    }
}

function renderProductos(productos) {
    const body  = document.getElementById('bodyProductos');
    const vacio = document.getElementById('mensajeVacioProd');

    if (productos.length === 0) {
        body.innerHTML = '';
        vacio.style.display = 'block';
        return;
    }

    vacio.style.display = 'none';
    body.innerHTML = productos.map(p => `
        <tr>
            <td>${p.codigo || '-'}</td>
            <td>${p.descripcion}</td>
            <td style="text-align:right">${formatCurrency(p.precio)}</td>
            <td style="text-align:center">
                <button class="btn-del" onclick="eliminarProducto(${p.id})" title="Eliminar">🗑️</button>
            </td>
        </tr>
    `).join('');
}

async function onCrearProducto(e) {
    e.preventDefault();
    const codigo      = document.getElementById('inputCodigoProd').value.trim();
    const descripcion = document.getElementById('inputDescripcionProd').value.trim();
    const precio      = parseFloat(document.getElementById('inputPrecioProd').value);

    if (!descripcion || isNaN(precio)) { alert('Completá descripción y precio.'); return; }

    try {
        const res = await fetch('/api/mesas/productos-bar', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ codigo, descripcion, precio, activo: true })
        });
        if (!res.ok) throw new Error(await res.text());
        cerrarModal('modalNuevoProducto');
        document.getElementById('formNuevoProducto').reset();
        cargarProductos();
    } catch (err) {
        console.error('Error creando producto:', err);
        alert('No se pudo crear el producto.');
    }
}

async function eliminarProducto(id) {
    if (!confirm('¿Eliminar este producto?')) return;
    try {
        const res = await fetch(`/api/mesas/productos-bar/${id}`, { method: 'DELETE' });
        if (!res.ok) throw new Error(await res.text());
        cargarProductos();
    } catch (err) {
        console.error('Error eliminando producto:', err);
        alert('No se pudo eliminar el producto.');
    }
}

function abrirModal(id) { document.getElementById(id).classList.add('activo'); }
function cerrarModal(id) { document.getElementById(id).classList.remove('activo'); }