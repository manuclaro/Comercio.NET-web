'use strict';

function fechaHoyLocal() {
    const now = new Date();
    return `${now.getFullYear()}-${String(now.getMonth()+1).padStart(2,'0')}-${String(now.getDate()).padStart(2,'0')}`;
}

document.addEventListener('DOMContentLoaded', () => {
    const hoy = fechaHoyLocal();
    document.getElementById('fechaDesde').value = hoy;
    document.getElementById('fechaHasta').value = hoy;

    const nombreEl = document.getElementById('nombreUsuario');
    if (nombreEl) {
        const nombre = localStorage.getItem('usuario_nombre') || 'Usuario';
        nombreEl.textContent = nombre;
    }

    const btnCerrar = document.getElementById('btnCerrarSesion');
    if (btnCerrar) btnCerrar.addEventListener('click', () => {
        localStorage.removeItem('auth_token');
        window.location.href = '/login.html';
    });

    cargarFacturas();
});

function formatCurrency(value) {
    return new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS' }).format(value || 0);
}

function formatFecha(valor) {
    if (!valor) return '-';
    const d = new Date(valor);
    if (isNaN(d)) return valor;
    return d.toLocaleDateString('es-AR');
}

function formatHora(valor) {
    if (!valor) return '-';
    const t = String(valor);
    const match = t.match(/T(\d{2}:\d{2})/);
    if (match) return match[1];
    if (t.includes(':')) return t.substring(0, 5);
    return t;
}

function authHeaders() {
    return { 'Authorization': 'Bearer ' + (localStorage.getItem('auth_token') || '') };
}

async function cargarFacturas() {
    const desde = document.getElementById('fechaDesde').value;
    const hasta = document.getElementById('fechaHasta').value;
    const cajero = document.getElementById('cajeroFacturas').value.trim();
    const formaPago = document.getElementById('formaPagoFacturas').value;
    const tipo = document.getElementById('tipoFacturaFiltro').value;

    if (!desde || !hasta) { alert('Seleccione fechas.'); return; }

    const params = new URLSearchParams();
    params.set('desde', desde);
    params.set('hasta', hasta);
    if (cajero) params.set('cajero', cajero);
    if (formaPago) params.set('formaPago', formaPago);
    if (tipo) params.set('tipoFactura', tipo);

    const qs = params.toString();

    try {
        const [facturasRes, resumenRes] = await Promise.all([
            fetch(`/api/facturas?${qs}`, { headers: authHeaders() }),
            fetch(`/api/facturas/resumen?${qs}`, { headers: authHeaders() })
        ]);

        if (!facturasRes.ok) {
            console.error('Error facturas:', facturasRes.status);
            renderTabla([]);
        } else {
            const data = await facturasRes.json();
            renderTabla(Array.isArray(data) ? data : []);
        }

        if (!resumenRes.ok) {
            renderResumen({});
        } else {
            const resumen = await resumenRes.json();
            renderResumen(resumen || {});
        }
    } catch (err) {
        console.error('Error:', err);
        renderTabla([]);
        renderResumen({});
    }
}

function renderResumen(r) {
    document.getElementById('resumenCards').innerHTML = `
        <div class="resumen-card">
            <div class="valor">${r.cantidadFacturas ?? 0}</div>
            <div class="etiqueta">Facturas</div>
        </div>
        <div class="resumen-card">
            <div class="valor">${formatCurrency(r.totalImporte ?? 0)}</div>
            <div class="etiqueta">Total</div>
        </div>
        <div class="resumen-card">
            <div class="valor">${formatCurrency(r.totalEfectivo ?? 0)}</div>
            <div class="etiqueta">&#x1F4B5; Efectivo</div>
        </div>
        <div class="resumen-card">
            <div class="valor">${formatCurrency(r.totalMercadoPago ?? 0)}</div>
            <div class="etiqueta">&#x1F4F1; Mercado Pago</div>
        </div>
        <div class="resumen-card">
            <div class="valor">${formatCurrency(r.totalDni ?? 0)}</div>
            <div class="etiqueta">&#x1FAAA; DNI</div>
        </div>
        <div class="resumen-card">
            <div class="valor">${formatCurrency(r.totalOtros ?? 0)}</div>
            <div class="etiqueta">&#x1F4DD; Otros</div>
        </div>
        <div class="resumen-card">
            <div class="valor">${formatCurrency(r.totalFacturasElectronicas ?? 0)}</div>
            <div class="etiqueta">&#x1F9FE; Fact. Electr.</div>
        </div>
        <div class="resumen-card">
            <div class="valor">${formatCurrency(r.totalCtaCte ?? 0)}</div>
            <div class="etiqueta">&#x1F4CB; Cta. Cte.</div>
        </div>
    `;
}

function cargarAyer() {
    const ayer = new Date();
    ayer.setDate(ayer.getDate() - 1);
    const str = `${ayer.getFullYear()}-${String(ayer.getMonth()+1).padStart(2,'0')}-${String(ayer.getDate()).padStart(2,'0')}`;
    document.getElementById('fechaDesde').value = str;
    document.getElementById('fechaHasta').value = str;
    cargarFacturas();
}

function badgePagoFactura(formaPago) {
    const fp = (formaPago || '').toLowerCase().trim();
    if (fp === 'efectivo') return `<span class="badge badge-efectivo">&#x1F4B5; Efectivo</span>`;
    if (fp.includes('mercado')) return `<span class="badge badge-mercadopago">&#x1F4F1; Mercado Pago</span>`;
    if (fp === 'dni') return `<span class="badge badge-dni">&#x1FAAA; DNI</span>`;
    return `<span class="badge badge-otro">&#x1F4DD; ${formaPago || 'Otro'}</span>`;
}

let facturaSeleccionada = null;

function renderTabla(facturas) {
    const body = document.getElementById('bodyFacturas');
    const mensaje = document.getElementById('mensajeFacturas');

    if (!facturas || facturas.length === 0) {
        body.innerHTML = '';
        mensaje.style.display = 'block';
        return;
    }

    mensaje.style.display = 'none';
    body.innerHTML = facturas.map(f => `
        <tr style="cursor:pointer;" onclick='abrirDetalle(${JSON.stringify(f).replace(/'/g,"&apos;")})'>
            <td>R-${f.numeroRemito}</td>
            <td>${f.nroFactura || '-'}</td>
            <td style="text-align:right"><strong>${formatCurrency(f.importeFinal)}</strong></td>
            <td>${badgePagoFactura(f.formaDePago)}</td>
            <td>${f.tipoFactura || '-'}</td>
            <td>${f.cajero || '-'}</td>
            <td>${f.usuarioVenta || '-'}</td>
            <td>${formatFecha(f.fecha)}</td>
            <td>${formatHora(f.hora)}</td>
            <td>${f.caeNumero || '-'}</td>
        </tr>
    `).join('');
}

async function abrirDetalle(factura) {
    facturaSeleccionada = factura;
    const modal = document.getElementById('modalDetalle');
    const titulo = document.getElementById('modalTitulo');
    const info = document.getElementById('modalInfo');
    const body = document.getElementById('modalDetalleBody');
    const msg = document.getElementById('modalMensaje');
    msg.style.display = 'none';

    titulo.textContent = `Detalle - Remito #${factura.numeroRemito}`;
    info.innerHTML = `<strong>Tipo:</strong> ${factura.tipoFactura || '-'} | <strong>Importe:</strong> ${formatCurrency(factura.importeFinal)} | <strong>CAE:</strong> ${factura.caeNumero || 'Sin CAE'}`;

    document.getElementById('modalFormaPago').value = factura.formaDePago || 'Efectivo';

    // Mostrar/ocultar boton FE segun si ya tiene CAE
    const btnFE = document.getElementById('btnGenerarFE');
    btnFE.style.display = factura.caeNumero ? 'none' : 'inline-block';

    body.innerHTML = '<tr><td colspan="5" style="text-align:center;padding:12px;">Cargando...</td></tr>';
    modal.style.display = 'flex';

    try {
        const res = await fetch(`/api/facturas/${factura.numeroRemito}/detalle`, { headers: authHeaders() });
        if (!res.ok) throw new Error('Error al obtener detalle');
        const items = await res.json();
        if (!items.length) {
            body.innerHTML = '<tr><td colspan="5" style="text-align:center;padding:12px;">Sin productos</td></tr>';
        } else {
            body.innerHTML = items.map(i => `
                <tr>
                    <td style="padding:4px 6px;">${i.codigo}</td>
                    <td style="padding:4px 6px;">${i.descripcion}</td>
                    <td style="padding:4px 6px;text-align:right;">${formatCurrency(i.precio)}</td>
                    <td style="padding:4px 6px;text-align:center;">${i.cantidad}</td>
                    <td style="padding:4px 6px;text-align:right;">${formatCurrency(i.total)}</td>
                </tr>
            `).join('');
        }
    } catch (err) {
        body.innerHTML = `<tr><td colspan="5" style="color:red;padding:12px;">${err.message}</td></tr>`;
    }
}

function cerrarModal() {
    document.getElementById('modalDetalle').style.display = 'none';
    facturaSeleccionada = null;
}

async function guardarFormaPago() {
    if (!facturaSeleccionada) return;
    const formaPago = document.getElementById('modalFormaPago').value;
    try {
        const res = await fetch(`/api/facturas/${facturaSeleccionada.numeroRemito}/forma-pago`, {
            method: 'PUT',
            headers: { ...authHeaders(), 'Content-Type': 'application/json' },
            body: JSON.stringify({ formaPago })
        });
        if (!res.ok) throw new Error('Error al actualizar');
        mostrarMensajeModal('Forma de pago actualizada correctamente');
        facturaSeleccionada.formaDePago = formaPago;
        cargarFacturas();
    } catch (err) {
        mostrarMensajeModal(err.message, true);
    }
}

async function generarFE() {
    if (!facturaSeleccionada) return;
    if (!confirm('Generar factura electronica para este remito?')) return;
    try {
        const res = await fetch(`/api/facturas/${facturaSeleccionada.numeroRemito}/generar-fe`, {
            method: 'POST',
            headers: authHeaders()
        });
        const data = await res.json();
        if (!res.ok) throw new Error(data.error || 'Error al generar FE');
        mostrarMensajeModal(data.mensaje);
        document.getElementById('btnGenerarFE').style.display = 'none';
        facturaSeleccionada.caeNumero = data.cae;
        facturaSeleccionada.nroFactura = data.nroFactura;
        cargarFacturas();
    } catch (err) {
        mostrarMensajeModal(err.message, true);
    }
}

async function reimprimir() {
    if (!facturaSeleccionada) return;
    try {
        // Obtener datos de facturacion para el encabezado
        const datosRes = await fetch('/api/caja/datos-facturacion', { headers: authHeaders() });
        const comercio = datosRes.ok ? await datosRes.json() : {};

        // Obtener items
        const itemsRes = await fetch(`/api/facturas/${facturaSeleccionada.numeroRemito}/detalle`, { headers: authHeaders() });
        const items = itemsRes.ok ? await itemsRes.json() : [];

        const f = facturaSeleccionada;
        const esFE = !!f.caeNumero;

        let cantTotal = 0;
        let itemsHtml = '';
        items.forEach(item => {
            cantTotal += item.cantidad;
            itemsHtml += `<tr>
                <td class="c">${item.cantidad}</td>
                <td>${item.descripcion}</td>
                <td class="r">$ ${Number(item.precio).toLocaleString('es-AR', {minimumFractionDigits:2})}</td>
                <td class="r">$ ${Number(item.total).toLocaleString('es-AR', {minimumFractionDigits:2})}</td>
            </tr>`;
        });

        const total = Number(f.importeFinal);

        if (esFE) {
            // Formato identico al de generacion original de FE en caja.js
            const tipoLetra = (f.tipoFactura || '').replace('Factura', '');
            const esMonotributo = (comercio.condicion || '').toLowerCase().includes('monotributo');
            const fecha = formatFecha(f.fecha);

            const html = `<!DOCTYPE html>
<html><head><meta charset="UTF-8"><title>Factura ${f.nroFactura}</title>
<style>
    * { margin: 0; padding: 0; box-sizing: border-box; }
    body { font-family: Arial, sans-serif; width: 80mm; margin: 0 auto; padding: 4mm; font-size: 9pt; }
    .center { text-align: center; }
    .r { text-align: right; }
    .c { text-align: center; }
    h1 { font-size: 16pt; margin: 4mm 0 1mm; }
    .domicilio { font-size: 8pt; color: #333; }
    .datos-fiscales { font-size: 7pt; margin: 3mm 0; line-height: 1.6; }
    .tipo-factura { font-size: 11pt; font-weight: bold; margin: 4mm 0; border-top: 1px solid #000; border-bottom: 1px solid #000; padding: 2mm 0; }
    table { width: 100%; border-collapse: collapse; margin: 2mm 0; }
    th { font-size: 7pt; font-weight: bold; border-bottom: 1px solid #000; padding: 1mm 0; text-align: left; }
    td { font-size: 8pt; padding: 1mm 0; }
    .totales { border-top: 2px solid #000; margin-top: 3mm; padding-top: 2mm; }
    .total-line { display: flex; justify-content: space-between; font-size: 9pt; }
    .total-final { font-size: 12pt; font-weight: bold; }
    .iva-info { font-size: 7pt; color: #666; font-style: italic; margin: 2mm 0; }
    .cae-info { font-size: 7pt; margin-top: 3mm; border-top: 1px dashed #000; padding-top: 2mm; }
    .gracias { text-align: center; margin-top: 4mm; font-size: 9pt; }
    @media print { body { width: 80mm; } }
</style></head><body>
    <div class="center">
        <p style="font-size:8pt;">Fecha: ${fecha}</p>
        <h1>${comercio.nombreComercio || 'Comercio'}</h1>
        <p class="domicilio">${comercio.domicilioComercio || ''}</p>
    </div>
    <div class="datos-fiscales">
        ${comercio.razonSocial ? 'Razon Social: ' + comercio.razonSocial + '<br>' : ''}
        ${comercio.cuit ? 'CUIT: ' + comercio.cuit + '<br>' : ''}
        ${comercio.domicilioFiscal ? 'Dom. Fiscal: ' + comercio.domicilioFiscal + (comercio.codigoPostal ? ' - CP: ' + comercio.codigoPostal : '') + '<br>' : ''}
        ${comercio.ingBrutos ? 'Ing. Brutos: ' + comercio.ingBrutos + '<br>' : ''}
        ${comercio.condicion ? 'Condicion IVA: ' + comercio.condicion + '<br>' : ''}
        ${comercio.inicioActividades ? 'Inicio Actividades: ' + comercio.inicioActividades : ''}
    </div>
    <div class="tipo-factura center">FACTURA ${tipoLetra} N&#176; ${(f.nroFactura || '').split(' ').pop()}</div>
    <table>
        <thead><tr><th class="c">C</th><th>PRODUCTO</th><th class="r">PRECIO</th><th class="r">TOTAL</th></tr></thead>
        <tbody>${itemsHtml}</tbody>
    </table>
    <div class="totales">
        <div class="total-line"><span>PRODUCTOS: ${cantTotal}</span><span>SUBTOTAL: $ ${(total + (f.importeDescuento || 0)).toLocaleString('es-AR', {minimumFractionDigits:2})}</span></div>
        ${f.porcentajeDescuento > 0 ? `<div class="total-line" style="color:#c00"><span>DESCUENTO (${f.porcentajeDescuento}%):</span><span>-$ ${Number(f.importeDescuento).toLocaleString('es-AR', {minimumFractionDigits:2})}</span></div>` : ''}
        <div class="total-line total-final"><span></span><span>TOTAL: $ ${total.toLocaleString('es-AR', {minimumFractionDigits:2})}</span></div>
    </div>
    ${esMonotributo ? '<p class="iva-info center">IVA incluido - Monotributo - No discriminado</p>' : ''}
    <div class="cae-info">
        CAE: ${f.caeNumero}<br>
        Comprobante: ${f.nroFactura}
    </div>
    <p class="gracias">Gracias por su compra!</p>
</body></html>`;

            const ventana = window.open('', '_blank', 'width=350,height=600');
            ventana.document.write(html);
            ventana.document.close();
            setTimeout(() => { ventana.print(); }, 500);
        } else {
            // Formato remito simple
            const html = `<!DOCTYPE html><html><head><meta charset="UTF-8"><title>Remito #${f.numeroRemito}</title>
                <style>body{font-family:monospace;font-size:12px;width:280px;margin:0 auto;padding:10px;}
                table{width:100%;border-collapse:collapse;} th,td{text-align:left;padding:2px 4px;}
                .center{text-align:center;} hr{border:none;border-top:1px dashed #000;margin:6px 0;}</style>
                </head><body>
                <div class="center"><strong>${comercio.nombreComercio || 'Comercio'}</strong></div>
                <div class="center">${comercio.domicilioComercio || ''}</div>
                <div class="center">CUIT: ${comercio.cuit || ''}</div>
                <hr/>
                <div class="center"><strong>Remito #${f.numeroRemito}</strong></div>
                <p>Fecha: ${formatFecha(f.fecha)} - Forma pago: ${f.formaDePago || '-'}</p>
                <hr/>
                <table><thead><tr><th>Desc.</th><th>Cant.</th><th>P.U.</th><th>Total</th></tr></thead>
                <tbody>${items.map(i => `<tr><td>${i.descripcion}</td><td>${i.cantidad}</td><td>$${Number(i.precio).toFixed(2)}</td><td>$${Number(i.total).toFixed(2)}</td></tr>`).join('')}</tbody></table>
                <hr/>
                ${f.porcentajeDescuento > 0 ? `<p style="text-align:right;color:#c00;">DESCUENTO (${f.porcentajeDescuento}%): -$${Number(f.importeDescuento).toFixed(2)}</p>` : ''}
                <p style="text-align:right;font-size:14px;"><strong>TOTAL: ${formatCurrency(f.importeFinal)}</strong></p>
                <hr/>
                <div class="center" style="margin-top:8px;">Gracias por su compra</div>
                </body></html>`;

            const win = window.open('', '_blank', 'width=320,height=600');
            win.document.write(html);
            win.document.close();
            setTimeout(() => { win.print(); }, 400);
        }
    } catch (err) {
        mostrarMensajeModal('Error al reimprimir: ' + err.message, true);
    }
}

function mostrarMensajeModal(texto, esError) {
    const el = document.getElementById('modalMensaje');
    el.textContent = texto;
    el.style.color = esError ? '#dc3545' : '#28a745';
    el.style.display = 'block';
    setTimeout(() => { el.style.display = 'none'; }, 4000);
}
