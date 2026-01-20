document.getElementById("loginForm").addEventListener("submit", async function (event) {
    event.preventDefault();

    const loadingOverlay = document.getElementById("loading");
    const errorMessage = document.querySelector(".errorMessage");

    // ✅ Validar captcha ANTES de enviar
    const captchaResponse = grecaptcha.getResponse();
    if (!captchaResponse) {
        showError("Please, solve the reCAPTCHA to continue.");
        return;
    }

    hideError();
    loadingOverlay.classList.remove("d-none");

    const formData = new FormData(this);

    try {
        const response = await fetch('/Login/ValidateLogin', {
            method: "POST",
            body: formData
        });

        const contentType = response.headers.get("content-type");

        if (!response.ok) {
            throw new Error("Error in the request");
        }

        if (contentType && contentType.includes("application/json")) {
            const result = await response.json();

            if (result.success) {
                window.location.href = result.redirectUrl;
            } else {
                showError(result.message);

                // 🔁 Reset obligatorio
                grecaptcha.reset();
            }
        }
    } catch (error) {
        console.error("Error:", error);
        showError("Network error, please try again");
        grecaptcha.reset();
    } finally {
        // ✅ OCULTAR, no eliminar
        loadingOverlay.classList.add("d-none");
    }
});

function showError(message) {
    const errorMessage = document.querySelector(".errorMessage");
    errorMessage.textContent = message;
    errorMessage.classList.remove("d-none");
}

function hideError() {
    const errorMessage = document.querySelector(".errorMessage");
    errorMessage.textContent = "";
    errorMessage.classList.add("d-none");
}
