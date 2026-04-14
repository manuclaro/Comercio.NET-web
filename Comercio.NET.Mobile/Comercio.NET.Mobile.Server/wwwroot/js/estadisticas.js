const API_BASE = '/api';
let token = null;
let graficoPie = null;

const COLORES_RUBRO = {
    'VERDULERIA': { bg: 'rgba(76, 175, 80, 0.85)',  border: '#388e3c' },
    'PANADERIA':  { bg: 'rgba(255, 193, 7, 0.85)',  border: '#f57f17' },
    'FIAMBRERIA': { bg: 'rgba(244, 67, 54, 0.85)',  border: '#c62828' },
    'CARNICERIA': { bg: 'rgba(156, 39, 176, 0.85)', border: '#6a1b9a' },
    'ALMACEN':    { bg: 'rgba(33, 150, 243, 0.85)', border: '#1565c0' },
};

const COLOR_DEFAULT = { bg: 'rgba(158, 158, 158, 0.8)', border: '#616161' };

document.addEventListener('DOMContentLoaded', async function () {
    token = localStorage.getItem('auth_token');

    if (!token) {
        window.location.href = '/login.html';
        return;
    }

    // Validar token
    try {
        const res = await fetch(`${API_BASE}/auth/validar`, {
            headers: { 'Authorization': `Bearer ${token}` }
        });
        const data = await res.json();
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

    // Fechas por defecto: mes actual
    const hoy = new Date();
    const primerDia = new Date(hoy.getFullYear(), hoy.getMonth(), 1);
    document.getElementById('inputDesde').value = formatearFechaInput(primerDia);
    document.getElementById('inputHasta').value = formatearFechaInput(hoy);

    document.getElementById('btnConsultar').addEventListener('click', consultarEstadisticas);
});

function formatearFechaInput(fecha) {
    return fecha.toISOString().split('T')[0];
}

async function consultarEstadisticas() {
    const desde = document.getElementById('inputDesde').value;
    const hasta = document.getElementById('inputHasta').value;

    if (!desde || !hasta) {
        mostrarError('Completá ambas fechas para consultar.');
        return;
    }

    if (desde > hasta) {
        mostrarError("La fecha 'Desde' no puede ser mayor que 'Hasta'.");
        return;
    }

    mostrarLoading(true);
    ocultarError();
    ocultarResultados();
    mostrarEstado('');

    try {
        const params = new URLSearchParams({ desde, hasta });
        const response = await fetch(`${API_BASE}/estadisticas/ventas-por-rubro?${params}`, {
            headers: { 'Authorization': `Bearer ${token}` }
        });

        if (response.status === 401) {
            localStorage.clear();
            window.location.href = '/login.html';
            return;
        }

        if (!response.ok) throw new Error(`Error ${response.status}`);

        const datos = await response.json();

        if (!datos || datos.length === 0) {
            mostrarEstado('No hay ventas registradas en ese período para los rubros consultados.');
            return;
        }

        renderizarEstadisticas(datos);

    } catch (err) {
        mostrarError(`Error al obtener estadísticas: ${err.message}`);
    } finally {
        mostrarLoading(false);
    }
}

function renderizarEstadisticas(datos) {
    const total = datos.reduce((acc, d) => acc + d.totalVentas, 0);

    // Resumen total
    document.getElementById('resumenTotal').innerHTML =
        `Total del período: <strong>${formatearMoneda(total)}</strong>`;

    // Tabla detalle
    const tbody = document.getElementById('tbodyDetalle');
    tbody.innerHTML = datos.map(d => {
        const pct = total > 0 ? ((d.totalVentas / total) * 100).toFixed(1) : '0.0';
        const color = (COLORES_RUBRO[d.rubro] || COLOR_DEFAULT).border;
        return `
            <tr>
                <td>
                    <span class="punto-color" style="background:${color}"></span>
                    ${capitalizarRubro(d.rubro)}
                </td>
                <td class="text-right">${formatearMoneda(d.totalVentas)}</td>
                <td class="text-right"><strong>${pct}%</strong></td>
            </tr>
        `;
    }).join('');

    document.getElementById('tfootTotal').innerHTML = `
        <tr class="fila-total">
            <td><strong>TOTAL</strong></td>
            <td class="text-right"><strong>${formatearMoneda(total)}</strong></td>
            <td class="text-right"><strong>100%</strong></td>
        </tr>
    `;

    // Gráfico de torta
    const labels   = datos.map(d => capitalizarRubro(d.rubro));
    const valores  = datos.map(d => d.totalVentas);
    const bgColors = datos.map(d => (COLORES_RUBRO[d.rubro] || COLOR_DEFAULT).bg);
    const bdColors = datos.map(d => (COLORES_RUBRO[d.rubro] || COLOR_DEFAULT).border);

    if (graficoPie) graficoPie.destroy();

    const ctx = document.getElementById('graficoPie').getContext('2d');
    graficoPie = new Chart(ctx, {
        type: 'doughnut',
        data: {
            labels,
            datasets: [{
                data: valores,
                backgroundColor: bgColors,
                borderColor: bdColors,
                borderWidth: 2,
                hoverOffset: 10
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            plugins: {
                legend: {
                    position: 'bottom',
                    labels: { font: { size: 13 }, padding: 16 }
                },
                tooltip: {
                    callbacks: {
                        label: function (ctx) {
                            const val = ctx.parsed;
                            const pct = total > 0 ? ((val / total) * 100).toFixed(1) : '0.0';
                            return ` ${formatearMoneda(val)} (${pct}%)`;
                        }
                    }
                }
            }
        }
    });

    document.getElementById('resultados').style.display = 'block';
}

function capitalizarRubro(rubro) {
    if (!rubro) return '';
    return rubro.charAt(0).toUpperCase() + rubro.slice(1).toLowerCase();
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