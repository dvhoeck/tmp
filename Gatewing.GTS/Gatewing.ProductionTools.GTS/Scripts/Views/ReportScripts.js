app.controller('ReportCtrl', ['$scope', '$http', '$mdDialog', '$mdMedia', '$mdToast', '$mdDialog', '$location', '$q', '$timeout', 'reportService', 'Excel', 'csv',
function ($scope, $http, $mdDialog, $mdMedia, $mdToast, $mdDialog, $location, $q, $timeout, reportService, Excel, csv) {
    $scope.clickedId = "";
    $scope.$parent.dataLoaded = true;
    $scope.includeRevision = true;
    $scope.includeSerial = true;
    $scope.onlyOpenRemarks = false;
    $scope.previousTabIndex = -1;
    $scope.componentNameFreeEntry = "";

    $scope.tabChange = function (index) {
        if (index != $scope.previousTabIndex) {
            $scope.resetData();
            $scope.previousTabIndex = index;
        }
    };

    $scope.collapse = function (id) {
        $scope.clickedId = id;
        var element = angular.element(document.querySelector('#' + id));
    };

    $scope.init = function () {
        $scope.$parent.dataLoaded = false;
        reportService.initReportingFilterData().then(function (response) {
            $scope.filterData = JSON.parse(response.data);
            $scope.$parent.dataLoaded = true;
        }, function (err) {
            showError(err, $scope);
            $scope.$parent.dataLoaded = true;
        })
    };

    $scope.selectedModelChange = function (model) {
        $scope.$parent.dataLoaded = false;
        $scope.ModelVersions = $scope.ProductComponents = $scope.selectedVersion = $scope.selectedComponent = null;
        if (model == "None") {
            $scope.$parent.dataLoaded = true;
        }
        else {
            reportService.initModelVersionData(model.BaseModelId).then(function (response) {
                $scope.ModelVersions = JSON.parse(response.data);
                $scope.$parent.dataLoaded = true;
            }, function (err) {
                $scope.$parent.dataLoaded = true;
                showError(err, $scope);
            })
        }
    };

    $scope.selectedVersionChange = function (version) {
        $scope.$parent.dataLoaded = false;
        $scope.ProductComponents = $scope.selectedComponent = null;
        if (version == "All") {
            $scope.$parent.dataLoaded = true;
        }
        else {
            reportService.initModelComponentNamesData(version.VersionNr, version.Id).then(function (response) {
                $scope.ProductComponents = JSON.parse(response.data);
                $scope.$parent.dataLoaded = true;
            }, function (err) {
                $scope.$parent.dataLoaded = true;
                showError(err, $scope);
            })
        }
    };

    $scope.selectedComponentChange = function (selectedComponent) {
        if (selectedComponent == "None") {
            $scope.selectedComponent = null;
            $scope.componentValue = "";
        } else {
            $scope.componentNameFreeEntry = null;
        }
    };

    $scope.componentNameFreeEntryChanged = function (componentNameFreeEntry) {
        $scope.selectedComponent = null;
        if (componentNameFreeEntry == "" || componentNameFreeEntry == null) {
            $scope.componentValue = "";
        }
    };

    $scope.exportToExcel = function (dataType) { // ex: '#my-table'
        var data = null;
        var warningMsg = "The system will export the data as a CSV. It's advised to open excel, go to the \"data\" ribbon or menu and choose import from text.";
        switch (dataType) {
            case 0:
                $scope.showAlert("Be advised", warningMsg, null);
                data = $scope.assemblies;
                csv.download(data, "Assemblies");
                break;
            case 1:
                $scope.showAlert("Be advised", warningMsg, null);
                data = $scope.assembliesContainingSerial;
                csv.download(data, "SerialUsedInAssemblies");
                break;
            case 2:
                data = $scope.remarkBetweenDates;
                $scope.showAlert("Be advised", warningMsg, null);
                csv.download(data, "RemarksInDateRange");
                break;
        }
    };

    $scope.refreshData = function (reportIndex) {
        $scope.$parent.dataLoaded = false;

        $scope.resetData();

        if (reportIndex == 0) {
            //$scope.getAssembliesBetweenDates();
            reportService.reportOnAssemblies($scope.selectedModel ? $scope.selectedModel.BaseModelId : null, $scope.selectedVersion ? $scope.selectedVersion.Id : null, $scope.selectedComponent ? $scope.selectedComponent.Id : null, $scope.componentNameFreeEntry, $scope.componentValue, $scope.filterSerial, $scope.filterData.StartDate, $scope.filterData.EndDate)
                .then(function (response) {
                    $scope.assemblies = JSON.parse(response.data);
                    $scope.$parent.dataLoaded = true;
                }, function (err) {
                    $scope.$parent.dataLoaded = true;
                    showError(err, $scope);
                });
        }

        if (reportIndex == 1) {
            $scope.getSerialUsageReport();
        }

        if (reportIndex == 2) {
            $scope.getRemarksBetweenDates();
        }
    };

    $scope.resetData = function () {
        $scope.assemblies = [];
        $scope.remarks = [];
    };

    $scope.getSerialUsageReport = function () {
        $scope.$parent.dataLoaded = false;
        reportService.getSerialUsage($scope.filterSerialUsage).then(function (response) {
            $scope.assembliesContainingSerial = JSON.parse(response.data);
            $scope.$parent.dataLoaded = true;
        }, function (err) {
            $scope.$parent.dataLoaded = true;
            showError(err, $scope);
        });
    };

    $scope.getRemarksBetweenDates = function () {
        $scope.$parent.dataLoaded = false;
        var response = reportService.getRemarksBetweenDates($scope.filterData.StartDate, $scope.filterData.EndDate, $scope.onlyOpenRemarks);
        response.then(function (result) {
            $scope.remarkBetweenDates = JSON.parse(result.data);
            showToast("Report retrieved.", $mdToast);
            $scope.$parent.dataLoaded = true;
        }, function (err) {
            $scope.$parent.dataLoaded = true;
            showError(err, $scope);
        });
    };
}
]);

app.factory('csv', function ($window) {
    return {
        download: function (data, fileNamePart) {
            var args = this.getDownloadArgs(data, fileNamePart);

            var data, filename, link;

            var csv = this.convertArrayOfObjectsToCSV(args);

            if (csv == null) return;

            fileName = args.fileName || 'export.csv';

            if (!csv.match(/^data:text\/csv/i)) {
                csv = 'data:text/csv;charset=utf-8,' + csv;
            }
            data = encodeURI(csv);

            link = document.createElement('a');
            link.setAttribute('href', data);
            link.setAttribute('download', fileName);
            link.click();
        },
        convertArrayOfObjectsToCSV: function (args) {
            var result, ctr, keys, columnDelimiter, lineDelimiter, data;

            data = args.data || null;
            if (data == null || !data.length) {
                return null;
            }

            columnDelimiter = args.columnDelimiter || ',';
            lineDelimiter = args.lineDelimiter || '\n';

            keys = Object.keys(data[0]);

            result = '';
            result += keys.join(columnDelimiter);
            result += lineDelimiter;

            data.forEach(function (item) {
                ctr = 0;
                keys.forEach(function (key) {
                    if (ctr > 0) result += columnDelimiter;

                    result += item[key];
                    ctr++;
                });
                result += lineDelimiter;
            });

            return result;
        },
        getDownloadArgs: function (data, fileNamePart) {
            var date = new Date();
            var fileName = date.getFullYear() + ("0" + (date.getMonth() + 1).toString()).slice(-2) + ("0" + date.getDate().toString()).slice(-2) + "_GTS_" + fileNamePart + ".csv";
            return {
                fileName: fileName,
                columnDelimiter: ";",
                lineDelimiter: "\n",
                data: data
            };
        }
    }
});

app.factory('Excel', function ($window) {
    var uri = 'data:application/vnd.ms-excel;base64,',
        template = '<html xmlns:o="urn:schemas-microsoft-com:office:office" xmlns:x="urn:schemas-microsoft-com:office:excel" xmlns="http://www.w3.org/TR/REC-html40"><head><!--[if gte mso 9]><xml><x:ExcelWorkbook><x:ExcelWorksheets><x:ExcelWorksheet><x:Name>{worksheet}</x:Name><x:WorksheetOptions><x:DisplayGridlines/></x:WorksheetOptions></x:ExcelWorksheet></x:ExcelWorksheets></x:ExcelWorkbook></xml><![endif]--></head><body><table>{table}</table></body></html>',
        templateMultiple = '<html xmlns:o="urn:schemas-microsoft-com:office:office" xmlns:x="urn:schemas-microsoft-com:office:excel" xmlns="http://www.w3.org/TR/REC-html40"><head><!--[if gte mso 9]><xml><x:ExcelWorkbook><x:ExcelWorksheets><x:ExcelWorksheet><x:Name>{worksheet}</x:Name><x:WorksheetOptions><x:DisplayGridlines/></x:WorksheetOptions></x:ExcelWorksheet></x:ExcelWorksheets></x:ExcelWorkbook></xml><![endif]--></head><body><table>{table1}</table><table>{table2}</table></body></html>',
        base64 = function (s) { return $window.btoa(unescape(encodeURIComponent(s))); },
        format = function (s, c) { return s.replace(/{(\w+)}/g, function (m, p) { return c[p]; }) };
    return {
        tableToExcel: function (tableId, worksheetName) {
            var table = $(tableId),
                ctx = { worksheet: worksheetName, table: table.html() },
                href = uri + base64(format(template, ctx));
            return href;
        },
        tableToExcelMultiple: function (tableId, tableId2, worksheetName) {
            var table = $(tableId),
                table2 = $(tableId2),
                ctxMultiple = { worksheet: worksheetName, table1: table.html(), worksheet2: worksheetName + " 2", table2: table2.html() },
                hrefMultiple = uri + base64(format(templateMultiple, ctxMultiple));
            return hrefMultiple;
        }
    };
});

app.service("reportService", function ($http) {
    this.getRemarksBetweenDates = function (start, end, onlyOpenRemarks) {
        var response = $http
        ({
            method: "post",
            url: "/Reports/GetRemarksBetweenDates",
            dataType: "json",
            data: { start: start, end: end, onlyOpenRemarks: onlyOpenRemarks }
        });
        return response;
    };

    this.initReportingFilterData = function () {
        var response = $http
            ({
                method: "get",
                url: "/Reports/InitReportingFilterData",
                dataType: "json",
            });
        return response;
    }

    this.initModelVersionData = function (baseModelId) {
        var response = $http
            ({
                method: "get",
                url: "/Reports/InitModelVersionData",
                dataType: "json",
                params: { baseModelId: baseModelId }
            });
        return response;
    }

    this.initModelComponentNamesData = function (versionNr, modelId) {
        var response = $http
            ({
                method: "get",
                url: "/Reports/InitModelComponentNamesData",
                dataType: "json",
                params: { versionNr: versionNr, modelId: modelId }
            });
        return response;
    }

    this.reportOnAssemblies = function (baseModelId, modelId, componentId, componentNameFreeInput, componentValue, productSerial, startDate, endDate) {
        var response = $http
            ({
                method: "get",
                url: "/Reports/ReportOnAssemblies",
                dataType: "json",
                params: { baseModelId: baseModelId, modelId: modelId, componentId: componentId, componentNameFreeInput: componentNameFreeInput, componentValue: componentValue, productSerial: productSerial, startDate: startDate, endDate: endDate }
            });
        return response;
    }

    this.getSerialUsage = function (serial) {
        var response = $http
        ({
            method: "get",
            url: "/Reports/GetSerialUsage",
            dataType: "json",
            params: { serial: serial }
        });
        return response;
    }
});