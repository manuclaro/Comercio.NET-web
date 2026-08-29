const API = '/api';
let token = null;
let idEditando = null;
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
        `${localStorage.getItem('usuario_completo') || 'Usuario'}`;
    document.getElementById('btnCerrarSesion').addEventListener('click', () => { localStorage.clear(); window.location.href = '/login.html'; });
    document.getElementById('btnBuscar').addEventListener('click', cargar);
    document.getElementById('txtBuscar').addEventListener('keydown', e => { if (e.key === 'Enter') cargar(); });
    document.getElementById('txtBuscar').addEventListener('input', () => {
        clearTimeout(timerBuscar); timerBuscar = setTimeout(cargar, 500);
    });
    document.getElementById('btnNuevo').addEventListener('click', () => abrirModal(null));
    document.getElementById('btnCerrarModal').addEventListener('click', cerrarModal);
    document.getElementById('btnCancelar').addEventListener('click', cerrarModal);
    document.getElementById('btnGuardar').addEventListener('click', guardar);
    cargar();
});

async function cargar() {
    const buscar = document.getElementById('txtBuscar').value.trim();
    document.getElementById('loading').style.display = '';
    try {
        const r = await fetch(`${API}/proveedores?buscar=${encodeURIComponent(buscar)}`, { headers: { Authorization: `Bearer ${token}` } });
        if (!r.ok) throw new Error(await r.text());
        renderTabla(await r.json());
    } catch (e) { mostrarMsgGlobal('error', e.message); }
    finally { document.getElementById('loading').style.display = 'none'; }
}

function renderTabla(proveedores) {
    const tbody = document.getElementById('tbodyProveedores');
    tbody.innerHTML = '';
    if (!proveedores.length) {
        tbody.innerHTML = '<tr><td colspan="7" style="text-align:center;padding:2rem;color:#777;">No hay proveedores.</td></tr>';
        return;
    }
    for (const p of proveedores) {
        const tr = document.createElement('tr');
        tr.innerHTML = `
            <td><strong>${esc(p.nombre)}</strong></td>
            <td>${esc(p.cuit)}</td>
            <td>${esc(p.telefono)}</td>
            <td>${esc(p.email)}</td>
            <td>${esc(p.direccion)}</td>
            <td>${esc(p.contacto)}</td>
            <td class="text-center">
                <div class="row-actions" style="justify-content:center;">
                    <button class="btn-edit" onclick="abrirModal(${p.id})">Editar</button>
                    <button class="btn-danger" onclick="eliminar(${p.id},'${esc(p.nombre)}')">Eliminar</button>
                </div>
            </td>`;
        tbody.appendChild(tr);
    }
}

async function abrirModal(id) {
    idEditando = id;
    limpiarMsgModal();
    if (id) {
        document.getElementById('modalTitulo').textContent = 'Editar Proveedor';
        try {
            const r = await fetch(`${API}/proveedores/${id}`, { headers: { Authorization: `Bearer ${token}` } });
            const p = await r.json();
            document.getElementById('fNombre').value   = p.nombre;
            document.getElementById('fCuit').value     = p.cuit;
            document.getElementById('fTelefono').value = p.telefono;
            document.getElementById('fEmail').value    = p.email;
            document.getElementById('fDireccion').value= p.direccion;
            document.getElementById('fContacto').value = p.contacto;
        } catch (e) { mostrarMsgModal('error', e.message); }
    } else {
        document.getElementById('modalTitulo').textContent = 'Nuevo Proveedor';
        ['fNombre','fCuit','fTelefono','fEmail','fDireccion','fContacto'].forEach(id => document.getElementById(id).value = '');
    }
    document.getElementById('modalProveedor').style.display = 'flex';
    document.getElementById('fNombre').focus();
}

function cerrarModal() { document.getElementById('modalProveedor').style.display = 'none'; idEditando = null; }

async function guardar() {
    limpiarMsgModal();
    const nombre = document.getElementById('fNombre').value.trim();
    if (!nombre) { mostrarMsgModal('error', 'El nombre es requerido.'); return; }
    const body = {
        nombre,
        cuit:      document.getElementById('fCuit').value.trim(),
        telefono:  document.getElementById('fTelefono').value.trim(),
        email:     document.getElementById('fEmail').value.trim(),
        direccion: document.getElementById('fDireccion').value.trim(),
        contacto:  document.getElementById('fContacto').value.trim()
    };
    const btn = document.getElementById('btnGuardar');
    btn.disabled = true; btn.textContent = 'Guardando...';
    try {
        const url    = idEditando ? `${API}/proveedores/${idEditando}` : `${API}/proveedores`;
        const method = idEditando ? 'PUT' : 'POST';
        const r = await fetch(url, { method, headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` }, body: JSON.stringify(body) });
        const d = await r.json();
        if (!r.ok) throw new Error(d.error || 'Error');
        mostrarMsgModal('ok', d.mensaje);
        setTimeout(() => { cerrarModal(); cargar(); }, 900);
    } catch (e) { mostrarMsgModal('error', e.message); }
    finally { btn.disabled = false; btn.textContent = 'Guardar'; }
}

async function eliminar(id, nombre) {
    if (!confirm(`¿Dar de baja al proveedor "${nombre}"?`)) return;
    try {
        const r = await fetch(`${API}/proveedores/${id}`, { method: 'DELETE', headers: { Authorization: `Bearer ${token}` } });
        const d = await r.json();
        if (!r.ok) throw new Error(d.error);
        mostrarMsgGlobal('ok', d.mensaje);
        cargar();
    } catch (e) { mostrarMsgGlobal('error', e.message); }
}

function mostrarMsgGlobal(tipo, msg) {
    const el = document.getElementById('msgGlobal');
    el.className = tipo === 'ok' ? 'msg-ok' : 'msg-err';
    el.textContent = msg; el.style.display = '';
    setTimeout(() => { el.style.display = 'none'; }, 4000);
}
function mostrarMsgModal(tipo, msg) {
    const el = document.getElementById('modalMsg');
    el.className = 'full ' + (tipo === 'ok' ? 'msg-ok' : 'msg-err');
    el.textContent = msg; el.style.display = '';
}
function limpiarMsgModal() { const el = document.getElementById('modalMsg'); el.style.display = 'none'; }
function esc(s) { return String(s ?? '').replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;'); }
