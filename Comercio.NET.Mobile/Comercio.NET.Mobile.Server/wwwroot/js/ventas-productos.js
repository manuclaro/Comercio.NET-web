'use strict';

let _todos = [];

document.addEventListener('DOMContentLoaded', async () => {
    const token = localStorage.getItem('auth_token');
    if (!token) { window.location.href = '/login.html'; return; }

    try {
        const res  = await fetch('/api/auth/validar', { headers: { 'Authorization': `Bearer ${token}` } });
        const data = await res.json();
        if (!data.valido) { localStorage.clear(); window.location.href = '/login.html'; return; }
    } catch {
        localStorage.clear(); window.location.href = '/login.html'; return;
    }

    const rol = (localStorage.getItem('usuario_rol') || '').toLowerCase();
    if (rol !== 'pizzeria') { window.location.href = '/dashboard.html'; return; }

    const nombre = localStorage.getItem('usuario_completo') || localStorage.getItem('usuario_nombre') || 'Usuario';
    document.getElementById('nombreUsuarioVP').textContent = `👤 ${nombre}`;

    // Fechas por defecto: hoy
    const hoy = new Date().toISOString().slice(0, 10);
    document.getElementById('filtroDesde').value = hoy;
    document.getElementById('filtroHasta').value = hoy;

    document.getElementById('btnSalir').addEventListener('click', () => {
        localStorage.clear(); window.location.href = '/login.html';
    });
    document.getElementById('btnBuscar').addEventListener('click', buscar);
    document.getElementById('btnLimpiar').addEventListener('click', limpiar);
    document.getElementById('filtroProducto').addEventListener('input', aplicarFiltrosLocales);
    document.getElementById('filtroTipo').addEventListener('change', aplicarFiltrosLocales);
    document.getElementById('filtroMozo').addEventListener('change', aplicarFiltrosLocales);
    document.getElementById('filtroFormaPago').addEventListener('change', aplicarFiltrosLocales);

    await buscar();
});

function formatCurrency(value) {
    return new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS' }).format(value);
}

function formatFecha(isoString) {
    if (!isoString) return '-';
    const local = isoString.replace(/Z$/, '').replace(/[+-]\d{2}:\d{2}$/, '');
    const d = new Date(local);
    return d.toLocaleDateString('es-AR', { day: '2-digit', month: '2-digit', year: 'numeric' });
}

async function buscar() {
    const desde = document.getElementById('filtroDesde').value;
    const hasta = document.getElementById('filtroHasta').value;

    if (!desde || !hasta) { alert('Seleccioná un rango de fechas.'); return; }

    const btn = document.getElementById('btnBuscar');
    btn.textContent = '⏳ Buscando...';
    btn.disabled    = true;

    try {
        const res  = await fetch(`/api/mesas/ventas-por-producto?fechaDesde=${desde}&fechaHasta=${hasta}`);
        const data = res.ok ? await res.json() : [];
        _todos = Array.isArray(data) ? data : [];
        poblarFiltros(_todos);
        aplicarFiltrosLocales();
    } catch (err) {
        console.error('Error cargando ventas por producto:', err);
        renderTabla([]);
    } finally {
        btn.textContent = 'Buscar';
        btn.disabled    = false;
    }
}

function poblarFiltros(datos) {
    poblarSelect('filtroTipo',      datos, v => (v.tipoProducto ?? '').trim(), 'Todos los tipos');
    poblarSelect('filtroMozo',      datos, v => (v.mozo         ?? '').trim(), 'Todos los mozos');
    poblarSelect('filtroFormaPago', datos, v => (v.formaPago    ?? '').trim(), 'Todas las formas de pago');
}

function poblarSelect(id, datos, fn, placeholder) {
    const sel     = document.getElementById(id);
    const actual  = sel.value;
    const valores = [...new Set(datos.map(fn).filter(v => v !== ''))].sort();

    sel.innerHTML = `<option value="">${placeholder}</option>`;
    valores.forEach(v => {
        const opt       = document.createElement('option');
        opt.value       = v;
        opt.textContent = v;
        if (v === actual) opt.selected = true;
        sel.appendChild(opt);
    });
}

function aplicarFiltrosLocales() {
    const textoProd = document.getElementById('filtroProducto').value.trim().toLowerCase();
    const tipo      = document.getElementById('filtroTipo').value;
    const mozo      = document.getElementById('filtroMozo').value;
    const formaPago = document.getElementById('filtroFormaPago').value;

    const filtrados = _todos.filter(v => {
        if (textoProd && !(v.descripcion ?? '').toLowerCase().includes(textoProd) &&
                         !(v.codigo      ?? '').toLowerCase().includes(textoProd)) return false;
        if (tipo      && (v.tipoProducto ?? '').trim() !== tipo)      return false;
        if (mozo      && (v.mozo         ?? '').trim() !== mozo)      return false;
        if (formaPago && (v.formaPago    ?? '').trim() !== formaPago) return false;
        return true;
    });

    renderTabla(filtrados);
}

function limpiar() {
    const hoy = new Date().toISOString().slice(0, 10);
    document.getElementById('filtroDesde').value    = hoy;
    document.getElementById('filtroHasta').value    = hoy;
    document.getElementById('filtroProducto').value = '';
    document.getElementById('filtroTipo').value     = '';
    document.getElementById('filtroMozo').value     = '';
    document.getElementById('filtroFormaPago').value = '';
    buscar();
}

function renderTabla(datos) {
    const body   = document.getElementById('bodyVP');
    const vacio  = document.getElementById('mensajeVacioVP');
    const resumen = document.getElementById('resumenVP');

    const totalGeneral  = datos.reduce((acc, v) => acc + (v.totalRecaudado  ?? 0), 0);
    const totalCantidad = datos.reduce((acc, v) => acc + (v.cantidadTotal   ?? 0), 0);

    resumen.textContent =
        `${datos.length} registro(s) · ${totalCantidad} unidades · Total: ${formatCurrency(totalGeneral)}`;

    if (datos.length === 0) {
        body.innerHTML   = '';
        vacio.style.display = 'block';
        return;
    }

    vacio.style.display = 'none';

    body.innerHTML = datos.map(v => {
        const tipo      = (v.tipoProducto ?? '').trim() || '-';
        const mozo      = (v.mozo         ?? '').trim() || '-';
        const formaPago = (v.formaPago    ?? '').trim() || '-';

        return `
        <tr>
            <td>${v.codigo || '-'}</td>
            <td><strong>${v.descripcion || '-'}</strong></td>
            <td>${tipo}</td>
            <td style="text-align:center">${v.cantidadTotal}</td>
            <td style="text-align:right">${formatCurrency(v.precioUnitario)}</td>
            <td style="text-align:right"><strong>${formatCurrency(v.totalRecaudado)}</strong></td>
            <td>${mozo}</td>
            <td>${formaPago}</td>
            <td>${formatFecha(v.fechaApertura)}</td>
        </tr>`;
    }).join('');
}