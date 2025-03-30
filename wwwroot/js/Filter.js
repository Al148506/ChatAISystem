    document.addEventListener("DOMContentLoaded", function () {
        const sortOrderSelect = document.querySelector('select[name="sortOrder"]');
    const alpOrderSelect = document.querySelector('select[name="alpOrder"]');

    function disableOtherFilter(changedSelect, otherSelect) {
            if (changedSelect.value) {
        otherSelect.value = ""; // Resetea el otro filtro
    otherSelect.setAttribute("disabled", "disabled");
            } else {
        otherSelect.removeAttribute("disabled");
            }
        }

    // Eventos para ambos select
    sortOrderSelect.addEventListener("change", function () {
        disableOtherFilter(sortOrderSelect, alpOrderSelect);
        });

    alpOrderSelect.addEventListener("change", function () {
        disableOtherFilter(alpOrderSelect, sortOrderSelect);
        });

    // Asegurar que solo un filtro esté habilitado al cargar la página
    if (sortOrderSelect.value) {
        alpOrderSelect.setAttribute("disabled", "disabled");
        }
    if (alpOrderSelect.value) {
        sortOrderSelect.setAttribute("disabled", "disabled");
        }
    });

