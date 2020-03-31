function FastlySettingsController( $scope, $cookies ) {
    var vm = this;

    vm.title = "Settings";
    vm.PurgeOnPublishToggle = purgeOnPublishToggle;
    vm.saveSettings = saveSettings;
    vm.saveButtonClass = "btn-action";
    vm.saveButtonText = "Save Settings"

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

    function init() {
        vm.loading = true;
        getSettingsData();
    }

    function purgeOnPublishToggle() {
        if (vm.settings.PurgeOnPublish === true) {
            $cookies.remove("FASTLY-UMB-DEBUG", {
                path: "/"
            });
            vm.settings.PurgeOnPublish = false;
        }
        else {
            $cookies.put("FASTLY-UMB-DEBUG", "true", {
                path: "/",
                expires: "Tue, 01 Jan 2100 00:00:01 GMT"
            });
            vm.settings.PurgeOnPublish = true;
        }
    }

    function saveSettings() {
        $.ajax({
            type: "POST",
            url: "/umbraco/backoffice/FastlyUmbraco/FastlyAPI/SaveFastlySettings",
            dataType: 'text',
            contentType: 'json',
            data: JSON.stringify(vm.settings),
            success: function (data) {
                $scope.$apply(function () {
                    vm.saveButtonClass = "btn-success";
                    vm.saveButtonText = "Settings Saved!";
                });

                alert('Settings Saved');
            },
            error: function (xhr) {
                $scope.$apply(function () {
                    vm.saveButtonClass = "btn-danger";
                    vm.saveButtonText = "Error! Try Again";
                });
                
                alert('Error: Failed to save settings');
                console.log('Error: ', xhr.status + ' (' + xhr.statusText + ')');
            }
        });
    }

    function getSettingsData() {
        $.ajax({
            type: "GET",
            url: "/umbraco/backoffice/FastlyUmbraco/FastlyAPI/GetFastlySettings",
            dataType: 'json',
            success: function (data) {
                if (data) {
                    vm.loading = false;
                    $scope.$apply(function () {
                        vm.settings = data.fastlySettings;
                    });
                } else {
                    console.log('error: no data returned');
                }
            },
            error: function (xhr) {
                console.log('Error: ', xhr.status + ' (' + xhr.statusText + ')');
            }
        });
    }

    init();   
}
angular.module("umbraco").controller("FastlySettingsController", FastlySettingsController)
.directive('stringToNumber', function () {
    return {
        require: 'ngModel',
        link: function (scope, element, attrs, ngModel) {
            ngModel.$parsers.push(function (value) {
                return '' + value;
            });
            ngModel.$formatters.push(function (value) {
                return parseFloat(value);
            });
        }
    };
});