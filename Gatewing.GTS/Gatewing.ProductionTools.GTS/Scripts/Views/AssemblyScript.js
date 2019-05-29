app.controller('AssemblyCtrl', ['$scope', '$rootScope', '$http', '$mdDialog', '$mdMedia', '$mdToast', '$mdSidenav', '$mdDialog', '$location', '$q', 'assemblyService', 'dataFactory', '$sce', '$mdBottomSheet', '$timeout', 'filterFilter',
function ($scope, $rootScope, $http, $mdDialog, $mdMedia, $mdToast, $mdSidenav, $mdDialog, $location, $q, assemblyService, dataFactory, $sce, $mdBottomSheet, $timeout, filterFilter) {
    $scope.id = function ($scope, $location) {
        urlEnding = window.location.toString().split("/")[window.location.toString().split("/").length - 1];

        if (urlEnding.indexOf("?") > -1) {
            // split to check for QC indication
            splitUrlEnding = urlEnding.split("?");
            qcIndicator = splitUrlEnding[1];

            // verify indicator
            if (qcIndicator.indexOf("=") > -1) {
                $mdSidenav('left').close();
            }

            return splitUrlEnding[0];
        } else {
            return urlEnding;
        }
    }

    $scope.batchModeItems = [];

    $scope.InitAssembly = function () {
        $scope.$parent.dataLoaded = false;
        if ($scope.id().length === 36) {
            $scope.setScrollY(false);
            $scope.hideTitleBar();
        }

        $scope.getProductAssembly($scope.id()).then(function (response) {
            $scope.createThemeList();
            $scope.$parent.dataLoaded = true;
        })
    };

    $scope.togglePublicSerialInput = function () {
        $scope.displayPublicSerialInput = !$scope.displayPublicSerialInput;
    }

    $scope.togglePublicSerialHistory = function () {
        $scope.displayPublicSerialHistory = !$scope.displayPublicSerialHistory;
    }

    $scope.setPublicSerialFromInput = function (assyId, publicSerial) {
        assemblyService.setPublicSerial(assyId, publicSerial).then(function (response) {
            showToast(response.statusText, $mdToast);
            $scope.displayPublicSerialInput = false;
            $scope.InitAssembly();
        }, function (err) {
            showError(err, $scope);
        })
    }

    $scope.getProductAssembly = function (id) {
        var deferred = $q.defer();
        dataFactory.getProductAssembly($scope.id()).then(function (response) {
            var result = JSON.parse(response.data);
            $scope.assembly = result.Assembly;
            $scope.tooling = result.Assembly.Tooling;
            $scope.assembly.FormattedStartDate = new Date(result.Assembly.StartDate).toLocaleDateString();
            $scope.remarks = result.Remarks;
            $scope.grouped = result.Grouped;
            $scope.states = result.States;
            $scope.archivedPublicProductSerials = result.ArchivedPublicProductSerials;
            $rootScope.notes = result.Notes;

            deferred.resolve(true);
        }, function () {
            deferred.reject(false);
        });

        return deferred.promise;
    };

    $scope.applyConfig = function (assembly, config, doAdd) {
        $scope.showConfirm('Are you sure', 'Confirm your configuration (' + config.Name + ') selection, this cannot be undone', 'Confirm config selection', 'Select config', 'cancel', null).then(function () {
            assemblyService.addOrRemoveProductModelConfigurationFromAssembly(assembly.Id, config.Id, doAdd).then(function (response) {
                showToast(response.statusText, $mdToast);
                $scope.InitAssembly();
            }, function (err) {
                showError(err, $scope);
            })
        })
    }

    $scope.checkComponentAssemblyConfigurations = function (componentAssembly) {
        var result = false;

        if (componentAssembly.ProductModelConfigurations.map(function (e) { return e.Name; }).indexOf("default") > -1) {
            result = true;
        } else {
            componentAssembly.ProductModelConfigurations.forEach(function (element, index) {
                if ($scope.isInConfig($scope.assembly.SelectedProductModelConfigurations, element)) {
                    result = true;
                }
            });
        }

        return result;
    }

    $scope.currentWorkInstructionSummary = "";
    $scope.currentWorkInstructionImage = "";
    $scope.currentWorkInstructionDescription = "";

    $scope.pageLockedWhenInRemark = true;

    $scope.instructionIndex = 1;
    $scope.currentAssemblyId = '';

    $scope.isLocked = function (groupName, index) {
        if ($scope.pageLockedWhenInRemark == false) {
            return false;
        } else {
            var results = ($scope.assembly.ProductModelState.Name != groupName || index != $scope.assembly.CurrentStateIndex || $scope.remarks.length > 0)
            return results;
        }
    }

    $scope.displayWorkInstruction = function (component) {
        if ($scope.currentAssemblyId != component.Id) {
            $scope.instructionIndex = 1;
            $scope.instructions = component.WorkInstructions;
            $scope.currentAssemblyId = component.Id;
        }
    }

    $scope.previousInstruction = function () {
        if ($scope.instructionIndex > 1) {
            $scope.instructionIndex = parseInt($scope.instructionIndex) - 1;
            var id = "wi_" + $scope.instructionIndex;

            $scope.setInstruction($scope.instructionIndex);
            //$("#" + id).focus();
            //$("#mainAssemblyDiv").focus();
        }
    }

    $scope.nextInstruction = function () {
        if ($scope.instructionIndex < $scope.instructions.length) {
            $scope.instructionIndex = parseInt($scope.instructionIndex) + 1;
            var id = "wi_" + $scope.instructionIndex;

            $scope.setInstruction($scope.instructionIndex);
            //$("#" + id).focus();
            //$("#mainAssemblyDiv").focus();
        }
    }

    $scope.setInstruction = function (index) {
        $scope.instructionIndex = index;
    }

    $scope.persistAssemblyComponentChange = function (componentAssembly) {
        //alert(componentAssembly);
    }

    $scope.focusIndex = "1";

    /*
    $scope.$on('ngRepeatFinished', function (ngRepeatFinishedEvent) {
        alert("init finished");
        placeFocus($scope);
    });
    */

    $scope.updateIsRunning = false;
    $scope.maxRetryAttempts = 5;

    $scope.inputBlur = function (componentAssembly, doRevision, retryCount) {
        if (!retryCount) {
            retryCount = 1;
        }

        if (retryCount > $scope.maxRetryAttempts) {
            $scope.showAlert("Server busy", "Maximum retries performed, could not update field.", null);
            return;
        }

        if (!$scope.updateIsRunning) {
            $scope.updateIsRunning = true;
            if (doRevision) {
                if ($scope.batchModeItems && $scope.batchModeItems.length > 0) {
                    assemblyService.updateComponentAssemblies($scope.batchModeItems, componentAssembly).then(function (response) {
                        //$scope.handleUpdateAssemblyCall(response);
                    }, function (err) {
                        $scope.updateIsRunning = false;
                        $scope.handleUpdateAssemblyError(componentAssembly, err);
                    });
                }
            }

            var response = assemblyService.updateComponentAssembly(componentAssembly);
            response.then(function (response) {
                $scope.handleUpdateAssemblyCall(response, componentAssembly);
                $scope.updateIsRunning = false;
            }, function (err) {
                $scope.handleUpdateAssemblyError(componentAssembly, err);
                $scope.updateIsRunning = false;
            });
        } else {
            showToast("A component assembly update is still running, waiting 2 seconds to retry. Attempt " + retryCount + " / " + $scope.maxRetryAttempts, $mdToast);
            $timeout(function () {
                retryCount++;
                $scope.inputBlur(componentAssembly, doRevision, retryCount);
            }, 2000);
        }
    };

    $scope.inputBlurContainer = function (componentAssembly) {
    }

    $scope.handleUpdateAssemblyCall = function (response, componentAssembly) {
        var result = JSON.parse(response.data);
        $scope.assembly.Progress = result.Progress;
        componentAssembly.NHVersion = result.NHVersion;
        showToast("Assembly component updated!", $mdToast, "bottom right");
    };

    $scope.handleUpdateAssemblyError = function (componentAssembly, err) {
        var serial = componentAssembly.Serial;
        $("input").each(function (element, array, index) {
            if (document.getElementById(this.id).value == serial) {
                document.getElementById(this.id).value = "";
            }
        });
        handleError($scope, err.data);
    };

    $scope.theme = [];
    $scope.createThemeList = function () {
        $scope.theme = [];
        $scope.grouped.forEach(function callback(group, index, array) {
            group.components.forEach(function callback(component, index2, array2) {
                if (component.ProductModelState.Name === "Automation") {
                    $scope.theme.push('docsLight');
                } else {
                    $scope.theme.push('default');
                }
            });
        });
    };

    $scope.inputFocus = function (componentAssembly) {
        $scope.cycleInstructions = true;

        $scope.createThemeList();
        $scope.theme[componentAssembly.SequenceOrder] = "docsDark";

        $scope.displayWorkInstruction(componentAssembly, componentAssembly.Id);

        $scope.cycleInstructions = false;
    }

    $scope.inputFocusContainer = function (componentAssembly) {
        $scope.cycleInstructions = true;

        $scope.createThemeList();
        $scope.theme[componentAssembly.SequenceOrder] = "docsDark";

        $scope.displayWorkInstruction(componentAssembly, componentAssembly.Id);

        $scope.cycleInstructions = false;
    }

    $scope.placeFocusOnNextEmptyInputField = function () {
        showToast("Next empty input field selected", $mdToast);
        placeFocus(".assemblyInputField");
    }

    $scope.scrap = function (id) {
        $scope.$parent.isSaving = true;
        var response = assemblyService.scrap(id);
        response.then(function (result) {
            showToast(result.statusText, $mdToast);
            sleep(2000).then(() => {
                $scope.InitAssembly();
                $scope.$parent.isSaving = false;
            });
        }, function (err) {
            var el = document.createElement('html');
            el.innerHTML = err.data;
            var title = el.getElementsByTagName('title')[0].innerHTML; // Live NodeList of your anchor eleme
            showError(title, $scope);
            $scope.$parent.isSaving = false;
        });
    }

    $scope.deportToState = function (id, toNextState) {
        if ($scope.batchModeItems && $scope.batchModeItems.length > 0) {
            $scope.showConfirm("Batch deport?", "Are you sure you want to deport " + ($scope.batchModeItems.length + 1) + " assemblies to their " + (toNextState ? "next" : "previous") + " state?", "Confirm batch deport", "OK", "Cancel").then(function () {
                $scope.doDeportToState(id, toNextState);
            });
        } else {
            $scope.doDeportToState(id, toNextState);
        }
    }

    $scope.doDeportToState = function (id, toNextState) {
        $scope.$parent.isSaving = true;
        var response = assemblyService.deportToState(id, toNextState, $scope.batchModeItems);
        response.then(function (result) {
            showToast(result.statusText, $mdToast);
            sleep(2000).then(() => {
                $scope.getProductAssembly($scope.id()).then(function (response) {
                    if ($scope.assembly.ProductModelState.PreventBatchMode) {
                        $scope.batchModeItems = [];
                    }
                })

                $scope.$parent.isSaving = false;
            });
        }, function (err) {
            var el = document.createElement('html');
            el.innerHTML = err.data;
            var title = el.getElementsByTagName('title')[0].innerHTML; // Live NodeList of your anchor eleme
            showError(title, $scope);
            $scope.$parent.isSaving = false;
        });
    };

    $scope.deliver = function (id) {
        $scope.$parent.isSaving = true;
        var response = assemblyService.deliver(id);
        response.then(function (result) {
            showToast("Assembly set to 'delivered'", $mdToast);
            sleep(2000).then(() => {
                $scope.InitAssembly();
                $scope.$parent.isSaving = false;
            });
        }, function (err) {
            var el = document.createElement('html');
            el.innerHTML = err.data;
            var title = el.getElementsByTagName('title')[0].innerHTML; // Live NodeList of your anchor eleme
            showError(title, $scope);
            $scope.$parent.isSaving = false;
        });
    }

    $scope.putAssemblyInRemark = function (id) {
        var response = assemblyService.putAssemblyInRemark(id);
        response.then(function (result) {
            window.location.replace("/Remark/Remark/" + id);
        });
    }

    $scope.getRevisionName = function (productComponent) {
        return 'componentAssemblyRevision_' + productComponent.SequenceOrder
    };

    $scope.getSerialName = function (productComponent) {
        return 'componentAssemblySerial_' + productComponent.SequenceOrder
    };

    $scope.unScrap = function (id) {
        assemblyService.unScrap(id).then(function () {
            hideError($scope);
            showToast("Assembly is no longer scrapped.", $mdToast);
            sleep(2000).then(() => {
                $scope.InitAssembly();
                $scope.$parent.isSaving = false;
            });
        }, function (err) {
            showError(err.statusText, $scope);
        });
    };

    // doubled in _layout.cshtml
    $scope.doDeleteAssembly = function (ev, id, serial) {
        $scope.deleteAssembly(ev, id, serial).then(function () {
            $scope.initAssemblyList();
        }, function (err) {
            console.log(err);
        });
    };

    $scope.initAssemblyList = function () {
        $scope.$parent.dataLoaded = false;
        $scope.paginationReady = false;
        assemblyService.getAssemblies(($scope.startDate != null ? $scope.startDate.getFullYear() + "-" + ($scope.startDate.getMonth() + 1) + "-" + $scope.startDate.getDate() : ""), ($scope.endDate != null ? $scope.endDate.getFullYear() + "-" + ($scope.endDate.getMonth() + 1) + "-" + $scope.endDate.getDate() : ""), $scope.dataType, $scope.modelType).then(function (result) {
            hideError($scope);
            $scope.assemblies = JSON.parse(result.data);
            $scope.$parent.dataLoaded = true;
            showToast("Assemblies retrieved.", $mdToast);
        }, function (err) {
            showError(err.statusText, $scope);
        });
    }

    $scope.initData = function () {
        var startDate = new Date();
        startDate.setDate(startDate.getDate() - 7);
        $scope.startDate = startDate;

        var endDate = new Date();
        $scope.endDate = endDate;

        $scope.dataType = "all";

        $scope.modelType = 1;

        $scope.initAssemblyList();
    }

    $scope.onKeyDown = function ($event) {
        // left = 37
        // right = 39
        // pageUp = 33
        // pageDown = 34
        // N = 78
        // $event.altKey for ALT

        var key = (window.event ? $event.keyCode : $event.which);
        //console.log("Key pressed: " + key);

        if (key == 37 || key == 33) {
            $scope.previousInstruction();
        }
        if (key == 39 || key == 34) {
            $scope.nextInstruction();
        }

        if (key == 78) {
            if ($event.altKey) {
                $scope.deportToState($scope.assembly.Id, true);
            }
        }
    }

    $scope.navigateTo = function (url) {
        window.location.href = url;
    }

    $scope.showQCListDialog = function (productComponentId, assemblyId) {
        assemblyService.getQCsForBaseModelId(productComponentId, assemblyId).then(function (response) {
            componentAssemblies = JSON.parse(response.data);
            showDialog(null, true, 'qc-list-template.html', $scope, $mdMedia, $mdDialog, $mdToast, {
                selectedQCView: -1, componentAssemblies: componentAssemblies
            });
            //$scope.$parent.closeSidenav('left');
        });
    }

    $scope.showQCFormDialog = function (componentAssemblyId, baseModelId) {
        assemblyService.createQC(componentAssemblyId, baseModelId).then(function (response) {
            baseModelIdFromServer = $scope.asyncUrl + "Products/Assembly/" + JSON.parse(response.data) + "?qc=true";

            $scope.InitAssembly();
            $scope.iframe = {
                src: baseModelIdFromServer
            };
            $scope.address = $scope.trustSrc($scope.iframe.src);
            $scope.$parent.isSaving = false;
            showDialog(null, true, 'qc-template.html', $scope, $mdMedia, $mdDialog, $mdToast, $scope.address);
            //$scope.$parent.closeSidenav('left');
        }, function (err) {
            $scope.$parent.isSaving = false;
            showError(err.statusText, $scope);
        });
    }

    $scope.trustSrc = function (src) {
        return $sce.trustAsResourceUrl(src);
    }

    $scope.isInQCMode = function () {
        var qString = $location.search();
        if (qString["qc"]) {
            $scope.setSideNavHidden(true);
            return true;
        }
        //$scope.hideSideNav = false;
        return false;
    }

    $scope.closeMenuAndTitle = function () {
        if ($scope.isInQCMode()) {
            //$scope.$parent.closeSidenav('left');
            $scope.$parent.hideTitleBar();
        }
    }

    $scope.showNotesDialog = function () {
        showDialog(null, true, 'notes-template.html', $scope, $mdMedia, $mdDialog, $mdToast, $rootScope.notes);
    }

    $scope.addNote = function (text) {
        if (text == "") {
            return;
        }
        assemblyService.addNote($scope.id(), text).then(function (response) {
            result = JSON.parse(response.data);
            $rootScope.notes = result;
            //$rootScope.$apply();
            hideError($scope);
            showToast("Note added.", $mdToast);
            //showDialog(null, true, 'notes-template.html', $scope, $mdMedia, $mdDialog, $mdToast);
        }, function (errr) {
            showError(errr.statusText, $scope);
        });
    }

    $scope.unLockPage = function () {
        $scope.pageLockedWhenInRemark = !$scope.pageLockedWhenInRemark;

        if ($scope.pageLockedWhenInRemark == false) {
            $scope.addNote("An administrator unlocked the assembly to make adjustments");
        }

        $scope.setScrollY(!$scope.pageLockedWhenInRemark);
    }

    /*
    $scope.test = function (bool) {
        alert(bool);
    }
    */

    $scope.openMenu = function ($mdMenu, ev) {
        originatorEv = ev;
        $mdMenu.open(ev);
    }

    $scope.showBottomSheet = function () {
        $scope.focusField = true;
        $scope.batchModeWarnings = "";
        //alert("show bottom sheet");
        $mdBottomSheet.show({
            templateUrl: 'batchmode-template.html',
            controller: DialogController,
            locals: {
                batchModeItems: $scope.batchModeItems,
                focusField: $scope.focusField,
                batchModeWarnings: $scope.batchModeWarnings,
                VerifyBatchModeSerials: $scope.VerifyBatchModeSerials,
                assembly: $scope.assembly,
                grouped: $scope.grouped
            },
        }).then(function (clickedItem) {
        }).catch(function (error) {
            // User clicked outside or hit escape
            $scope.VerifyBatchModeSerials();
        });
    }

    $scope.VerifyBatchModeSerials = function () {
        if (!$scope.batchModeItems || $scope.batchModeItems.length === 0) {
            return;
        }

        $scope.batchModeBusy = true;
        var serials = JSON.stringify($scope.batchModeItems);
        assemblyService.validateBatchSerials($scope.assembly.ProductSerial, $scope.assembly.BaseModelId, serials).then(function (response) {
            if (response.statusText != "No serials passed") {
                var data = JSON.parse(response.data);
                $scope.batchModeItems = data;
                $scope.batchModeBusy = false;
                showCustomToast("Batch mode serials are validated.", $mdToast, "bottom right", "share", "accent", 5000);
            }
        }, function (errr) {
            handleError($scope, errr.data);
            $scope.batchModeItems = [];
            $scope.batchModeBusy = false;
        });
    }

    function DialogController($scope, $mdDialog, batchModeItems, focusField, batchModeWarnings, VerifyBatchModeSerials, assembly, grouped) {
        $scope.batchModeItems = batchModeItems;
        $scope.focusField = focusField;
        $scope.batchModeWarnings = batchModeWarnings;
        $scope.VerifyBatchModeSerials = VerifyBatchModeSerials;
        $scope.assembly = assembly;
        $scope.grouped = grouped;

        $scope.addSerial = function (chip) {
            var regEx = new RegExp($scope.assembly.IdMask);
            if (regEx.test(chip) == false) {
                $scope.batchModeWarnings = "Input doesn't match serial format (" + $scope.assembly.IdMask + ").";
                return null;
            } else {
                if (chip === $scope.assembly.ProductSerial) {
                    $scope.batchModeWarnings = "You've tried to enter a serial that 's the same as for the main assembly (serial found at the top of the page)";
                    return null;
                }

                $scope.batchModeWarnings = "";
            }

            newItem = newBatchModeItem(chip, $scope.grouped)

            return newItem;
        };
    }

    $scope.openMenu = function ($mdMenu, ev) {
        originatorEv = ev;
        $mdMenu.open(ev);
    }

    $scope.rollBackToState = function (assemblyId, stateIndex) {
        $scope.showConfirm("Confirm roll back", "Are you sure you want to roll back to a particular state and erase all data after that state?", "Confirm", "Yes", "No", null).then(function (data) {
            assemblyService.rollBackToState(assemblyId, stateIndex).then(function (response) {
                if (response.status === 200) {
                    $scope.InitAssembly();
                }
            }, function (errr) {
                showError(errr.statusText, $scope);
            });
        }, function (error) {
            showError(error.statusText, $scope);
        });
    }

    $scope.listItemClick = function ($index) {
        var clickedItem = $scope.batchModeItems[$index];
    };

    $scope.updateBatchSerial = function (assemblySerial, componentId, serialValue, pattern) {
        if (assemblySerial && componentId && pattern) {
            if (!serialValue) {
                serialValue = "";
            }

            var regex = new RegExp(pattern);
            if (regex.test(serialValue) || serialValue === "") {
                assemblyService.updateBatchSerial(assemblySerial, $scope.assembly.BaseModelId, componentId, serialValue).then(function (response) {
                    showToast("Batch mode serial updated", $mdToast);
                }, function (err) {
                    showError(err.statusText, $scope);
                    $scope.VerifyBatchModeSerials();
                })
            }
        }
    };

    $scope.getInputIndex = function (batchModeItem, componentId) {
        for (var i = 0; i < batchModeItem.serialInputs.length; i++) {
            if (batchModeItem.serialInputs[i].componentId === componentId) {
                return i;
            }
        }
    };

    $scope.getInputValueFromBatchModeItem = function (productComponentId, serial) {
        for (var i = 0; i < $scope.batchModeItems.length; i++) {
            if (serial === $scope.batchModeItems[i].serial) {
                if ($scope.batchModeItems[i].serialInputs) {
                    for (var c = 0; c < $scope.batchModeItems[i].serialInputs.length; c++) {
                        if (productComponentId === $scope.batchModeItems[i].serialInputs[c].componentId) {
                            return $scope.batchModeItems[i].serialInputs[c].value;
                        }
                    }
                }
            }
        }
    };
}]);

app.directive('onFinishRender', function ($timeout) {
    return function (scope, element, attrs) {
        angular.element(element).css('color', 'blue');
        if (scope.$last) {
            $timeout(function () {
                placeFocus();
            });
        }
    };
});

app.factory('dataFactory', ['$http', '$mdToast', 'assemblyService', function ($http, $mdToast, assemblyService) {
    var vm = this;

    vm.getProductAssembly = function (id) {
        return assemblyService.getProductAssembly(id);
    }

    vm.getAssemblies = function () {
        return assemblyService.getAssemblies();
    }

    return vm;
}]);

app.service("assemblyService", function ($http) {
    // get products
    this.getProductAssembly = function (id) {
        var response = $http
        ({
            method: "get",
            url: "/Products/GetProductAssemblyJson/" + id,
            dataType: "json"
        });
        return response;
    }

    this.updateComponentAssembly = function (component) {
        var response = $http
        ({
            method: "post",
            url: "/Products/UpdateComponentAssembly",
            dataType: "json",
            data: {
                objectAsString: JSON.stringify(component)
            }
        });
        return response;
    }

    this.updateComponentAssemblies = function (assemblySerials, componentAssembly) {
        var response = $http
        ({
            method: "post",
            url: "/Products/UpdateComponentAssemblies",
            dataType: "json",
            data: {
                objectAsString: JSON.stringify({ assemblySerials: assemblySerials, componentAssembly: componentAssembly })
            }
        });

        return response;
    }

    this.updateAssembly = function (assembly) {
        var response = $http
        ({
            method: "post",
            url: "/Products/UpdateAssembly",
            dataType: "json",
            data: {
                objectAsString: JSON.stringify(assembly)
            }
        });
        return response;
    }

    this.putAssemblyInRemark = function (id) {
        var response = $http
        ({
            method: "get",
            url: "/Remark/AddRemarkToAssembly/" + id,
            dataType: "json"
        });
        return response;
    }

    this.deportToState = function (id, toNextState, batchModeItems) {
        var response = $http
        ({
            method: "post",
            url: "/Products/DeportToState/",
            dataType: "json",
            data: {
                id: id, toNextState: toNextState, batchModeItems: JSON.stringify(batchModeItems)
            }
        });
        return response;
    }

    this.deliver = function (id) {
        var response = $http
        ({
            method: "get",
            url: "/Products/Deliver/",
            dataType: "json",
            params: {
                id: id
            }
        });
        return response;
    }

    this.scrap = function (id) {
        var response = $http
        ({
            method: "get",
            url: "/Products/Scrap/" + id,
            dataType: "json"
        });
        return response;
    }

    this.unScrap = function (id) {
        var response = $http
        ({
            method: "get",
            url: "/Products/UnScrap/" + id,
            dataType: "json"
        });
        return response;
    }

    this.getAssemblies = function (start, end, dataType, modelType) {
        var response = $http
        ({
            method: "get",
            url: "/Products/GetAssembliesJson",
            dataType: "json",
            params: {
                start: start, end: end, dataType: dataType, modelType: modelType
            }
        });
        return response;
    }

    this.addNote = function (id, text) {
        var response = $http
        ({
            method: "get",
            url: "/Products/AddNote/",
            dataType: "json",
            params: {
                assemblyId: id, text: text
            }
        });
        return response;
    }

    this.createQC = function (componentAssemblyId, qCModelIdq) {
        var response = $http
        ({
            method: "get",
            url: "/Products/CreateQCAssembly/",
            dataType: "json",
            params: {
                componentAssemblyId: componentAssemblyId, qCModelIdq: qCModelIdq
            }
        });
        return response;
    }

    this.getQCsForBaseModelId = function (productComponentId, productAssemblyId) {
        var response = $http
        ({
            method: "get",
            url: "/Products/GetQCsForBaseModelId/",
            dataType: "json",
            params: {
                productComponentId: productComponentId, productAssemblyId: productAssemblyId
            }
        });
        return response;
    }

    this.rollBackToState = function (assemblyId, stateIndex) {
        var response = $http
        ({
            method: "get",
            url: "/Products/RollBackToState/",
            dataType: "json",
            params: {
                assemblyId: assemblyId, stateIndex: stateIndex
            }
        });
        return response;
    }

    this.validateBatchSerials = function (mainProductSerial, baseModelId, serialsAsString) {
        var response = $http
           ({
               method: "post",
               url: "/Products/ValidateBatchSerials/",
               dataType: "json",
               data: {
                   mainProductSerial: mainProductSerial,
                   baseModelId: baseModelId,
                   serialsAsString: serialsAsString
               }
           });
        return response;
    }

    this.updateBatchSerial = function (productSerial, baseModelId, componentId, value) {
        var response = $http
               ({
                   method: "post",
                   url: "/Products/UpdateBatchSerial/",
                   dataType: "json",
                   data: {
                       productSerial: productSerial,
                       baseModelId: baseModelId,
                       componentId: componentId,
                       value: value
                   }
               });
        return response;
    }

    this.deleteAssembly = function (id) {
        var response = $http
        ({
            method: "get",
            url: "/Products/DeleteAssembly/" + id,
            dataType: "json"
        });
        return response;
    }

    this.getAssemblyThatReferencesThis = function (id) {
        var response = $http
        ({
            method: "get",
            url: "/Products/GetAssemblyThatReferencesThisJson/" + id,
            dataType: "json"
        });
        return response;
    }

    this.addOrRemoveProductModelConfigurationFromAssembly = function (assemblyId, configId, doAdd) {
        var response = $http
        ({
            method: "get",
            url: "/Products/AddOrRemoveProductModelConfigurationFromAssembly/",
            dataType: "json",
            params: { assemblyId: assemblyId, configId: configId, doAdd: doAdd }
        });
        return response;
        //AddOrRemoveProductModelConfigurationFromAssembly
    }

    this.setPublicSerial = function (assyId, publicSerial) {
        var response = $http
            ({
                method: "get",
                url: "/Products/SetPublicSerial/",
                dataType: "json",
                params: { assyId: assyId, publicSerial: publicSerial }
            });
        return response;
        //SetPublicSerial
    }
});

app.controller('ListBottomSheetCtrl', function ($scope, $mdBottomSheet) {
})

function newBatchModeItem(serial, grouped) {
    return {
        serial: serial,
        input: []
    };
};
/*
function placeFocus() {
    //alert("focus");
    $(".assemblyInputField").each(function (index, element) {
        if ($(this).val() == "") {
            setTimeout(function () {
                $(".assemblyInputField")[index].focus(); //$(".assemblyInputField")[index].blur(); $(".assemblyInputField")[index].focus();
            }, 50);
            return false;
        }
    });
}*/

function showDialog(ev, clickOutsideToClose, template, $scope, $mdMedia, $mdDialog, $mdToast, data) {
    var scope = $scope;
    var useFullScreen = ($mdMedia('sm') || $mdMedia('xs')) && $scope.customFullscreen;
    $mdDialog.show({
        controller: ['$scope', '$mdDialog', 'data', function ($scope, $mdDialog, data) {
            $scope.hide = function () {
                $mdDialog.hide();
            };
            $scope.cancel = function () {
                $mdDialog.cancel();
            };
            $scope.answer = function (answer) {
                $mdDialog.hide(answer);
            };

            $scope.data = data;
            $scope.addNote = scope.addNote;
        }],
        templateUrl: template,
        targetEvent: ev,
        clickOutsideToClose: false,
        fullscreen: useFullScreen,
        locals: {
            data: data
        }
    })
    .then(function (answer) {
        if (answer == "cancel") {
            $scope.status = 'You cancelled the dialog.';
        }
    }, function () {
        $scope.status = 'You cancelled the dialog.';
    });
    $scope.$watch(function () {
        return $mdMedia('xs') || $mdMedia('sm');
    }, function (wantsFullScreen) {
        $scope.customFullscreen = (wantsFullScreen === true);
    });
}