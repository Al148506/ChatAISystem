document.getElementById("registerForm").addEventListener("submit", async function (event) {
    event.preventDefault(); // Evitar el envío tradicional del formulario
    // Limpiar errores previos
    document.querySelectorAll(".errorMessage").forEach((error) => {
        error.textContent = "";
        error.classList.add("d-none");
    });
    // Obtener los campos del formulario
    const emailField = document.querySelector('input[name="Email"]');
    const usernameField = document.querySelector('input[name="Username"]');
    const passwordField = document.querySelector('input[name="Password"]');
    const confirmPasswordField = document.querySelector('input[name="ConfirmPassword"]');
    let hasError = false;

    // Validar correo
    if (!emailField.value.trim()) {
        showError(emailField, "Por favor, ingrese su correo electrónico.");
        hasError = true;
    } else if (!isValidEmail(emailField.value)) {
        showError(emailField, "El correo electrónico no tiene un formato válido.");
        hasError = true;
    }

    // Validar username
    if (!usernameField.value.trim()) {
        showError(usernameField, "Por favor, ingrese su nombre de usuario.");
        hasError = true;
    } 
    // Validar contraseña
    if (!passwordField.value.trim()) {
        showError(passwordField, "Por favor, ingrese su contraseña.");
        hasError = true;
    } else if (passwordField.value.trim().length < 6) {
        showError(passwordField, "La contraseña debe tener al menos 6 caracteres.");
        hasError = true;
    }

    // Validar confirmación de contraseña
    if (passwordField.value.trim() !== confirmPasswordField.value.trim()) {
        showError(confirmPasswordField, "Las contraseñas no coinciden.");
        hasError = true;
    }

    if (hasError) {
        return; // Detener envío si hay errores
    } 

    // Obtener los datos del formulario
    const formData = new FormData(this);

    try {
        // Realizar la solicitud AJAX
        const response = await fetch('/Register/Register', { // Asegúrate de que esta URL sea correcta
            method: "POST",
            body: formData
        });

        if (response.ok) {
            const result = await response.json();

            if (result.success) {
                alert(result.message); // Muestra mensaje de éxito
                window.location.href = result.redirectUrl;
            } else {
                const errorMessage = document.querySelector(".errorMessage");
                errorMessage.textContent = result.message;
                errorMessage.classList.remove("d-none");
            }
        } else {
            console.error("Error en la solicitud:", response.statusText);
        }
    } catch (error) {
        console.error("Error de red:", error);
    }
});
// Mostrar mensajes de error debajo de los campos
function showError(field, message) {
    // Buscar el contenedor de error asociado al campo de entrada
    const errorContainer = field.closest(".input-group").nextElementSibling;

    // Verificar si el contenedor existe y tiene la clase de error
    if (errorContainer && errorContainer.classList.contains("errorMessage")) {
        errorContainer.textContent = message; // Actualizar el mensaje de error
        errorContainer.classList.remove("d-none"); // Mostrar el mensaje
    }
}

function isValidEmail(email) {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
}
