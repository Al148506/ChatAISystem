document.getElementById("registerForm").addEventListener("submit", async function (event) {
    event.preventDefault(); // Evitar el envío tradicional

    limpiarErrores();

    const emailField = document.querySelector('input[name="Email"]');
    const usernameField = document.querySelector('input[name="Username"]');
    const passwordField = document.querySelector('input[name="Password"]');
    const confirmPasswordField = document.querySelector('input[name="ConfirmPassword"]');

    // Si algún campo está vacío, mostrar el mensaje general
    if (!emailField.value.trim() || !usernameField.value.trim() || !passwordField.value.trim() || !confirmPasswordField.value.trim()) {
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
        const response = await fetch('/Register/Register', {
            method: "POST",
            body: formData
        });

        const result = await response.json();
        if (result.success) {
            alert(result.message);
            window.location.href = result.redirectUrl;
        } else {
            mostrarErrorGeneral(result.message);
        }
    } catch (error) {
        console.error("Error de red:", error);
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
