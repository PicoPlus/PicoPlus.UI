// wwwroot/js/interop.js

function initializeSelect2() {
    $('.select').select2();
}

function getSelectedValue(selectId) {
    var selectElement = document.getElementById(selectId);
    var selectedValue = selectElement.value;
    console.log('Selected value:', selectedValue);
    return selectedValue;
}


function ShowToast(title, msg, Icon) {
    Swal.fire({
        icon: Icon,
        title: title,
        toast: true,
        text: msg,
        showConfirmButton: false,
        timer: 3000,
        timerProgressBar: true,
    });
}


window.modalInterop = {
    showModal: function (id) {
        var modalElement = document.getElementById(id);
        var modal = new bootstrap.Modal(modalElement);
        modal.show();
    },
    hideModal: function (id) {
        var modalElement = document.getElementById(id);
        var modal = bootstrap.Modal.getInstance(modalElement);
        if (modal) {
            modal.hide();
        }
    }
};


