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
document.addEventListener('DOMContentLoaded', async function() {
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

    // Establecer fecha actual
    fechaInput.valueAsDate = new Date();

    // Cargar cajeros
    cargarCajeros();

    // Event listeners
    btnHoy.addEventListener('click', () => {
        fechaInput.valueAsDate = new Date();
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

// Mostrar datos en la interfaz
function mostrarResultado(data) {
    document.getElementById('totalIngresos').textContent = formatearMoneda(data.totalIngresos);
    document.getElementById('cajeroInfo').textContent = data.cajero || 'Todos los cajeros';
    document.getElementById('dni').textContent = formatearMoneda(data.dni);
    document.getElementById('efectivo').textContent = formatearMoneda(data.efectivo);
    document.getElementById('mercadoPago').textContent = formatearMoneda(data.mercadoPago);
    document.getElementById('otro').textContent = formatearMoneda(data.otro);
    document.getElementById('facturaC').textContent = formatearMoneda(data.facturaC);
    document.getElementById('pagosProveedores').textContent = formatearMoneda(data.pagosProveedores);
    document.getElementById('efectivoNeto').textContent = formatearMoneda(data.efectivoNeto);
    document.getElementById('cantidadVentas').textContent = data.cantidadVentas;
    document.getElementById('fechaConsulta').textContent = new Date(data.fecha).toLocaleDateString('es-AR');
    document.getElementById('ultimaActualizacion').textContent = new Date().toLocaleString('es-AR');
    
    document.getElementById('resultado').style.display = 'block';
}

// Formatear moneda
function formatearMoneda(valor) {
    return new Intl.NumberFormat('es-AR', {
        style: 'currency',
        currency: 'ARS'
    }).format(valor);
}