// js/login.js

document.getElementById('loginForm').addEventListener('submit', async (e) => {
    e.preventDefault();
    console.log("1. Formulario enviado...");
    
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
        console.log("2. Enviando petición al servidor...");
        const response = await fetch('http://localhost:5116/api/Auth/login', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ username, password })
        });

        console.log("3. Respuesta recibida. Status:", response.status);

        if (!response.ok) {
            throw new Error(`Error HTTP: ${response.status}. Verifique sus credenciales.`);
        }

        const data = await response.json();
        console.log("4. Datos recibidos:", data);

        // Guardar Token y datos del usuario
        localStorage.setItem('sitrac_token', data.token);
        localStorage.setItem('sitrac_user', JSON.stringify({ 
            nombre: data.nombreCompleto, 
            rol: data.rol 
        }));

        console.log("5. Token guardado. Redirigiendo a index.html...");
        // Redirigir a la consola principal
        window.location.href = 'index.html';

    } catch (err) {
        console.error('[Login] Error capturado:', err);
        errorDiv.textContent = err.message;
        alert("Error de login: " + err.message); // Alerta para asegurar que se vea
        btn.disabled = false;
        btn.textContent = 'Iniciar Sesión';
    }
});