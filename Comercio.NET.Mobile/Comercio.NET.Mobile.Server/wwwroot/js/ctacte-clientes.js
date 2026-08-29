const API = '/api';
let token = null;
let idDetalleActual = null;
let nombreDetalleActual = '';
let idClienteEditando = null;
let timerBuscar = null;

document.addEventListener('DOMContentLoaded', async () => {
    token = localStorage.getItem('auth_token');
    if (!token) { window.location.href = '/login.html'; return; }
    try {
        const r = await fetch(`${API}/auth/validar`, { headers: { Authorization: `Bearer ${token}` } });
        const d = await r.json();
        if (!d.valido) { localStorage.clear(); window.location.href = '/login.html'; return; }
    } catch { localStorage.clear(); window.location.href = '/login.html'; return; }

    document.getElementById('nombreUsuario').textContent =
        `?? ${localStorage.getItem('usuario_completo') || 'Usuario'}`;
    document.getElementById('btnCerrarSesion').addEventListener('click', () => { localStorage.clear(); window.location.href = '/login.html'; });
    document.getElementById('btnBuscar').addEventListener('click', cargar);
    document.getElementById('txtBuscar').addEventListener('keydown', e => { if (e.key === 'Enter') cargar(); });
    document.getElementById('txtBuscar').addEventListener('input', () => { clearTimeout(timerBuscar); timerBuscar = setTimeout(cargar, 500); });
    document.getElementById('btnNuevo').addEventListener('click', () => abrirModalCliente(null));
    // Detalle
    document.getElementById('btnCerrarDetalle').addEventListener('click', () => { document.getElementById('modalDetalle').style.display = 'none'; });
    document.getElementById('btnCerrarDetalle2').addEventListener('click', () => { document.getElementById('modalDetalle').style.display = 'none'; });
    document.getElementById('btnAbrirPago').addEventListener('click', () => {
        document.getElementById('modalDetalle').style.display = 'none';
        abrirModalPago(idDetalleActual, nombreDetalleActual);
    });
    // Pago
    document.getElementById('btnCerrarPago').addEventListener('click', () => { document.getElementById('modalPago').style.display = 'none'; });
    document.getElementById('btnCancelarPago').addEventListener('click', () => { document.getElementById('modalPago').style.display = 'none'; });
    document.getElementById('btnConfirmarPago').addEventListener('click', confirmarPago);
    // Cliente
    document.getElementById('btnCerrarCliente').addEventListener('click', () => { document.getElementById('modalCliente').style.display = 'none'; });
    document.getElementById('btnCancelarCliente').addEventListener('click', () => { document.getElementById('modalCliente').style.display = 'none'; });
    document.getElementById('btnGuardarCliente').addEventListener('click', guardarCliente);

    cargar();
});

async function cargar() {
    const buscar = document.getElementById('txtBuscar').value.trim();
    document.getElementById('loading').style.display = '';
    try {
        const r = await fetch(`${API}/ctacte-clientes?buscar=${encodeURIComponent(buscar)}`, { headers: { Authorization: `Bearer ${token}` } });
        if (!r.ok) throw new Error(await r.text());
        renderTabla(await r.json());
    } catch (e) { mostrarMsgGlobal('error', `Error cargando clientes: ${e.message}`); }
    finally { document.getElementById('loading').style.display = 'none'; }
}

function renderTabla(clientes) {
    const tbody = document.getElementById('tbodyClientes');
    tbody.innerHTML = '';
    if (!clientes.length) {
        tbody.innerHTML = '<tr><td colspan="8" style="text-align:center;padding:2rem;color:#777;">No hay clientes registrados.</td></tr>';
        return;
    }
    for (const c of clientes) {
        const saldoClass = c.saldoDeudor > 0 ? 'saldo-positivo' : 'saldo-cero';
        const tr = document.createElement('tr');
        tr.innerHTML = `
            <td><strong>${esc(c.nombre)}</strong></td>
            <td>${esc(c.dni)}</td>
            <td>${esc(c.telefono)}</td>
            <td class="text-right">${fmt(c.totalCompras)}</td>
            <td class="text-right">${fmt(c.totalPagado)}</td>
            <td class="text-right"><span class="${saldoClass}">${fmt(c.saldoDeudor)}</span></td>
            <td>${c.ultimaCompra ? fmtFecha(c.ultimaCompra) : '—'}</td>
            <td class="text-center">
                <div class="row-actions">
                    <button class="btn-detail" onclick="verDetalle(${c.id},'${esc(c.nombre)}')">?? Ver</button>
                    <button class="btn-pago"   onclick="abrirModalPago(${c.id},'${esc(c.nombre)}')">?? Pago</button>
                    <button class="btn-edit"   onclick="abrirModalCliente(${c.id})">??</button>
                </div>
            </td>`;
        tbody.appendChild(tr);
    }
}

async function verDetalle(id, nombre) {
    idDetalleActual      = id;
    nombreDetalleActual  = nombre;
    document.getElementById('detalleTitulo').textContent = `?? ${nombre}`;
    document.getElementById('cuentaResumen').innerHTML   = '';
    document.getElementById('tbodyMovimientos').innerHTML = '';
    document.getElementById('movLoading').style.display  = '';
    document.getElementById('movVacio').style.display    = 'none';
    document.getElementById('modalDetalle').style.display = 'flex';

    try {
        // Datos resumen del cliente
        const rc = await fetch(`${API}/ctacte-clientes/${id}`, { headers: { Authorization: `Bearer ${token}` } });
        if (rc.ok) {
            const c = await rc.json();
            const saldoColor = c.saldoDeudor > 0 ? '#c62828' : '#2e7d32';
            document.getElementById('cuentaResumen').innerHTML = `
                <div class="cuenta-card">
                    <div class="label">Total Compras</div>
                    <div class="valor" style="color:#1565c0">${fmt(c.totalCompras)}</div>
                </div>
                <div class="cuenta-card">
                    <div class="label">Total Pagado</div>
                    <div class="valor" style="color:#2e7d32">${fmt(c.totalPagado)}</div>
                </div>
                <div class="cuenta-card">
                    <div class="label">Saldo Deudor</div>
                    <div class="valor" style="color:${saldoColor}">${fmt(c.saldoDeudor)}</div>
                </div>`;
        }

        // Movimientos
        const rm = await fetch(`${API}/ctacte-clientes/${id}/movimientos`, { headers: { Authorization: `Bearer ${token}` } });
        const movimientos = await rm.json();
        document.getElementById('movLoading').style.display = 'none';
        if (!movimientos.length) { document.getElementById('movVacio').style.display = ''; return; }
        const tbody = document.getElementById('tbodyMovimientos');
        for (const m of movimientos) {
            const tr = document.createElement('tr');
            const cls = m.tipo === 'Venta' ? 'tipo-venta' : 'tipo-pago';
            const montoSign = m.tipo === 'Venta' ? `+${fmt(m.monto)}` : `-${fmt(m.monto)}`;
            tr.innerHTML = `
                <td>${fmtFecha(m.fecha)}</td>
                <td><span class="${cls}">${m.tipo === 'Venta' ? '?? Venta' : '?? Pago'}</span></td>
                <td class="text-right"><strong class="${cls}">${montoSign}</strong></td>
                <td>${esc(m.medioPago ?? '')}</td>
                <td>${esc(m.referencia ?? '')}</td>
                <td>${esc(m.usuario ?? '')}</td>`;
            tbody.appendChild(tr);
        }
    } catch (e) {
        document.getElementById('movLoading').style.display = 'none';
        mostrarMsgGlobal('error', e.message);
    }
}

function abrirModalPago(id, nombre) {
    idDetalleActual = id;
    nombreDetalleActual = nombre;
    document.getElementById('pagoClienteNombre').textContent = `Cliente: ${nombre}`;
    document.getElementById('fPagoMonto').value = '';
    document.getElementById('fPagoMedio').value = 'Efectivo';
    document.getElementById('fPagoRef').value   = '';
    limpiarMsgPago();
    document.getElementById('modalPago').style.display = 'flex';
    document.getElementById('fPagoMonto').focus();
}

async function confirmarPago() {
    limpiarMsgPago();
    const monto = parseFloat(document.getElementById('fPagoMonto').value);
    if (!monto || monto <= 0) { mostrarMsgPago('error', 'Ingresá un monto válido mayor a cero.'); return; }
    const body = {
        monto,
        medioPago:  document.getElementById('fPagoMedio').value,
        referencia: document.getElementById('fPagoRef').value.trim(),
        usuario:    localStorage.getItem('usuario_nombre') || ''
    };
    const btn = document.getElementById('btnConfirmarPago');
    btn.disabled = true; btn.textContent = 'Guardando...';
    try {
        const r = await fetch(`${API}/ctacte-clientes/${idDetalleActual}/pagos`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
            body: JSON.stringify(body)
        });
        const d = await r.json();
        if (!r.ok) throw new Error(d.error || 'Error');
        mostrarMsgPago('ok', d.mensaje);
        setTimeout(() => { document.getElementById('modalPago').style.display = 'none'; cargar(); }, 900);
    } catch (e) { mostrarMsgPago('error', e.message); }
    finally { btn.disabled = false; btn.textContent = '?? Confirmar Pago'; }
}

function abrirModalCliente(id) {
    idClienteEditando = id;
    limpiarMsgCliente();
    if (id) {
        document.getElementById('clienteTitulo').textContent = '?? Editar Cliente';
        // Buscar en tabla actual
        fetch(`${API}/ctacte-clientes/${id}`, { headers: { Authorization: `Bearer ${token}` } })
            .then(r => r.json()).then(c => {
                document.getElementById('fClienteNombre').value = c.nombre;
                document.getElementById('fClienteDni').value    = c.dni;
                document.getElementById('fClienteTel').value    = c.telefono;
                document.getElementById('fClienteEmail').value  = c.email;
            }).catch(() => {});
    } else {
        document.getElementById('clienteTitulo').textContent = '? Nuevo Cliente';
        ['fClienteNombre','fClienteDni','fClienteTel','fClienteEmail'].forEach(i => document.getElementById(i).value = '');
    }
    document.getElementById('modalCliente').style.display = 'flex';
    document.getElementById('fClienteNombre').focus();
}

async function guardarCliente() {
    limpiarMsgCliente();
    const nombre = document.getElementById('fClienteNombre').value.trim();
    if (!nombre) { mostrarMsgCliente('error', 'El nombre es requerido.'); return; }
    const body = {
        nombre,
        dni:      document.getElementById('fClienteDni').value.trim(),
        telefono: document.getElementById('fClienteTel').value.trim(),
        email:    document.getElementById('fClienteEmail').value.trim()
    };
    const btn = document.getElementById('btnGuardarCliente');
    btn.disabled = true; btn.textContent = 'Guardando...';
    try {
        const url    = idClienteEditando ? `${API}/ctacte-clientes/${idClienteEditando}` : `${API}/ctacte-clientes`;
        const method = idClienteEditando ? 'PUT' : 'POST';
        const r = await fetch(url, { method, headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` }, body: JSON.stringify(body) });
        const d = await r.json();
        if (!r.ok) throw new Error(d.error || 'Error');
        mostrarMsgCliente('ok', d.mensaje);
        setTimeout(() => { document.getElementById('modalCliente').style.display = 'none'; cargar(); }, 900);
    } catch (e) { mostrarMsgCliente('error', e.message); }
    finally { btn.disabled = false; btn.textContent = '?? Guardar'; }
}

function mostrarMsgGlobal(tipo, msg) {
    const el = document.getElementById('msgGlobal');
    el.className = tipo === 'ok' ? 'msg-ok' : 'msg-err';
    el.textContent = msg; el.style.display = '';
    setTimeout(() => { el.style.display = 'none'; }, 5000);
}
function mostrarMsgPago(tipo, msg) {
    const el = document.getElementById('pagoMsg');
    el.className = tipo === 'ok' ? 'msg-ok' : 'msg-err';
    el.textContent = msg; el.style.display = '';
}
function limpiarMsgPago() { const el = document.getElementById('pagoMsg'); el.style.display = 'none'; }
function mostrarMsgCliente(tipo, msg) {
    const el = document.getElementById('clienteMsg');
    el.className = tipo === 'ok' ? 'msg-ok' : 'msg-err';
    el.textContent = msg; el.style.display = '';
}
function limpiarMsgCliente() { const el = document.getElementById('clienteMsg'); el.style.display = 'none'; }
function esc(s) { return String(s ?? '').replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;'); }
function fmt(n) { return new Intl.NumberFormat('es-AR', { style:'currency', currency:'ARS' }).format(n ?? 0); }
function fmtFecha(d) {
    if (!d) return '—';
    const f = new Date(d);
    return isNaN(f) ? String(d).substring(0,10) : f.toLocaleString('es-AR', { day:'2-digit', month:'2-digit', year:'numeric', hour:'2-digit', minute:'2-digit' });
}
