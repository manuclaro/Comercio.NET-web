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
    document.getElementById('nombreUsuarioProd').textContent = `👤 ${nombre}`;

    cargarProductos();

    document.getElementById('btnSalir').addEventListener('click', () => {
        localStorage.clear(); window.location.href = '/login.html';
    });
    document.getElementById('btnNuevoProducto').addEventListener('click', () => abrirModalNuevo());
    document.getElementById('btnCancelarProducto').addEventListener('click', () => cerrarModal('modalProducto'));
    document.getElementById('formProducto').addEventListener('submit', onGuardarProducto);
});

function formatCurrency(value) {
    return new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS' }).format(value);
}

async function cargarProductos() {
    try {
        const res = await fetch('/api/mesas/productos-bar');
        if (!res.ok) {
            console.error('Error GET /api/mesas/productos-bar:', res.status, await res.text());
            renderProductos([]);
            return;
        }
        const productos = await res.json();
        console.log('Productos recibidos:', productos);
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
                <button class="btn-del" style="color:#1565c0;font-size:1.1rem;margin-right:6px"
                    onclick='abrirModalEditar(${p.id}, ${JSON.stringify(p.codigo)}, ${JSON.stringify(p.descripcion)}, ${p.precio})'
                    title="Editar">✏️</button>
                <button class="btn-del" onclick="eliminarProducto(${p.id})" title="Eliminar">🗑️</button>
            </td>
        </tr>
    `).join('');
}

function abrirModalNuevo() {
    document.getElementById('tituloModalProducto').textContent = 'Agregar producto';
    document.getElementById('inputIdProd').value       = '';
    document.getElementById('inputCodigoProd').value   = '';
    document.getElementById('inputDescripcionProd').value = '';
    document.getElementById('inputPrecioProd').value   = '';
    abrirModal('modalProducto');
}

function abrirModalEditar(id, codigo, descripcion, precio) {
    document.getElementById('tituloModalProducto').textContent = 'Editar producto';
    document.getElementById('inputIdProd').value       = id;
    document.getElementById('inputCodigoProd').value   = codigo ?? '';
    document.getElementById('inputDescripcionProd').value = descripcion ?? '';
    document.getElementById('inputPrecioProd').value   = precio ?? '';
    abrirModal('modalProducto');
}

async function onGuardarProducto(e) {
    e.preventDefault();
    const id          = document.getElementById('inputIdProd').value;
    const codigo      = document.getElementById('inputCodigoProd').value.trim();
    const descripcion = document.getElementById('inputDescripcionProd').value.trim();
    const precio      = parseFloat(document.getElementById('inputPrecioProd').value);

    if (!descripcion || isNaN(precio)) { alert('Completá descripción y precio.'); return; }

    const esEdicion = !!id;
    const url    = esEdicion ? `/api/mesas/productos-bar/${id}` : '/api/mesas/productos-bar';
    const method = esEdicion ? 'PUT' : 'POST';

    try {
        const res = await fetch(url, {
            method,
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ codigo, descripcion, precio, activo: true })
        });
        if (!res.ok) throw new Error(await res.text());
        cerrarModal('modalProducto');
        document.getElementById('formProducto').reset();
        cargarProductos();
    } catch (err) {
        console.error('Error guardando producto:', err);
        alert('No se pudo guardar el producto.');
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