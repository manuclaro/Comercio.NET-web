'use strict';

document.addEventListener('DOMContentLoaded', () => {
    document.getElementById('fechaVentas').value = hoy();
    cargarVentas();
});

function hoy() {
    return new Date().toISOString().split('T')[0];
}

function formatCurrency(value) {
    return new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS' }).format(value);
}

function badgePago(formaPago, esCtaCte) {
    if (esCtaCte) return `<span class="badge badge-ctacte">Cta. Cte.</span>`;
    const fp = (formaPago || '').toLowerCase();
    if (fp === 'efectivo') return `<span class="badge badge-efectivo">Efectivo</span>`;
    if (fp.includes('tarjeta')) return `<span class="badge badge-tarjeta">Tarjeta</span>`;
    return `<span class="badge badge-otro">${formaPago || '-'}</span>`;
}

async function cargarVentas() {
    const fecha       = document.getElementById('fechaVentas').value || hoy();
    const cajero      = document.getElementById('cajeroVentas').value.trim();
    const formaPago   = document.getElementById('formaPagoVentas').value;

    let url = `/api/ventas?fecha=${fecha}`;
    if (cajero)    url += `&numeroCajero=${cajero}`;
    if (formaPago) url += `&formaPago=${encodeURIComponent(formaPago)}`;

    try {
        const [ventasRes, resumenRes] = await Promise.all([
            fetch(url),
            fetch(`/api/ventas/resumen?fecha=${fecha}${cajero ? `&numeroCajero=${cajero}` : ''}`)
        ]);

        const ventas  = await ventasRes.json();
        const resumen = await resumenRes.json();

        renderResumen(resumen);
        renderTabla(ventas);
    } catch (err) {
        console.error('Error cargando ventas:', err);
    }
}

function renderResumen(r) {
    document.getElementById('resumenCards').innerHTML = `
        <div class="resumen-card">
            <div class="valor">${formatCurrency(r.totalVendido)}</div>
            <div class="etiqueta">Total vendido</div>
        </div>
        <div class="resumen-card">
            <div class="valor">${r.cantidadTransacciones}</div>
            <div class="etiqueta">Transacciones</div>
        </div>
        <div class="resumen-card">
            <div class="valor">${r.cantidadProductos}</div>
            <div class="etiqueta">Productos</div>
        </div>
        <div class="resumen-card">
            <div class="valor">${formatCurrency(r.totalEfectivo)}</div>
            <div class="etiqueta">Efectivo</div>
        </div>
        <div class="resumen-card">
            <div class="valor">${formatCurrency(r.totalTarjeta)}</div>
            <div class="etiqueta">Tarjeta</div>
        </div>
        <div class="resumen-card">
            <div class="valor">${formatCurrency(r.totalCtaCte)}</div>
            <div class="etiqueta">Cta. Cte.</div>
        </div>
    `;
}

function renderTabla(ventas) {
    const body    = document.getElementById('bodyVentas');
    const mensaje = document.getElementById('mensajeVentas');

    if (!ventas || ventas.length === 0) {
        body.innerHTML = '';
        mensaje.style.display = 'block';
        return;
    }

    mensaje.style.display = 'none';
    body.innerHTML = ventas.map(v => `
        <tr>
            <td>#${v.nroFactura}</td>
            <td>${v.codigo ?? '-'}</td>
            <td>
                ${v.descripcion ?? '-'}
                ${v.esOferta ? `<span class="badge badge-oferta">🎁 ${v.nombreOferta || 'Oferta'}</span>` : ''}
            </td>
            <td style="text-align:center">${v.cantidad}</td>
            <td style="text-align:right">${formatCurrency(v.precio)}</td>
            <td style="text-align:right"><strong>${formatCurrency(v.total)}</strong></td>
            <td>${badgePago(v.formaPago, v.esCtaCte)}${v.esCtaCte ? ` <small>${v.nombreCtaCte}</small>` : ''}</td>
            <td>${v.tipoFactura || '-'}</td>
            <td>${v.usuarioVenta || '-'}</td>
            <td>${v.hora || '-'}</td>
        </tr>
    `).join('');
}