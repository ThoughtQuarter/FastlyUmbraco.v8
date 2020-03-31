function PurgeButtonEditorController($scope, editorState, contentResource ) {
    var vm = this;

    vm.purgePage = purgePage;

    function purgePage() {
        $scope.editorState = editorState;
        vm.data = $scope.editorState.current.id.toString();

        $.ajax({
            type: "POST",
            url: "/umbraco/backoffice/FastlyUmbraco/FastlyAPI/PurgeURLByIDAsync",
            data: vm.data,
            //timeout: 10000, // 10 seconds for getting result, otherwise error.
            success: function (data) {
                alert("Page Purged");
            },
            error: function (xhr) {
                console.log('Error: ', xhr.status + ' (' + xhr.statusText + ')');
            }
        });
    }

    function init() {
    
    }

    init();
}
angular.module("umbraco").controller("PurgeButtonEditorController", PurgeButtonEditorController);