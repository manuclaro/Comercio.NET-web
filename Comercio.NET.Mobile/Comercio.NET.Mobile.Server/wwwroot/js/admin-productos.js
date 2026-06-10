const API = '/api';
let token = null;
let paginaActual = 1;
const TAMANO_PAGINA = 50;
let codigoEditando = null; // null = nuevo producto
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
        localStorage.getItem('usuario_completo') || localStorage.getItem('usuario_nombre') || 'Usuario';

    document.getElementById('btnCerrarSesion').addEventListener('click', () => { localStorage.clear(); window.location.href = '/login.html'; });

    document.getElementById('btnBuscar').addEventListener('click', () => { paginaActual = 1; cargar(); });
    document.getElementById('txtBuscar').addEventListener('keydown', e => { if (e.key === 'Enter') { paginaActual = 1; cargar(); } });
    document.getElementById('txtBuscar').addEventListener('input', () => {
        clearTimeout(timerBuscar);
        timerBuscar = setTimeout(() => { paginaActual = 1; cargar(); }, 500);
    });
    document.getElementById('btnAnterior').addEventListener('click', () => { if (paginaActual > 1) { paginaActual--; cargar(); } });
    document.getElementById('btnSiguiente').addEventListener('click', () => { paginaActual++; cargar(); });
    document.getElementById('btnNuevo').addEventListener('click', () => abrirModal(null));
    document.getElementById('btnCerrarModal').addEventListener('click', cerrarModal);
    document.getElementById('btnCancelarModal').addEventListener('click', cerrarModal);
    document.getElementById('btnGuardar').addEventListener('click', guardar);

    // Calcular precio sugerido cuando cambia costo o porcentaje
    document.getElementById('fCosto').addEventListener('input', calcularPrecioSugerido);
    document.getElementById('fPorcentaje').addEventListener('input', calcularPrecioSugerido);

    // Regla: EditarPrecio=SI => PermiteAcumular forzado a NO y bloqueado
    document.getElementById('fEditarPrecio').addEventListener('change', function() {
        const sel = document.getElementById('fPermiteAcumular');
        if (this.value === 'true') {
            sel.value    = 'false';
            sel.disabled = true;
        } else {
            sel.disabled = false;
        }
    });

    // Validar código único al perder foco (solo en modo nuevo)
    document.getElementById('fCodigo').addEventListener('blur', async function() {
        if (codigoEditando) return; // solo validar en nuevo
        const codigo = this.value.trim();
        if (!codigo) return;
        limpiarMsgModal();
        try {
            const r = await fetch(`${API}/productos/verificar/${encodeURIComponent(codigo)}`, { headers: { Authorization: `Bearer ${token}` } });
            const d = await r.json();
            if (d.existe) {
                mostrarMsgModal('error', `El codigo ${codigo} ya existe. Elija otro codigo.`);
                this.focus();
            }
        } catch (e) {
            mostrarMsgModal('error', `Error verificando codigo: ${e.message}`);
        }
    });

    // Cargar rubros y marcas para datalist
    cargarRubrosMarcas();
    cargar();
});

async function cargar() {
    const buscar = document.getElementById('txtBuscar').value.trim();
    mostrarLoading(true);
    try {
        const url = `${API}/productos/admin?buscar=${encodeURIComponent(buscar)}&pagina=${paginaActual}&tamano=${TAMANO_PAGINA}`;
        const r = await fetch(url, { headers: { Authorization: `Bearer ${token}` } });
        if (!r.ok) throw new Error(await r.text());
        const data = await r.json();
        const productos = data.productos || [];
        const total     = data.total || 0;
        const totalPaginas = Math.max(1, Math.ceil(total / TAMANO_PAGINA));
        renderTabla(productos);
        document.getElementById('contadorResultados').textContent =
            `Mostrando ${productos.length} de ${total} producto(s)`;
        document.getElementById('paginaInfo').textContent =
            `Pagina ${paginaActual} de ${totalPaginas}`;
        document.getElementById('btnAnterior').disabled = paginaActual === 1;
        document.getElementById('btnSiguiente').disabled = paginaActual >= totalPaginas;
    } catch (e) {
        mostrarMsgGlobal('error', `Error cargando productos: ${e.message}`);
    } finally {
        mostrarLoading(false);
    }
}

function renderTabla(productos) {
    const tbody = document.getElementById('tbodyProductos');
    tbody.innerHTML = '';
    if (!productos.length) {
        tbody.innerHTML = '<tr><td colspan="9" style="text-align:center;padding:2rem;color:#777;">No se encontraron productos.</td></tr>';
        return;
    }
    for (const p of productos) {
        const tr = document.createElement('tr');
        tr.innerHTML = `
            <td><code>${esc(p.codigo)}</code></td>
            <td>${esc(p.descripcion)}</td>
            <td>${esc(p.marca)}</td>
            <td class="text-right">${fmt(p.costo)}</td>
            <td class="text-center">${p.porcentaje ?? 0}%</td>
            <td class="text-right"><strong>${fmt(p.precio)}</strong></td>
            <td class="text-center">${p.stock}</td>
            <td class="text-center">${p.activo ? '<span class="badge-activo">Activo</span>' : '<span class="badge-inactivo">Inactivo</span>'}</td>
            <td class="text-center">
                <button class="btn-edit" title="Editar" onclick="abrirModal('${esc(p.codigo)}')">&#9998;</button>
            </td>`;
        tbody.appendChild(tr);
    }
}

async function abrirModal(codigo) {
    codigoEditando = codigo;
    limpiarMsgModal();
    document.getElementById('grupoActivo').style.display = codigo ? '' : 'none';
    document.getElementById('fCodigo').readOnly = !!codigo;

    if (codigo) {
        document.getElementById('modalTitulo').textContent = 'Editar Producto';
        mostrarLoading(true);
        try {
            const r = await fetch(`${API}/productos/${encodeURIComponent(codigo)}`, { headers: { Authorization: `Bearer ${token}` } });
            if (!r.ok) throw new Error('Producto no encontrado');
            const p = await r.json();
            document.getElementById('fCodigo').value        = p.codigo;
            document.getElementById('fDescripcion').value   = p.descripcion;
            document.getElementById('fRubro').value         = p.rubro;
            document.getElementById('fMarca').value         = p.marca;
            document.getElementById('fProveedor').value     = p.proveedor ?? '';
            document.getElementById('fCosto').value         = p.costo;
            document.getElementById('fPrecio').value        = p.precio;
            document.getElementById('fStock').value         = p.stock;
            document.getElementById('fPorcentaje').value    = p.porcentaje ?? 0;
            document.getElementById('fIva').value           = String(p.iva ?? 21);
            const editarPrecioVal = String(p.editarPrecio ?? false);
            document.getElementById('fEditarPrecio').value  = editarPrecioVal;
            document.getElementById('fPermiteAcumular').value = String(p.permiteAcumular ?? true);
            document.getElementById('fPermiteAcumular').disabled = editarPrecioVal === 'true';
            document.getElementById('fActivo').value        = String(p.activo ?? true);
        } catch (e) {
            mostrarMsgModal('error', e.message);
        } finally {
            mostrarLoading(false);
        }
    } else {
        document.getElementById('modalTitulo').textContent = 'Nuevo Producto';
        ['fCodigo','fDescripcion','fRubro','fMarca','fProveedor'].forEach(id => document.getElementById(id).value = '');
        document.getElementById('fCosto').value      = '0';
        document.getElementById('fPrecio').value     = '0';
        document.getElementById('fStock').value      = '0';
        document.getElementById('fPorcentaje').value = '0';
        document.getElementById('fIva').value        = '21';
        document.getElementById('fEditarPrecio').value = 'false';
        document.getElementById('fPermiteAcumular').value = 'true';
        document.getElementById('fPermiteAcumular').disabled = false;
    }

    document.getElementById('modalProducto').style.display = 'flex';
    document.getElementById('fCodigo').focus();
}

function calcularPrecioSugerido() {
    const costo = parseFloat(document.getElementById('fCosto').value) || 0;
    const pct   = parseFloat(document.getElementById('fPorcentaje').value) || 0;
    if (costo > 0) {
        const sugerido = costo * (1 + pct / 100);
        document.getElementById('fPrecio').value = sugerido.toFixed(2);
    }
}

function cerrarModal() {
    document.getElementById('modalProducto').style.display = 'none';
    codigoEditando = null;
}

async function guardar() {
    limpiarMsgModal();
    const codigo      = document.getElementById('fCodigo').value.trim();
    const descripcion = document.getElementById('fDescripcion').value.trim();
    if (!codigo)      { mostrarMsgModal('error', 'El código es requerido.'); return; }
    if (!descripcion) { mostrarMsgModal('error', 'La descripción es requerida.'); return; }

    const btn = document.getElementById('btnGuardar');
    btn.disabled = true;
    btn.textContent = 'Guardando...';

    try {
        let url, method, body;
        if (codigoEditando) {
            // Editar completo
            url    = `${API}/productos/${encodeURIComponent(codigoEditando)}/completo`;
            method = 'PUT';
            body   = {
                descripcion:  descripcion,
                rubro:        document.getElementById('fRubro').value.trim(),
                marca:        document.getElementById('fMarca').value.trim(),
                proveedor:    document.getElementById('fProveedor').value.trim(),
                costo:        parseFloat(document.getElementById('fCosto').value) || 0,
                precio:       parseFloat(document.getElementById('fPrecio').value) || 0,
                stock:        parseInt(document.getElementById('fStock').value) || 0,
                porcentaje:       parseInt(document.getElementById('fPorcentaje').value) || 0,
                iva:          parseFloat(document.getElementById('fIva').value) || 21,
                editarPrecio: document.getElementById('fEditarPrecio').value === 'true',
                permiteAcumular: document.getElementById('fPermiteAcumular').value === 'true',
                activo:       document.getElementById('fActivo').value === 'true'
            };
        } else {
            url    = `${API}/productos`;
            method = 'POST';
            body   = {
                codigo,
                descripcion,
                rubro:        document.getElementById('fRubro').value.trim(),
                marca:        document.getElementById('fMarca').value.trim(),
                proveedor:    document.getElementById('fProveedor').value.trim(),
                costo:        parseFloat(document.getElementById('fCosto').value) || 0,
                precio:       parseFloat(document.getElementById('fPrecio').value) || 0,
                stock:        parseInt(document.getElementById('fStock').value) || 0,
                porcentaje:   parseInt(document.getElementById('fPorcentaje').value) || 0,
                iva:          parseFloat(document.getElementById('fIva').value) || 21,
                editarPrecio: document.getElementById('fEditarPrecio').value === 'true',
                permiteAcumular: document.getElementById('fPermiteAcumular').value === 'true'
            };
        }

        const r = await fetch(url, {
            method,
            headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
            body: JSON.stringify(body)
        });
        const d = await r.json();
        if (!r.ok) throw new Error(d.error || 'Error al guardar');

        mostrarMsgModal('ok', d.mensaje || 'Guardado correctamente.');
        if (!codigoEditando && d.codigo) {
            // Nuevo producto: buscar y posicionar
            setTimeout(() => {
                cerrarModal();
                buscarYPosicionar(d.codigo);
            }, 900);
        } else {
            // Edición: simplemente recargar
            setTimeout(() => { cerrarModal(); cargar(); }, 900);
        }
    } catch (e) {
        mostrarMsgModal('error', e.message);
    } finally {
        btn.disabled = false;
        btn.textContent = 'Guardar';
    }
}

async function cargarRubrosMarcas() {
    try {
        const [rR, rM] = await Promise.all([
            fetch(`${API}/productos/rubros`, { headers: { Authorization: `Bearer ${token}` } }),
            fetch(`${API}/productos/marcas`, { headers: { Authorization: `Bearer ${token}` } })
        ]);
        const rubros = await rR.json();
        const marcas = await rM.json();
        const dlR = document.getElementById('listaRubros');
        const dlM = document.getElementById('listaMarcas');
        rubros.forEach(r => { const o = document.createElement('option'); o.value = r; dlR.appendChild(o); });
        marcas.forEach(m => { const o = document.createElement('option'); o.value = m; dlM.appendChild(o); });
    } catch { /* no crítico */ }
}

function mostrarLoading(v) { document.getElementById('loading').style.display = v ? '' : 'none'; }
function mostrarMsgGlobal(tipo, msg) {
    const el = document.getElementById('msgGlobal');
    el.className = tipo === 'ok' ? 'msg-ok' : 'msg-err';
    el.textContent = msg;
    el.style.display = '';
    setTimeout(() => { el.style.display = 'none'; }, 4000);
}
function mostrarMsgModal(tipo, msg) {
    const el = document.getElementById('modalMsg');
    el.className = 'full ' + (tipo === 'ok' ? 'msg-ok' : 'msg-err');
    el.textContent = msg;
    el.style.display = '';
}
function limpiarMsgModal() { const el = document.getElementById('modalMsg'); el.style.display = 'none'; el.textContent = ''; }
function esc(s) { return String(s ?? '').replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;'); }
function fmt(n) { return new Intl.NumberFormat('es-AR', { style:'currency', currency:'ARS' }).format(n ?? 0); }

async function buscarYPosicionar(codigo) {
    // Buscar el producto por código exacto y mostrar la página donde está
    document.getElementById('txtBuscar').value = codigo;
    paginaActual = 1;
    await cargar();
    // Resaltar fila si está visible
    const celdas = Array.from(document.querySelectorAll('#tbodyProductos td code'));
    const celda = celdas.find(c => c.textContent.trim() === codigo);
    if (celda) {
        const fila = celda.closest('tr');
        fila.style.backgroundColor = '#fffacd'; // amarillo suave
        fila.scrollIntoView({ behavior: 'smooth', block: 'center' });
        setTimeout(() => { fila.style.backgroundColor = ''; }, 3000);
    }
}
