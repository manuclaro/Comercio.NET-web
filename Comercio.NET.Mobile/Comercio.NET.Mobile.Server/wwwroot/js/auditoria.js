'use strict';

document.addEventListener('DOMContentLoaded', () => {
    const hoy = new Date().toISOString().split('T')[0];
    document.getElementById('desdeAudit').value = hoy;
    document.getElementById('hastaAudit').value = hoy;
    cargarAuditoria();
});

function formatCurrency(value) {
    return new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS' }).format(value);
}

function formatFecha(isoString) {
    const d = new Date(isoString);
    return `${d.toLocaleDateString('es-AR')} ${d.toLocaleTimeString('es-AR', { hour: '2-digit', minute: '2-digit' })}`;
}

function badgeTipo(registro) {
    const motivo = (registro.motivoEliminacion || '').toUpperCase();

    if (motivo.includes('ANULACIÓN') || registro.esEliminacionCompleta === true)
        return `<span class="badge badge-completa">Anulación completa</span>`;

    if (motivo.includes('REDUCCIÓN'))
        return `<span class="badge badge-reduccion">Reducción cantidad</span>`;

    return `<span class="badge badge-parcial">Eliminación parcial</span>`;
}

async function cargarAuditoria() {
    const desde   = document.getElementById('desdeAudit').value;
    const hasta   = document.getElementById('hastaAudit').value;
    const usuario = document.getElementById('usuarioAudit').value.trim();
    const cajero  = document.getElementById('cajeroAudit').value.trim();

    let url = `/api/auditoria?desde=${desde}&hasta=${hasta}`;
    if (usuario) url += `&usuario=${encodeURIComponent(usuario)}`;
    if (cajero)  url += `&numeroCajero=${cajero}`;

    try {
        const res = await fetch(url);

        if (!res.ok) {
            const errorData = await res.text();
            console.error('Error del servidor:', res.status, errorData);
            renderTotales([]);
            renderTabla([]);
            return;
        }

        const registros = await res.json();
        const lista = Array.isArray(registros) ? registros : [];

        renderTotales(lista);
        renderTabla(lista);
    } catch (err) {
        console.error('Error cargando auditoría:', err);
        renderTotales([]);
        renderTabla([]);
    }
}

function renderTotales(registros) {
    const banner = document.getElementById('totalesBanner');

    if (!registros || registros.length === 0) {
        banner.style.display = 'none';
        return;
    }

    const totalImporte = registros.reduce((acc, r) => acc + (r.totalEliminado ?? 0), 0);

    document.getElementById('totalRegistros').textContent = `📋 ${registros.length} registros`;
    document.getElementById('totalImporte').textContent   = `💸 Total eliminado: ${formatCurrency(totalImporte)}`;
    banner.style.display = 'flex';
}

function renderTabla(registros) {
    const body    = document.getElementById('bodyAuditoria');
    const mensaje = document.getElementById('mensajeAuditoria');

    if (!registros || registros.length === 0) {
        body.innerHTML = '';
        mensaje.style.display = 'block';
        return;
    }

    mensaje.style.display = 'none';
    body.innerHTML = registros.map(r => `
        <tr>
            <td>${formatFecha(r.fechaEliminacion)}</td>
            <td>#${r.numeroFactura}</td>
            <td>${r.codigoProducto ?? '-'}</td>
            <td>${r.descripcionProducto ?? '-'}</td>
            <td style="text-align:center">${r.cantidad}</td>
            <td style="text-align:right">${formatCurrency(r.precioUnitario)}</td>
            <td style="text-align:right"><strong>${formatCurrency(r.totalEliminado)}</strong></td>
            <td title="${r.motivoEliminacion ?? ''}">${(r.motivoEliminacion ?? '-').substring(0, 30)}${(r.motivoEliminacion ?? '').length > 30 ? '…' : ''}</td>
            <td>${r.usuarioEliminacion ?? '-'}</td>
            <td>${r.numeroCajero}</td>
            <td>${badgeTipo(r)}</td>
        </tr>
    `).join('');
}