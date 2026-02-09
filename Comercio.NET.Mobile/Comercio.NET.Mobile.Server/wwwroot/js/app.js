// Configuración de la API
const API_URL = window.location.origin + '/api/ArqueoCaja';

// Elementos del DOM
const fecha = document.getElementById('fecha');
const cajero = document.getElementById('cajero');
const btnHoy = document.getElementById('btnHoy');
const btnActualizar = document.getElementById('btnActualizar');
const loading = document.getElementById('loading');
const error = document.getElementById('error');
const resultado = document.getElementById('resultado');

// Inicializar
document.addEventListener('DOMContentLoaded', () => {
    // Establecer fecha de hoy
    fecha.valueAsDate = new Date();

    // Cargar lista de cajeros
    cargarCajeros();

    // Cargar datos iniciales
    cargarArqueo();

    // Event listeners
    btnHoy.addEventListener('click', () => {
        fecha.valueAsDate = new Date();
        cargarArqueo();
    });

    btnActualizar.addEventListener('click', cargarArqueo);

    // Recargar cuando cambie el cajero
    cajero.addEventListener('change', cargarArqueo);
});

// Función para cargar lista de cajeros
async function cargarCajeros() {
    try {
        const response = await fetch(`${API_URL}/cajeros`);

        if (!response.ok) {
            console.error('Error al cargar cajeros');
            return;
        }

        const cajeros = await response.json();

        // Limpiar opciones existentes (excepto "Todos")
        cajero.innerHTML = '<option value="">Todos los cajeros</option>';

        // Agregar cajeros al select
        cajeros.forEach(c => {
            const option = document.createElement('option');
            option.value = c;
            option.textContent = c;
            cajero.appendChild(option);
        });

    } catch (err) {
        console.error('Error al cargar cajeros:', err);
    }
}

// Función para cargar arqueo de caja
async function cargarArqueo() {
    try {
        mostrarLoading(true);
        ocultarError();

        const fechaSeleccionada = fecha.value;
        const cajeroSeleccionado = cajero.value;

        let url = `${API_URL}/fecha/${fechaSeleccionada}`;

        // Agregar parámetro de cajero si hay uno seleccionado
        if (cajeroSeleccionado) {
            url += `?cajero=${encodeURIComponent(cajeroSeleccionado)}`;
        }

        console.log('Consultando:', url);

        const response = await fetch(url);

        if (!response.ok) {
            throw new Error(`Error ${response.status}: ${response.statusText}`);
        }

        const datos = await response.json();

        console.log('Datos recibidos:', datos);

        mostrarDatos(datos);
        actualizarUltimaActualizacion();

    } catch (err) {
        console.error('Error:', err);
        mostrarError('Error al cargar los datos: ' + err.message);
    } finally {
        mostrarLoading(false);
    }
}

// Mostrar datos en la interfaz
function mostrarDatos(datos) {
    document.getElementById('totalIngresos').textContent = formatearMoneda(datos.totalIngresos);
    document.getElementById('dni').textContent = formatearMoneda(datos.dni);
    document.getElementById('efectivo').textContent = formatearMoneda(datos.efectivo);
    document.getElementById('mercadoPago').textContent = formatearMoneda(datos.mercadoPago);
    document.getElementById('otro').textContent = formatearMoneda(datos.otro);
    document.getElementById('cantidadVentas').textContent = datos.cantidadVentas;
    document.getElementById('fechaConsulta').textContent = formatearFecha(datos.fecha);

    // Mostrar información del cajero
    const cajeroInfo = document.getElementById('cajeroInfo');
    if (datos.cajero) {
        cajeroInfo.textContent = `Cajero: ${datos.cajero}`;
        cajeroInfo.style.display = 'block';
    } else {
        cajeroInfo.textContent = 'Todos los cajeros';
        cajeroInfo.style.display = 'block';
    }

    resultado.style.display = 'block';
}

// Formatear moneda
function formatearMoneda(valor) {
    return new Intl.NumberFormat('es-AR', {
        style: 'currency',
        currency: 'ARS'
    }).format(valor);
}

// Formatear fecha
function formatearFecha(fechaStr) {
    const fecha = new Date(fechaStr);
    return fecha.toLocaleDateString('es-AR', {
        weekday: 'long',
        year: 'numeric',
        month: 'long',
        day: 'numeric'
    });
}

// Mostrar/ocultar loading
function mostrarLoading(mostrar) {
    loading.style.display = mostrar ? 'block' : 'none';
    resultado.style.display = mostrar ? 'none' : resultado.style.display;
}

// Mostrar error
function mostrarError(mensaje) {
    error.textContent = mensaje;
    error.style.display = 'block';
    resultado.style.display = 'none';
}

// Ocultar error
function ocultarError() {
    error.style.display = 'none';
}

// Actualizar timestamp
function actualizarUltimaActualizacion() {
    const ahora = new Date();
    document.getElementById('ultimaActualizacion').textContent =
        ahora.toLocaleTimeString('es-AR');
}