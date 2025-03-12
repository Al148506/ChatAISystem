document.addEventListener("DOMContentLoaded", function () {
    var modalEl = document.getElementById("genericModal");
    modalEl.addEventListener("show.bs.modal", function (event) {
        var button = event.relatedTarget;
        var type = button.getAttribute("data-type"); // Tipo de modal (delete/details)
        var name = button.getAttribute("data-name") || "";
        var description = button.getAttribute("data-description") || "";
        var id = button.getAttribute("data-id") || "";
        var avatar = button.getAttribute("data-avatar") || "";

        var title = "";
        var content = "";
        var footer = "";

        if (type === "delete") {
            title = "Confirm Delete";
            content = `
                <div class="p-3">
                    <h4 class="mb-4 text-danger"><i class="fas fa-exclamation-triangle me-2"></i>Warning</h4>
                    <p><strong>Character Name:</strong> ${name}</p>
                    <p><strong>Description:</strong> ${description.replace(/(\r\n|\n|\r)/gm, " ")}</p>
                    <div class="alert alert-warning mt-3">
                        <i class="fas fa-info-circle me-2"></i> This action cannot be undone. Are you sure you want to delete this character?
                    </div>
                </div>
            `;
            footer = `
                <form id="deleteForm" asp-action="Delete" method="post">
                    <input type="hidden" name="id" value="${id}" />
                    <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                    <button type="submit" class="btn btn-danger">Confirm Delete</button>
                </form>
            `;
        } else if (type === "details") {
            title = "Character Details";
            content = `
                <div class="container-fluid">
                    <div class="row">
                        <div class="col-md-4 text-center">
                            <img src="${avatar}" alt="Character Avatar" class="img-fluid rounded-circle mb-3" />
                            <h5 class="mt-3 mb-0">${name}</h5>
                        </div>
                        <div class="col-md-8">
                            <div class="card">
                                <div class="card-header text-white">
                                    <h4>Character Profile</h4>
                                </div>
                                <div class="card-body">
                                    <h6 class="card-subtitle mb-2 text-muted">Description</h6>
                                    <p class="card-text" style="white-space: pre-line;">${description}</p>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            `;
            footer = `<button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Close</button>`;
        }

        document.getElementById("modalTitle").innerHTML = title;
        document.getElementById("modalContent").innerHTML = content;
        document.getElementById("modalFooter").innerHTML = footer;
    });
});
