// ================================================================
//  caja.js    Lgica del Punto de Venta
//  Funcionalidades:
//  - Verificacin y apertura de turno antes de operar
//  - Bsqueda/escaneo con cantidad previa configurable
//  - Ticket con edicin de cantidad por clic en tem
//  - Modal de cobro con deteccin de formas de pago que
//    requieren Factura Electrnica (configurado en API)
//  - Soporte conexin directa a BD o via SqlBridge (transparente)
// ================================================================

(function () {
    'use strict';

    // ?? Estado ??????????????????????????????????????????????????????
    const state = {
        token:              localStorage.getItem('auth_token')       || '',
        usuario:            localStorage.getItem('usuario_nombre')   || 'Cajero',
        cajero:             parseInt(localStorage.getItem('numero_cajero') || '1', 10),
        nroRemito:          '',
        items:              [],   // { id, codigo, descripcion, cantidad, precio, total }
        total:              0,
        formaPago:          'Efectivo',
        turnoAbierto:       false,
        // Configuracin cargada desde API
        formasPagoConFactura:    [],
        formasPagoDisponibles:   [],
        // Modal editar cantidad
        editando: null,   // { id, descripcion, cantidadActual }
    };

    const REMITO_KEY = 'caja_nro_remito';

    // ?? Referencias DOM ?????????????????????????????????????????????
    const inputBusqueda      = document.getElementById('input-busqueda');
    const inputCantidad      = document.getElementById('input-cantidad');
    const btnBuscar          = document.getElementById('btn-buscar');
    const resultadosLista    = document.getElementById('resultados-lista');
    const ticketNro          = document.getElementById('ticket-nro');
    const ticketItems        = document.getElementById('ticket-items');
    const ticketTotal        = document.getElementById('ticket-total');
    const btnCobrar          = document.getElementById('btn-cobrar');
    const btnCancelarTicket  = document.getElementById('btn-cancelar-ticket');
    const headerInfo         = document.getElementById('header-info');
    const btnLogout          = document.getElementById('btn-logout');

    const turnoAlerta        = document.getElementById('turno-alerta');
    const btnAbrirTurno      = document.getElementById('btn-abrir-turno');

    const modalEditarCant    = document.getElementById('modal-editar-cantidad');
    const editarDesc         = document.getElementById('editar-desc');
    const editarCantInput    = document.getElementById('editar-cantidad-input');
    const btnEditarCancelar  = document.getElementById('btn-editar-cancelar');
    const btnEditarConfirmar = document.getElementById('btn-editar-confirmar');

    const modalPago          = document.getElementById('modal-pago');
    const modalTotal         = document.getElementById('modal-total');
    const formasPagoEl       = document.getElementById('formas-pago');
    const efectivoFields     = document.getElementById('efectivo-fields');
    const inputRecibido      = document.getElementById('input-recibido');
    const vueltoValor        = document.getElementById('vuelto-valor');
    const btnModalCancelar   = document.getElementById('btn-modal-cancelar');
    const btnModalConfirmar  = document.getElementById('btn-modal-confirmar');

    const feAviso            = document.getElementById('fe-aviso');
    const feAvisoTexto       = document.getElementById('fe-aviso-texto');
    const feTipo             = document.getElementById('fe-tipo');
    const feTipoSelect       = document.getElementById('fe-tipo-select');
    const feCuitInput        = document.getElementById('fe-cuit-input');

    const toastEl            = document.getElementById('toast');

    const modalPrecio        = document.getElementById('modal-editar-precio');
    const precioDesc         = document.getElementById('precio-desc');
    const precioInput        = document.getElementById('precio-input');
    const btnPrecioCancelar  = document.getElementById('btn-precio-cancelar');
    const btnPrecioConfirmar = document.getElementById('btn-precio-confirmar');

    // Resolve function for the price modal promise
    let _resolverPrecio = null;

    // ?? Inicializacin ???????????????????????????????????????????????
    async function init() {
        if (!state.token) { window.location.href = '/login.html'; return; }
        headerInfo.textContent = localStorage.getItem('usuario_nombre') || state.usuario;

        // 1. Cargar configuracin de caja (formas de pago, FE)
        await cargarConfig();

        // 2. Verificar turno activo
        await verificarTurno();

        // 3. Cargar ticket activo si lo hay
        const savedRemito = sessionStorage.getItem(REMITO_KEY);
        if (savedRemito) {
            await cargarTicketExistente(savedRemito);
        } else {
            await nuevoRemito();
        }

        bindEvents();
    }

    // ?? Configuracin ????????????????????????????????????????????????
    async function cargarConfig() {
        try {
            const res  = await apiFetch('/api/caja/config');
            if (!res) return;
            const data = await res.json();
            state.formasPagoConFactura  = data.formasPagoConFactura  || ['DNI', 'Mercado Pago'];
            state.formasPagoDisponibles = ['Efectivo', 'DNI', 'Mercado Pago', 'Otro'];
            state.condicionIVA = data.condicionIVA || 'Monotributo';
            state.ctaCteHabilitado = data.ctaCteHabilitado || false;
            state.ctaCteClientes = data.ctaCteClientes || [];
            state.descuentoOpciones = data.descuentoOpciones || [];
            state.descuentoRestringirPorPago = data.descuentoRestringirPorPago || false;
            state.descuentoMetodosPagoPermitidos = data.descuentoMetodosPagoPermitidos || [];
            inicializarCtaCte();
            inicializarDescuento();
        } catch {
            state.formasPagoConFactura  = ['DNI', 'Mercado Pago'];
            state.formasPagoDisponibles = ['Efectivo', 'DNI', 'Mercado Pago', 'Otro'];
            state.ctaCteHabilitado = false;
            state.ctaCteClientes = [];
            state.descuentoOpciones = [];
            state.descuentoRestringirPorPago = false;
            state.descuentoMetodosPagoPermitidos = [];
        }
    }

    function inicializarCtaCte() {
        const section = document.getElementById('ctacte-section');
        if (!section) return;
        if (!state.ctaCteHabilitado) { section.style.display = 'none'; return; }
        section.style.display = 'block';

        const select = document.getElementById('ctacte-select');
        if (!select) return;
        select.innerHTML = '<option value="">-- Seleccionar cliente --</option>';
        state.ctaCteClientes.forEach(c => {
            select.innerHTML += `<option value="${c}">${c}</option>`;
        });

        const chk = document.getElementById('chk-ctacte');
        const clienteBox = document.getElementById('ctacte-cliente-box');
        if (!chk || !clienteBox) return;

        chk.onchange = () => {
            clienteBox.style.display = chk.checked ? 'block' : 'none';
            // Cuando es CtaCte, ocultar formas de pago y FE
            const formasPagoDiv = document.getElementById('formas-pago');
            const feAviso = document.getElementById('fe-aviso');
            const feTipo = document.getElementById('fe-tipo');
            const efectivoFields = document.getElementById('efectivo-fields');
            if (chk.checked) {
                formasPagoDiv.style.display = 'none';
                feAviso.style.display = 'none';
                feTipo.style.display = 'none';
                efectivoFields.style.display = 'none';
            } else {
                formasPagoDiv.style.display = '';
                renderFormasPago();
            }
        };

        document.getElementById('btn-ctacte-agregar').onclick = async () => {
            const input = document.getElementById('ctacte-nuevo');
            const nombre = input.value.trim();
            if (!nombre) return;
            try {
                const res = await apiFetch('/api/caja/ctacte/agregar', {
                    method: 'POST',
                    body: JSON.stringify({ nombre })
                });
                const data = await res.json();
                if (data.ok) {
                    state.ctaCteClientes = data.clientes;
                    select.innerHTML = '<option value="">-- Seleccionar cliente --</option>';
                    state.ctaCteClientes.forEach(c => {
                        select.innerHTML += `<option value="${c}">${c}</option>`;
                    });
                    select.value = nombre;
                    input.value = '';
                    toast('Cliente agregado');
                }
            } catch { toast('Error al agregar cliente', 'error'); }
        };
    }

    function inicializarDescuento() {
        const section = document.getElementById('descuento-section');
        if (!section || !state.descuentoOpciones.length) return;

        const select = document.getElementById('descuento-select');
        select.innerHTML = state.descuentoOpciones.map(o => `<option value="${o}">${o}%</option>`).join('');

        const chk = document.getElementById('chk-descuento');
        const detalle = document.getElementById('descuento-detalle');

        chk.onchange = () => {
            select.style.display = chk.checked ? 'inline-block' : 'none';
            actualizarDescuento();
        };
        select.onchange = () => actualizarDescuento();
    }

    function actualizarDescuento() {
        const section = document.getElementById('descuento-section');
        if (!section) return;

        const chk = document.getElementById('chk-descuento');
        const select = document.getElementById('descuento-select');
        const detalle = document.getElementById('descuento-detalle');

        // Show/hide section based on payment restriction
        if (state.descuentoOpciones.length === 0) {
            section.style.display = 'none';
            return;
        }

        const permitido = !state.descuentoRestringirPorPago || state.descuentoMetodosPagoPermitidos.includes(state.formaPago);
        section.style.display = permitido ? 'block' : 'none';

        // If payment changed to non-permitted, uncheck and reset
        if (!permitido && chk.checked) {
            chk.checked = false;
            select.style.display = 'none';
        }

        if (chk.checked) {
            const porc = parseFloat(select.value) || 0;
            const imp = Math.round(state.total * porc / 100 * 100) / 100;
            const totalFinal = state.total - imp;
            detalle.style.display = 'block';
            detalle.textContent = `Descuento ${porc}%: -$${imp.toLocaleString('es-AR', {minimumFractionDigits:2})} | Total final: $${totalFinal.toLocaleString('es-AR', {minimumFractionDigits:2})}`;
            modalTotal.textContent = formatMoney(totalFinal);
        } else {
            detalle.style.display = 'none';
            detalle.textContent = '';
            modalTotal.textContent = formatMoney(state.total);
        }
    }

    // ?? Turno ????????????????????????????????????????????????????????
    async function verificarTurno() {
        try {
            const res  = await apiFetch('/api/turno/activo');
            if (!res) return;
            const data = await res.json();
            state.turnoAbierto = !!data.abierto;
        } catch {
            state.turnoAbierto = false;
        }
        actualizarEstadoTurno();
    }

    function actualizarEstadoTurno() {
        turnoAlerta.style.display  = state.turnoAbierto ? 'none' : 'flex';
        btnCobrar.disabled         = !state.turnoAbierto || state.items.length === 0;
        inputBusqueda.disabled     = !state.turnoAbierto;
        btnBuscar.disabled         = !state.turnoAbierto;
    }

    async function abrirTurno() {
        // Mostrar prompt para ingresar monto inicial
        const montoStr = prompt('Ingrese el monto de efectivo en caja al abrir el turno:', '0');
        if (montoStr === null) return; // Canceló
        const monto = parseFloat(montoStr.replace(',', '.')) || 0;

        // Leer valores frescos de localStorage (pueden haber cambiado desde el login)
        const cajeroActual = parseInt(localStorage.getItem('numero_cajero') || '1', 10);
        const usuarioActual = localStorage.getItem('usuario_nombre') || 'Cajero';
        state.cajero = cajeroActual;
        state.usuario = usuarioActual;

        try {
            const res  = await apiFetch('/api/turno/abrir', { 
                method: 'POST', 
                body: JSON.stringify({ montoInicial: monto, numeroCajero: cajeroActual, usuario: usuarioActual }) 
            });
            if (!res) return;
            const data = await res.json();
            state.turnoAbierto = true;
            actualizarEstadoTurno();
            toast('Turno abierto correctamente', 'ok');
            inputBusqueda.focus();
        } catch (e) {
            toast('Error al abrir turno', 'error');
        }
    }

    // ?? API helpers ??????????????????????????????????????????????????
    function authHeaders() {
        return { 'Content-Type': 'application/json', 'Authorization': `Bearer ${state.token}` };
    }

    async function apiFetch(url, opts = {}) {
        opts.headers = { ...authHeaders(), ...(opts.headers || {}) };
        const res = await fetch(url, opts);
        if (res.status === 401) { window.location.href = '/login.html'; return null; }
        return res;
    }

    // ?? Nmero de remito ?????????????????????????????????????????????
    async function nuevoRemito() {
        try {
            const res  = await apiFetch('/api/caja/nuevo-remito');
            const data = await res.json();
            state.nroRemito = data.nroRemito;
            state.items     = [];
            state.total     = 0;
            sessionStorage.setItem(REMITO_KEY, state.nroRemito);
            renderTicket();
        } catch { toast('Error al generar remito', 'error'); }
    }

    async function cargarTicketExistente(nroRemito) {
        try {
            const res  = await apiFetch(`/api/caja/ticket/${encodeURIComponent(nroRemito)}`);
            const data = await res.json();
            state.nroRemito = data.nroRemito;
            state.items     = data.items;
            state.total     = data.total;
            renderTicket();
        } catch { await nuevoRemito(); }
    }

    // ?? Bsqueda de productos ????????????????????????????????????????
    async function buscarProductos(termino) {
        if (!termino.trim() || !state.turnoAbierto) {
            resultadosLista.innerHTML = '';
            return;
        }
        resultadosLista.innerHTML = '<div class="sin-resultados">Buscando...</div>';
        try {
            const res  = await apiFetch(`/api/productos/buscar?termino=${encodeURIComponent(termino)}`);
            const data = await res.json();

            if (!data || data.length === 0) {
                resultadosLista.innerHTML = `<div class="sin-resultados">Sin resultados para "${escHtml(termino)}"</div>`;
                return;
            }

            // Coincidencia exacta por cdigo ? agregar directo
            const exacto = data.find(p => p.codigo.toLowerCase() === termino.toLowerCase());
            if (exacto || data.length === 1) {
                await agregarProducto(exacto || data[0]);
                inputBusqueda.value = '';
                resultadosLista.innerHTML = '';
                inputBusqueda.focus();
                return;
            }

            renderResultados(data);
        } catch {
            resultadosLista.innerHTML = '<div class="sin-resultados">Error al buscar</div>';
            toast('Error en la bsqueda', 'error');
        }
    }

    function renderResultados(productos) {
        resultadosLista.innerHTML = '';
        productos.forEach(p => {
            const div = document.createElement('div');
            div.className = 'resultado-item';
            div.innerHTML = `
                <div>
                    <div class="resultado-descripcion">${escHtml(p.descripcion)}</div>
                    <div class="resultado-detalle">${escHtml(p.codigo)} &middot; Stock: ${p.stock ?? '-'} &middot; ${escHtml(p.rubro || '')}</div>
                </div>
                <div class="resultado-precio">${formatMoney(p.precio)}</div>`;
            div.addEventListener('click', async () => {
                await agregarProducto(p);
                inputBusqueda.value = '';
                resultadosLista.innerHTML = '';
                inputBusqueda.focus();
            });
            resultadosLista.appendChild(div);
        });
    }

    // ?? Agregar producto ?????????????????????????????????????????????
    async function agregarProducto(p) {
        const cantidad = Math.max(1, parseInt(inputCantidad.value, 10) || 1);
        inputCantidad.value = 1;

        let precio = p.precio;
        const esEditable = !!p.editarPrecio;
        const permiteAcumular = p.permiteAcumular !== false; // default true si no está definido

        if (esEditable) {
            const nuevoPrecio = await pedirPrecioModal(p.descripcion, p.precio);
            if (nuevoPrecio === null) return; // cancelado
            precio = nuevoPrecio;
            // Persistir el nuevo precio en la base de datos
            try {
                await apiFetch(`/api/productos/${encodeURIComponent(p.codigo)}`, {
                    method: 'PUT',
                    body: JSON.stringify({ costo: p.costo || 0, precio: precio, stock: p.stock || 0 })
                });
            } catch { /* no bloquear la venta si falla la persistencia */ }
        }

        const body = {
            nroRemito:     state.nroRemito,
            codigo:        p.codigo,
            descripcion:   p.descripcion,
            cantidad,
            precio:        precio,
            costo:         p.costo     || 0,
            rubro:         p.rubro     || '',
            marca:         p.marca     || '',
            proveedor:     p.proveedor || '',
            porcentajeIva: p.porcentajeIva || 21,
            esCtaCte:      false,
            nombreCtaCte:  '',
            editarPrecio:  esEditable,
            permiteAcumular: permiteAcumular
        };

        try {
            const res  = await apiFetch('/api/caja/agregar-item', { method: 'POST', body: JSON.stringify(body) });
            const item = await res.json();

            // Si permiteAcumular=true → acumular cantidad en el ítem existente
            // Si permiteAcumular=false → siempre nueva línea (aunque el código exista)
            if (permiteAcumular) {
                // Acumular cantidad si el código ya existe
                const idx = state.items.findIndex(i => i.codigo === item.codigo);
                if (idx >= 0) state.items[idx] = item; else state.items.unshift(item);
            } else {
                // No acumular: siempre nueva línea
                state.items.unshift(item);
            }
            recalcularTotal();
            renderTicket();
            toast(`${p.descripcion} x${cantidad}`, 'ok');
        } catch { toast('Error al agregar producto', 'error'); }
    }

    // ?? Editar cantidad de tem ??????????????????????????????????????
    function abrirEditarCantidad(item) {
        state.editando        = item;
        editarDesc.textContent = item.descripcion;
        editarCantInput.value  = item.cantidad;
        modalEditarCant.classList.add('visible');
        setTimeout(() => { editarCantInput.focus(); editarCantInput.select(); }, 150);
    }

    async function confirmarEditarCantidad() {
        const nuevaCantidad = parseInt(editarCantInput.value, 10);
        if (!nuevaCantidad || nuevaCantidad < 1) { toast('Cantidad invlida', 'error'); return; }
        const item = state.editando;
        cerrarEditarCantidad();
        try {
            const res    = await apiFetch(`/api/caja/item/${item.id}`, {
                method: 'PATCH',
                body: JSON.stringify({ cantidad: nuevaCantidad })
            });
            const updated = await res.json();
            const idx = state.items.findIndex(i => i.id === item.id);
            if (idx >= 0) state.items[idx] = updated;
            recalcularTotal();
            renderTicket();
        } catch { toast('Error al actualizar cantidad', 'error'); }
    }

    function cerrarEditarCantidad() {
        modalEditarCant.classList.remove('visible');
        state.editando = null;
    }

    // ?? Eliminar tem ????????????????????????????????????????????????
    async function eliminarItem(id) {
        try {
            const usuario = localStorage.getItem('usuario_nombre') || 'web';
            const cajero = localStorage.getItem('usuario_cajero') || '0';
            await apiFetch(`/api/caja/item/${id}?usuario=${encodeURIComponent(usuario)}&cajero=${cajero}`, { method: 'DELETE' });
            state.items = state.items.filter(i => i.id !== id);
            recalcularTotal();
            renderTicket();
        } catch { toast('Error al eliminar item', 'error'); }
    }

    // ↕ Cancelar ticket ↕↕↕↕↕↕↕↕↕↕↕↕↕↕↕↕↕↕↕↕↕↕↕↕↕↕↕↕↕↕↕↕↕↕↕↕↕↕↕↕↕↕↕↕↕
    async function cancelarTicket() {
        if (!state.items.length) { await nuevoRemito(); return; }
        if (!confirm('Cancelar el ticket actual? Se eliminaran todos los productos.')) return;
        try {
            const usuario = localStorage.getItem('usuario_nombre') || 'web';
            const cajero = localStorage.getItem('usuario_cajero') || '0';
            await apiFetch(`/api/caja/ticket/${encodeURIComponent(state.nroRemito)}?usuario=${encodeURIComponent(usuario)}&cajero=${cajero}`, { method: 'DELETE' });
            sessionStorage.removeItem(REMITO_KEY);
            await nuevoRemito();
            toast('Ticket cancelado');
        } catch { toast('Error al cancelar ticket', 'error'); }
    }

    // ?? Render ticket ????????????????????????????????????????????????
    function recalcularTotal() {
        state.total = state.items.reduce((s, i) => s + i.total, 0);
    }

    function renderTicket() {
        ticketNro.textContent   = state.nroRemito || '...';
        ticketTotal.textContent = formatMoney(state.total);
        btnCobrar.disabled      = !state.turnoAbierto || state.items.length === 0;

        if (state.items.length === 0) {
            ticketItems.innerHTML = '<div class="ticket-vacio">Sin productos en el ticket</div>';
            return;
        }

        ticketItems.innerHTML = '';
        state.items.forEach(item => {
            const row = document.createElement('div');
            row.className = 'ticket-item';
            row.title     = 'Clic para editar cantidad';
            row.innerHTML = `
                <div>
                    <div class="ticket-item-desc">${escHtml(item.descripcion)}</div>
                    <div class="ticket-item-cant">${formatMoney(item.precio)} × ${item.cantidad}
                        <span class="ticket-item-edit-hint">✏ editar</span></div>
                </div>
                <div class="ticket-item-total">${formatMoney(item.total)}</div>
                <button class="btn-quitar-item" data-id="${item.id}" title="Quitar">✕</button>`;
            // Clic en fila ? editar cantidad
            row.addEventListener('click', e => {
                if (e.target.closest('.btn-quitar-item')) return;
                abrirEditarCantidad(item);
            });
            ticketItems.appendChild(row);
        });
    }

    // ?? Modal de pago ????????????????????????????????????????????????
    function renderFormasPago() {
        formasPagoEl.innerHTML = '';
        state.formasPagoDisponibles.forEach((fp, i) => {
            const conFe = state.formasPagoConFactura.includes(fp);
            const btn   = document.createElement('button');
            btn.className   = `btn-forma-pago${conFe ? ' con-fe' : ''}${fp === 'Efectivo' ? ' activo' : ''}`;
            btn.dataset.pago = fp;
            btn.textContent  = fp + (conFe ? ' (FE)' : '');
            // Colores por forma de pago
            if (fp === 'DNI') btn.style.cssText = 'background:#e8f5e9; border-color:#66bb6a; color:#2e7d32;';
            if (fp === 'Mercado Pago') btn.style.cssText = 'background:#e3f2fd; border-color:#42a5f5; color:#1565c0;';
            formasPagoEl.appendChild(btn);
        });
        // Seleccionar Efectivo
        state.formaPago = 'Efectivo';
    }

    function iconoPago(fp) {
        return '';
    }

    function abrirModalPago() {
        renderFormasPago();
        state.formaPago        = 'Efectivo';
        modalTotal.textContent = formatMoney(state.total);
        inputRecibido.value    = '';
        vueltoValor.textContent = formatMoney(0);
        actualizarAvisoFE();
        efectivoFields.classList.add('visible');
        // Reset CtaCte
        const chkCtaCte = document.getElementById('chk-ctacte');
        if (chkCtaCte) { chkCtaCte.checked = false; chkCtaCte.onchange && chkCtaCte.onchange(); }
        // Reset FE opcional
        const chkFeOpc = document.getElementById('chk-fe-opcional');
        if (chkFeOpc) chkFeOpc.checked = false;
        // Reset descuento
        const chkDesc = document.getElementById('chk-descuento');
        if (chkDesc) { chkDesc.checked = false; document.getElementById('descuento-select').style.display = 'none'; }
        actualizarDescuento();
        document.getElementById('formas-pago').style.display = '';
        modalPago.classList.add('visible');
        if (state.formaPago === 'Efectivo') setTimeout(() => inputRecibido.focus(), 200);
    }

    function cerrarModalPago() { modalPago.classList.remove('visible'); }

    function actualizarAvisoFE() {
        const requiereFE = state.formasPagoConFactura.includes(state.formaPago);
        const chkFEOpcional = document.getElementById('chk-fe-opcional');
        const feOpcionalChecked = chkFEOpcional?.checked || false;
        const generaFE = requiereFE || feOpcionalChecked;

        feAviso.style.display = generaFE ? 'block' : 'none';

        // Mostrar/ocultar checkbox de FE opcional (solo si NO es obligatorio)
        const feOpcionalSection = document.getElementById('fe-opcional-section');
        if (feOpcionalSection) {
            feOpcionalSection.style.display = requiereFE ? 'none' : 'block';
        }

        if (generaFE && state.condicionIVA === 'Monotributo') {
            feTipo.style.display = 'none';
            feTipoSelect.value = 'FacturaC';
            feCuitInput.style.display = 'none';
            feAvisoTexto.textContent = `Generando Factura C (Monotributo)`;
        } else if (generaFE) {
            // Responsable Inscripto: solo Factura A y B
            feTipo.style.display = 'flex';
            feTipoSelect.innerHTML = '<option value="FacturaB">Factura B (consumidor final)</option><option value="FacturaA">Factura A (requiere CUIT)</option>';
            feTipoSelect.value = 'FacturaB';
            feCuitInput.style.display = 'none';
            feAvisoTexto.textContent = `Seleccione tipo de factura`;
        } else {
            feTipo.style.display = 'none';
            feCuitInput.style.display = 'none';
        }
    }

    function actualizarVuelto() {
        const recibido = parseFloat(inputRecibido.value) || 0;
        const vuelto   = recibido - state.total;
        vueltoValor.textContent  = formatMoney(Math.max(0, vuelto));
        vueltoValor.style.color  = vuelto >= 0 ? 'var(--color-success)' : 'var(--color-danger)';
    }

    async function confirmarPago() {
        btnModalConfirmar.disabled    = true;
        btnModalConfirmar.textContent = 'Procesando...';

        const requiereFE   = state.formasPagoConFactura.includes(state.formaPago) || (document.getElementById('chk-fe-opcional')?.checked || false);
        const esCtaCte = document.getElementById('chk-ctacte')?.checked || false;
        const nombreCtaCte = esCtaCte ? (document.getElementById('ctacte-select').value || '') : '';

        if (esCtaCte && !nombreCtaCte) {
            toast('Seleccione un cliente de Cta. Cte.', 'error');
            btnModalConfirmar.disabled    = false;
            btnModalConfirmar.textContent = 'Confirmar cobro';
            return;
        }

        const tipoFactura  = esCtaCte ? 'Remito'
            : (requiereFE 
                ? (state.condicionIVA === 'Monotributo' ? 'FacturaC' : (feTipoSelect.value || 'FacturaB')) 
                : 'Remito');
        const cuitCliente  = tipoFactura === 'FacturaA' ? feCuitInput.value.replace(/-/g, '').trim() : '';

        if (tipoFactura === 'FacturaA' && !cuitCliente) {
            toast('Ingrese el CUIT del cliente para Factura A', 'error');
            btnModalConfirmar.disabled    = false;
            btnModalConfirmar.textContent = 'Confirmar cobro';
            feCuitInput.focus();
            return;
        }

        if (tipoFactura === 'FacturaA' && !validarCUIT(cuitCliente)) {
            toast('El CUIT ingresado no es valido. Verifique los numeros.', 'error');
            btnModalConfirmar.disabled    = false;
            btnModalConfirmar.textContent = 'Confirmar cobro';
            feCuitInput.focus();
            return;
        }

        const chkDesc = document.getElementById('chk-descuento');
        const descSelect = document.getElementById('descuento-select');
        const porcentajeDescuento = (chkDesc && chkDesc.checked) ? (parseFloat(descSelect.value) || 0) : 0;
        const importeDescuento = Math.round(state.total * porcentajeDescuento / 100 * 100) / 100;

        const body = {
            nroRemito:    state.nroRemito,
            formaPago:    esCtaCte ? 'Cuenta Corriente' : state.formaPago,
            tipoFactura,
            cuitCliente,
            numeroCajero: state.cajero,
            usuarioVenta: state.usuario,
            esCtaCte:     esCtaCte,
            nombreCtaCte: nombreCtaCte,
            porcentajeDescuento: porcentajeDescuento,
            importeDescuento: importeDescuento,
            pagos: [{ medioPago: esCtaCte ? 'Cuenta Corriente' : state.formaPago, importe: state.total - importeDescuento, observaciones: '' }]
        };

        // Guardar items antes de confirmar (para impresion)
        const itemsParaImprimir = [...state.items];
        const totalParaImprimir = state.total;
        const remitoParaImprimir = state.nroRemito;
        const descuentoParaImprimir = { porcentaje: porcentajeDescuento, importe: importeDescuento };

        try {
            const res  = await apiFetch('/api/caja/confirmar', { method: 'POST', body: JSON.stringify(body) });
            const data = await res.json();

            if (data.ok) {
                cerrarModalPago();
                sessionStorage.removeItem(REMITO_KEY);
                let aviso = data.mensaje || 'Venta registrada';
                let tipo = 'ok';
                if (requiereFE && !data.cae) {
                    tipo = 'error';
                }
                toast(aviso, tipo);

                // Imprimir factura o remito (si el checkbox esta marcado)
                const debeImprimir = document.getElementById('chk-imprimir').checked;
                if (debeImprimir) {
                    if (data.cae && data.nroFactura) {
                        imprimirFactura({
                            nroFactura: data.nroFactura,
                            cae: data.cae,
                            tipoFactura,
                            items: itemsParaImprimir,
                            total: totalParaImprimir,
                            nroRemito: remitoParaImprimir,
                            formaPago: state.formaPago,
                            usuario: state.usuario,
                            cuitCliente: cuitCliente,
                            descuento: descuentoParaImprimir
                        });
                    } else if (!requiereFE) {
                        imprimirRemito({
                            nroRemito: remitoParaImprimir,
                            items: itemsParaImprimir,
                            total: totalParaImprimir,
                            formaPago: state.formaPago,
                            usuario: state.usuario,
                            descuento: descuentoParaImprimir
                        });
                    }
                }

                resultadosLista.innerHTML = '<div class="sin-resultados">Escanee o busque un producto para comenzar</div>';
                feCuitInput.value = '';
                await nuevoRemito();
                inputBusqueda.focus();
            } else {
                toast(data.mensaje || 'Error al confirmar', 'error');
            }
        } catch { toast('Error al confirmar la venta', 'error'); }
        finally {
            btnModalConfirmar.disabled    = false;
            btnModalConfirmar.textContent = 'Confirmar cobro';
        }
    }

    // ?? Eventos
    function bindEvents() {
        // Turno
        btnAbrirTurno.addEventListener('click', abrirTurno);

        // Bsqueda
        inputBusqueda.addEventListener('keydown', e => {
            if (e.key === 'Enter') { e.preventDefault(); buscarProductos(inputBusqueda.value.trim()); }
        });
        inputBusqueda.addEventListener('input', () => {
            if (!inputBusqueda.value.trim()) {
                resultadosLista.innerHTML = '';
            }
        });
        btnBuscar.addEventListener('click', () => buscarProductos(inputBusqueda.value.trim()));

        // Cantidad: Enter en cantidad enfoca bsqueda
        inputCantidad.addEventListener('keydown', e => {
            if (e.key === 'Enter') { e.preventDefault(); inputBusqueda.focus(); inputBusqueda.select(); }
        });

        // Quitar tem del ticket
        ticketItems.addEventListener('click', e => {
            const btn = e.target.closest('.btn-quitar-item');
            if (!btn) return;
            e.stopPropagation();
            eliminarItem(parseInt(btn.dataset.id, 10));
        });

        // Cancelar ticket
        btnCancelarTicket.addEventListener('click', cancelarTicket);

        // Abrir modal de pago
        btnCobrar.addEventListener('click', abrirModalPago);

        // Formas de pago
        formasPagoEl.addEventListener('click', e => {
            const btn = e.target.closest('.btn-forma-pago');
            if (!btn) return;
            formasPagoEl.querySelectorAll('.btn-forma-pago').forEach(b => b.classList.remove('activo'));
            btn.classList.add('activo');
            state.formaPago = btn.dataset.pago;
            efectivoFields.classList.toggle('visible', state.formaPago === 'Efectivo');
            actualizarAvisoFE();
            actualizarDescuento();
            if (state.formaPago === 'Efectivo') inputRecibido.focus();
        });

        // Factura A ? mostrar campo CUIT
        feTipoSelect.addEventListener('change', () => {
            feCuitInput.style.display = feTipoSelect.value === 'FacturaA' ? 'block' : 'none';
            if (feTipoSelect.value === 'FacturaA') feCuitInput.focus();
        });

        // Auto-formateo CUIT
        feCuitInput.addEventListener('input', () => formatearCUIT(feCuitInput));

        // FE opcional checkbox
        const chkFeOpcional = document.getElementById('chk-fe-opcional');
        if (chkFeOpcional) chkFeOpcional.addEventListener('change', actualizarAvisoFE);

        // Vuelto
        inputRecibido.addEventListener('input', actualizarVuelto);

        // Modal editar cantidad
        btnEditarCancelar.addEventListener('click', cerrarEditarCantidad);
        btnEditarConfirmar.addEventListener('click', confirmarEditarCantidad);
        editarCantInput.addEventListener('keydown', e => {
            if (e.key === 'Enter') confirmarEditarCantidad();
            if (e.key === 'Escape') cerrarEditarCantidad();
        });
        modalEditarCant.addEventListener('click', e => { if (e.target === modalEditarCant) cerrarEditarCantidad(); });

        // Modal pago
        btnModalCancelar.addEventListener('click', cerrarModalPago);
        btnModalConfirmar.addEventListener('click', confirmarPago);
        modalPago.addEventListener('click', e => { if (e.target === modalPago) cerrarModalPago(); });

        // Logout
        btnLogout.addEventListener('click', () => {
            sessionStorage.removeItem(REMITO_KEY);
            localStorage.removeItem('auth_token');
            window.location.href = '/login.html';
        });

        // Modal editar precio
        btnPrecioCancelar.addEventListener('click', cancelarPrecioModal);
        btnPrecioConfirmar.addEventListener('click', confirmarPrecioModal);
        precioInput.addEventListener('keydown', e => {
            if (e.key === 'Enter') confirmarPrecioModal();
            if (e.key === 'Escape') cancelarPrecioModal();
        });
        modalPrecio.addEventListener('click', e => { if (e.target === modalPrecio) cancelarPrecioModal(); });

        // Atajos globales
        document.addEventListener('keydown', e => {
            if (e.key === 'F2')  { e.preventDefault(); inputBusqueda.focus(); inputBusqueda.select(); }
            if (e.key === 'Escape') { cerrarModalPago(); cerrarEditarCantidad(); }
            if (e.key === 'F10' && !btnCobrar.disabled) { e.preventDefault(); abrirModalPago(); }
        });
    }

    // ?? Modal precio editable ??????????????????????????????????????????
    function pedirPrecioModal(descripcion, precioActual) {
        return new Promise(resolve => {
            _resolverPrecio = resolve;
            precioDesc.textContent = descripcion;
            precioInput.value = precioActual || '';
            modalPrecio.classList.add('visible');
            setTimeout(() => { precioInput.focus(); precioInput.select(); }, 150);
        });
    }

    function confirmarPrecioModal() {
        const val = precioInput.value.replace(/\./g, '').replace(',', '.');
        const parsed = parseFloat(val) || parseFloat(precioInput.value);
        if (isNaN(parsed) || parsed <= 0) { toast('Precio invalido', 'error'); return; }
        modalPrecio.classList.remove('visible');
        if (_resolverPrecio) { _resolverPrecio(parsed); _resolverPrecio = null; }
    }

    function cancelarPrecioModal() {
        modalPrecio.classList.remove('visible');
        if (_resolverPrecio) { _resolverPrecio(null); _resolverPrecio = null; }
    }

    // ?? Utilidades ???????????????????????????????????????????????????
    function formatMoney(val) {
        return new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS' }).format(val || 0);
    }

    function validarCUIT(cuit) {
        const digits = cuit.replace(/\D/g, '');
        if (digits.length !== 11) return false;
        const mult = [5, 4, 3, 2, 7, 6, 5, 4, 3, 2];
        let suma = 0;
        for (let i = 0; i < 10; i++) suma += parseInt(digits[i]) * mult[i];
        const resto = suma % 11;
        const verificador = resto === 0 ? 0 : resto === 1 ? 9 : 11 - resto;
        return parseInt(digits[10]) === verificador;
    }

    function formatearCUIT(input) {
        let val = input.value.replace(/\D/g, '');
        if (val.length > 11) val = val.substring(0, 11);
        if (val.length > 2 && val.length <= 10) {
            val = val.substring(0, 2) + '-' + val.substring(2);
        } else if (val.length > 10) {
            val = val.substring(0, 2) + '-' + val.substring(2, 10) + '-' + val.substring(10);
        }
        input.value = val;
    }

    function escHtml(str) {
        return String(str || '')
            .replace(/&/g, '&amp;').replace(/</g, '&lt;')
            .replace(/>/g, '&gt;').replace(/"/g, '&quot;');
    }

    let toastTimer;
    function toast(msg, tipo = '') {
        toastEl.textContent = msg;
        toastEl.className   = `toast ${tipo} show`;
        clearTimeout(toastTimer);
        toastTimer = setTimeout(() => toastEl.classList.remove('show'), 2800);
    }

    // ?? Imprimir remito ??????????????????????????????????????????????
    async function imprimirRemito(datos) {
        try {
            const res = await apiFetch('/api/caja/datos-facturacion');
            const comercio = res ? await res.json() : {};

            const ahora = new Date();
            const fecha = ahora.toLocaleDateString('es-AR');
            const hora = ahora.toLocaleTimeString('es-AR', { hour: '2-digit', minute: '2-digit' });

            let itemsHtml = '';
            let cantTotal = 0;
            datos.items.forEach(item => {
                cantTotal += item.cantidad;
                itemsHtml += `<tr>
                    <td class="c">${item.cantidad}</td>
                    <td>${item.descripcion}</td>
                    <td class="r">$ ${item.precio.toLocaleString('es-AR', {minimumFractionDigits:2})}</td>
                    <td class="r">$ ${item.total.toLocaleString('es-AR', {minimumFractionDigits:2})}</td>
                </tr>`;
            });

            const html = `<!DOCTYPE html>
<html><head><meta charset="UTF-8"><title>Remito ${datos.nroRemito}</title>
<style>
    * { margin: 0; padding: 0; box-sizing: border-box; }
    body { font-family: Arial, sans-serif; width: 80mm; margin: 0 auto; padding: 4mm; font-size: 9pt; }
    .center { text-align: center; }
    .r { text-align: right; }
    .c { text-align: center; }
    h1 { font-size: 16pt; margin: 4mm 0 1mm; }
    .domicilio { font-size: 8pt; color: #333; }
    .tipo-factura { font-size: 11pt; font-weight: bold; margin: 4mm 0; border-top: 1px solid #000; border-bottom: 1px solid #000; padding: 2mm 0; }
    table { width: 100%; border-collapse: collapse; margin: 2mm 0; }
    th { font-size: 7pt; font-weight: bold; border-bottom: 1px solid #000; padding: 1mm 0; text-align: left; }
    td { font-size: 8pt; padding: 1mm 0; }
    .totales { border-top: 2px solid #000; margin-top: 3mm; padding-top: 2mm; }
    .total-line { display: flex; justify-content: space-between; font-size: 9pt; }
    .total-final { font-size: 12pt; font-weight: bold; }
    .info { font-size: 7pt; margin-top: 3mm; border-top: 1px dashed #000; padding-top: 2mm; }
    .gracias { text-align: center; margin-top: 4mm; font-size: 9pt; }
    @media print { body { width: 80mm; } }
</style></head><body>
    <div class="center">
        <p style="font-size:8pt;">Fecha: ${fecha}<br>Hora: ${hora}</p>
        <h1>${comercio.nombreComercio || 'Comercio'}</h1>
        <p class="domicilio">${comercio.domicilioComercio || ''}</p>
    </div>
    <div class="tipo-factura center">REMITO ${datos.nroRemito}</div>
    <table>
        <thead><tr><th class="c">C</th><th>PRODUCTO</th><th class="r">PRECIO</th><th class="r">TOTAL</th></tr></thead>
        <tbody>${itemsHtml}</tbody>
    </table>
    <div class="totales">
        <div class="total-line"><span>PRODUCTOS: ${cantTotal}</span><span>SUBTOTAL: $ ${datos.total.toLocaleString('es-AR', {minimumFractionDigits:2})}</span></div>
        ${datos.descuento && datos.descuento.porcentaje > 0 ? `<div class="total-line" style="color:#c00"><span>DESCUENTO (${datos.descuento.porcentaje}%):</span><span>-$ ${datos.descuento.importe.toLocaleString('es-AR', {minimumFractionDigits:2})}</span></div>` : ''}
        <div class="total-line total-final"><span></span><span>TOTAL: $ ${(datos.total - (datos.descuento ? datos.descuento.importe : 0)).toLocaleString('es-AR', {minimumFractionDigits:2})}</span></div>
    </div>
    <div class="info">
        Forma de pago: ${datos.formaPago}<br>
        Vendedor: ${datos.usuario}
    </div>
    <p class="gracias">Gracias por su compra!</p>
</body></html>`;

            const ventana = window.open('', '_blank', 'width=350,height=600');
            ventana.document.write(html);
            ventana.document.close();
            setTimeout(() => { ventana.print(); }, 500);
        } catch (err) {
            console.error('Error al imprimir remito:', err);
        }
    }

    // ?? Imprimir factura ??????????????????????????????????????????????
    async function imprimirFactura(datos) {
        try {
            // Obtener datos de facturacion del comercio
            const res = await apiFetch('/api/caja/datos-facturacion');
            const comercio = res ? await res.json() : {};

            const ahora = new Date();
            const fecha = ahora.toLocaleDateString('es-AR');
            const hora = ahora.toLocaleTimeString('es-AR', { hour: '2-digit', minute: '2-digit' });

            const tipoLetra = datos.tipoFactura.replace('Factura', '');
            const esMonotributo = (comercio.condicion || '').toLowerCase().includes('monotributo');
            const esFacturaA = tipoLetra === 'A';

            let itemsHtml = '';
            let cantTotal = 0;
            let ivaTotal = 0;
            let baseImponible = 0;

            datos.items.forEach(item => {
                cantTotal += item.cantidad;
                const porcentajeIva = item.porcentajeIva || 21;
                const neto = item.total / (1 + porcentajeIva / 100);
                const ivaItem = item.total - neto;

                if (esFacturaA) {
                    ivaTotal += ivaItem;
                    baseImponible += neto;
                    itemsHtml += `<tr>
                        <td class="c">${item.cantidad}</td>
                        <td>${item.descripcion} <small style="color:#666">(${porcentajeIva}%)</small></td>
                        <td class="r">$ ${item.precio.toLocaleString('es-AR', {minimumFractionDigits:2})}</td>
                        <td class="r">$ ${item.total.toLocaleString('es-AR', {minimumFractionDigits:2})}</td>
                    </tr>`;
                } else {
                    itemsHtml += `<tr>
                        <td class="c">${item.cantidad}</td>
                        <td>${item.descripcion}</td>
                        <td class="r">$ ${item.precio.toLocaleString('es-AR', {minimumFractionDigits:2})}</td>
                        <td class="r">$ ${item.total.toLocaleString('es-AR', {minimumFractionDigits:2})}</td>
                    </tr>`;
                }
            });

            // Seccion IVA para Factura A
            let ivaSection = '';
            if (esFacturaA) {
                ivaSection = `
                    <div style="border-top:1px dashed #000; margin-top:3mm; padding-top:2mm; font-size:7pt;">
                        <strong>=== RESUMEN IVA ===</strong><br>
                        Base Imponible: $ ${baseImponible.toLocaleString('es-AR', {minimumFractionDigits:2})}<br>
                        IVA 21%: $ ${ivaTotal.toLocaleString('es-AR', {minimumFractionDigits:2})}<br>
                        <strong>Total: $ ${datos.total.toLocaleString('es-AR', {minimumFractionDigits:2})}</strong>
                    </div>
                    ${datos.cuitCliente ? `<div style="font-size:7pt; margin-top:2mm;">CUIT Cliente: ${datos.cuitCliente}</div>` : ''}`;
            }

            let ivaNote = '';
            if (esMonotributo && !esFacturaA) {
                ivaNote = '<p class="iva-info center">IVA incluido - Monotributo - No discriminado</p>';
            }

            const html = `<!DOCTYPE html>
<html><head><meta charset="UTF-8"><title>Factura ${datos.nroFactura}</title>
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
        <p style="font-size:8pt;">Fecha: ${fecha}<br>Hora: ${hora}</p>
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
    <div class="tipo-factura center">FACTURA ${tipoLetra} N&#176; ${datos.nroFactura.split(' ').pop()}</div>
    <table>
        <thead><tr><th class="c">C</th><th>PRODUCTO</th><th class="r">PRECIO</th><th class="r">TOTAL</th></tr></thead>
        <tbody>${itemsHtml}</tbody>
    </table>
    <div class="totales">
        <div class="total-line"><span>PRODUCTOS: ${cantTotal}</span><span>SUBTOTAL: $ ${datos.total.toLocaleString('es-AR', {minimumFractionDigits:2})}</span></div>
        ${datos.descuento && datos.descuento.porcentaje > 0 ? `<div class="total-line" style="color:#c00"><span>DESCUENTO (${datos.descuento.porcentaje}%):</span><span>-$ ${datos.descuento.importe.toLocaleString('es-AR', {minimumFractionDigits:2})}</span></div>` : ''}
        <div class="total-line total-final"><span></span><span>TOTAL: $ ${(datos.total - (datos.descuento ? datos.descuento.importe : 0)).toLocaleString('es-AR', {minimumFractionDigits:2})}</span></div>
    </div>
    ${ivaNote}
    ${ivaSection}
    <div class="cae-info">
        CAE: ${datos.cae}<br>
        Comprobante: ${datos.nroFactura}
    </div>
    <p class="gracias">Gracias por su compra!</p>
</body></html>`;

            const ventana = window.open('', '_blank', 'width=350,height=600');
            ventana.document.write(html);
            ventana.document.close();
            setTimeout(() => { ventana.print(); }, 500);
        } catch (err) {
            console.error('Error al imprimir factura:', err);
        }
    }

    // ?? Arrancar ?????????????????????????????????????????????????????
    init();

})();

