app.controller('ShippingCtrl', ['$scope', '$http', '$mdDialog', '$mdMedia', '$mdToast', '$mdDialog', '$mdSidenav', '$location', '$q', '$timeout', "shippingService",
function ($scope, $http, $mdDialog, $mdMedia, $mdToast, $mdDialog, $mdSidenav, $location, $q, $timeout, shippingService) {
    $scope.boxNr = 0;

    $scope.toggleAddress = false;
    $scope.selectedBOM = "none";
    $scope.activeBOM = { bomNr: "none", isActive: false };
    $scope.groupingColorArray = ['green', 'red', 'blue', 'yellow', 'purple', 'orange'];
    $scope.selectedTabIndex = -1;

    $scope.displayBoxUi = false;
    $scope.displayBoxUiToggle = function () {
        $scope.displayBoxUi = !$scope.displayBoxUi;
    };

    $scope.boxesInUse = [];

    $scope.onTabChanges = function (tabIndex) {
        $scope.selectedTabIndex = tabIndex;
    }

    $scope.initShippings = function () {
        shippingService.getShippings().then(function (response) {
            $scope.shippingPreparation = JSON.parse(response.data);
            $scope.dataLoaded = true;
            showToast("Shippings retrieved.", $mdToast);
        });
    };

    $scope.InitShippingManagement = function () {
        $scope.$parent.dataLoaded = false;
        shippingService.getShippingManagementData().then(function (response) {
            var results = JSON.parse(response.data);
            $scope.shippings = results.ShippingPreparations;
            $scope.boxes = results.ShippingBoxes;
            $scope.partNumbers = results.PartNumbers;
            $scope.$parent.dataLoaded = true;
        }, function (err) {
            showError(err, $scope);
            $scope.$parent.dataLoaded = true;
        })
    };


    $scope.boxSelectionChanged = function (orderLine, boxNr) {
        alert(orderLineNr + " " +  boxNr)
    };

    $scope.getLocationStartLetter = function (location) {
        return location.substr(0, 1);
    };

    $scope.GetNrArray = function (number) {
        // we'll always have at least one element 
        if (number <= 0) {
            number = 1;
        }
        return new Array(parseInt(number));
    };

    $scope.UpdateOrderLine = function (orderLine) {
        if (orderLine.FieldInput && orderLine.FieldInput != "")  {
            shippingService.updateOrderLine(orderLine).then(function (response) {
                showToast("Orderline updated", $mdToast);
            }, function (err) {
                showError(err, $scope);
            });
        }
    };


    $scope.startStopShippingPreparation = function (doStart) {
        $scope.dataLoaded = false;
        shippingService.startStopShippingPreparation(doStart, $scope.selectedBOM).then(function (response) {
            var toastMsg = "Shipping with BOM #" + $scope.selectedBOM + " has " + (doStart ? "started" : "stopped");
            showToast(toastMsg, $mdToast);
            $scope.initShippings();
        });
    };

    $scope.stopAllPreparations = function () {
        $scope.showConfirm('Please confirm', 'Do you want to stop ALL active shipments?', 'Stop all?', 'STOP ALL', 'cancel').then(function (response) {
            $scope.shippingPreparation.BomList.forEach(function (element, index) {
                shippingService.startStopShippingPreparation(false, element.BomNr).then(function (response) {
                    var toastMsg = "Shipping with BOM #" + element.BomNr + " has stopped";
                    showToast(toastMsg, $mdToast);
                });
            });
            $scope.initShippings();
        });
    };

    $scope.filterOutInActive = function (bomList) {
        if (!bomList) {
            return null;
        }

        var newList = [];
        bomList.forEach(function (element, index) {
            if (element.IsActive) {
                newList.push(element);
            }
        });

        return newList;
    };
}]);

//app.directive('boxSelection', function () {
//    return {
//        replace: true,
//        template: '<'
//    };
//});

app.service("shippingService", function ($http) {
    this.getShippings = function () {
        var response = $http
        ({
            method: "get",
            url: "/Shipping/PrepareShippingJSON",
            dataType: "json"
        });
        return response;
    };

    this.startStopShippingPreparation = function (doStart, bomNr) {
        var response = $http
        ({
            method: "get",
            url: "/Shipping/PersistAndStartStopShippingPreparation/",
            dataType: "json",
            params: { doStart: doStart, bomNr: bomNr }
        });
        return response;
    };

    this.updateOrderLine = function (orderLine) {
        var response = $http
        ({
            method: "post",
            url: "/Shipping/UpdateOrderLine/",
            dataType: "json",
            params: { orderLineJSON: JSON.stringify(orderLine)}
        });
        return response;
    };

    ///InitShippingManagement

    this.getShippingManagementData = function () {
        var response = $http
        ({
            method: "get",
            url: "/Shipping/InitShippingManagement/",
            dataType: "json",
        });
        return response;
    };
});

app.directive('onFinishRender', function ($timeout) {
    return function (scope, element, attrs) {
        if (scope.$last) {
            $timeout(function () {
                placeFocus("input");
            });
        }
    };
});

app.directive('ngRightClick', function ($parse) {
    return function (scope, element, attrs) {
        var fn = $parse(attrs.ngRightClick);
        element.bind('contextmenu', function (event) {
            scope.$apply(function () {
                event.preventDefault();
                fn(scope, { $event: event });
            });
        });
    };
});

app.directive('iconShippingStock', function () {
    return {
        replace: true,
        template: '<md-icon md-font-set="fa fa-fw fa-box" alt="stock" md-colors="{color: \'accent-800\'}"></md-icon>'
    };
});

app.directive('iconShippingPallet', function () {
    return {
        replace: true,
        template: '<md-icon md-font-set="fa fa-fw fa-pallet" title="pallet" md-colors="{color: \'accent-600\'}"></md-icon>'
    };
});

app.directive('iconShippingShipping', function () {
    return {
        replace: true,
        template: '<md-icon md-font-set="fa fa-fw fa-shipping-fast" title="Shipping zone" md-colors="{color: \'accent-400\'}"></md-icon>'
    };
});

app.directive('iconShippingDocument', function () {
    return {
        replace: true,
        template: '<md-icon md-font-set="fa fa-fw fa-file-invoice" title="document" md-colors="{color: \'accent-200\'}"></md-icon>'
    };
});

app.directive('iconShippingFloor', function () {
    return {
        replace: true,
        template: '<md-icon md-font-set="fa fa-fw fa-warehouse" title="Floor" md-colors="{color: \'accent-200\'}"></md-icon>'
    };
});

app.directive('shippingActivated', function () {
    return {
        replace: true,
        scope: {
            active: '='
        },
        template: '<div style="margin-top: 0px;" class="md-caption {{active ? \'chip\' : \'chip inActiveChip\'}}">{{active ? "ACTIVE": "INACTIVE"}}</div>'
    };
});