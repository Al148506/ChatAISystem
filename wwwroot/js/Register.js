document.getElementById("registerForm").addEventListener("submit", async function (event) {
    event.preventDefault(); // Evitar el envío tradicional

    limpiarErrores();

    const fields = ['Email', 'Username', 'Password', 'ConfirmPassword'].map(name => document.querySelector(`input[name="${name}"]`));
    const [emailField, usernameField, passwordField, confirmPasswordField] = fields;

    // Si algún campo está vacío, mostrar el mensaje general
    if (fields.some(field => !field.value.trim())) {
        mostrarErrorGeneral("Favor de completar todos los campos.");
        return;
    }

    // Si la contraseña tiene menos de 6 caracteres o no coinciden
    if (passwordField.value.length < 6 || passwordField.value !== confirmPasswordField.value) {
        mostrarErrorGeneral("Verifique la contraseña ingresada.");
        return;
    }

    const formData = new FormData(this);

    try {
        const response = await fetch('/Register/Create', {
            method: "POST",
            body: formData
        });
        // Clonar la respuesta para depuración
        const responseClone = response.clone(); 
        // Captura la respuesta en texto
        const text = await responseClone.text();
        console.log("Respuesta del servidor:", text); // 👈 Esto mostrará la respuesta en consola

        // Verifica si la respuesta es exitosa
        if (!response.ok) {
            console.error("Error en la solicitud:", response.status);
            mostrarErrorGeneral("Error en el servidor. Intente más tarde.");
            return;
        }

        const result = await response.json();

        if (result.success) {
            alert(result.message);
            window.location.href = result.redirectUrl;
        } else {
            mostrarErrorGeneral(result.message);
            if (typeof grecaptcha !== "undefined") {
                grecaptcha.reset();
            }
      
        }
    } catch (error) {
        console.error("Error de red:", error);
        mostrarErrorGeneral("Error en la conexión con el servidor.");
    }

});

// ✅ Funciones auxiliares
function limpiarErrores() {
    const errorMessage = document.querySelector(".errorMessage");
    errorMessage.textContent = "";
    errorMessage.classList.add("d-none");
}

function mostrarErrorGeneral(message) {
    const errorMessage = document.querySelector(".errorMessage");
    errorMessage.textContent = message;
    errorMessage.classList.remove("d-none");
}
