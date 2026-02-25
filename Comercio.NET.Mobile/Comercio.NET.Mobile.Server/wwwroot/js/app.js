// Constantes
const API_BASE = '/api';
let token = null;

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
    try {
        const response = await fetch(`${API_BASE}/auth/validar`, {
            headers: { 'Authorization': `Bearer ${token}` }
        });
        const data = await response.json();
        if (!data.valido) {
            cerrarSesion();
            return;
        }
    } catch {
        cerrarSesion();
        return;
    }

    // Mostrar nombre de usuario en el header
    const nombreCompleto = localStorage.getItem('usuario_completo')
        || localStorage.getItem('usuario_nombre')
        || 'Usuario';
    document.getElementById('nombreUsuario').textContent = `👤 ${nombreCompleto}`;

    // Cerrar sesión
    document.getElementById('btnCerrarSesion').addEventListener('click', cerrarSesion);

    inicializarApp();
});

function cerrarSesion() {
    localStorage.clear();
    window.location.href = '/login.html';
}

function inicializarApp() {
    const fechaInput   = document.getElementById('fecha');
    const cajeroSelect = document.getElementById('cajero');
    const btnHoy       = document.getElementById('btnHoy');
    const btnActualizar = document.getElementById('btnActualizar');

    const establecerFechaHoy = () => {
        const ahora = new Date();
        const año  = ahora.getFullYear();
        const mes  = String(ahora.getMonth() + 1).padStart(2, '0');
        const dia  = String(ahora.getDate()).padStart(2, '0');
        fechaInput.value = `${año}-${mes}-${dia}`;
    };

    establecerFechaHoy();
    cargarCajeros();

    btnHoy.addEventListener('click', () => {
        establecerFechaHoy();
        cargarArqueo();
    });
    btnActualizar.addEventListener('click', cargarArqueo);
    fechaInput.addEventListener('change', cargarArqueo);
    cajeroSelect.addEventListener('change', cargarArqueo);

    cargarArqueo();
}

async function cargarCajeros() {
    try {
        const response = await fetch(`${API_BASE}/arqueocaja/cajeros`, {
            headers: { 'Authorization': `Bearer ${token}` }
        });
        if (response.status === 401) { cerrarSesion(); return; }

        const cajeros = await response.json();
        const select  = document.getElementById('cajero');
        select.innerHTML = '<option value="">Todos los cajeros</option>';
        cajeros.forEach(c => {
            const opt = document.createElement('option');
            opt.value = c;
            opt.textContent = c;
            select.appendChild(opt);
        });
    } catch (err) {
        console.error('Error cargando cajeros:', err);
    }
}

async function cargarArqueo() {
    const loadingEl  = document.getElementById('loading');
    const errorEl    = document.getElementById('error');
    const resultadoEl = document.getElementById('resultado');

    loadingEl.style.display  = 'flex';
    errorEl.style.display    = 'none';
    resultadoEl.style.display = 'none';

    try {
        const fecha  = document.getElementById('fecha').value;
        const cajero = document.getElementById('cajero').value;
        const params = new URLSearchParams();
        if (cajero) params.append('cajero', cajero);

        const response = await fetch(`${API_BASE}/arqueocaja/fecha/${fecha}?${params}`, {
            headers: { 'Authorization': `Bearer ${token}` }
        });

        if (response.status === 401) { cerrarSesion(); return; }
        if (!response.ok) throw new Error(`Error ${response.status}`);

        const data = await response.json();
        mostrarResultado(data);
    } catch (err) {
        errorEl.textContent    = `Error: ${err.message}`;
        errorEl.style.display  = 'block';
    } finally {
        loadingEl.style.display = 'none';
    }
}

function mostrarResultado(data) {
    document.getElementById('totalIngresos').textContent  = formatearMoneda(data.totalIngresos);
    document.getElementById('cajeroInfo').textContent     = data.cajero || 'Todos los cajeros';
    document.getElementById('dni').textContent            = formatearMoneda(data.dni);
    document.getElementById('efectivo').textContent       = formatearMoneda(data.efectivo);
    document.getElementById('mercadoPago').textContent    = formatearMoneda(data.mercadoPago);
    document.getElementById('otro').textContent           = formatearMoneda(data.otro);
    document.getElementById('facturaC').textContent       = formatearMoneda(data.facturaC);
    document.getElementById('cantidadVentas').textContent = data.cantidadVentas;
    document.getElementById('fechaConsulta').textContent  = new Date(data.fecha).toLocaleDateString('es-AR');
    document.getElementById('ultimaActualizacion').textContent = new Date().toLocaleString('es-AR');

    // Pagos a proveedores (clickeable si hay datos)
    const pagosEl = document.getElementById('pagosProveedores');
    pagosEl.textContent = formatearMoneda(data.pagosProveedores);
    if (data.pagosProveedores > 0) {
        pagosEl.style.cursor         = 'pointer';
        pagosEl.style.textDecoration = 'underline';
        pagosEl.title = 'Click para ver detalle';
        const nuevo = pagosEl.cloneNode(true);
        pagosEl.parentNode.replaceChild(nuevo, pagosEl);
        nuevo.addEventListener('click', abrirModalDetalleProveedores);
    } else {
        pagosEl.style.cursor         = 'default';
        pagosEl.style.textDecoration = 'none';
    }

    // Efectivo neto
    const efectivoNetoEl = document.getElementById('efectivoNeto');
    const efectivoNeto   = data.efectivoNeto ?? (data.efectivo - data.pagosProveedores);
    efectivoNetoEl.textContent = formatearMoneda(efectivoNeto);
    efectivoNetoEl.classList.toggle('negativo', efectivoNeto < 0);

    document.getElementById('resultado').style.display = 'block';
}

async function abrirModalDetalleProveedores() {
    const fechaInput  = document.getElementById('fecha').value;
    const cajeroInput = document.getElementById('cajero').value;

    const modal     = crearModal();
    document.body.appendChild(modal);
    const modalBody = modal.querySelector('.modal-body');
    modalBody.innerHTML = '<div class="loading"><div class="spinner"></div><span>Cargando detalles...</span></div>';

    try {
        const params = new URLSearchParams({ fecha: fechaInput });
        if (cajeroInput) params.append('cajero', cajeroInput);

        const response = await fetch(`${API_BASE}/arqueocaja/pagos-proveedores?${params}`, {
            headers: { 'Authorization': `Bearer ${token}`, 'Accept': 'application/json' }
        });

        const text = await response.text();
        let data;
        try { data = JSON.parse(text); }
        catch { throw new Error(`Respuesta inválida del servidor`); }

        if (!response.ok) throw new Error(data.error || `Error ${response.status}`);

        mostrarDetallesEnModal(modalBody, data);
    } catch (err) {
        modalBody.innerHTML = `<div class="no-data" style="color:#c62828;">❌ ${err.message}</div>`;
    }
}

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
    modal.querySelector('.btn-close').addEventListener('click', () => modal.remove());
    modal.querySelector('.btn-cerrar').addEventListener('click', () => modal.remove());
    modal.addEventListener('click', e => { if (e.target === modal) modal.remove(); });
    return modal;
}

function mostrarDetallesEnModal(container, detalles) {
    if (!detalles || detalles.length === 0) {
        container.innerHTML = '<div class="no-data">No hay pagos a proveedores en esta fecha</div>';
        return;
    }

    const total = detalles.reduce((sum, p) => sum + p.monto, 0);

    container.innerHTML = `
        <table class="tabla-detalle">
            <thead>
                <tr>
                    <th>Hora</th>
                    <th>Proveedor</th>
                    <th class="text-right">Monto</th>
                </tr>
            </thead>
            <tbody>
                ${detalles.map(p => `
                    <tr>
                        <td>${new Date(p.fechaPago).toLocaleTimeString('es-AR', { hour: '2-digit', minute: '2-digit' })}</td>
                        <td><strong>${p.proveedor}</strong></td>
                        <td class="text-right"><strong>${formatearMoneda(p.monto)}</strong></td>
                    </tr>
                `).join('')}
            </tbody>
            <tfoot>
                <tr class="total-row">
                    <td colspan="2" class="text-right"><strong>TOTAL:</strong></td>
                    <td class="text-right"><strong>${formatearMoneda(total)}</strong></td>
                </tr>
            </tfoot>
        </table>
    `;

    const btnExportar = document.querySelector('.btn-exportar');
    if (btnExportar) {
        btnExportar.style.display = 'inline-block';
        btnExportar.onclick = () => exportarDetallesCSV(detalles);
    }
}

function exportarDetallesCSV(detalles) {
    const fecha = document.getElementById('fecha').value;
    let csv = 'Hora,Proveedor,Monto\n';
    detalles.forEach(p => {
        const hora = new Date(p.fechaPago).toLocaleTimeString('es-AR', { hour: '2-digit', minute: '2-digit' });
        csv += `${hora},"${p.proveedor}",${p.monto}\n`;
    });
    const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
    const url  = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `PagosProveedores_${fecha.replace(/-/g, '')}.csv`;
    link.click();
    URL.revokeObjectURL(url);
}

function formatearMoneda(valor) {
    return new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS' }).format(valor || 0);
}