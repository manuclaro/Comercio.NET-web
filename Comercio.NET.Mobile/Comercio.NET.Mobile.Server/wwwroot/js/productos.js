const API_BASE = '/api';
let token = null;
let searchTimer = null;

// Producto actualmente en edición
let productoEditando = null;

document.addEventListener('DOMContentLoaded', async function () {
    token = localStorage.getItem('auth_token');

    if (!token) {
        window.location.href = '/login.html';
        return;
    }

    // Validar token
    try {
        const response = await fetch(`${API_BASE}/auth/validar`, {
            headers: { 'Authorization': `Bearer ${token}` }
        });
        const data = await response.json();
        if (!data.valido) {
            localStorage.clear();
            window.location.href = '/login.html';
            return;
        }
    } catch {
        localStorage.clear();
        window.location.href = '/login.html';
        return;
    }

    // Mostrar usuario
    const nombreCompleto = localStorage.getItem('usuario_completo')
        || localStorage.getItem('usuario_nombre')
        || 'Usuario';
    document.getElementById('nombreUsuario').textContent = `👤 ${nombreCompleto}`;

    // Cerrar sesión
    document.getElementById('btnCerrarSesion').addEventListener('click', () => {
        localStorage.clear();
        window.location.href = '/login.html';
    });

    // Checkbox "Mostrar Costo": muestra/oculta la columna en tiempo real
    document.getElementById('chkMostrarCosto').addEventListener('change', actualizarVisibilidadCosto);

    // Eventos de búsqueda
    const txtBuscar = document.getElementById('txtBuscar');
    const btnBuscar = document.getElementById('btnBuscar');

    btnBuscar.addEventListener('click', () => buscarProductos(txtBuscar.value.trim()));

    txtBuscar.addEventListener('keydown', (e) => {
        if (e.key === 'Enter') buscarProductos(txtBuscar.value.trim());
    });

    // Búsqueda automática con debounce (500ms)
    txtBuscar.addEventListener('input', () => {
        clearTimeout(searchTimer);
        const termino = txtBuscar.value.trim();
        if (!termino) {
            mostrarEstado('Ingresá un término para buscar productos.');
            ocultarResultados();
            return;
        }
        searchTimer = setTimeout(() => buscarProductos(termino), 500);
    });

    // ── Eventos del modal ──
    document.getElementById('btnCerrarModal').addEventListener('click', cerrarModal);
    document.getElementById('btnCancelarModal').addEventListener('click', cerrarModal);
    document.getElementById('btnGuardarCambios').addEventListener('click', guardarCambios);

    // Cerrar con Escape
    document.addEventListener('keydown', (e) => {
        if (e.key === 'Escape') cerrarModal();
    });

    // Cerrar al hacer clic fuera del modal
    document.getElementById('modalEdicion').addEventListener('click', (e) => {
        if (e.target === document.getElementById('modalEdicion')) cerrarModal();
    });

    // Cálculo automático del precio cuando cambian costo o porcentaje
    document.getElementById('editCosto').addEventListener('input', calcularPrecioSugerido);
    document.getElementById('editPorcentaje').addEventListener('input', calcularPrecioSugerido);

    // Ajuste de stock con botones +/-
    document.getElementById('btnStockMenos').addEventListener('click', () => ajustarStock(-1));
    document.getElementById('btnStockMas').addEventListener('click', () => ajustarStock(1));

    // Aplicar estado inicial del checkbox (oculto por defecto)
    actualizarVisibilidadCosto();
});

// ─────────────────────────────────────────────
//  BÚSQUEDA
// ─────────────────────────────────────────────

function actualizarVisibilidadCosto() {
    const mostrar = document.getElementById('chkMostrarCosto').checked;
    document.querySelectorAll('.col-costo').forEach(el => {
        el.style.display = mostrar ? '' : 'none';
    });
}

async function buscarProductos(termino) {
    if (!termino) {
        mostrarEstado('Ingresá un término para buscar productos.');
        ocultarResultados();
        return;
    }

    mostrarLoading(true);
    ocultarError();
    ocultarResultados();
    mostrarEstado('');

    try {
        const params = new URLSearchParams({ termino });
        const response = await fetch(`${API_BASE}/productos/buscar?${params}`, {
            headers: { 'Authorization': `Bearer ${token}` }
        });

        if (response.status === 401) {
            localStorage.clear();
            window.location.href = '/login.html';
            return;
        }

        if (!response.ok) throw new Error(`Error ${response.status}`);

        const productos = await response.json();
        renderizarProductos(productos);

    } catch (err) {
        mostrarError(`Error al buscar productos: ${err.message}`);
    } finally {
        mostrarLoading(false);
    }
}

function renderizarProductos(productos) {
    const tbody = document.getElementById('tbodyProductos');
    const contador = document.getElementById('contadorResultados');
    const divResultados = document.getElementById('resultados');

    if (!productos || productos.length === 0) {
        mostrarEstado('No se encontraron productos.');
        return;
    }

    contador.textContent = `${productos.length} producto(s) encontrado(s)`;

    tbody.innerHTML = productos.map(p => {
        const stockClass = p.stock <= 0 ? 'stock-cero'
            : p.stock <= 5  ? 'stock-bajo'
            : p.stock <= 10 ? 'stock-medio'
            : 'stock-ok';

        return `
            <tr class="fila-producto" data-codigo="${escapeAttr(p.codigo)}"
                data-descripcion="${escapeAttr(p.descripcion)}"
                data-costo="${p.costo}"
                data-precio="${p.precio}"
                data-stock="${p.stock}"
                title="Tocar para editar">
                <td class="col-landscape"><code>${p.codigo}</code></td>
                <td>${p.descripcion}</td>
                <td class="col-landscape"><span class="badge-rubro">${p.rubro || '-'}</span></td>
                <td class="col-costo text-right costo">${formatearMoneda(p.costo)}</td>
                <td class="text-right precio">${formatearMoneda(p.precio)}</td>
                <td class="text-center"><span class="badge-stock ${stockClass}">${p.stock}</span></td>
            </tr>
        `;
    }).join('');

    divResultados.style.display = 'block';

    // Click en fila → abrir modal de edición
    tbody.querySelectorAll('.fila-producto').forEach(tr => {
        tr.addEventListener('click', () => {
            abrirModalEdicion({
                codigo:      tr.dataset.codigo,
                descripcion: tr.dataset.descripcion,
                costo:       parseFloat(tr.dataset.costo),
                precio:      parseFloat(tr.dataset.precio),
                stock:       parseInt(tr.dataset.stock, 10),
            });
        });
    });

    actualizarVisibilidadCosto();
}

// ─────────────────────────────────────────────
//  MODAL DE EDICIÓN
// ─────────────────────────────────────────────

function abrirModalEdicion(producto) {
    productoEditando = producto;

    document.getElementById('modalEdicionDesc').textContent =
        `[${producto.codigo}] ${producto.descripcion}`;

    document.getElementById('editCosto').value       = producto.costo;
    document.getElementById('editPrecio').value      = producto.precio;
    document.getElementById('editStock').value       = producto.stock;

    // Calcular porcentaje actual a partir del costo y precio existentes
    if (producto.costo > 0) {
        const pctActual = ((producto.precio / producto.costo) - 1) * 100;
        document.getElementById('editPorcentaje').value = parseFloat(pctActual.toFixed(2));
    } else {
        document.getElementById('editPorcentaje').value = '';
    }

    actualizarInfoPrecioCalculado();
    ocultarMensajesModal();

    document.getElementById('modalEdicion').style.display = 'flex';
    document.getElementById('editCosto').focus();
}

function cerrarModal() {
    document.getElementById('modalEdicion').style.display = 'none';
    productoEditando = null;
}

function calcularPrecioSugerido() {
    const costo = parseFloat(document.getElementById('editCosto').value) || 0;
    const pct   = parseFloat(document.getElementById('editPorcentaje').value) || 0;

    if (costo > 0 && pct >= 0) {
        const precioCalculado = costo * (1 + pct / 100);
        document.getElementById('editPrecio').value = precioCalculado.toFixed(2);
    }

    actualizarInfoPrecioCalculado();
}

function actualizarInfoPrecioCalculado() {
    const costo  = parseFloat(document.getElementById('editCosto').value) || 0;
    const pct    = parseFloat(document.getElementById('editPorcentaje').value) || 0;
    const precio = parseFloat(document.getElementById('editPrecio').value) || 0;
    const info   = document.getElementById('precioCalculadoInfo');

    if (costo > 0 && precio > 0) {
        const pctReal = ((precio / costo) - 1) * 100;
        info.textContent = `Margen real: ${pctReal.toFixed(1)}%`;
        info.className = 'precio-calculado-info ' + (pctReal < pct - 0.05 ? 'margen-reducido' : 'margen-ok');
    } else {
        info.textContent = '';
    }
}

// Actualizar la info de margen cuando el usuario edita el precio manualmente
document.addEventListener('DOMContentLoaded', () => {
    // Este listener se agrega después del renderizado inicial
});

function ajustarStock(delta) {
    const input = document.getElementById('editStock');
    const val = (parseInt(input.value, 10) || 0) + delta;
    input.value = Math.max(0, val);
}

async function guardarCambios() {
    if (!productoEditando) return;

    const costo  = parseFloat(document.getElementById('editCosto').value);
    const pct    = parseFloat(document.getElementById('editPorcentaje').value) || 0;
    const precio = parseFloat(document.getElementById('editPrecio').value);
    const stock  = parseInt(document.getElementById('editStock').value, 10);

    // Validaciones
    if (isNaN(costo) || costo < 0) {
        mostrarErrorModal('El costo debe ser un número mayor o igual a cero.');
        return;
    }
    if (isNaN(precio) || precio < 0) {
        mostrarErrorModal('El precio debe ser un número mayor o igual a cero.');
        return;
    }
    if (isNaN(stock) || stock < 0) {
        mostrarErrorModal('El stock debe ser un número mayor o igual a cero.');
        return;
    }

    setBtnGuardarCargando(true);
    ocultarMensajesModal();

    try {
        const response = await fetch(`${API_BASE}/productos/${encodeURIComponent(productoEditando.codigo)}`, {
            method: 'PUT',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ costo, porcentaje: pct, precio, stock })
        });

        if (response.status === 401) {
            localStorage.clear();
            window.location.href = '/login.html';
            return;
        }

        const data = await response.json();

        if (!response.ok) {
            mostrarErrorModal(data.error || `Error ${response.status}`);
            return;
        }

        // Actualizar los datos en el DOM de la fila correspondiente
        actualizarFilaEnTabla(productoEditando.codigo, costo, precio, stock);

        mostrarExitoModal('✅ Producto actualizado correctamente.');

        // Cerrar modal luego de un breve instante
        setTimeout(() => cerrarModal(), 1400);

    } catch (err) {
        mostrarErrorModal(`Error de conexión: ${err.message}`);
    } finally {
        setBtnGuardarCargando(false);
    }
}

function actualizarFilaEnTabla(codigo, costo, precio, stock) {
    const tbody = document.getElementById('tbodyProductos');
    const fila  = tbody.querySelector(`tr[data-codigo="${CSS.escape(codigo)}"]`);
    if (!fila) return;

    // Actualizar atributos data para futuras ediciones
    fila.dataset.costo  = costo;
    fila.dataset.precio = precio;
    fila.dataset.stock  = stock;

    // Actualizar celdas visibles
    const stockClass = stock <= 0 ? 'stock-cero'
        : stock <= 5  ? 'stock-bajo'
        : stock <= 10 ? 'stock-medio'
        : 'stock-ok';

    fila.querySelector('.costo').textContent  = formatearMoneda(costo);
    fila.querySelector('.precio').textContent = formatearMoneda(precio);
    fila.querySelector('.badge-stock').textContent  = stock;
    fila.querySelector('.badge-stock').className    = `badge-stock ${stockClass}`;

    // Efecto visual de confirmación
    fila.classList.add('fila-actualizada');
    setTimeout(() => fila.classList.remove('fila-actualizada'), 2000);
}

// ─────────────────────────────────────────────
//  UTILIDADES MODAL
// ─────────────────────────────────────────────

function setBtnGuardarCargando(cargando) {
    const btn   = document.getElementById('btnGuardarCambios');
    const texto = document.getElementById('btnGuardarTexto');
    btn.disabled  = cargando;
    texto.textContent = cargando ? '⏳ Guardando...' : '💾 Guardar';
}

function mostrarErrorModal(msg) {
    const el = document.getElementById('modalError');
    el.textContent = msg;
    el.style.display = 'block';
    document.getElementById('modalExito').style.display = 'none';
}

function mostrarExitoModal(msg) {
    const el = document.getElementById('modalExito');
    el.textContent = msg;
    el.style.display = 'block';
    document.getElementById('modalError').style.display = 'none';
}

function ocultarMensajesModal() {
    document.getElementById('modalError').style.display = 'none';
    document.getElementById('modalExito').style.display = 'none';
}

// ─────────────────────────────────────────────
//  UTILIDADES GENERALES
// ─────────────────────────────────────────────

function escapeAttr(str) {
    return String(str ?? '')
        .replace(/&/g, '&amp;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;');
}

function formatearMoneda(valor) {
    return new Intl.NumberFormat('es-AR', {
        style: 'currency',
        currency: 'ARS'
    }).format(valor || 0);
}

function mostrarLoading(visible) {
    document.getElementById('loading').style.display = visible ? 'flex' : 'none';
}

function mostrarError(msg) {
    const el = document.getElementById('error');
    el.textContent = msg;
    el.style.display = 'block';
}

function ocultarError() {
    document.getElementById('error').style.display = 'none';
}

function mostrarEstado(msg) {
    const el = document.getElementById('estadoMensaje');
    el.textContent = msg;
    el.style.display = msg ? 'block' : 'none';
}

function ocultarResultados() {
    document.getElementById('resultados').style.display = 'none';
}

// Actualizar info de margen cuando el usuario edita el precio manualmente
document.addEventListener('DOMContentLoaded', () => {
    document.getElementById('editPrecio').addEventListener('input', actualizarInfoPrecioCalculado);
});