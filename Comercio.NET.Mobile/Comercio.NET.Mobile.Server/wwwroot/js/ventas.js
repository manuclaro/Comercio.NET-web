'use strict';

/** Obtiene la fecha de hoy en formato yyyy-MM-dd usando hora local (Argentina). */
function fechaHoyLocal() {
    const now = new Date();
    const y = now.getFullYear();
    const m = String(now.getMonth() + 1).padStart(2, '0');
    const d = String(now.getDate()).padStart(2, '0');
    return `${y}-${m}-${d}`;
}

document.addEventListener('DOMContentLoaded', () => {
    const hoy = fechaHoyLocal();
    document.getElementById('fechaDesde').value = hoy;
    document.getElementById('fechaHasta').value = hoy;
    cargarVentas();
});

function formatCurrency(value) {
    return new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS' }).format(value);
}

function formatHora(valor) {
    if (!valor) return '-';
    const t = String(valor);
    const match = t.match(/T(\d{2}:\d{2})/);
    if (match) return match[1];
    if (t.includes(':')) return t.substring(0, 5);
    return t;
}

function badgePago(formaPago, esCtaCte) {
    if (esCtaCte) return `<span class="badge badge-ctacte">Cta. Cte.</span>`;

    const fp = (formaPago || '').toLowerCase().trim();

    if (fp === 'efectivo')     return `<span class="badge badge-efectivo">💵 Efectivo</span>`;
    if (fp === 'mercado pago') return `<span class="badge badge-mercadopago">📱 Mercado Pago</span>`;
    if (fp === 'dni')          return `<span class="badge badge-dni">🪪 DNI</span>`;

    return `<span class="badge badge-otro">📝 ${formaPago || 'Otro'}</span>`;
}

async function cargarVentas() {
    const fechaDesde  = document.getElementById('fechaDesde').value;
    const fechaHasta  = document.getElementById('fechaHasta').value;
    const cajero      = document.getElementById('cajeroVentas').value.trim();
    const formaPago   = document.getElementById('formaPagoVentas').value;
    const tipoFactura = document.getElementById('tipoFacturaVentas').value;

    if (!fechaDesde || !fechaHasta) {
        alert('Debe seleccionar las fechas Desde y Hasta.');
        return;
    }

    let urlVentas  = '/api/ventas';
    let urlResumen = '/api/ventas/resumen';
    const params   = new URLSearchParams();

    params.set('desde', fechaDesde);
    params.set('hasta', fechaHasta);

    if (cajero)      params.set('numeroCajero', cajero);
    if (formaPago)   params.set('formaPago', formaPago);
    if (tipoFactura) params.set('tipoFactura', tipoFactura);

    const qs = params.toString();
    urlVentas  += `?${qs}`;
    urlResumen += `?${qs}`;

    try {
        const [ventasRes, resumenRes] = await Promise.all([
            fetch(urlVentas),
            fetch(urlResumen)
        ]);

        // Verificar si hay turno abierto
        const turnoRes  = await fetch('/api/turno/activo');
        const turnoData = turnoRes.ok ? await turnoRes.json() : null;
        const avisoEl   = document.getElementById('avisoSinTurno');
        if (avisoEl) avisoEl.style.display = turnoData?.abierto ? 'none' : 'block';

        if (!ventasRes.ok) {
            console.error('Error en /api/ventas:', ventasRes.status, await ventasRes.text());
            renderTabla([]);
        } else {
            const data = await ventasRes.json();
            renderTabla(Array.isArray(data) ? data : []);
        }

        if (!resumenRes.ok) {
            console.error('Error en /api/ventas/resumen:', resumenRes.status, await resumenRes.text());
            renderResumen({});
        } else {
            const resumen = await resumenRes.json();
            renderResumen(resumen && typeof resumen === 'object' ? resumen : {});
        }

    } catch (err) {
        console.error('Error cargando ventas:', err);
        renderTabla([]);
        renderResumen({});
    }
}

function renderResumen(r) {
    document.getElementById('resumenCards').innerHTML = `
        <div class="resumen-card">
            <div class="valor">${formatCurrency(r.totalVendido ?? 0)}</div>
            <div class="etiqueta">Total vendido</div>
        </div>
        <div class="resumen-card">
            <div class="valor">${r.cantidadTransacciones ?? 0}</div>
            <div class="etiqueta">Transacciones</div>
        </div>
        <div class="resumen-card">
            <div class="valor">${r.cantidadProductos ?? 0}</div>
            <div class="etiqueta">Productos</div>
        </div>
        <div class="resumen-card">
            <div class="valor">${formatCurrency(r.totalEfectivo ?? 0)}</div>
            <div class="etiqueta">💵 Efectivo</div>
        </div>
        <div class="resumen-card">
            <div class="valor">${formatCurrency(r.totalMercadoPago ?? 0)}</div>
            <div class="etiqueta">📱 Mercado Pago</div>
        </div>
        <div class="resumen-card">
            <div class="valor">${formatCurrency(r.totalDni ?? 0)}</div>
            <div class="etiqueta">🪪 DNI</div>
        </div>
        <div class="resumen-card">
            <div class="valor">${formatCurrency(r.totalOtros ?? 0)}</div>
            <div class="etiqueta">📝 Otros</div>
        </div>
        <div class="resumen-card">
            <div class="valor">${formatCurrency(r.totalCtaCte ?? 0)}</div>
            <div class="etiqueta">📋 Cta. Cte.</div>
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
            <td>${formatHora(v.hora)}</td>
        </tr>
    `).join('');
}