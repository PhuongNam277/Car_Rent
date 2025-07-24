@*
    For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860
*@
@{
        }


$(function () {
    // Add Role
    $("#btnAddRole").click(function () {
        $.get("/Admin/CreateRole", function (data) {
            $("#roleModalLabel").text("Add Role");
            $("#roleModalBody").html(data);
            $("#roleModal").modal("show");
        });
    });

    // Edit Role
    $(".btn-edit-role").click(function () {
        const id = $(this).data("id");
        $.get("/Admin/EditRole/" + id, function (data) {
            $("#roleModalLabel").text("Edit Role");
            $("#roleModalBody").html(data);
            $("#roleModal").modal("show");
        });
    });

    // Delete Role
    $(".btn-delete-role").click(function () {
        const id = $(this).data("id");
        $.get("/Admin/DeleteRole/" + id, function (data) {
            $("#roleModalLabel").text("Delete Role");
            $("#roleModalBody").html(data);
            $("#roleModal").modal("show");
        });
    });
});


