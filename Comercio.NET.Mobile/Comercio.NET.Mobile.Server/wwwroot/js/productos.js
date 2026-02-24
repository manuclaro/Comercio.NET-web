const API_BASE = '/api';
let token = null;
let searchTimer = null;

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

    // Aplicar estado inicial del checkbox (oculto por defecto)
    actualizarVisibilidadCosto();
});

function actualizarVisibilidadCosto() {
    const mostrar = document.getElementById('chkMostrarCosto').checked;
    // Mostrar/ocultar tanto el <th> como todas las <td> de la columna costo
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
            : p.stock <= 5 ? 'stock-bajo'
                : p.stock <= 10 ? 'stock-medio'
                    : 'stock-ok';

        return `
            <tr>
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

    // Aplicar visibilidad del costo a las filas recién renderizadas
    actualizarVisibilidadCosto();
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