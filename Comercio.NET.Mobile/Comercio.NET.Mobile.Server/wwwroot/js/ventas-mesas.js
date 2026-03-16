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
    document.getElementById('nombreUsuarioVentas').textContent = `👤 ${nombre}`;

    cargarVentas();

    document.getElementById('btnSalir').addEventListener('click', () => {
        localStorage.clear(); window.location.href = '/login.html';
    });
    document.getElementById('btnVolverVentas').addEventListener('click', volverAListado);
});

function formatCurrency(value) {
    return new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS' }).format(value);
}

function formatFecha(isoString) {
    if (!isoString) return '-';
    const local = isoString.replace(/Z$/, '').replace(/[+-]\d{2}:\d{2}$/, '');
    const d = new Date(local);
    return `${d.toLocaleTimeString('es-AR', { hour: '2-digit', minute: '2-digit' })}`;
}

async function cargarVentas() {
    try {
        const res = await fetch('/api/mesas/ventas-dia');
        const ventas = res.ok ? await res.json() : [];
        renderVentas(Array.isArray(ventas) ? ventas : []);
    } catch (err) {
        console.error('Error cargando ventas:', err);
        renderVentas([]);
    }
}

function renderVentas(ventas) {
    const body  = document.getElementById('bodyVentas');
    const vacio = document.getElementById('mensajeVacioVentas');

    const totalDia = ventas.reduce((acc, v) => acc + (v.total ?? 0), 0);
    document.getElementById('resumenDia').textContent =
        `Total del día: ${formatCurrency(totalDia)} · ${ventas.length} mesa(s)`;

    if (ventas.length === 0) {
        body.innerHTML = '';
        vacio.style.display = 'block';
        return;
    }

    vacio.style.display = 'none';
    body.innerHTML = ventas.map(v => `
        <tr style="cursor:pointer" onclick="verDetalle(${v.mesaId}, 'Mesa #${v.numeroMesa}', '${v.mozo}', '${v.estado}')">
            <td>#${v.numeroMesa}</td>
            <td>${v.mozo || '-'}</td>
            <td>${formatFecha(v.fechaApertura)}</td>
            <td>${v.fechaCierre ? formatFecha(v.fechaCierre) : '-'}</td>
            <td>
                <span style="color:${v.estado === 'Abierta' ? '#2e7d32' : '#c62828'};font-weight:600">
                    ${v.estado}
                </span>
            </td>
            <td style="text-align:right;font-weight:600">${formatCurrency(v.total)}</td>
            <td>${v.formaPago || '-'}</td>
        </tr>
    `).join('');
}

async function verDetalle(mesaId, titulo, mozo, estado) {
    try {
        const res = await fetch(`/api/mesas/${mesaId}/items`);
        const items = res.ok ? await res.json() : [];

        document.getElementById('tituloDet').textContent =
            `${titulo} — ${mozo || 'Sin mozo'} · ${estado}`;

        const body = document.getElementById('bodyDetalle');
        if (!Array.isArray(items) || items.length === 0) {
            body.innerHTML = '<tr><td colspan="5" style="text-align:center;color:#999;padding:1rem">Sin consumos registrados</td></tr>';
        } else {
            body.innerHTML = items.map(i => `
                <tr>
                    <td>${i.codigo || '-'}</td>
                    <td>${i.descripcion}</td>
                    <td style="text-align:center">${i.cantidad}</td>
                    <td style="text-align:right">${formatCurrency(i.precioUnitario)}</td>
                    <td style="text-align:right"><strong>${formatCurrency(i.subtotal)}</strong></td>
                </tr>
            `).join('');
        }

        const total = items.reduce((acc, i) => acc + (i.subtotal ?? 0), 0);
        document.getElementById('totalDetalleDia').textContent = `Total: ${formatCurrency(total)}`;

        document.getElementById('vistaListado').style.display = 'none';
        document.getElementById('vistaDetalle').style.display  = 'block';
    } catch (err) {
        console.error('Error cargando detalle:', err);
        alert('No se pudo cargar el detalle.');
    }
}

function volverAListado() {
    document.getElementById('vistaDetalle').style.display  = 'none';
    document.getElementById('vistaListado').style.display  = 'block';
}