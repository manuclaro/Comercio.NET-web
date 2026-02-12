// Constantes
const API_BASE = '/api';

// Configuración de la API
const API_URL = window.location.origin + '/api/ArqueoCaja';

// Estado global
let token = null;
let usuarioActual = null;

// Elementos del DOM
const fecha = document.getElementById('fecha');
const cajero = document.getElementById('cajero');
const btnHoy = document.getElementById('btnHoy');
const btnActualizar = document.getElementById('btnActualizar');
const loading = document.getElementById('loading');
const error = document.getElementById('error');
const resultado = document.getElementById('resultado');

// Verificar autenticación al cargar
document.addEventListener('DOMContentLoaded', async function () {
    token = localStorage.getItem('auth_token');

    if (!token) {
        window.location.href = '/login.html';
        return;
    }

    // Validar token
    const tokenValido = await validarToken();
    if (!tokenValido) {
        cerrarSesion();
        return;
    }

    // Cargar datos de usuario
    usuarioActual = {
        nombre: localStorage.getItem('usuario_nombre'),
        nombreCompleto: localStorage.getItem('usuario_completo'),
        rol: localStorage.getItem('usuario_rol')
    };

    // Mostrar info de usuario
    mostrarInfoUsuario();

    // Inicializar aplicación
    inicializarApp();
});

async function validarToken() {
    try {
        const response = await fetch(`${API_BASE}/auth/validar`, {
            headers: {
                'Authorization': `Bearer ${token}`
            }
        });
        const data = await response.json();
        return data.valido;
    } catch (error) {
        console.error('Error validando token:', error);
        return false;
    }
}

function mostrarInfoUsuario() {
    const header = document.querySelector('header');
    const userInfo = document.createElement('div');
    userInfo.className = 'user-info';
    userInfo.innerHTML = `
        <span>👤 ${usuarioActual.nombreCompleto || usuarioActual.nombre}</span>
        <button id="btnCerrarSesion" class="btn btn-secondary">Cerrar Sesión</button>
    `;
    header.appendChild(userInfo);

    document.getElementById('btnCerrarSesion').addEventListener('click', cerrarSesion);
}

function cerrarSesion() {
    localStorage.clear();
    window.location.href = '/login.html';
}

function inicializarApp() {
    const fechaInput = document.getElementById('fecha');
    const cajeroSelect = document.getElementById('cajero');
    const btnHoy = document.getElementById('btnHoy');
    const btnActualizar = document.getElementById('btnActualizar');

    // ✅ CORREGIDO: Establecer fecha actual en formato local
    const establecerFechaHoy = () => {
        const ahora = new Date();
        const año = ahora.getFullYear();
        const mes = String(ahora.getMonth() + 1).padStart(2, '0');
        const dia = String(ahora.getDate()).padStart(2, '0');
        fechaInput.value = `${año}-${mes}-${dia}`;
    };

    establecerFechaHoy();

    // Cargar cajeros
    cargarCajeros();

    // Event listeners
    btnHoy.addEventListener('click', () => {
        establecerFechaHoy();
        cargarArqueo();
    });

    btnActualizar.addEventListener('click', cargarArqueo);
    fechaInput.addEventListener('change', cargarArqueo);
    cajeroSelect.addEventListener('change', cargarArqueo);

    // Cargar datos iniciales
    cargarArqueo();
}

// Función para cargar lista de cajeros
async function cargarCajeros() {
    try {
        const response = await fetch(`${API_BASE}/arqueocaja/cajeros`, {
            headers: {
                'Authorization': `Bearer ${token}`
            }
        });

        if (response.status === 401) {
            cerrarSesion();
            return;
        }

        const cajeros = await response.json();
        const select = document.getElementById('cajero');

        // Limpiar opciones existentes (excepto "Todos")
        select.innerHTML = '<option value="">Todos los cajeros</option>';

        // Agregar cajeros al select
        cajeros.forEach(cajero => {
            const option = document.createElement('option');
            option.value = cajero;
            option.textContent = cajero;
            select.appendChild(option);
        });
    } catch (error) {
        console.error('Error cargando cajeros:', error);
    }
}

// Función para cargar arqueo de caja
async function cargarArqueo() {
    const loading = document.getElementById('loading');
    const error = document.getElementById('error');
    const resultado = document.getElementById('resultado');

    loading.style.display = 'block';
    error.style.display = 'none';
    resultado.style.display = 'none';

    try {
        const fecha = document.getElementById('fecha').value;
        const cajero = document.getElementById('cajero').value;

        const params = new URLSearchParams();
        if (cajero) params.append('cajero', cajero);

        const response = await fetch(
            `${API_BASE}/arqueocaja/fecha/${fecha}?${params}`,
            {
                headers: {
                    'Authorization': `Bearer ${token}`
                }
            }
        );

        if (response.status === 401) {
            cerrarSesion();
            return;
        }

        const data = await response.json();

        mostrarResultado(data);
    } catch (err) {
        error.textContent = `Error: ${err.message}`;
        error.style.display = 'block';
    } finally {
        loading.style.display = 'none';
    }
}

// ✅ Mostrar datos en la interfaz (ACTUALIZADO)
function mostrarResultado(data) {
    document.getElementById('totalIngresos').textContent = formatearMoneda(data.totalIngresos);
    document.getElementById('cajeroInfo').textContent = data.cajero || 'Todos los cajeros';
    document.getElementById('dni').textContent = formatearMoneda(data.dni);
    document.getElementById('efectivo').textContent = formatearMoneda(data.efectivo);
    document.getElementById('mercadoPago').textContent = formatearMoneda(data.mercadoPago);
    document.getElementById('otro').textContent = formatearMoneda(data.otro);
    document.getElementById('facturaC').textContent = formatearMoneda(data.facturaC);

    // ✅ NUEVO: Mostrar pagos a proveedores (clickeable)
    const pagosElement = document.getElementById('pagosProveedores');
    pagosElement.textContent = formatearMoneda(data.pagosProveedores);

    // ✅ Hacer clickeable solo si hay pagos
    if (data.pagosProveedores > 0) {
        pagosElement.style.cursor = 'pointer';
        pagosElement.style.textDecoration = 'underline';
        pagosElement.title = 'Click para ver detalle';

        // Remover event listeners previos
        const newElement = pagosElement.cloneNode(true);
        pagosElement.parentNode.replaceChild(newElement, pagosElement);

        // Agregar nuevo event listener
        newElement.addEventListener('click', () => abrirModalDetalleProveedores());
    } else {
        pagosElement.style.cursor = 'default';
        pagosElement.style.textDecoration = 'none';
        pagosElement.title = '';
    }

    // ✅ NUEVO: Mostrar efectivo neto
    const efectivoNetoElement = document.getElementById('efectivoNeto');
    const efectivoNeto = data.efectivoNeto || (data.efectivo - data.pagosProveedores);
    efectivoNetoElement.textContent = formatearMoneda(efectivoNeto);

    // ✅ Marcar en rojo si es negativo
    if (efectivoNeto < 0) {
        efectivoNetoElement.classList.add('negativo');
    } else {
        efectivoNetoElement.classList.remove('negativo');
    }

    document.getElementById('cantidadVentas').textContent = data.cantidadVentas;
    document.getElementById('fechaConsulta').textContent = new Date(data.fecha).toLocaleDateString('es-AR');
    document.getElementById('ultimaActualizacion').textContent = new Date().toLocaleString('es-AR');

    document.getElementById('resultado').style.display = 'block';
}

// ✅ NUEVA FUNCIÓN: Abrir modal con detalle de proveedores
async function abrirModalDetalleProveedores() {
    const fecha = document.getElementById('fecha').value;
    const cajero = document.getElementById('cajero').value;

    // Crear modal
    const modal = crearModal();
    document.body.appendChild(modal);

    // Mostrar loading en el modal
    const modalBody = modal.querySelector('.modal-body');
    modalBody.innerHTML = '<div class="loading"><div class="spinner"></div><p>Cargando detalles...</p></div>';

    try {
        const params = new URLSearchParams({ fecha });
        if (cajero) params.append('cajero', cajero);

        const response = await fetch(
            `${API_BASE}/arqueocaja/pagos-proveedores?${params}`,
            {
                headers: {
                    'Authorization': `Bearer ${token}`
                }
            }
        );

        if (!response.ok) throw new Error('Error cargando detalles');

        const detalles = await response.json();

        mostrarDetallesEnModal(modalBody, detalles);
    } catch (error) {
        modalBody.innerHTML = `<div class="error">Error cargando detalles: ${error.message}</div>`;
    }
}

// ✅ NUEVA FUNCIÓN: Crear estructura del modal
function crearModal() {
    const modal = document.createElement('div');
    modal.className = 'modal-overlay';
    modal.innerHTML = `
        <div class="modal-content">
            <div class="modal-header">
                <h3>🧾 Detalle de Pagos a Proveedores</h3>
                <button class="btn-close">&times;</button>
            </div>
            <div class="modal-body"></div>
            <div class="modal-footer">
                <button class="btn btn-secondary btn-cerrar">Cerrar</button>
                <button class="btn btn-primary btn-exportar" style="display:none;">📄 Exportar CSV</button>
            </div>
        </div>
    `;

    // Event listeners para cerrar
    modal.querySelector('.btn-close').addEventListener('click', () => modal.remove());
    modal.querySelector('.btn-cerrar').addEventListener('click', () => modal.remove());
    modal.addEventListener('click', (e) => {
        if (e.target === modal) modal.remove();
    });

    return modal;
}

// ✅ NUEVA FUNCIÓN: Mostrar detalles en el modal
function mostrarDetallesEnModal(container, detalles) {
    if (!detalles || detalles.length === 0) {
        container.innerHTML = '<div class="no-data">No hay pagos a proveedores en esta fecha</div>';
        return;
    }

    // Calcular total
    const total = detalles.reduce((sum, p) => sum + p.monto, 0);

    // Crear tabla
    const tabla = `
        <table class="tabla-detalle">
            <thead>
                <tr>
                    <th>Hora</th>
                    <th>Proveedor</th>
                    <th>Concepto</th>
                    <th>Forma de Pago</th>
                    <th class="text-right">Monto</th>
                </tr>
            </thead>
            <tbody>
                ${detalles.map(pago => `
                    <tr>
                        <td>${new Date(pago.fechaPago).toLocaleTimeString('es-AR', { hour: '2-digit', minute: '2-digit' })}</td>
                        <td><strong>${pago.nombreProveedor}</strong></td>
                        <td>${pago.concepto || '-'}</td>
                        <td><span class="badge-forma-pago">${pago.formaPago}</span></td>
                        <td class="text-right"><strong>${formatearMoneda(pago.monto)}</strong></td>
                    </tr>
                `).join('')}
            </tbody>
            <tfoot>
                <tr class="total-row">
                    <td colspan="4" class="text-right"><strong>TOTAL:</strong></td>
                    <td class="text-right"><strong>${formatearMoneda(total)}</strong></td>
                </tr>
            </tfoot>
        </table>
    `;

    container.innerHTML = tabla;

    // Mostrar botón de exportar
    const btnExportar = document.querySelector('.btn-exportar');
    btnExportar.style.display = 'inline-block';
    btnExportar.onclick = () => exportarDetallesCSV(detalles);
}

// ✅ NUEVA FUNCIÓN: Exportar a CSV
function exportarDetallesCSV(detalles) {
    const fecha = document.getElementById('fecha').value;

    // Crear contenido CSV
    let csv = 'Hora,Proveedor,Concepto,Forma de Pago,Monto\n';
    detalles.forEach(pago => {
        const hora = new Date(pago.fechaPago).toLocaleTimeString('es-AR', { hour: '2-digit', minute: '2-digit' });
        csv += `${hora},"${pago.nombreProveedor}","${pago.concepto || '-'}","${pago.formaPago}",${pago.monto}\n`;
    });

    // Descargar archivo
    const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `PagosProveedores_${fecha.replace(/-/g, '')}.csv`;
    link.click();
    window.URL.revokeObjectURL(url);
}

// Formatear moneda
function formatearMoneda(valor) {
    return new Intl.NumberFormat('es-AR', {
        style: 'currency',
        currency: 'ARS'
    }).format(valor);
}