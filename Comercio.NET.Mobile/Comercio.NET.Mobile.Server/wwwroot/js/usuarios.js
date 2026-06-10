const API = '/api';
let token = null;
let idEditando = null;
let idPwdCambio = null;
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
        `?? ${localStorage.getItem('usuario_completo') || 'Usuario'}`;
    document.getElementById('btnCerrarSesion').addEventListener('click', () => { localStorage.clear(); window.location.href = '/login.html'; });
    document.getElementById('btnBuscar').addEventListener('click', cargar);
    document.getElementById('txtBuscar').addEventListener('keydown', e => { if (e.key === 'Enter') cargar(); });
    document.getElementById('txtBuscar').addEventListener('input', () => { clearTimeout(timerBuscar); timerBuscar = setTimeout(cargar, 500); });
    document.getElementById('btnNuevo').addEventListener('click', () => abrirModal(null));
    document.getElementById('btnCerrarModal').addEventListener('click', cerrarModal);
    document.getElementById('btnCancelar').addEventListener('click', cerrarModal);
    document.getElementById('btnGuardar').addEventListener('click', guardar);
    document.getElementById('btnCerrarModalPwd').addEventListener('click', cerrarModalPwd);
    document.getElementById('btnCancelarPwd').addEventListener('click', cerrarModalPwd);
    document.getElementById('btnGuardarPwd').addEventListener('click', guardarPassword);
    cargar();
});

async function cargar() {
    const buscar = document.getElementById('txtBuscar').value.trim();
    document.getElementById('loading').style.display = '';
    try {
        const r = await fetch(`${API}/usuarios?buscar=${encodeURIComponent(buscar)}`, { headers: { Authorization: `Bearer ${token}` } });
        if (!r.ok) throw new Error(await r.text());
        renderTabla(await r.json());
    } catch (e) { mostrarMsgGlobal('error', e.message); }
    finally { document.getElementById('loading').style.display = 'none'; }
}

function renderTabla(usuarios) {
    const tbody = document.getElementById('tbodyUsuarios');
    tbody.innerHTML = '';
    if (!usuarios.length) {
        tbody.innerHTML = '<tr><td colspan="6" style="text-align:center;padding:2rem;color:#777;">No hay usuarios.</td></tr>';
        return;
    }
    for (const u of usuarios) {
        const rolBadge = u.rol === 'Admin' ? 'badge-admin' : u.rol === 'Supervisor' ? 'badge-supervisor' : 'badge-cajero';
        const tr = document.createElement('tr');
        tr.innerHTML = `
            <td><strong>${esc(u.nombreUsuario)}</strong></td>
            <td>${esc(u.nombre)} ${esc(u.apellido)}</td>
            <td><span class="badge ${rolBadge}">${esc(u.rol)} (N${u.nivel})</span></td>
            <td class="text-center">${u.numeroCajero}</td>
            <td class="text-center">${u.activo ? '<span class="badge badge-activo">Activo</span>' : '<span class="badge badge-inactivo">Inactivo</span>'}</td>
            <td class="text-center">
                <div class="row-actions">
                    <button class="btn-edit" onclick="abrirModal(${u.id})">?? Editar</button>
                    <button class="btn-pwd" onclick="abrirModalPwd(${u.id})">?? Pwd</button>
                    <button class="btn-toggle-activo" onclick="toggleActivo(${u.id},${u.activo})">${u.activo ? '?? Desact.' : '? Activar'}</button>
                </div>
            </td>`;
        tbody.appendChild(tr);
    }
}

async function abrirModal(id) {
    idEditando = id;
    limpiarMsgModal();
    document.getElementById('pwdHint').textContent = id ? '(dejar vacío para no cambiar)' : '(requerida para usuarios nuevos)';
    document.getElementById('fPassword').value = '';
    if (id) {
        document.getElementById('modalTitulo').textContent = '?? Editar Usuario';
        try {
            const r = await fetch(`${API}/usuarios/${id}`, { headers: { Authorization: `Bearer ${token}` } });
            const u = await r.json();
            document.getElementById('fUsuario').value  = u.nombreUsuario;
            document.getElementById('fNombre').value   = u.nombre;
            document.getElementById('fApellido').value = u.apellido;
            document.getElementById('fNivel').value    = String(u.nivel);
            document.getElementById('fCajero').value   = u.numeroCajero;
        } catch (e) { mostrarMsgModal('error', e.message); }
    } else {
        document.getElementById('modalTitulo').textContent = '? Nuevo Usuario';
        ['fUsuario','fNombre','fApellido'].forEach(id => document.getElementById(id).value = '');
        document.getElementById('fNivel').value  = '1';
        document.getElementById('fCajero').value = '1';
    }
    document.getElementById('modalUsuario').style.display = 'flex';
    document.getElementById('fUsuario').focus();
}

function cerrarModal() { document.getElementById('modalUsuario').style.display = 'none'; idEditando = null; }

async function guardar() {
    limpiarMsgModal();
    const nombreUsuario = document.getElementById('fUsuario').value.trim();
    if (!nombreUsuario) { mostrarMsgModal('error', 'El nombre de usuario es requerido.'); return; }
    const pwd = document.getElementById('fPassword').value;
    if (!idEditando && !pwd) { mostrarMsgModal('error', 'La contraseña es requerida para nuevos usuarios.'); return; }
    const body = {
        nombreUsuario,
        nombre:      document.getElementById('fNombre').value.trim(),
        apellido:    document.getElementById('fApellido').value.trim(),
        nivel:       parseInt(document.getElementById('fNivel').value),
        numeroCajero:parseInt(document.getElementById('fCajero').value) || 1,
        password:    pwd || null
    };
    const btn = document.getElementById('btnGuardar');
    btn.disabled = true; btn.textContent = 'Guardando...';
    try {
        const url    = idEditando ? `${API}/usuarios/${idEditando}` : `${API}/usuarios`;
        const method = idEditando ? 'PUT' : 'POST';
        const r = await fetch(url, { method, headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` }, body: JSON.stringify(body) });
        const d = await r.json();
        if (!r.ok) throw new Error(d.error || 'Error');
        mostrarMsgModal('ok', d.mensaje);
        setTimeout(() => { cerrarModal(); cargar(); }, 900);
    } catch (e) { mostrarMsgModal('error', e.message); }
    finally { btn.disabled = false; btn.textContent = '?? Guardar'; }
}

function abrirModalPwd(id) {
    idPwdCambio = id;
    document.getElementById('fNuevaPwd').value = '';
    document.getElementById('fConfirmPwd').value = '';
    limpiarMsgPwd();
    document.getElementById('modalPwd').style.display = 'flex';
    document.getElementById('fNuevaPwd').focus();
}

function cerrarModalPwd() { document.getElementById('modalPwd').style.display = 'none'; idPwdCambio = null; }

async function guardarPassword() {
    limpiarMsgPwd();
    const pwd1 = document.getElementById('fNuevaPwd').value;
    const pwd2 = document.getElementById('fConfirmPwd').value;
    if (!pwd1) { mostrarMsgPwd('error', 'La contraseña no puede estar vacía.'); return; }
    if (pwd1 !== pwd2) { mostrarMsgPwd('error', 'Las contraseñas no coinciden.'); return; }
    const btn = document.getElementById('btnGuardarPwd');
    btn.disabled = true; btn.textContent = 'Guardando...';
    try {
        const r = await fetch(`${API}/usuarios/${idPwdCambio}/password`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
            body: JSON.stringify({ nuevaPassword: pwd1 })
        });
        const d = await r.json();
        if (!r.ok) throw new Error(d.error || 'Error');
        mostrarMsgPwd('ok', d.mensaje);
        setTimeout(cerrarModalPwd, 900);
    } catch (e) { mostrarMsgPwd('error', e.message); }
    finally { btn.disabled = false; btn.textContent = '?? Cambiar'; }
}

async function toggleActivo(id, activoActual) {
    const nuevoEstado = !activoActual;
    if (!confirm(`¿${nuevoEstado ? 'Activar' : 'Desactivar'} este usuario?`)) return;
    try {
        const r = await fetch(`${API}/usuarios/${id}/toggle?activo=${nuevoEstado}`, { method: 'POST', headers: { Authorization: `Bearer ${token}` } });
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
function mostrarMsgPwd(tipo, msg) {
    const el = document.getElementById('modalPwdMsg');
    el.className = tipo === 'ok' ? 'msg-ok' : 'msg-err';
    el.textContent = msg; el.style.display = '';
}
function limpiarMsgPwd() { const el = document.getElementById('modalPwdMsg'); el.style.display = 'none'; }
function esc(s) { return String(s ?? '').replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;'); }
