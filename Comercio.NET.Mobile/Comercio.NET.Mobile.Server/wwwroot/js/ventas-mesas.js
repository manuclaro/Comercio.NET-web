'use strict';

let _todasLasVentas = [];

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
    document.getElementById('nombreUsuarioVentas').textContent = `👤 ${nombre}`;

    await cargarVentas();

    document.getElementById('btnSalir').addEventListener('click', () => {
        localStorage.clear(); window.location.href = '/login.html';
    });
    document.getElementById('btnVolverVentas').addEventListener('click', volverAListado);
    document.getElementById('btnFiltrar').addEventListener('click', aplicarFiltros);
    document.getElementById('btnLimpiarFiltros').addEventListener('click', limpiarFiltros);
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
        _todasLasVentas = Array.isArray(ventas) ? ventas : [];
        poblarFiltros(_todasLasVentas);
        renderVentas(_todasLasVentas);
    } catch (err) {
        console.error('Error cargando ventas:', err);
        renderVentas([]);
    }
}

function poblarFiltros(ventas) {
    // Mozos únicos
    const mozos = [...new Set(ventas.map(v => v.mozo).filter(Boolean))].sort();
    const selMozo = document.getElementById('filtroMozo');
    const mozoActual = selMozo.value;
    selMozo.innerHTML = '<option value="">Todos los mozos</option>';
    mozos.forEach(m => {
        const opt = document.createElement('option');
        opt.value = m;
        opt.textContent = m;
        if (m === mozoActual) opt.selected = true;
        selMozo.appendChild(opt);
    });

    // Formas de pago únicas
    const formas = [...new Set(ventas.map(v => v.formaPago).filter(Boolean))].sort();
    const selFP = document.getElementById('filtroFormaPago');
    const fpActual = selFP.value;
    selFP.innerHTML = '<option value="">Todas las formas de pago</option>';
    formas.forEach(f => {
        const opt = document.createElement('option');
        opt.value = f;
        opt.textContent = f;
        if (f === fpActual) opt.selected = true;
        selFP.appendChild(opt);
    });
}

function aplicarFiltros() {
    const mozo      = document.getElementById('filtroMozo').value;
    const formaPago = document.getElementById('filtroFormaPago').value;
    const estado    = document.getElementById('filtroEstado').value;

    const filtradas = _todasLasVentas.filter(v => {
        if (mozo      && v.mozo      !== mozo)      return false;
        if (formaPago && v.formaPago !== formaPago)  return false;
        if (estado    && v.estado    !== estado)     return false;
        return true;
    });

    renderVentas(filtradas);
}

function limpiarFiltros() {
    document.getElementById('filtroMozo').value      = '';
    document.getElementById('filtroFormaPago').value = '';
    document.getElementById('filtroEstado').value    = '';
    renderVentas(_todasLasVentas);
}

function renderVentas(ventas) {
    const body  = document.getElementById('bodyVentas');
    const vacio = document.getElementById('mensajeVacioVentas');

    const totalDia = ventas.reduce((acc, v) => acc + (v.total ?? 0), 0);
    document.getElementById('resumenDia').textContent =
        `Total filtrado: ${formatCurrency(totalDia)} · ${ventas.length} mesa(s)`;

    if (ventas.length === 0) {
        body.innerHTML = '';
        vacio.style.display = 'block';
        return;
    }

    vacio.style.display = 'none';
    body.innerHTML = ventas.map(v => `
        <tr style="cursor:pointer" onclick="verDetalle(${v.mesaId}, 'Mesa #${v.numeroMesa}', ${JSON.stringify(v.mozo)}, ${JSON.stringify(v.estado)}, ${JSON.stringify(v.formaPago)})">
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

async function verDetalle(mesaId, titulo, mozo, estado, formaPago) {
    try {
        const res = await fetch(`/api/mesas/${mesaId}/items`);
        const items = res.ok ? await res.json() : [];

        const estadoLabel = estado === 'Abierta'
            ? '<span style="color:#2e7d32;font-weight:600">● Abierta</span>'
            : '<span style="color:#c62828;font-weight:600">● Cerrada</span>';

        document.getElementById('tituloDet').innerHTML =
            `${titulo} — ${mozo || 'Sin mozo'} &nbsp;${estadoLabel}` +
            (formaPago ? ` &nbsp;· 💳 ${formaPago}` : '');

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