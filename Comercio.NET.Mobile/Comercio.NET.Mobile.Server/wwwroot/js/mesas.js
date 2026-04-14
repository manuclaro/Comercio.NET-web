'use strict';

let mesaActivaId = null;
let turnoActivo  = null;

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
    if (rol !== 'pizzeria') {
        window.location.href = rol === 'admin' ? '/dashboard.html' : '/login.html';
        return;
    }

    const nombre = localStorage.getItem('usuario_completo') || localStorage.getItem('usuario_nombre') || 'Usuario';
    const spanNombre = document.getElementById('nombreUsuarioMesas');
    if (spanNombre) spanNombre.textContent = `👤 ${nombre}`;

    await Promise.all([cargarMozos(), cargarProductosBar(), cargarFormasPago()]);
    await verificarTurno();

    document.getElementById('btnSalir').addEventListener('click', () => {
        localStorage.clear();
        window.location.href = '/login.html';
    });

    document.getElementById('btnNuevaMesa').addEventListener('click', () => {
        if (!turnoActivo) { alert('Primero abrí un turno para poder abrir mesas.'); return; }
        abrirModal('modalNuevaMesa');
    });
    document.getElementById('btnCerrarNuevaMesa').addEventListener('click', () => cerrarModal('modalNuevaMesa'));
    document.getElementById('formNuevaMesa').addEventListener('submit', onAbrirMesa);

    document.getElementById('btnVolverMesas').addEventListener('click', volverALista);
    document.getElementById('btnAgregarItem').addEventListener('click', onAgregarItem);
    document.getElementById('btnCerrarMesa').addEventListener('click', () => abrirModal('modalCerrarMesa'));
    document.getElementById('btnCancelarCierre').addEventListener('click', () => cerrarModal('modalCerrarMesa'));
    document.getElementById('formCerrarMesa').addEventListener('submit', onCerrarMesa);

    // Turno
    document.getElementById('btnAbrirTurno').addEventListener('click', onAbrirTurno);
    document.getElementById('btnCerrarTurno').addEventListener('click', () => abrirModal('modalCerrarTurno'));
    document.getElementById('btnCancelarCierreTurno').addEventListener('click', () => cerrarModal('modalCerrarTurno'));
    document.getElementById('btnConfirmarCierreTurno').addEventListener('click', onCerrarTurno);

    // ── Delegación: tabla desktop ──────────────────────────────────────────
    document.getElementById('bodyItems').addEventListener('click', async (e) => {
        const btnEliminar  = e.target.closest('[data-accion="eliminar-item"]');
        const btnConfirmar = e.target.closest('[data-accion="confirmar-cantidad"]');

        if (btnEliminar) {
            await eliminarItem(Number(btnEliminar.dataset.id));
        }
        if (btnConfirmar) {
            const itemId        = Number(btnConfirmar.dataset.id);
            const input         = document.querySelector(`input[data-cantidad-id="${itemId}"]`);
            const nuevaCantidad = parseInt(input?.value, 10);
            if (!input || isNaN(nuevaCantidad) || nuevaCantidad < 1) {
                alert('Ingresá una cantidad válida (mínimo 1).');
                return;
            }
            await actualizarCantidadItem(itemId, nuevaCantidad);
        }
    });

    // ── Delegación: cards móvil ────────────────────────────────────────────
    document.getElementById('itemsCards').addEventListener('click', async (e) => {
        const btnMenos    = e.target.closest('[data-accion="cant-menos"]');
        const btnMas      = e.target.closest('[data-accion="cant-mas"]');
        const btnOk       = e.target.closest('[data-accion="cant-ok"]');
        const btnEliminar = e.target.closest('[data-accion="eliminar-card"]');

        if (btnMenos) {
            const input = document.querySelector(`input[data-card-id="${btnMenos.dataset.id}"]`);
            if (input) input.value = Math.max(1, parseInt(input.value, 10) - 1);
        }
        if (btnMas) {
            const input = document.querySelector(`input[data-card-id="${btnMas.dataset.id}"]`);
            if (input) input.value = parseInt(input.value, 10) + 1;
        }
        if (btnOk) {
            const itemId        = Number(btnOk.dataset.id);
            const input         = document.querySelector(`input[data-card-id="${itemId}"]`);
            const nuevaCantidad = parseInt(input?.value, 10);
            if (!input || isNaN(nuevaCantidad) || nuevaCantidad < 1) {
                alert('Ingresá una cantidad válida (mínimo 1).');
                return;
            }
            await actualizarCantidadItem(itemId, nuevaCantidad);
        }
        if (btnEliminar) {
            await eliminarItem(Number(btnEliminar.dataset.id));
        }
    });
});

// ─── Turno ────────────────────────────────────────────────────────────────────

async function verificarTurno() {
    try {
        const res  = await fetch('/api/turno/activo');
        const data = res.ok ? await res.json() : null;

        turnoActivo = data?.abierto ? data.turno : null;
        renderBannerTurno();
        await cargarMesas();
    } catch (err) {
        console.error('Error verificando turno:', err);
        renderBannerTurno();
        await cargarMesas();
    }
}

function renderBannerTurno() {
    const banner      = document.getElementById('bannedTurno');
    const info        = document.getElementById('turnoInfo');
    const btnAbrir    = document.getElementById('btnAbrirTurno');
    const btnCerrar   = document.getElementById('btnCerrarTurno');

    banner.style.display = 'flex';

    if (turnoActivo) {
        banner.className   = 'turno-banner turno-abierto';
        info.textContent   = `🟢 Turno abierto desde ${formatFecha(turnoActivo.fechaApertura)}`;
        btnAbrir.style.display  = 'none';
        btnCerrar.style.display = 'inline-block';
    } else {
        banner.className   = 'turno-banner turno-cerrado';
        info.textContent   = '🔴 Sin turno abierto';
        btnAbrir.style.display  = 'inline-block';
        btnCerrar.style.display = 'none';
    }
}

async function onAbrirTurno() {
    try {
        const res = await fetch('/api/turno/abrir', { method: 'POST' });
        if (!res.ok) throw new Error(await res.text());
        turnoActivo = await res.json();
        renderBannerTurno();
    } catch (err) {
        console.error('Error abriendo turno:', err);
        alert('No se pudo abrir el turno.');
    }
}

async function onCerrarTurno() {
    cerrarModal('modalCerrarTurno');
    try {
        const res = await fetch('/api/turno/cerrar', { method: 'POST' });
        if (!res.ok) {
            const body = await res.json().catch(() => ({}));
            alert(body.error || 'No se pudo cerrar el turno.');
            return;
        }
        turnoActivo = null;
        renderBannerTurno();
        await cargarMesas();
    } catch (err) {
        console.error('Error cerrando turno:', err);
        alert('No se pudo cerrar el turno.');
    }
}

// ─── Utilidades ───────────────────────────────────────────────────────────────

function formatCurrency(value) {
    return new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS' }).format(value);
}

function formatFecha(isoString) {
    if (!isoString) return '-';
    const local = isoString.replace(/Z$/, '').replace(/[+-]\d{2}:\d{2}$/, '');
    const d = new Date(local);
    return `${d.toLocaleDateString('es-AR')} ${d.toLocaleTimeString('es-AR', { hour: '2-digit', minute: '2-digit' })}`;
}

// ─── Carga selects ────────────────────────────────────────────────────────────

async function cargarMozos() {
    try {
        const res = await fetch('/api/mesas/mozos');
        if (!res.ok) { console.error('Error GET /api/mesas/mozos:', res.status, await res.text()); return; }
        const mozos = await res.json();
        const select = document.getElementById('selectMozo');
        select.innerHTML = '<option value="">-- Seleccioná un mozo --</option>';
        if (!Array.isArray(mozos) || mozos.length === 0) {
            const opt = document.createElement('option');
            opt.disabled = true;
            opt.textContent = '(Sin mozos cargados)';
            select.appendChild(opt);
            return;
        }
        mozos.forEach(m => {
            const opt = document.createElement('option');
            opt.value = m.nombre;
            opt.textContent = m.nombre;
            select.appendChild(opt);
        });
    } catch (err) {
        console.error('Error cargando mozos:', err);
    }
}

async function cargarProductosBar() {
    try {
        const res = await fetch('/api/mesas/productos-bar');
        if (!res.ok) { console.error('Error GET /api/mesas/productos-bar:', res.status, await res.text()); return; }
        const productos = await res.json();
        const select = document.getElementById('selectProductoItem');
        select.innerHTML = '<option value="">-- Seleccioná un producto --</option>';
        if (!Array.isArray(productos) || productos.length === 0) {
            const opt = document.createElement('option');
            opt.disabled = true;
            opt.textContent = '(Sin productos cargados)';
            select.appendChild(opt);
            return;
        }
        productos.forEach(p => {
            const opt = document.createElement('option');
            opt.value = JSON.stringify({ codigo: p.codigo, descripcion: p.descripcion, precio: p.precio });
            opt.textContent = `${p.descripcion} — ${formatCurrency(p.precio)}`;
            select.appendChild(opt);
        });
    } catch (err) {
        console.error('Error cargando productos bar:', err);
    }
}

async function cargarFormasPago() {
    try {
        const res = await fetch('/api/mesas/formas-pago');
        if (!res.ok) { console.error('Error GET /api/mesas/formas-pago:', res.status); return; }
        const formas = await res.json();
        const select = document.getElementById('selectFormaPago');
        select.innerHTML = '<option value="">-- Seleccioná --</option>';
        if (Array.isArray(formas) && formas.length > 0) {
            formas.forEach(f => {
                const opt = document.createElement('option');
                opt.value = f.descripcion;
                opt.textContent = f.descripcion;
                select.appendChild(opt);
            });
        }
    } catch (err) {
        console.error('Error cargando formas de pago:', err);
    }
}

// ─── Carga lista de mesas ─────────────────────────────────────────────────────

async function cargarMesas() {
    try {
        const res = await fetch('/api/mesas');
        const mesas = res.ok ? await res.json() : [];
        renderMesas(Array.isArray(mesas) ? mesas : []);
    } catch (err) {
        console.error('Error cargando mesas:', err);
        renderMesas([]);
    }
}

function renderMesas(mesas) {
    const grid    = document.getElementById('gridMesas');
    const vacio   = document.getElementById('mensajeVacio');
    const panel   = document.getElementById('panelDetalle');
    const listado = document.getElementById('listadoMesas');

    panel.style.display   = 'none';
    listado.style.display = 'block';

    if (mesas.length === 0) {
        grid.innerHTML = '';
        vacio.style.display = 'block';
        return;
    }

    vacio.style.display = 'none';
    grid.innerHTML = mesas.map(m => `
        <div class="card-mesa" onclick="abrirDetalleMesa(${m.id})">
            <h2>Mesa #${m.numeroMesa}</h2>
            <p class="mozo">🧑‍🍳 ${m.mozo || '-'}</p>
            <p class="mozo">🕐 ${formatFecha(m.fechaApertura)}</p>
            <p class="total">${formatCurrency(m.total)}</p>
        </div>
    `).join('');
}

// ─── Detalle de mesa ──────────────────────────────────────────────────────────

async function abrirDetalleMesa(mesaId) {
    mesaActivaId = mesaId;

    try {
        const [mesaRes, itemsRes] = await Promise.all([
            fetch(`/api/mesas/${mesaId}`),
            fetch(`/api/mesas/${mesaId}/items`)
        ]);

        const mesa  = mesaRes.ok  ? await mesaRes.json()  : null;
        const items = itemsRes.ok ? await itemsRes.json() : [];

        if (!mesa) return;

        document.getElementById('tituloDetalle').textContent =
            `Mesa #${mesa.numeroMesa} — ${mesa.mozo || 'Sin mozo'}`;

        const listaItems = Array.isArray(items) ? items : [];
        renderItems(listaItems);
        renderItemCards(listaItems);
        actualizarTotalDetalle(mesa.total);

        document.getElementById('listadoMesas').style.display = 'none';
        document.getElementById('panelDetalle').style.display = 'block';
    } catch (err) {
        console.error('Error abriendo detalle:', err);
    }
}

function renderItems(items) {
    const body = document.getElementById('bodyItems');

    if (items.length === 0) {
        body.innerHTML = '<tr><td colspan="6" style="text-align:center;color:#999;padding:1rem">Sin consumos cargados</td></tr>';
        return;
    }

    body.innerHTML = items.map(i => `
        <tr>
            <td>${i.codigo ?? '-'}</td>
            <td>${i.descripcion ?? '-'}</td>
            <td>
                <div class="cantidad-cell">
                    <input type="number" data-cantidad-id="${i.id}" value="${i.cantidad}" min="1" />
                    <button class="btn-confirmar" data-accion="confirmar-cantidad" data-id="${i.id}" title="Confirmar cantidad">✔</button>
                </div>
            </td>
            <td style="text-align:right">${formatCurrency(i.precioUnitario)}</td>
            <td style="text-align:right"><strong>${formatCurrency(i.subtotal)}</strong></td>
            <td style="text-align:center">
                <button class="btn-del" data-accion="eliminar-item" data-id="${i.id}" title="Eliminar">🗑️</button>
            </td>
        </tr>
    `).join('');
}

function renderItemCards(items) {
    const container = document.getElementById('itemsCards');

    if (items.length === 0) {
        container.innerHTML = '<p style="text-align:center;color:#999;padding:1.5rem">Sin consumos cargados</p>';
        return;
    }

    container.innerHTML = items.map(i => `
        <div class="item-card">
            <div class="item-card-top">
                <div>
                    <div class="item-card-nombre">${i.descripcion ?? '-'}</div>
                    <div class="item-card-codigo">${i.codigo ?? ''}</div>
                </div>
                <button class="btn-del-card" data-accion="eliminar-card" data-id="${i.id}" title="Eliminar">🗑️</button>
            </div>
            <div class="item-card-bottom">
                <div class="item-card-precios">
                    <span>${formatCurrency(i.precioUnitario)} c/u</span><br>
                    <strong>Subtotal: ${formatCurrency(i.subtotal)}</strong>
                </div>
                <div class="item-card-cantidad">
                    <button class="btn-cant" data-accion="cant-menos" data-id="${i.id}">−</button>
                    <input type="number" data-card-id="${i.id}" value="${i.cantidad}" min="1" />
                    <button class="btn-cant" data-accion="cant-mas" data-id="${i.id}">+</button>
                    <button class="btn-cant-ok" data-accion="cant-ok" data-id="${i.id}" title="Confirmar">✔</button>
                </div>
            </div>
        </div>
    `).join('');
}

function actualizarTotalDetalle(total) {
    document.getElementById('totalDetalle').textContent = `Total: ${formatCurrency(total)}`;
}

function volverALista() {
    mesaActivaId = null;
    document.getElementById('panelDetalle').style.display  = 'none';
    document.getElementById('listadoMesas').style.display  = 'block';
    cargarMesas();
}

// ─── Abrir mesa ───────────────────────────────────────────────────────────────

async function onAbrirMesa(e) {
    e.preventDefault();
    const numeroMesa = parseInt(document.getElementById('inputNumeroMesa').value, 10);
    const mozo = document.getElementById('selectMozo').value.trim();

    if (!mozo) { alert('Seleccioná un mozo.'); return; }

    try {
        const res = await fetch('/api/mesas', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ numeroMesa, mozo })
        });

        if (!res.ok) throw new Error(await res.text());

        cerrarModal('modalNuevaMesa');
        document.getElementById('formNuevaMesa').reset();
        cargarMesas();
    } catch (err) {
        console.error('Error abriendo mesa:', err);
        alert('No se pudo abrir la mesa.');
    }
}

// ─── Agregar ítem ─────────────────────────────────────────────────────────────

async function onAgregarItem() {
    if (!mesaActivaId) return;

    const selectProducto = document.getElementById('selectProductoItem');
    if (!selectProducto.value) { alert('Seleccioná un producto.'); return; }

    const producto = JSON.parse(selectProducto.value);
    const cantidad = parseInt(document.getElementById('itemCantidad').value, 10);

    if (isNaN(cantidad) || cantidad < 1) { alert('Ingresá una cantidad válida.'); return; }

    try {
        const res = await fetch(`/api/mesas/${mesaActivaId}/items`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                codigo:         producto.codigo,
                descripcion:    producto.descripcion,
                precioUnitario: producto.precio,
                cantidad
            })
        });

        if (!res.ok) throw new Error(await res.text());

        selectProducto.value = '';
        document.getElementById('itemCantidad').value = '1';

        await abrirDetalleMesa(mesaActivaId);
    } catch (err) {
        console.error('Error agregando ítem:', err);
        alert('No se pudo agregar el ítem.');
    }
}

// ─── Actualizar cantidad ──────────────────────────────────────────────────────

async function actualizarCantidadItem(itemId, cantidad) {
    try {
        const res = await fetch(`/api/mesas/items/${itemId}`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ cantidad })
        });
        if (!res.ok) throw new Error(await res.text());
        await abrirDetalleMesa(mesaActivaId);
    } catch (err) {
        console.error('Error actualizando cantidad:', err);
        alert('No se pudo actualizar la cantidad.');
    }
}

// ─── Eliminar ítem ────────────────────────────────────────────────────────────

async function eliminarItem(itemId) {
    if (!confirm('¿Eliminar este consumo?')) return;

    try {
        const res = await fetch(`/api/mesas/items/${itemId}`, { method: 'DELETE' });
        if (!res.ok) throw new Error(await res.text());
        await abrirDetalleMesa(mesaActivaId);
    } catch (err) {
        console.error('Error eliminando ítem:', err);
        alert('No se pudo eliminar el ítem.');
    }
}

// ─── Cerrar mesa e imprimir ticket ────────────────────────────────────────────

async function onCerrarMesa(e) {
    e.preventDefault();
    if (!mesaActivaId) return;

    const formaPago = document.getElementById('selectFormaPago').value;

    try {
        const [mesaRes, itemsRes] = await Promise.all([
            fetch(`/api/mesas/${mesaActivaId}`),
            fetch(`/api/mesas/${mesaActivaId}/items`)
        ]);

        const mesa  = mesaRes.ok  ? await mesaRes.json()  : null;
        const items = itemsRes.ok ? await itemsRes.json() : [];

        const res = await fetch(`/api/mesas/${mesaActivaId}/cerrar`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ formaPago })
        });

        if (!res.ok) throw new Error(await res.text());

        cerrarModal('modalCerrarMesa');
        generarTicket(mesa, items, formaPago);
        window.print();
        volverALista();
    } catch (err) {
        console.error('Error cerrando mesa:', err);
        alert('No se pudo cerrar la mesa.');
    }
}

function generarTicket(mesa, items, formaPago) {
    const total = items.reduce((acc, i) => acc + (i.subtotal ?? 0), 0);
    const linea = '─'.repeat(32);

    document.getElementById('ticketPrint').innerHTML = `
        <div style="width:300px;margin:0 auto;padding:8px">
            <h2 style="text-align:center;margin-bottom:4px">🍺 BAR</h2>
            <p style="text-align:center;font-size:11px">${new Date().toLocaleString('es-AR')}</p>
            <p>${linea}</p>
            <p><strong>Mesa:</strong> #${mesa?.numeroMesa ?? '-'}</p>
            <p><strong>Mozo:</strong> ${mesa?.mozo ?? '-'}</p>
            <p>${linea}</p>
            <table style="width:100%;font-size:12px;border-collapse:collapse">
                <thead>
                    <tr>
                        <th style="text-align:left">Descripción</th>
                        <th>Cant</th>
                        <th style="text-align:right">Subtotal</th>
                    </tr>
                </thead>
                <tbody>
                    ${items.map(i => `
                        <tr>
                            <td>${i.descripcion}</td>
                            <td style="text-align:center">${i.cantidad}</td>
                            <td style="text-align:right">${formatCurrency(i.subtotal)}</td>
                        </tr>
                    `).join('')}
                </tbody>
            </table>
            <p>${linea}</p>
            <p style="text-align:right;font-size:14px"><strong>TOTAL: ${formatCurrency(total)}</strong></p>
            <p><strong>Forma de pago:</strong> ${formaPago}</p>
            <p style="text-align:center;margin-top:8px">¡Gracias por su visita!</p>
        </div>
    `;
}

// ─── Utilidades modal ─────────────────────────────────────────────────────────

function abrirModal(id) { document.getElementById(id).classList.add('activo'); }
function cerrarModal(id) { document.getElementById(id).classList.remove('activo'); }