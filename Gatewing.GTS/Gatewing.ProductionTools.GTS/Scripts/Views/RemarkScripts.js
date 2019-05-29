app.controller('RemarkCtrl', ['$scope', '$http', '$mdDialog', '$mdMedia', '$mdToast', '$mdDialog', '$location', '$q', '$mdExpansionPanel', 'remarkService', 'dataFactory',
    function ($scope, $http, $mdDialog, $mdMedia, $mdToast, $mdDialog, $location, $q, $mdExpansionPanel, remarkService, dataFactory) {
        $scope.status = '  ';

        //$scope.id = function ($scope, $location) {
        $scope.id = function () {
            return window.location.toString().split("/")[window.location.toString().split("/").length - 1]
        }
        $scope.remarkSymptoms = [];

        $scope.uploadImages = function (entity, newImages) {
            var formData = new FormData();
            angular.forEach(newImages, function (obj) {
                if (!obj.isRemote) {
                    formData.append('files[]', obj.lfFile);
                }
            });
            var uploadResponse = remarkService.uploadImage(formData);
            return uploadResponse.then(function (result) {
                hideError($scope);
                showToast(result.statusText + " uploaded to server.", $mdToast);
                images = result.data;
                if (entity.Images) {
                    entity.Images = entity.Images.concat(images);
                } else {
                    entity.Images = images;
                }
            }, function (err) {
                showDialogError(err, $scope);
            });
        };

        // testing
        $scope.addRemark = function (ev) {
            // get assembly id from URL and add remark for assembly serverside, returns fullremark collection
            var response = remarkService.addRemarkToAssembly($scope.id());
            response.then(function (result) {
                $scope.remarkSymptoms = result.data;
                ParseRemarksAndHandleDates($scope, result.data);
                showToast("Added remark symptom to remark workflow.", $mdToast);
            });
        }

        $scope.addCauseToSymptom = function (id) {
            var response = remarkService.addCauseToSymptom(id);
            response.then(function (result) {
                $scope.remarkSymptoms.forEach(function (element, index, array) {
                    if (element.Id == id) {
                        result.data.CauseDate = new Date(parseInt(result.data.CauseDate.substr(6)));
                        if (element.RemarkSymptomCauses) {
                            element.RemarkSymptomCauses.push(result.data);
                        } else { // create array upon adding first element
                            element.RemarkSymptomCauses = [result.data];
                        }
                        showToast("Added cause to remark symptom.", $mdToast);
                    }
                });
            });
        }

        $scope.addSolutionToCause = function (id) {
            var response = remarkService.addSolutionToCause(id);
            response.then(function (result) {
                $scope.remarkSymptoms.forEach(function (element, index, array) {
                    element.RemarkSymptomCauses.forEach(function (element, index, array) {
                        if (element.Id == id) {
                            result.data.SolutionDate = new Date(parseInt(result.data.SolutionDate.substr(6)));
                            element.RemarkSymptomSolution = result.data;
                            showToast("Added solution to remark cause.", $mdToast);
                        }
                    });
                });
            });
        }

        $scope.addCauseAndSolutionPairForRemarkWithId = function (id, panelId) {
            //alert(panelId);
            var response = remarkService.AddCauseAndSolutionToRemarkJson($scope.id(), id);
            response.then(function (data) {
                //alert(data);
                ParseRemarksAndHandleDates($scope, data.data);

                $mdExpansionPanel().waitFor(panelId).then(function (instance) {
                    instance.expand();
                });
            });
        }

        $scope.deleteSymptom = function (id) {
            //alert(panelId);
            var response = remarkService.deleteRemarkSymptom(id);
            response.then(function (data) {
                if (data.status == 200) {
                    showToast("Archived remark symptom.", $mdToast);
                    var i = -1;
                    $scope.remarkSymptoms.forEach(function (element, index, arrau) {
                        if (element.Id == id) {
                            i = index;
                        }
                    });
                    $scope.remarkSymptoms.splice(i, 1);
                } else {
                    showToast("An error occured.", $mdToast);
                }
            });
        }

        $scope.willReopen = function (event, causeId, symptom) {
            var willReopen = false;
            if (symptom.Resolved) {
                if (symptom.RemarkSymptomCauses.length == 1) {
                    willReopen = true;
                } else {
                    var counter = 0;
                    for (var i = 0; i < symptom.RemarkSymptomCauses.length; i++) {
                        var cause = symptom.RemarkSymptomCauses[i];
                        if (i.Id !== causeId) {
                            if (cause.RemarkSymptomSolution && cause.RemarkSymptomSolution.Successful) {
                                counter++;
                            }
                        }
                    }
                    if (counter == 1) {
                        willReopen = true;
                    }
                }
            }
            if (willReopen) {
                var confirm = $mdDialog.confirm()
                    .title('Edit cause/solution.')
                    .textContent('This action will reopen the remark.')
                    .ariaLabel('Edit cause/solution')
                    .ok('Reopen remark')
                    .cancel('Cancel');

                return $mdDialog.show(confirm).then(function () {
                    symptom.Resolved = false;
                    return Promise.resolve(true);
                });
            } else {
                return Promise.resolve();
            }
        }

        $scope.toggleSolutionSuccess = function (event, id, cause, symptom) {
            if (!cause.RemarkSymptomSolution.Successful) {
                // reset value
                cause.RemarkSymptomSolution.Successful = !cause.RemarkSymptomSolution.Successful;

                $scope.willReopen(event, id, symptom).then(function (saveNeeded) {
                    cause.RemarkSymptomSolution.Successful = false;
                });
            }
        }

        $scope.deleteCause = function (event, id, symptom) {
            $scope.willReopen(event, id, symptom).then(function (saveNeeded) {
                var response = remarkService.deleteRemarkCause(id, symptom);
                response.then(function (data) {
                    if (data.status == 200) {
                        showToast("Archived remark symptom cause.", $mdToast);
                        var i = -1;
                        $scope.remarkSymptoms.forEach(function (remarkSymptom, index, array) {
                            remarkSymptom.RemarkSymptomCauses.forEach(function (remarkSymptomCause, index2, array2) {
                                if (remarkSymptomCause.Id == id) {
                                    i = index2;
                                }
                            });
                            remarkSymptom.RemarkSymptomCauses.splice(i, 1);
                        });

                        if (saveNeeded) {
                            $scope.saveRemarks();
                        }
                    } else {
                        showToast("An error occured.", $mdToast);
                    }
                });
            });
        }

        $scope.deleteSolution = function (event, id, cause, symptom) {
            $scope.willReopen(event, cause.Id, symptom).then(function (saveNeeded) {
                var response = remarkService.deleteRemarkSolution(id);
                response.then(function (data) {
                    if (data.status == 200) {
                        showToast("Archived remark symptom cause.", $mdToast);
                        var i = -1;
                        $scope.remarkSymptoms.forEach(function (element, index, array) {
                            element.RemarkSymptomCauses.forEach(function (element2, index2, array2) {
                                if (element2.RemarkSymptomSolution) {
                                    if (element2.RemarkSymptomSolution.Id == id) {
                                        element2.RemarkSymptomSolution = null;
                                    }
                                }
                            });
                        });

                        if (saveNeeded) {
                            $scope.saveRemarks();
                        }
                    } else {
                        showToast("An error occured.", $mdToast);
                    }
                });
            });
        }

        $scope.initStatesAndTypes = function (tabIndex) {
            // Retrieve data
            dataFactory.getModelStates().then(function (response) {
                $scope.modelStates = response.data;
            });
            dataFactory.getRemarkTypes().then(function (response) {
                $scope.remarkTypes = response.data;
            });
            dataFactory.getRemarkCauseTypes().then(function (response) {
                $scope.causeTypes = response.data;
            });
            dataFactory.getRemarkSolutionTypes().then(function (response) {
                $scope.solutionTypes = response.data;
            });

            $scope.selectedTabIndex = tabIndex;

            showToast("States and types retrieved.", $mdToast);
        }

        // load add / edit data
        $scope.addEditModelState = function (ev, id, name, description, preventBatchMode) {
            $scope.modelState = { Name: name, Id: id, Description: description, PreventBatchMode: preventBatchMode };
            dialogShowAddEdit(ev, true, 'modelstate-template.html', $scope, $mdMedia, $mdDialog, $mdToast, remarkService);
        }
        $scope.addEditRemarkType = function (ev, id, name, description) {
            $scope.remarkType = { Name: name, Id: id, Description: description };
            dialogShowAddEdit(ev, true, 'remarkType-template.html', $scope, $mdMedia, $mdDialog, $mdToast, remarkService);
        }
        $scope.addEditCauseType = function (ev, id, name, description) {
            $scope.causeType = { Name: name, Id: id, Description: description };
            dialogShowAddEdit(ev, true, 'causeType-template.html', $scope, $mdMedia, $mdDialog, $mdToast, remarkService);
        }
        $scope.addEditSolutionType = function (ev, id, name, description) {
            $scope.solutionType = { Name: name, Id: id, Description: description };
            dialogShowAddEdit(ev, true, 'solutionType-template.html', $scope, $mdMedia, $mdDialog, $mdToast, remarkService);
        }

        // Save data
        $scope.saveProductModelState = function () {
            var response = remarkService.saveProductModelState($scope.modelState);
            response.then(function (data) {
                hideError($scope);
                showToast(data.statusText, $mdToast);
                $scope.initStatesAndTypes(0);
                $mdDialog.hide();
            }, function (errr) {
                showDialogError(errr, $scope);
            });
        }
        $scope.saveRemarkSymptomType = function () {
            //alert("saveRemarkSymptomType");
            var response = remarkService.saveRemarkType($scope.remarkType);
            response.then(function (data) {
                hideError($scope);
                showToast(data.statusText, $mdToast);
                $scope.initStatesAndTypes(1);
                $mdDialog.hide();
            }, function (errr) {
                showDialogError(errr, $scope);
            });
        }
        $scope.saveRemarkCauseType = function () {
            //alert("saveRemarkCauseType'");
            var response = remarkService.saveRemarkCauseType($scope.causeType);
            response.then(function (data) {
                hideError($scope);
                showToast(data.statusText, $mdToast);
                $scope.initStatesAndTypes(2);
                $mdDialog.hide();
            }, function (errr) {
                showDialogError(errr, $scope);
            });
        }
        $scope.saveRemarkSolutionType = function () {
            //alert("saveRemarkSolutionType'");
            var response = remarkService.saveRemarkSolutionType($scope.solutionType);
            response.then(function (data) {
                hideError($scope);
                showToast(data.statusText, $mdToast);
                $scope.initStatesAndTypes(3);
                $mdDialog.hide();
            }, function (errr) {
                showDialogError(errr, $scope);
            });
        }

        // remove data
        $scope.deleteModelState = function (ev, id, name) {
            var confirm = $mdDialog.confirm()
                  .title('Would you like to delete this product model state?')
                  .textContent('Click delete to archive model state "' + name + '". This cannot be undone.')
                  .ariaLabel('Delete model state')
                  .targetEvent(ev)
                  .ok('Delete!')
                  .cancel('Cancel');

            $mdDialog.show(confirm).then(function () {
                var response = remarkService.deleteModelState(id);
                response.then(function (data) {
                    //hideError($scope);
                    showToast("Product model state deleted", $mdToast);
                    sleep(2000).then(() => {
                        window.location.reload();
                    });
                }, function (err) {
                    showError(err, $scope);
                });
            }, function () {
                showError("Delete canceled.", $scope);
            });
        }
        $scope.deleteRemarkType = function (ev, id, name) {
            var confirm = $mdDialog.confirm()
                  .title('Would you like to delete this remark symptom type?')
                  .textContent('Click delete to archive remark symptom type: "' + name + '". This cannot be undone.')
                  .ariaLabel('Delete remark symptom type')
                  .targetEvent(ev)
                  .ok('Delete!')
                  .cancel('Cancel');

            $mdDialog.show(confirm).then(function () {
                var response = remarkService.deleteRemarkType(id);
                response.then(function (data) {
                    //hideError($scope);
                    showToast("Remark symptom type deleted", $mdToast);
                    sleep(2000).then(() => {
                        window.location.reload();
                    });
                }, function (err) {
                    showError(err, $scope);
                });
            }, function () {
                showError("Delete canceled.", $scope);
            });
        }
        $scope.deleteCauseType = function (ev, id, name) {
            var confirm = $mdDialog.confirm()
                  .title('Would you like to delete this remark cause type?')
                  .textContent('Click delete to archive remark cause type: "' + name + '". This cannot be undone.')
                  .ariaLabel('Delete remark cause type')
                  .targetEvent(ev)
                  .ok('Delete!')
                  .cancel('Cancel');

            $mdDialog.show(confirm).then(function () {
                var response = remarkService.deleteRemarkCauseType(id);
                response.then(function (data) {
                    //hideError($scope);
                    showToast("Remark cause type deleted", $mdToast);
                    sleep(2000).then(() => {
                        window.location.reload();
                    });
                }, function (err) {
                    showError(err, $scope);
                });
            }, function () {
                showError("Delete canceled.", $scope);
            });
        }
        $scope.deleteSolutionType = function (ev, id, name) {
            var confirm = $mdDialog.confirm()
                  .title('Would you like to delete this remark solution type?')
                  .textContent('Click delete to archive remark solution type: "' + name + '". This cannot be undone.')
                  .ariaLabel('Delete remark solution type')
                  .targetEvent(ev)
                  .ok('Delete!')
                  .cancel('Cancel');

            $mdDialog.show(confirm).then(function () {
                var response = remarkService.deleteRemarkSolutionType(id);
                response.then(function (data) {
                    //hideError($scope);
                    showToast("Remark cause solution deleted", $mdToast);
                    sleep(2000).then(() => {
                        window.location.reload();
                    });
                }, function (err) {
                    showError(err, $scope);
                });
            }, function () {
                showError("Delete canceled.", $scope);
            });
        }

        $scope.saveRemarks = function () {
            var response = remarkService.saveRemarks($scope.remarkSymptoms);
            response.then(function (result) {
                if (result.status == 200) {
                    showToast("Remarks, causes and solutions saved to database.", $mdToast);
                }
            }, function (error) {
                /*var el = document.createElement('html');
                el.innerHTML = error.data;
                var title = el.getElementsByTagName('title')[0].innerHTML; // Live NodeList of your anchor eleme*/
                showError(error, $scope);
                $scope.initRemarks();
            });
        }

        $scope.initRemarkHistory = function () {
            dataFactory.DisplayRemarkHistoryJson($scope.id()).then(function (response) {
                $scope.remarkSymptoms = JSON.parse(response.data);
                ParseRemarksAndHandleDates($scope, response.data);
            });

            dataFactory.getStats($scope.id()).then(function (result) {
                $scope.stats = result.data;
                //alert($scope.stats.TotalTime);
            });
        };

        $scope.initRemarkList = function () {
            $scope.$parent.dataLoaded = false;
            dataFactory.getRemarkList().then(function (result) {
                $scope.$parent.dataLoaded = true;
                $scope.remarks = JSON.parse(result.data);
                $scope.remarks.forEach(function (remark, index, array) {
                    //var tmp3 = new Date(remark.CreationDate);
                    remark.CreationDate = new Date(remark.CreationDate);
                });
            });
        }

        $scope.initRemarks = function () {
            dataFactory.getSymptomTypes().then(function (response) {
                $scope.remarkTypes = response.data;
            });

            dataFactory.getCauseTypes().then(function (response) {
                $scope.causeTypes = response.data;
            });

            dataFactory.getSolutionTypes().then(function (response) {
                $scope.solutionTypes = response.data;
            });

            dataFactory.DisplayRemarkJson($scope.id()).then(function (response) {
                $scope.remarkSymptoms = response.data.results;
                if ($scope.remarkSymptoms.length > 0) {
                    ParseRemarksAndHandleDates($scope, response.data);
                }
            });

            showToast("Remarks retrieved.", $mdToast);
        };
        
        $scope.loadCauseForm = function () {
            //alert("start form load");
        }

        $scope.removeImage = function (entity, index) {
            entity.Images.splice(index, 1);
        }

        /*
        $scope.closeErrorContainer = function () {
            hideError($scope);
        }
        */

        $scope.searchText = null;

        $scope.querySearchReference = function (query) {
            dataFactory.searchComponentAssemblies(query)
                .then(function (response) {
                    $scope.data = response.data;
                    return response.data;
                });
        }

        $scope.causeComponentAssemblyChanged = function (componentAssembly, cause) {
            if (componentAssembly && componentAssembly.ComponentSerial && !cause.RemarkSymptomSolution.PreviousComponentSerial) {
                cause.RemarkSymptomSolution.PreviousComponentSerial = componentAssembly.ComponentSerial
            }
        };



        $scope.doPrint = function () {
            window.print();
        };

    }]).config(function ($mdThemingProvider) {
        // Configure a dark theme with primary foreground yellow
        $mdThemingProvider.theme('docsDark', 'default')
          .primaryPalette('deep-orange')
            .dark();
        $mdThemingProvider.theme('docsLight')
            .primaryPalette('deep-orange');
    });

function ParseRemarksAndHandleDates($scope, data) {
    $scope.ProductAssemblyId = $scope.id();
    //$scope.remarkSymptoms = data;
    $scope.remarkSymptoms.forEach(function (element, index, array) {
        $scope.AssemblyName = element.ProductSerial + " (" + element.ProductName + ")";
        $scope.ProductAssemblyId = element.ProductAssemblyId;
        element.CreationDate = new Date(parseInt(element.CreationDate.substr(6)));
        if (element.RemarkSymptomCauses) {
            element.RemarkSymptomCauses.forEach(function (causeElement, causeIndex, causeArray) {
                causeElement.CauseDate = new Date(parseInt(causeElement.CauseDate.substr(6)));
                if (causeElement.RemarkSymptomSolution) {
                    causeElement.RemarkSymptomSolution.SolutionDate = new Date(parseInt(causeElement.RemarkSymptomSolution.SolutionDate.substr(6)));
                }
            });
        }
    });
}

app.controller('RemarkChildCtrl', ['$scope', 'Lightbox',
    function ($scope, Lightbox) {
        // had to seperate these because solution is nested within cause
        $scope.symptomImages = [];
        $scope.causeImages = [];
        $scope.solutionImages = [];

        $scope.handleImages = function (entity, images) {
            return $scope.$parent.$parent.uploadImages(entity, images);
        }

        $scope.$watch('symptomImages.length', function (newVal, oldVal) {
            if (newVal) {
                $scope.handleImages($scope.$parent.symptom, $scope.symptomImages).then(function () {
                    $scope.symptomImages = [];
                });
            }
        });

        $scope.$watch('causeImages.length', function (newVal, oldVal) {
            if (newVal) {
                $scope.handleImages($scope.$parent.cause, $scope.causeImages).then(function () {
                    $scope.causeImages = [];
                });
            }
        });

        $scope.$watch('solutionImages.length', function (newVal, oldVal) {
            if (newVal) {
                $scope.handleImages($scope.$parent.cause.RemarkSymptomSolution, $scope.solutionImages).then(function () {
                    $scope.solutionImages = [];
                });
            }
        });

        $scope.formatImageUrls = function (imageUrls) {
            var formatted = [];
            for (var i = 0; i < imageUrls.length; i++) {
                formatted.push("/Content/Images/Remarks/" + imageUrls[i].ImagePath);
            }

            return formatted;
        }

        $scope.openSymptomLightboxModal = function (index) {
            Lightbox.openModal($scope.formatImageUrls($scope.$parent.symptom.Images), index);
        }

        $scope.openCauseLightboxModal = function (index) {
            Lightbox.openModal($scope.formatImageUrls($scope.$parent.cause.Images), index);
        }

        $scope.openSolutionLightboxModal = function (index) {
            Lightbox.openModal($scope.formatImageUrls($scope.$parent.cause.RemarkSymptomSolution.Images), index);
        };
    }]);

app.factory('dataFactory', ['$http', '$mdToast', 'remarkService', function ($http, $mdToast, remarkService) {
    var vm = this;
    vm.getModelStates = function () {
        return remarkService.getModelStates();
    };

    vm.getRemarkTypes = function () {
        return remarkService.getRemarkTypes();
    };

    vm.getRemarkCauseTypes = function () {
        return remarkService.getRemarkCauseTypes();
    };

    vm.getRemarkSolutionTypes = function () {
        return remarkService.getRemarkSolutionTypes();
    };

    vm.DisplayRemarkJson = function (id) {
        return remarkService.DisplayRemarkJson(id);
    };

    vm.DisplayRemarkHistoryJson = function (id) {
        return remarkService.DisplayRemarkHistoryJson(id);
    };

    vm.getSymptomTypes = function (id) {
        return remarkService.getSymptomTypes(id);
    };

    vm.getCauseTypes = function (id) {
        return remarkService.getCauseTypes(id);
    };

    vm.getSolutionTypes = function (id) {
        return remarkService.getSolutionTypes(id);
    };

    vm.getStats = function (id) {
        return remarkService.getRemarkStatsForAssembly(id);
    };

    vm.getRemarkList = function () {
        return remarkService.getRemarkList();
    };

    return vm;
}]);

app.service("remarkService", function ($http) {
    // get model states
    this.getModelStates = function () {
        var response = $http
        ({
            method: "get",
            url: "/Remark/GetProductModelStatesForRemarksJson",
            dataType: "json"
        });
        return response;
    };

    // delete model states
    this.deleteModelState = function (id) {
        var response = $http
        ({
            method: "get",
            url: "/Remark/DeleteProductModelState/" + id,
            dataType: "json"
        });
        return response;
    };

    // save / update model states
    this.saveProductModelState = function (modelState) {
        var response = $http
        ({
            method: "post",
            url: "/Remark/AddEditProductModelStateJson",
            dataType: "json",
            data: { objectAsString: JSON.stringify(modelState) }
        });
        return response;
    };

    // get remark symptom types
    this.getRemarkTypes = function () {
        var response = $http
        ({
            method: "get",
            url: "/Remark/GetRemarkTypesJson",
            dataType: "json"
        });
        return response;
    };

    // save / update remark symptom type
    this.saveRemarkType = function (remarkType) {
        var response = $http
        ({
            method: "post",
            url: "/Remark/AddEditRemarkTypeJson",
            dataType: "json",
            data: { objectAsString: JSON.stringify(remarkType) }
        });
        return response;
    };

    // delete remark symptom type
    this.deleteRemarkType = function (id) {
        var response = $http
        ({
            method: "get",
            url: "/Remark/DeleteRemarkType/" + id,
            dataType: "json"
        });
        return response;
    };

    // get remark cause types
    this.getRemarkCauseTypes = function () {
        var response = $http
        ({
            method: "get",
            url: "/Remark/GetRemarkCauseTypesJson",
            dataType: "json"
        });
        return response;
    };

    // save / update remark cause types
    this.saveRemarkCauseType = function (causeType) {
        var response = $http
        ({
            method: "post",
            url: "/Remark/AddEditRemarkCauseTypeJson",
            dataType: "json",
            data: { objectAsString: JSON.stringify(causeType) }
        });
        return response;
    };

    // delete remark cause types
    this.deleteRemarkCauseType = function (id) {
        var response = $http
        ({
            method: "get",
            url: "/Remark/DeleteRemarkCauseType/" + id,
            dataType: "json"
        });
        return response;
    };

    // get remark solution types
    this.getRemarkSolutionTypes = function () {
        var response = $http
        ({
            method: "get",
            url: "/Remark/GetRemarkSolutionTypesJson",
            dataType: "json"
        });
        return response;
    };

    // save / update remark solution types
    this.saveRemarkSolutionType = function (solutionType) {
        var response = $http
        ({
            method: "post",
            url: "/Remark/AddEditRemarkSolutionTypeJson",
            dataType: "json",
            data: { objectAsString: JSON.stringify(solutionType) }
        });
        return response;
    };

    // delete remark solution types
    this.deleteRemarkSolutionType = function (id) {
        var response = $http
        ({
            method: "get",
            url: "/Remark/DeleteRemarkSolutionType/" + id,
            dataType: "json"
        });
        return response;
    };

    // get remark symptoms
    this.DisplayRemarkJson = function (id) {
        //alert(id);
        var response = $http
        ({
            method: "get",
            url: "/Remark/DisplayRemarkJson/" + id,
            dataType: "json"
        });
        return response;
    };

    // get remark symptoms
    this.DisplayRemarkHistoryJson = function (id) {
        //alert(id);
        var response = $http
        ({
            method: "get",
            url: "/Remark/DisplayRemarkHistoryJson/" + id,
            dataType: "json"
        });
        return response;
    };

    // get remark symptom types
    this.getSymptomTypes = function () {
        var response = $http
        ({
            method: "get",
            url: "/Remark/GetRemarkTypesJson/",
            dataType: "json"
        });
        return response;
    }

    // get remark symptom cause types
    this.getCauseTypes = function () {
        var response = $http
        ({
            method: "get",
            url: "/Remark/GetRemarkCauseTypesJson/",
            dataType: "json"
        });
        return response;
    }

    // get remark symptom types
    this.getSolutionTypes = function () {
        var response = $http
        ({
            method: "get",
            url: "/Remark/GetRemarkSolutionTypesJson/",
            dataType: "json"
        });
        return response;
    }

    // add cause / solution pair to existing remark
    this.AddCauseAndSolutionToRemarkJson = function (assemblyId, remarkId) {
        var response = $http
        ({
            method: "get",
            url: "/Remark/AddCauseAndSolutionToRemarkJson/?assemblyId=" + assemblyId + "&remarkId=" + remarkId,
            dataType: "json"
        });
        return response;
    }

    // creates a remark and add to assembly
    this.addRemarkToAssembly = function (id) {
        var response = $http
        ({
            method: "get",
            url: "/Remark/AddRemarkToAssembly/" + id,
            dataType: "json"
        });
        return response;
    }

    // save remarks
    this.saveRemarks = function (remarks) {
        var response = $http
        ({
            method: "post",
            url: "/Remark/Save",
            dataType: "json",
            data: { remarkCollectionAsString: JSON.stringify(remarks) }
        });
        return response;
    }

    // add cause to symptom
    this.addCauseToSymptom = function (id) {
        var response = $http({
            method: "get",
            url: "/Remark/AddCauseToSymptom/" + id,
            dataType: "json"
        });
        return response;
    }

    // add solution to cause
    this.addSolutionToCause = function (id) {
        var response = $http({
            method: "get",
            url: "/Remark/AddSolutionToCause/" + id,
            dataType: "json"
        });
        return response;
    }

    // delete remark symptom
    this.deleteRemarkSymptom = function (id) {
        var response = $http({
            method: "get",
            url: "/Remark/ArchiveRemarkSymptom/" + id,
            dataType: "json"
        });
        return response;
    }

    // delete remark cause
    this.deleteRemarkCause = function (id) {
        var response = $http({
            method: "get",
            url: "/Remark/ArchiveRemarkCause/" + id,
            dataType: "json"
        });
        return response;
    }

    // delete remark solution
    this.deleteRemarkSolution = function (id) {
        var response = $http({
            method: "get",
            url: "/Remark/ArchiveRemarkSolution/" + id,
            dataType: "json"
        });
        return response;
    }

    this.getRemarkStatsForAssembly = function (id) {
        var response = $http({
            method: "get",
            url: "/Remark/GetRemarkStatsForAssembly/" + id,
            dataType: "json"
        });
        return response;
    }

    // upload image
    this.uploadImage = function (formData) {
        var response = $http.post('/Remark/UploadImages', formData, {
            transformRequest: angular.identity,
            headers: { 'Content-Type': undefined }
        });
        return response;
    };

    this.getRemarkList = function () {
        var response = $http({
            method: "get",
            url: "/Remark/GetRemarkList/",
            dataType: "json"
        });
        return response;
    }

    this.printRemarks = function (id) {
        var response = $http({
            method: "get",
            url: "/Remark/PrintRemarksJSON/" + id,
            dataType: "json"
        });
        return response;
    }
});

function dialogShowAddEdit(ev, clickOutsideToClose, template, $scope, $mdMedia, $mdDialog, $mdToast, remarkService) {
    var scope = $scope;

    var useFullScreen = ($mdMedia('sm') || $mdMedia('xs')) && $scope.customFullscreen;
    $mdDialog.show({
        controller: ['$scope', '$mdDialog', function ($scope, $mdDialog) {
            $scope.hide = function () {
                $mdDialog.hide();
            };
            $scope.cancel = function () {
                $mdDialog.cancel();
            };
            $scope.answer = function (answer) {
                //alert(answer);
                //  $mdDialog.hide(answer);
            };
            $scope.modelState = scope.modelState;
            $scope.remarkType = scope.remarkType;
            $scope.causeType = scope.causeType;
            $scope.solutionType = scope.solutionType;
            $scope.saveProductModelState = scope.saveProductModelState;
            $scope.saveRemarkSymptomType = scope.saveRemarkSymptomType;
            $scope.saveRemarkCauseType = scope.saveRemarkCauseType;
            $scope.saveRemarkSolutionType = scope.saveRemarkSolutionType;
            $scope.dialogActionResult = scope.dialogActionResult = [];
            $scope.closeErrorContainer = scope.closeErrorContainer;
        }],
        templateUrl: template,
        parent: angular.element(document.body),
        targetEvent: ev,
        clickOutsideToClose: true,
        fullscreen: useFullScreen,
        locals: {
            modelState: $scope.modelState,
            remarkType: $scope.remarkType,
            causeType: $scope.causeType,
            solutionType: $scope.causeType,
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