function CacheDurationDropdownEditorController($scope, editorState, contentResource) {
    var vm = this;

    vm.dropdownOptions = {
        "1 min": "60",
        "10 mins": "600",
        "30 mins": "1800",
        "1 hour": "3600",
        "2 hours": "7200",
        "3 hours": "10800",
        "1 day": "86400",
        "1 week": "604800"
    };
}
angular.module("umbraco").controller("CacheDurationDropdownEditorController", CacheDurationDropdownEditorController);