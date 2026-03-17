document.addEventListener('DOMContentLoaded', function () {
    const token = localStorage.getItem('auth_token');
    if (token) {
        validarTokenYRedirigir(token);
    }

    const form     = document.getElementById('loginForm');
    const btnLogin = document.getElementById('btnLogin');
    const errorDiv = document.getElementById('error');

    form.addEventListener('submit', async function (e) {
        e.preventDefault();

        const usuario = document.getElementById('usuario').value.trim();
        const clave   = document.getElementById('clave').value;

        if (!usuario || !clave) {
            mostrarError('Por favor complete todos los campos');
            return;
        }

        btnLogin.disabled     = true;
        btnLogin.textContent  = 'Iniciando sesión...';
        errorDiv.style.display = 'none';

        try {
            const response = await fetch('/api/auth/login', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ usuario, clave })
            });

            const data = await response.json();

            if (data.exito && data.token) {
                localStorage.setItem('auth_token',       data.token);
                localStorage.setItem('usuario_nombre',   data.usuario.nombreUsuario);
                localStorage.setItem('usuario_completo', data.usuario.nombreCompleto || '');
                localStorage.setItem('usuario_rol',      data.usuario.rol || '');

                // Redirigir según rol
                if ((data.usuario.rol || '').toLowerCase() === 'pizzeria') {
                    window.location.href = '/dashboard-pizzeria.html';
                } else {
                    window.location.href = '/dashboard.html';
                }
            } else {
                mostrarError(data.mensaje || 'Usuario o contraseña incorrectos');
            }
        } catch (error) {
            console.error('Error:', error);
            mostrarError('Error de conexión. Intente nuevamente.');
        } finally {
            btnLogin.disabled    = false;
            btnLogin.textContent = 'Iniciar Sesión';
        }
    });

    function mostrarError(mensaje) {
        errorDiv.textContent   = mensaje;
        errorDiv.style.display = 'block';
    }

    async function validarTokenYRedirigir(token) {
        try {
            const response = await fetch('/api/auth/validar', {
                headers: { 'Authorization': `Bearer ${token}` }
            });

            const data = await response.json();
            if (data.valido) {
                const rol = localStorage.getItem('usuario_rol') || '';
                if (rol.toLowerCase() === 'pizzeria') {
                    window.location.href = '/dashboard-pizzeria.html';
                } else {
                    window.location.href = '/dashboard.html';
                }
            } else {
                localStorage.clear();
            }
        } catch (error) {
            console.error('Error validando token:', error);
            localStorage.clear();
        }
    }
});