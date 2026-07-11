window.interop = {
    initializeSelect2: function () {
        if ($('.select2').length) {
            $('.select2').select2();
            console.log("Elements with class 'pSelect' found for Select2 initialization.");
        } else {
            console.log("Elements with class 'pSelect' not found for Select2 initialization.");
        }
    },

    addSelect2ChangeListener: function () {
        const mySelectElement = document.getElementById(elementId);

        if (mySelectElement) {
            mySelectElement.addEventListener("change", function (event) {
                const selectedValue = event.target.value;
                console.log("Selected value:", selectedValue);

                // Notify Blazor about the change
                DotNet.invokeMethodAsync('YourAppNamespace', 'OnPipelineChanged', selectedValue);
            });
        } else {
            console.error("Element with ID '" + elementId + "' not found.");
        }
    }
}
