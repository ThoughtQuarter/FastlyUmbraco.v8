angular.module("umbraco").controller("FastlyOverviewController", FastlyOverviewController);

function FastlyOverviewController($scope, $window) {
    var vm = this;

    vm.title = "Fastly Umbraco";
    vm.urlInput = "";
    vm.purgeurl = purgeUrl;
    vm.purgeall = purgeAll;

    Chart.defaults.global.responsive = true;
    Chart.defaults.global.elements.point.radius = 0;
    Chart.defaults.global.elements.point.hitRadius = 4;
    Chart.defaults.global.tooltips.enabled = true;
    Chart.defaults.global.animation.duration = 0;
    Chart.defaults.global.maintainAspectRatio = false;

    var hitPercentChart;
    var requestsChart;
    var httpErrorChart;
    var errorRatioChart;

    var requestsData;
    
    function GetHitPercentageChart() {
        var ctx = document.getElementById('hitPercentChart');

        hitPercentChart = new Chart(ctx, {
            type: 'line',
            data: {
                datasets: [
                    {
                        label: 'Hit Ratio Average',
                        fill: false,
                        borderColor: "#7c7c7c",
                        borderDash: [10, 10],
                        data: []
                    },
                    {
                        label: 'Hit Ratio',
                        backgroundColor: '#d4eab8',
                        borderColor: "#62a240",
                        data: []
                    }                    
                ]
            },
            options: {
                scales: {
                    xAxes: [
                        {
                            id: 'x-axis-0',
                            type: 'time',
                            distribution: 'series',
                            time: {
                                unit: 'day',
                                displayFormats: {
                                    day: 'MMM DD'
                                }
                            },
                            gridLines: {
                                display: false
                            }
                        }
                    ],
                    yAxes: [
                        {
                            id: 'y-axis-0',
                            type: 'linear',
                            gridLines: {
                                display: false
                            },
                            ticks: {
                                min: 0,
                                max: 1,
                                stepSize: .2,
                                callback: function (value) { return value * 100 + "%" }
                            }
                        }
                    ]
                }
            }
        });

        hitPercentChart.canvas.parentNode.style.height = '190px';

        $.ajax({
            type: "POST",
            url: "/umbraco/backoffice/FastlyUmbraco/FastlyAPI/GetFastlyStats",
            dataType: 'json',
            data: JSON.stringify({
                "fieldName": "hit_ratio",
                "byFormat": "hour",
                "fromUTC": "",
                "toUTC": ""
            }),
            success: function (data) {
                //console.log('succes: ', data);
                var i;
                var hitRatioSum = 0;
                for (i = 0; i < data.data.length; i++) {
                    hitPercentChart.data.datasets[1].data.push({ x: data.data[i].start_time * 1000, y: data.data[i].hit_ratio });
                    hitRatioSum += data.data[i].hit_ratio;
                }

                let avg = hitRatioSum / hitPercentChart.data.datasets[1].data.length;

                hitPercentChart.data.datasets[0].data = [{ x: hitPercentChart.data.datasets[1].data[0].x, y: avg }, { x: hitPercentChart.data.datasets[1].data[i - 1].x, y: avg }];
                vm.hitRatioAvg = (avg * 100).toFixed(2) + "%";

                hitPercentChart.update();
                $scope.$apply();
            },
            error: function (xhr) {
                console.log('Error: ', xhr.status + ' (' + xhr.statusText + ')');
            }
        });        
    }

    function GetRequestsChart() {
        var ctx = document.getElementById('requestsChart');

        requestsChart = new Chart(ctx, {
            type: 'line',
            data: {
                datasets: [
                    {
                        label: 'Requests Average',
                        fill: false,
                        borderColor: "#7c7c7c",
                        borderDash: [10, 10],
                        data: []
                    },
                    {
                        label: 'Requests',
                        backgroundColor: '#ceecf9',
                        borderColor: "#1b658f",
                        data: []
                    }
                ]
            },
            options: {
                scales: {
                    xAxes: [
                        {
                            id: 'x-axis-0',
                            type: 'time',
                            distribution: 'series',
                            time: {
                                unit: 'day',
                                displayFormats: {
                                    day: 'MMM DD'
                                }
                            },
                            gridLines: {
                                display: false
                            }
                        }
                    ],
                    yAxes: [
                        {
                            id: 'y-axis-0',
                            type: 'linear',
                            gridLines: {
                                display: false
                            },
                            ticks: {
                                min: 0,
                                stepSize: 1000,
                                callback: function (value) { return nFormatter(value, 1) }
                            }
                        }
                    ]
                }
            }
        });

        requestsChart.canvas.parentNode.style.height = '190px';

        $.ajax({
            type: "POST",
            url: "/umbraco/backoffice/FastlyUmbraco/FastlyAPI/GetFastlyStats",
            dataType: 'json',
            data: JSON.stringify({
                "fieldName": "requests",
                "byFormat": "hour",
                "fromUTC": "",
                "toUTC": ""
            }),
            success: function (data) {
                //console.log('succes requests: ', data);
                var requestSum = 0;
                var i;
                for (i = 0; i < data.data.length; i++) {
                    requestsChart.data.datasets[1].data.push({ x: data.data[i].start_time * 1000, y: data.data[i].requests });
                    requestSum += data.data[i].requests;
                }
                requestsData = requestsChart.data.datasets[1].data;
                let avg = requestSum / requestsData.length;

                requestsChart.data.datasets[0].data = [{ x: requestsData[0].x, y: avg }, { x: requestsData[i - 1].x, y: avg }];
                vm.requestAvg = nFormatter(avg, 1);

                requestsChart.update();
                $scope.$apply();

                GetErrorRatioChart();
            },
            error: function (xhr) {
                console.log('Error: ', xhr.status + ' (' + xhr.statusText + ')');
            }
        });
    }

    function SetupErrorRatioChart() {
        var ctx = document.getElementById('errorRatioChart');

        errorRatioChart = new Chart(ctx, {
            type: 'line',
            data: {
                datasets: [
                    {
                        label: '4XX Errors',
                        backgroundColor: '#f6ccda',
                        borderColor: "#e882a4",
                        data: []
                    },
                    {
                        label: '503 Errors',
                        backgroundColor: '#c22423',
                        borderColor: "#d66c6c",
                        data: []
                    },
                    {
                        label: 'Other 5XX Errors',
                        backgroundColor: '#fd8a8d',
                        borderColor: "#f05454",
                        data: []
                    }
                ]
            },
            options: {
                scales: {
                    xAxes: [
                        {
                            id: 'x-axis-0',
                            type: 'time',
                            distribution: 'series',
                            time: {
                                unit: 'day',
                                displayFormats: {
                                    day: 'MMM DD'
                                }
                            },
                            gridLines: {
                                display: false
                            }
                        }
                    ],
                    yAxes: [
                        {
                            id: 'y-axis-0',
                            type: 'linear',
                            gridLines: {
                                display: false
                            },
                            ticks: {
                                min: 0,
                                callback: function (value) { return Math.round(value * 100) + "%" }
                            }
                        }
                    ]
                },
                legend: {
                    display: true,
                    labels: {
                        boxWidth: 20
                    }
                }
            }
        });

        errorRatioChart.canvas.parentNode.style.height = '190px';
    }

    function GetErrorRatioChart() {
        
        $.ajax({
            type: "POST",
            url: "/umbraco/backoffice/FastlyUmbraco/FastlyAPI/GetFastlyStats",
            dataType: 'json',
            data: JSON.stringify({
                "fieldName": "status_4xx",
                "byFormat": "hour",
                "fromUTC": "",
                "toUTC": ""
            }),
            success: function (data) {
                //console.log('requests: ', requestsData);
                //console.log('succes status_4xx: ', data);
                for (var i = 0; i < data.data.length; i++) {
                    errorRatioChart.data.datasets[0].data.push({ x: data.data[i].start_time * 1000, y: data.data[i].status_4xx / requestsData[i].y });
                    httpErrorChart.data.datasets[0].data.push({ x: data.data[i].start_time * 1000, y: data.data[i].status_4xx });
                }
                errorRatioChart.update();
                httpErrorChart.update();
            },
            error: function (xhr) {
                console.log('Error: ', xhr.status + ' (' + xhr.statusText + ')');
            }
        });

        $.ajax({
            type: "POST",
            url: "/umbraco/backoffice/FastlyUmbraco/FastlyAPI/GetFastlyStats",
            dataType: 'json',
            data: JSON.stringify({
                "fieldName": "status_503",
                "byFormat": "hour",
                "fromUTC": "",
                "toUTC": ""
            }),
            success: function (data) {
                //console.log('succes status_503: ', data);
                for (var i = 0; i < data.data.length; i++) {
                    errorRatioChart.data.datasets[1].data.push({ x: data.data[i].start_time * 1000, y: data.data[i].status_503 / requestsData[i].y });
                    httpErrorChart.data.datasets[1].data.push({ x: data.data[i].start_time * 1000, y: data.data[i].status_503 });
                }
                errorRatioChart.update();
                httpErrorChart.update();
            },
            error: function (xhr) {
                console.log('Error: ', xhr.status + ' (' + xhr.statusText + ')');
            }
        });

        $.ajax({
            type: "POST",
            url: "/umbraco/backoffice/FastlyUmbraco/FastlyAPI/GetFastlyStats",
            dataType: 'json',
            data: JSON.stringify({
                "fieldName": "status_5xx",
                "byFormat": "hour",
                "fromUTC": "",
                "toUTC": ""
            }),
            success: function (data) {
                //console.log('succes status_5xx: ', data);
                for (var i = 0; i < data.data.length; i++) {
                    errorRatioChart.data.datasets[2].data.push({ x: data.data[i].start_time * 1000, y: data.data[i].status_5xx / requestsData[i].y });
                    httpErrorChart.data.datasets[2].data.push({ x: data.data[i].start_time * 1000, y: data.data[i].status_5xx });
                }
                //console.log('data: ', hitPercentChart.data.datasets[0].data);
                errorRatioChart.update();
                httpErrorChart.update();
            },
            error: function (xhr) {
                console.log('Error: ', xhr.status + ' (' + xhr.statusText + ')');
            }
        });
    }

    function SetHttpClientServerErrorChart() {
        var ctx = document.getElementById('httpErrorsChart');

        httpErrorChart = new Chart(ctx, {
            type: 'line',
            data: {
                datasets: [
                    {
                        label: 'Client 4XX',
                        backgroundColor: '#f6ccda',
                        borderColor: "#e882a4",
                        data: []
                    },
                    {
                        label: '503',
                        backgroundColor: '#c22423',
                        borderColor: "#d66c6c",
                        data: []
                    },
                    {
                        label: 'Other 5XX',
                        backgroundColor: '#fd8a8d',
                        borderColor: "#f05454",
                        data: []
                    }
                ]
            },
            options: {
                scales: {
                    xAxes: [
                        {
                            id: 'x-axis-0',
                            type: 'time',
                            distribution: 'series',
                            time: {
                                unit: 'day',
                                displayFormats: {
                                    day: 'MMM DD'
                                }
                            },
                            gridLines: {
                                display: false
                            }
                        }
                    ],
                    yAxes: [
                        {
                            id: 'y-axis-0',
                            type: 'linear',
                            gridLines: {
                                display: false
                            },
                            ticks: {
                                min: 0,
                                stepSize: 1000,
                                callback: function (value) { return nFormatter(value, 1) }
                            }
                        }
                    ]
                },
                legend: {
                    display: true,
                    labels: {
                        boxWidth: 20
                    }
                }
            }
        });

        httpErrorChart.canvas.parentNode.style.height = '190px';
    }

    function purgeUrl() {

        if (isNullOrWhitespace(vm.urlInput) === false) {
            $.ajax({
                type: "POST",
                url: "/umbraco/backoffice/FastlyUmbraco/FastlyAPI/PurgeURLAsync",
                data: vm.urlInput,
                success: function (data) {
                    alert("Purged Url");
                },
                error: function (xhr) {
                    console.log('Error: ', xhr.status + ' (' + xhr.statusText + ')');
                }
            });
        } else {
            alert("Empty URL Field");
        }
    }

    function purgeAll() {
        $.ajax({
            type: "POST",
            url: "/umbraco/backoffice/FastlyUmbraco/FastlyAPI/PurgeAll",
            success: function (data) {
                alert("Purged All");
            },
            error: function (xhr) {
                console.log('Error: ', xhr.status + ' (' + xhr.statusText + ')');
            }
        });
    }

    function nFormatter(num, digits) {
        var si = [
            { value: 1, symbol: "" },
            { value: 1E3, symbol: "k" },
            { value: 1E6, symbol: "M" },
            { value: 1E9, symbol: "G" },
            { value: 1E12, symbol: "T" },
            { value: 1E15, symbol: "P" },
            { value: 1E18, symbol: "E" }
        ];
        var rx = /\.0+$|(\.[0-9]*[1-9])0+$/;
        var i;
        for (i = si.length - 1; i > 0; i--) {
            if (num >= si[i].value) {
                break;
            }
        }
        return (num / si[i].value).toFixed(digits).replace(rx, "$1") + si[i].symbol;
    }

    function loadCharts() {
        GetRequestsChart();
        GetHitPercentageChart();
        SetupErrorRatioChart();
        SetHttpClientServerErrorChart();
        vm.loading = false;
    }

    function isNullOrWhitespace(input) {

        if (typeof input === 'undefined' || input == null) return true;

        return input.replace(/\s/g, '').length < 1;
    }

    function init() {
        vm.loading = true;
    }

    angular.element(document).ready(function () {
        loadCharts();
    });

    init();
}