import { SitracAPI } from './api.js';

document.getElementById('loginForm').addEventListener('submit', async (e) => {
    e.preventDefault();

    const username = document.getElementById('username').value.trim();
    const password = document.getElementById('password').value;
    const btn = document.getElementById('btnLogin');
    const errorDiv = document.getElementById('loginError');

    if (!username || !password) {
        errorDiv.textContent = 'Por favor, complete todos los campos.';
        return;
    }

    btn.disabled = true;
    btn.textContent = 'Autenticando...';
    errorDiv.textContent = '';

    try {
        const data = await SitracAPI.login(username, password);

        localStorage.setItem('sitrac_token', data.token);
        localStorage.setItem('sitrac_user', JSON.stringify({
            nombre: data.nombreCompleto,
            rol: data.rol
        }));

        window.location.href = 'index.html';
    } catch (err) {
        errorDiv.textContent = err.message;
        btn.disabled = false;
        btn.textContent = 'Iniciar Sesión';
    }
});