app.controller('ProductsCtrl', ['$scope', '$http', '$rootScope', '$mdDialog', '$mdMedia', '$mdToast', '$location', '$q', '$mdExpansionPanel', 'productsService', 'dataFactory', 'filterFilter',
    function ($scope, $http, $rootScope, $mdDialog, $mdMedia, $mdToast, $location, $q, $mdExpansionPanel, productsService, dataFactory, filterFilter) {
        $scope.status = '  ';

        $scope.requirement = ["None", "Revision", "Serial", "", "Both"];

        $scope.addEditModelConfiguration = { name: "" };

        $scope.guidRegEx = "^[{(]?[0-9a-fA-F]{8}[-]?([0-9a-fA-F]{4}[-]?){3}[0-9a-fA-F]{12}[)}]?$";

        $scope.doReorder = false;

        $scope.addEditTool = {};

        $scope.mouseOverIndex = -1;

        $scope.addEditModelConfiguration

        $scope.configButtonText = "ADD CONFIG";

        $scope.resetOrder = function () {
            window.location.reload();
        }

        $scope.test = function () {
            alert("test");
        }

        $scope.editConfigName = function (config) {
            $scope.configButtonText = "EDIT CONFIG NAME";
            $scope.addEditModelConfiguration = config;
        }

        $scope.cancelAddEditProductModelConfiguration = function () {
            $scope.configButtonText = "ADD CONFIG";
            $scope.addEditModelConfiguration = {};
        }

        $scope.addRemoveFromComponentConfigs = function (component, config, doAdd) {
            $scope.$parent.isSaving = true;
            if (doAdd) {
                component.ProductModelConfigurations.push(config);
            } else {
                component.ProductModelConfigurations.splice(component.ProductModelConfigurations.indexOf(config), 1);
            }

            productsService.AddOrRemoveProductModelConfigurationFromProductComponent(component.Id, config.Id, doAdd).then(function (response) {
                showToast(response.statusText, $mdToast);
                $scope.$parent.isSaving = false;
            }, function (err) {
                showDialogError(err, $scope);
            });
        }

        $scope.addProductModelConfiguration = function (name) {
            $scope.$parent.isSaving = true;
            if (!$scope.addEditModelConfiguration.Id) {
                // determine index
                var configIndex = -1; //$scope.product.ProductModelConfigurations.length;

                $scope.product.ProductModelConfigurations.forEach(function (item, index) {
                    if (item.ConfigIndex >= configIndex) {
                        configIndex = item.ConfigIndex + 1;
                    }
                });

                var color = $scope.getColor(configIndex);

                productsService.addProductModelConfiguration($scope.id(), name, color, configIndex).then(function (response) {
                    showToast(response.statusText, $mdToast);
                    $scope.addEditModelConfiguration = {};
                    $scope.$parent.isSaving = false;
                    $scope.initProducts();
                }, function (err) {
                    showError(err, $scope);
                });
            } else {
                productsService.changeProductModelConfigurationName($scope.addEditModelConfiguration.Id, $scope.addEditModelConfiguration.Name).then(function (response) {
                    showToast(response.statusText, $mdToast);
                    $scope.addEditModelConfiguration = {};
                    $scope.$parent.isSaving = false;
                    $scope.initProducts();
                }, function (err) {
                    showError(err, $scope);
                });
            }
        }

        $scope.removeConfig = function (config) {
            $scope.$parent.isSaving = true;
            productsService.deleteProductModelConfiguration($scope.id(), config.Id).then(function (response) {
                showToast(response.statusText, $mdToast);
                $scope.$parent.isSaving = false;
                $scope.initProducts();
            }, function (err) {
                showError(err, $scope);
            });
        }

        $scope.datePart = function () {
            return new Date().getTime().toString();
        }

        $scope.id = function ($scope, $location) {
            return window.location.toString().split("/")[window.location.toString().split("/").length - 1]
        }

        $scope.$watch(function () {
            //console.log($scope.productComponent);
            if ($scope.productComponent) {
                return $scope.productComponent.UnderlyingProductModel;
            }
        }, function (newValue, oldValue) {
            if (newValue != null) {
                // set componentrequirement to revision and autofill idmask
                // requirement = serial || requirement = both
                if ($scope.productComponent.ComponentRequirement == "2" || $scope.productComponent.ComponentRequirement == "4") {
                    $scope.productComponent.SerialInputMask = newValue.IdMask;
                }

                // requirement = none
                if ($scope.productComponent.ComponentRequirement == 0) {
                    $scope.productComponent.ComponentRequirement = 2;
                    $scope.productComponent.SerialInputMask = newValue.IdMask;
                }

                // requirement = revision
                if ($scope.productComponent.ComponentRequirement == 1) {
                    $scope.productComponent.ComponentRequirement = 4;
                    $scope.productComponent.SerialInputMask = newValue.IdMask;
                }
            } else if (oldValue != null && newValue == null) {
                // clear revision mask if we went from an underlyingmodel to no underlyingmodel
                $scope.productComponent.SerialInputMask = "";
            }
        });

        $scope.UpdateRelease = function (selectedReleaseId) {
            $scope.getProducts(selectedReleaseId);
        }

        $scope.initReleaseHistory = function () {
            $scope.$parent.dataLoaded = false;
            $scope.initProducts();
            dataFactory.getReleaseList($scope.id())
            .then(function (result) {
                $scope.releaseList = JSON.parse(result.data);
                $scope.$parent.dataLoaded = true;
            });
        }

        $scope.initProducts = function () {
            $scope.getProducts($scope.id());
        };

        $scope.getProducts = function (id) {
            $scope.$parent.dataLoaded = false;
            dataFactory.getProduct(id).then(function (response) {
                var result = JSON.parse(response.data);
                $scope.product = result.Model;
                if (!$scope.product.PartNumbers) {
                    $scope.product.PartNumbers = [];
                }
                $scope.requirements = ["None", "Revision", "Serial", null, "Both"];
                $scope.productComponents = $scope.product.ProductComponents;
                $scope.components = $scope.product.ProductComponents;
                $scope.tooling = result.Model.Tooling;
                $scope.AssociatedProductModelList = result.AssociatedProductModelList;
                $scope.product.Date = new Date($scope.product.Date);
                $scope.ReleaseDate = new Date(result.ReleaseDate).toDateString();
                $scope.ReleaseId = result.ReleaseId;
                $scope.ReleaseVersion = result.ReleaseVersion;
                $scope.ProductId = result.ReleaseId || result.Model.Id
                $scope.PreviousReleaseComment = result.ReleaseComment;
                $scope.ModelType = result.Model.ModelType || "1";
                showToast("Product retrieved.", $mdToast);
                $scope.$parent.isSaving = false;
                $scope.$parent.dataLoaded = true;
            }, function (err) {
                showError(err, $scope);
            });
        }

        $scope.redirectToCurrentRelease = function (id) {
            window.location.href = "/Products/CurrentModelRelease/" + id;
        }

        $scope.modelState = null;

        $scope.$on('dragToReorder.reordered', function ($event, reordered) {
            $scope.doReorder = true;
        });

        $scope.initProductComponents = function () {
            dataFactory.getProductComponents($scope.id()).then(function (response) {
                $scope.productComponents = $scope.components = JSON.parse(response.data);
            })

            showToast("Model and components retrieved.", $mdToast);
        };

        $scope.initModelStates = function () {
            dataFactory.getModelStates().then(function (response) {
                $scope.modelStates = response.data;
            });
        }

        $scope.addProductComponent = function () {
            $scope.$parent.isSaving = false;
            $scope.productComponent = [];
            $scope.tooling = null;
            $scope.workInstructions = null;
            $scope.addEditTool = null;
            $scope.addEditInstruction = null;
            $scope.addNew = true;
            $scope.submitMsg = "SAVE";
            dataFactory.getModelStates().then(function (response) {
                $scope.modelStates = response.data;
                dialogShowAddEditComponent(null, true, 'component-template.html', $scope, $mdMedia, $mdDialog, $mdToast, productsService);
            });
        }

        $scope.editProductComponent = function (component, openDialog, tabIndex) {
            $scope.$parent.isSaving = false;
            $scope.productComponent = component;
            dataFactory.getModelStates().then(function (response) {
                $scope.modelStates = response.data;
                if ($scope.productComponent != null) {
                    $scope.addNew = false;
                    $scope.workInstructions = $scope.productComponent.WorkInstructions;
                } else {
                    $scope.addNew = true;
                    $scope.productComponent = {};
                    $scope.productComponent.ComponentRequirement = 0;
                }

                //$scope.dialogActionResult = ["Ik ben een test"];
                $scope.dialogActionResult = [];

                if (openDialog) {
                    $scope.submitMsg = "SAVE";
                    dialogShowAddEditComponent(null, true, 'component-template.html', $scope, $mdMedia, $mdDialog, $mdToast, productsService, $rootScope);
                } else {
                    $scope.addEditTool = {};
                    $scope.addEditInstruction = {};
                    $scope.files = [];
                }
            });
        }

        $scope.saveTool = function (tool) {
            $scope.$parent.isSaving = true;
            tool.ProductModels = tool.SimpleModelList;
            var response = productsService.saveTool(tool);
            response.then(function (result) {
                hideError($scope);
                showToast(result.statusText, $mdToast);
                $mdDialog.hide();
                $scope.initMasterToolList();
            }, function (errr) {
                showDialogError(errr, $scope);
            });
        }

        $scope.files = [];

        $scope.saveWorkInstruction = function (instruction, productComponent) {
            $scope.$parent.isSaving = true;
            // create formdata object containing files to be uploaded
            var fileName = "";
            var formData = new FormData();
            angular.forEach($scope.$parent.files, function (obj) {
                if (!obj.isRemote) {
                    fileName = obj.lfFileName;
                    formData.append('files[]', obj.lfFile);
                }
            });

            // post files to server
            var uploadResponse = productsService.uploadImage(formData);
            uploadResponse.then(function (result) {
                if (result.statusText != "No files selected to upload.") {
                    instruction.RelativeImagePath = result.statusText;
                }

                // init to 0 if NHVersion is null
                if (!$scope.addEditInstruction.NHVersion) {
                    $scope.addEditInstruction.NHVersion = 0;
                }

                var response = productsService.saveInstruction(instruction, productComponent);
                response.then(function (result) {
                    $scope.files = [];
                    $scope.addEditInstruction.NHVersion = $scope.addEditInstruction.NHVersion + 1;

                    hideError($scope);
                    showToast(result.statusText, $mdToast);
                    //$scope.initSingleProductComponent();

                    //$mdDialog.hide();

                    var response = productsService.getProductComponent($scope.id());
                    response.then(function (result) {
                        $scope.$parent.productComponent.WorkInstructions = JSON.parse(result.data).WorkInstructions;
                    });
                    $scope.$parent.isSaving = false;
                }, function (errr) {
                    showDialogError(errr, $scope);
                    $scope.$parent.isSaving = false;
                });
            }, function (err) {
                showDialogError(err, $scope);
                $scope.$parent.isSaving = false;
            });
        }

        $scope.initSingleProductComponent = function (sequenceOrder) {
            var response = productsService.getProductComponent($scope.id());
            response.then(function (result) {
                $scope.$parent.productComponent.WorkInstructions = JSON.parse(result.data).WorkInstructions;

                if (sequenceOrder) {
                    $scope.$parent.addEditInstruction = $scope.$parent.productComponent.WorkInstructions[sequenceOrder - 1];
                }
            });
        };

        $scope.editTool = function (tool) {
            $scope.addEditTool = tool;
            $scope.$parent.isSaving = false;
            $scope.dialogActionResult = [];
            dialogShowAddEditComponent(null, true, 'tool-template.html', $scope, $mdMedia, $mdDialog, $mdToast, productsService, $rootScope);
        }

        $scope.editInstruction = function (instruction) {
            $scope.files = [];
            $scope.addEditInstruction = instruction;

            $scope.dialogActionResult = [];

            dialogShowAddEditComponent(null, true, 'instruction-template.html', $scope, $mdMedia, $mdDialog, $mdToast, productsService, $rootScope);
        }

        $scope.archiveInstruction = function (id, productComponent) {
            $scope.showConfirm("Confirm archival", "Are you sure you want to archive this work instruction?", "Confirm", "Archive", "Cancel").then(function () {
                $scope.$parent.isSaving = true;
                var response = productsService.archiveInstruction(id);
                response.then(function (result) {
                    $scope.$parent.isSaving = false;
                    hideError($scope);
                    showToast(result.statusText, $mdToast);
                    $scope.initWorkInstructions();
                }, function (errr) {
                    showDialogError(errr, $scope);
                });
            });
        };

        $scope.showArchivalModal = function (tool) {
            $scope.addEditTool = tool;
            $scope.$parent.isSaving = false;
            dialogShowAddEditComponent(null, true, 'toolArchival-template.html', $scope, $mdMedia, $mdDialog, $mdToast, productsService);
        }

        $scope.archiveTool = function (tool) {
            $scope.$parent.isSaving = true;
            var response = productsService.archiveTool(tool.Id, tool.releaseComment);
            response.then(function (result) {
                hideError($scope);
                showToast(result.statusText, $mdToast);
                $scope.initMasterToolList();
            }, function (errr) {
                showDialogError(errr, $scope);
            });
        }

        $scope.saveProductComponent = function (productComponent) {
            $scope.$parent.isSaving = true;

            // create formdata object containing files to be uploaded
            if ($scope.productComponent == null) {
                $scope.productComponent = productComponent;
            }
            var response = productsService.saveProductComponent($scope.productComponent, $scope.id());
            response.then(function (data) {
                hideError($scope);
                showToast("Component saved.", $mdToast);
                $scope.productComponent = JSON.parse(data.data);
                if ($scope.addNew) {
                    $scope.addNew = false;
                }
                $mdDialog.hide();
                $scope.initProducts();
            }, function (errr) {
                showDialogError(errr, $scope);
            });
        };

        $scope.removeImage = function (id, productComponentId, sequenceOrder) {
            var response = productsService.removeImage(id)
            response.then(function (result) {
                $scope.addEditInstruction = JSON.parse(result.data);
                $scope.addEditInstruction.NHVersion = $scope.addEditInstruction.NHVersion + 1;
                $scope.addEditInstruction.RelativeImagePath = "placeholder.png";
                hideError($scope);
                showToast(result.statusText, $mdToast);

                var response = productsService.getProductComponent($scope.id());
                response.then(function (result) {
                    $scope.$parent.productComponent.WorkInstructions = JSON.parse(result.data).WorkInstructions;
                });
                $scope.$parent.isSaving = false;
            }, function (err) {
                showDialogError(err, $scope);
            });
        }

        //$scope.getProductComponent

        $scope.openAddEditComponentDialog = function (ev) {
            dialogShowAddEditComponent(ev, true, 'component-template.html', $scope, $mdMedia, $mdDialog, $mdToast, productsService);
        }

        $scope.reorderComponents = function () {
            var resultStr = "";
            $(".componentIdField").each(function () {
                resultStr = resultStr + ";" + $(this).val();
            });

            //alert(resultStr);

            var response = productsService.reorderComponents($scope.id(), resultStr);
            response.then(function (data) {
                //alert(data.status);
                if (data.status == 200) {
                    window.location.reload(true);
                }
            });
        }

        $scope.reorderInstructions = function (componentId) {
            var resultStr = "";
            $(".instructionIdField").each(function () {
                resultStr = resultStr + ";" + $(this).val();
            });

            var response = productsService.reorderInstructions(componentId, resultStr);
            response.then(function (data) {
                if (data.status == 200) {
                    showToast(data.statusText, $mdToast);
                    $scope.initWorkInstructions();
                }
            });
        }

        $scope.initProductsList = function () {
            $scope.$parent.dataLoaded = false;
            dataFactory.getProducts().then(function (response) {
                $scope.products = JSON.parse(response.data);
                showToast("Product list retrieved.", $mdToast);
                $scope.$parent.dataLoaded = true;
            })
        }

        $scope.saveProduct = function (model) {
            $scope.$parent.isSaving = true;

            CleanupDates($scope.product);
            var response = productsService.saveProduct($scope.product);
            response.then(function (response) {
                var modelId = JSON.parse(response.data).Id;

                // update url (in case of a newly created product)
                if (window.location.toString().endsWith('AddEditProductModel')) {
                    window.history.pushState('modelEditPageWithIdLoaded', 'Add / edit product model', '/Products/AddEditProductModel/' + modelId);
                }

                hideError($scope);
                showToast("Product saved", $mdToast);
                $scope.initProducts();
            }, function (err) {
                showError(err, $scope);
            });
        }

        $scope.addPartNumber = function($chip){
            return { Id: "00000000-0000-0000-0000-000000000000", Number: $chip, NHVersion: 0 };
        }

        //md-transform-chip="addPartNumber($chip)"

        $scope.deleteProduct = function (ev, id) {
            dataFactory.getProduct(id).then(function (response) {
                $scope.product = JSON.parse(response.data).Model;
                dialogShowDetail(ev, true, 'detailview-template.html', $scope, $mdMedia, $mdDialog, productsService, $scope.product.BaseModelId);
            })
        }

        $scope.deleteProductComponent = function (ev, component) {
            // Appending dialog to document.body to cover sidenav in docs app
            var confirm = $mdDialog.confirm()
                  .title('Would you like to delete this product component?')
                  .textContent('Click delete to archive this component. This cannot be undone.')
                  .ariaLabel('Delete component')
                  .targetEvent(ev)
                  .ok('Delete!')
                  .cancel('Cancel');

            $mdDialog.show(confirm).then(function () {
                var response = productsService.deleteProductComponent(component.Id);
                response.then(function (data) {
                    hideError($scope);
                    showToast("Product component deleted", $mdToast);

                    var index = $scope.product.ProductComponents.indexOf(component);
                    if (index > -1) {
                        $scope.product.ProductComponents.splice(index, 1);
                    }
                    $scope.initProducts();
                    sleep(1000).then(() => {
                        $scope.reorderComponents();
                    });
                }, function (err) {
                    showError(err.statusText, $scope);
                });
            }, function () {
                showError("Delete canceled.", $scope);
            });
        }

        $scope.release = function (comment) {
            if ($scope.id()) {
                $scope.$parent.isSaving = true;
                var response = $http({ method: "get", url: "/Products/ReleaseAssociatedModels/" + $scope.id(), params: { releaseComment: comment }, dataType: "json" });
                response.then(function () {
                    hideError($scope);
                    showToast("Product released", $mdToast);
                    sleep(2000).then(() => {
                        window.location.href = "/Products";
                    });
                }, function (err) {
                    showError(err, $scope);
                });
            }
        }

        $scope.reset = function () {
            $scope.product = null;
            $scope.productForm.$setPristine();
            window.location.href = "/Products";
        }

        $scope.selectedItem = null;
        $scope.selectedState = null;

        $scope.searchTextChange = function (text) {
        }

        $scope.selectedItemChange = function (item) {
        }

        $scope.data = null;
        $scope.selectedItem = null;
        $scope.searchText = null;

        $scope.querySearchReferencePromise = function (productId, query) {
            return new Promise(function (resolve, reject) {
                dataFactory.searchProductModel(productId, query)
                .then(function (response) {
                    var data = response.data;
                    if (data) {
                        $scope.data = data;
                        if (!$scope.productComponent) {
                            $scope.productComponent = {};
                        }
                        $scope.productComponent.ComponentRequirement = 2;
                        resolve(data);
                    }
                    else {
                        reject();
                    }
                });
            })
        }

        $scope.initWorkInstructions = function () {
            $scope.$parent.dataLoaded = false;
            $scope.workInstructions = [];
            dataFactory.getProductComponent($scope.id()).then(function (result) {
                $scope.productComponent = JSON.parse(result.data);
                $scope.workInstructions = $scope.productComponent.WorkInstructions;
                showToast("Instructions initialized.", $mdToast);
                $scope.$parent.dataLoaded = true;
                return $scope.workInstructions;
            });
        }

        $scope.initMasterToolList = function () {
            $scope.toolingList = [];
            $scope.$parent.dataLoaded = false;
            dataFactory.getToolingList().then(function (result) {
                $scope.toolingList = JSON.parse(result.data);
                showToast("Tooling list retrieved.", $mdToast);
                $scope.$parent.isSaving = false;
                $scope.$parent.dataLoaded = true;
            });
        }

        $scope.addToolFromMasterListToModelList = function () {
            var selected = $scope.toolingSelector;
            alreadyInList = $scope.tooling.find(function (searchObject) {
                return searchObject == selected;
            });

            if (!alreadyInList) {
                $scope.tooling.push(selected);
            }
        }

        $scope.removeToolFromModelToolList = function (tool) {
            var index = $scope.tooling.indexOf(tool);

            if (index > -1) {
                $scope.tooling.splice(index, 1);
            }

            productsService.removeToolFromToolList(tool.Id, $scope.id()).then(function (result) {
                showToast(result.statusText, $mdToast);
            }, function (err) {
                showError(err, $scope);
            });
        }

        $scope.saveModelToolList = function () {
            $scope.$parent.isSaving = true;
            $scope.product.Tooling = $scope.tooling;
            productsService.saveProduct($scope.product).then(function () {
                hideError($scope);
                showToast("Tool added to list", $mdToast);
                $scope.$parent.isSaving = false;
            }, function (err) {
                showError(err.statusText, $scope);
            });
        }

        $scope.showChanges = function (id, ev) {
            // get changes
            dataFactory.getModelChanges(id).then(function (result) {
                $scope.changeList = JSON.parse(result.data);
                dialogShowDetail(ev, true, 'change-list.html', $scope, $mdMedia, $mdDialog, productsService, $scope.product.BaseModelId);
            }, function (err) {
                showError(err.statusText, $scope);
            });
        };

        $scope.chooseWorkInstructionSourceForDisplay = function (data, sourceType) {
            $scope.$parent.dataLoaded = false;
            if (!data) {
                return;
            }

            var modelId = "00000000-0000-0000-0000-000000000000";
            var assemblyId = "00000000-0000-0000-0000-000000000000";

            if (sourceType != 0) {
                $scope.selectedModelForInstructions = "";

                assemblyId = data.Id;
            }

            if (sourceType != 1) {
                $scope.selectedAssembly = "";
                $scope.searchText = "";

                modelId = data.BaseModelId;
            }

            productsService.getWorkInstructions(modelId, assemblyId).then(function (response) {
                $scope.componentsAndInstructions = JSON.parse(response.data);
                $scope.$parent.dataLoaded = true;
            }, function (err) {
                handleError($scope, err.data);
                $scope.$parent.dataLoaded = true;
            })
        };

        $scope.showCloneProductDialog = function (ev, model) {
            $scope.cloneModel = { modelId: model.Id, name: model.Name, idMask: model.IdMask, description: model.Comment };
            dialogShowCloneForm(ev, false, "cloneView-template.html", $scope, $mdMedia, $mdDialog, $mdToast, $scope.cloneModel, $scope.cloneProductModel);
        };

        $scope.cloneProductModel = function (cloneModel) {
            productsService.cloneModel(cloneModel.modelId, cloneModel.name, cloneModel.idMask, cloneModel.description).then(function (response) {
                $scope.initProductsList();
                $mdDialog.hide();
            }, function (err) {
                showDialogError(err, $scope);
            })
        };
    }]);

app.factory('dataFactory', ['$http', '$mdToast', 'productsService', function ($http, $mdToast, productsService) {
    var vm = this;

    vm.addNewComponent = function (modelid) {
        return productsService.addNewComponent(modelid);
    };

    vm.getProducts = function () {
        return productsService.getProducts();
    };

    vm.getProductComponents = function (id) {
        return productsService.getProductComponents(id);
    };

    vm.getProduct = function (id) {
        return productsService.getProduct(id);
    };

    vm.getProductComponent = function (id) {
        return productsService.getProductComponent(id);
    }

    vm.searchProductModel = function (productId, searchString) {
        return productsService.searchProductModel(productId, searchString);
    }

    vm.getModelStates = function () {
        return productsService.getModelStates();
    }

    vm.getReleaseList = function (id) {
        return productsService.getReleaseList(id);
    }

    vm.getToolingList = function () {
        return productsService.getToolingList();
    }

    vm.getModelChanges = function (id) {
        return productsService.getModelChanges(id);
    }

    return vm;
}]);

app.service("productsService", function ($http) {
    // get products
    this.getProducts = function () {
        var response = $http
        ({
            method: "get",
            url: "/Products/GetProductsJson",
            dataType: "json"
        });
        return response;
    }

    // get ProductComponents
    this.getProductComponents = function (id) {
        var response = $http
        ({
            method: "get",
            url: "/Products/getProductComponentsJson/" + id,
            dataType: "json"
        });
        return response;
    }

    // search ProductComponents
    this.searchProductModel = function (productId, searchString) {
        var response = $http
        ({
            method: "get",
            url: "/Products/SearchProductModelJson",
            params: { productId: productId, searchString: searchString },
            dataType: "json"
        });
        return response;
    }

    // get a list of model states
    this.getModelStates = function (id) {
        var response = $http
        ({
            method: "get",
            url: "/Products/GetProductModelStatesForProductsJson",
            dataType: "json"
        });
        return response;
    }

    // create a new product component
    this.addNewComponent = function (modelId) {
        var response = $http
        ({
            method: "get",
            url: "/Products/AddNewComponent/" + modelId,
            dataType: "json"
        });
        return response;
    }

    // get a single product component
    this.getProductComponent = function (id) {
        var response = $http
        ({
            method: "get",
            url: "/Products/GetProductComponentJson/" + id,
            dataType: "json"
        });
        return response;
    }

    // get new or existing product
    this.getProduct = function (id) {
        var response = $http
        ({
            method: "get",
            url: "/Products/GetProductJson/" + id,
            dataType: "json"
        });
        return response;
    }

    // persist product
    this.saveProduct = function (product) {
        var response = $http
        ({
            method: "post",
            url: "/Products/SaveProductJson",
            dataType: "json",
            data: { objectAsString: JSON.stringify(product) }
        });
        return response;
    }

    // persist product component
    this.saveProductComponent = function (component, modelId) {
        var response = $http
        ({
            method: "post",
            url: "/Products/SaveProductComponentJson",
            dataType: "json",
            data: { objectAsString: JSON.stringify(component), modelIdAsString: modelId }
        });
        return response;
    }

    // upload image
    this.uploadImage = function (formData) {
        var response = $http.post('/Products/UploadImage', formData, {
            transformRequest: angular.identity,
            headers: { 'Content-Type': undefined }
        });
        return response;
    };

    // remove image
    this.removeImage = function (id) {
        var response = $http
        ({
            method: "get",
            url: "/Products/RemoveImage/" + id,
            dataType: "json"
        });
        return response;
    }

    // delete product
    this.deleteProduct = function (id) {
        var response = $http
        ({
            method: "get",
            url: "/Products/DeleteProductModel/" + id,
            dataType: "json"
        });
        return response;
    };

    // delete component
    this.deleteProductComponent = function (id) {
        var response = $http
        ({
            method: "get",
            url: "/Products/DeleteProductComponent/" + id,
            dataType: "json"
        });
        return response;
    };

    // reorder components
    this.reorderComponents = function (modelId, idString) {
        var response = $http
        ({
            method: "post",
            url: "/Products/ReorderComponents",
            data: { modelId: modelId, idString: idString },
            dataType: "json"
        });
        return response;
    };

    // reorder tools
    this.reorderTools = function (componentId, idString) {
        var response = $http
        ({
            method: "post",
            url: "/Products/ReorderTools",
            data: { componentId: componentId, idString: idString },
            dataType: "json"
        });
        return response;
    };

    // reorder tools
    this.reorderInstructions = function (componentId, idString) {
        var response = $http
        ({
            method: "post",
            url: "/Products/ReorderInstructions",
            data: { componentId: componentId, idString: idString },
            dataType: "json"
        });
        return response;
    };

    // save tooling
    this.saveTool = function (tool) {
        var response = $http({
            method: "post",
            url: "/Products/SaveToolJson",
            data: { objectAsString: JSON.stringify(tool) },
            dataType: "json"
        });
        return response;
    };

    // save instruction
    this.saveInstruction = function (instruction, productComponent) {
        var response = $http({
            method: "post",
            url: "/Products/SaveInstructionJson",
            data: { objectAsString: JSON.stringify(instruction), parentObjectAsString: JSON.stringify(productComponent) },
            dataType: "json"
        });
        return response;
    };

    // archive instruction
    this.archiveInstruction = function (id) {
        var response = $http({
            method: "post",
            url: "/Products/ArchiveInstruction/" + id,
            dataType: "json"
        });
        return response;
    }

    // archive tool
    this.archiveTool = function (id, releaseComment) {
        var response = $http({
            method: "post",
            url: "/Products/ArchiveTool/",
            data: { id: id, releaseComment: releaseComment },
            dataType: "json"
        });
        return response;
    }

    // remove tool from tool list
    this.removeToolFromToolList = function (id, modelId) {
        var response = $http({
            method: "post",
            url: "/Products/RemoveToolFromToolList/",
            data: { id: id, modelId: modelId },
            dataType: "json"
        });
        return response;
    }

    // release a list of models associated with particular tool
    this.ReleaseAssociatedModels = function (simpleModelList, releaseComment) {
        var response = $http({
            method: "post",
            url: "/Products/ReleaseAssociatedModels/",
            data: { simpleModelList: JSON.stringify(simpleModelList), releaseComment: releaseComment },
            dataType: "json"
        });
        return response;
    }

    this.getReleaseList = function (id) {
        var response = $http({
            method: "post",
            url: "/Products/GetReleaseListJson/" + id,
            dataType: "json"
        });
        return response;
    }

    this.getToolingList = function (id) {
        var response = $http({
            method: "post",
            url: "/Products/GetToolsJson/",
            dataType: "json"
        });
        return response;
    }

    this.getModelChanges = function (id) {
        var response = $http({
            method: "post",
            url: "/Products/GetModelChangesJson/" + id,
            dataType: "json"
        });
        return response;
    }

    this.getWorkInstructions = function (modelId, assemblyId) {
        var response = $http({
            method: "post",
            url: "/Products/InstructionsByModelAndAssemblyIds/",
            dataType: "json",
            data: { modelId: modelId, assemblyId: assemblyId }
        });
        return response;
    }

    this.cloneModel = function (modelId, name, idMask, description) {
        var response = $http({
            method: "get",
            url: "/Products/CloneModelJSON/",
            dataType: "json",
            params: { modelId: modelId, name: name, idMask: idMask, description: description }
        });
        return response;
    }

    this.changeProductModelConfigurationName = function (productModelConfigurationId, name) {
        var response = $http({
            method: "get",
            url: "/Products/ChangeProductModelConfigurationName/",
            dataType: "json",
            params: { productModelConfigurationId: productModelConfigurationId, name: name }
        });
        return response;
    }

    this.deleteProductModelConfiguration = function (productModelId, productModelConfigurationId) {
        var response = $http({
            method: "get",
            url: "/Products/DeleteProductModelConfiguration/",
            dataType: "json",
            params: { productModelId: productModelId, productModelConfigurationId: productModelConfigurationId }
        });
        return response;
    }

    this.addProductModelConfiguration = function (productModelId, name, color, index) {
        var response = $http({
            method: "get",
            url: "/Products/AddProductModelConfiguration/",
            dataType: "json",
            params: { productModelId: productModelId, name: name, color: color, index: index }
        });
        return response;
    }

    this.AddOrRemoveProductModelConfigurationFromProductComponent = function (componentId, configId, doAdd) {
        var response = $http({
            method: "get",
            url: "/Products/AddOrRemoveProductModelConfigurationFromProductComponent/",
            dataType: "json",
            params: { componentId: componentId, configId: configId, doAdd: doAdd }
        });
        return response;
    }
});

function dialogShowDetail(ev, clickOutsideToClose, template, $scope, $mdMedia, $mdDialog, productsService, id) {
    {
        scope = $scope;
        var prod = $scope.product;
        var currentModelId = "";
        var useFullScreen = ($mdMedia('sm') || $mdMedia('xs')) && $scope.customFullscreen;
        $mdDialog.show({
            controller: ['$scope', '$mdDialog', function ($scope, $mdDialog) {
                $scope.product = prod;
                $scope.hide = function () {
                    $mdDialog.hide();
                };
                $scope.cancel = function () {
                    $mdDialog.cancel();
                };
                $scope.answer = function (answer, modelId) {
                    currentModelId = modelId;
                    $mdDialog.hide(answer);
                };
                $scope.login = function () {
                    this.$mdDialog.hide({ username: this.username, password: this.password });
                };
                $scope.changeList = scope.changeList;
            }],
            templateUrl: template,
            parent: angular.element(document.body),
            targetEvent: ev,
            clickOutsideToClose: true,
            fullscreen: useFullScreen
        })
        .then(function (answer) {
            if (answer == "delete") {
                //alert(id);
                var response = productsService.deleteProduct(currentModelId);
                response.then(function (data) {
                    if (data.status == 200) {
                        window.location.reload(true);
                    }
                });
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
}

function dialogShowCloneForm(ev, clickOutsideToClose, template, $scope, $mdMedia, $mdDialog, $mdToast, model, modelFunction) {
    var useFullScreen = ($mdMedia('sm') || $mdMedia('xs')) && $scope.customFullscreen;
    $mdDialog.show({
        controller: CloneDialogController,
        templateUrl: template,
        targetEvent: ev,
        clickOutsideToClose: false,
        fullscreen: useFullScreen,
        locals: {
            cloneModel: model,
            cloneModelFunction: modelFunction
        }
    })
    .then(function (answer) {
        //alert("Resolve answer: " + answer);
        if (answer == "cancel") {
            $scope.status = 'You cancelled the dialog.';
        }
    }, function (answer) {
        //alert("Cancel / Reject answer: " + answer);
        $scope.status = 'You cancelled the dialog.';
    });
    $scope.$watch(function () {
        return $mdMedia('xs') || $mdMedia('sm');
    }, function (wantsFullScreen) {
        $scope.customFullscreen = (wantsFullScreen === true);
    });
}

function dialogShowAddEditComponent2(ev, clickOutsideToClose, template, $scope, $mdMedia, $mdDialog, $mdToast, productsService, id) {
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
                $mdDialog.hide(answer);
            };
            $scope.productComponent = scope.productComponent;
            $scope.addEditInstruction = scope.addEditInstruction;
        }],
        templateUrl: template,
        targetEvent: ev,
        clickOutsideToClose: false,
        fullscreen: useFullScreen,
        locals: {
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

function dialogShowAddEditComponent(ev, clickOutsideToClose, template, $scope, $mdMedia, $mdDialog, $mdToast, productsService) {
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
                $mdDialog.hide(answer);
            };
            $scope.productId = scope.ProductId;
            $scope.product = scope.product;
            $scope.components = scope.components;
            $scope.addNew = scope.addNew;
            $scope.submitMsg = scope.submitMsg;
            $scope.addEditTool = scope.addEditTool;
            $scope.addEditInstruction = scope.addEditInstruction;
            $scope.$parent.isSaving = scope.isSaving;
            $scope.saveTool = scope.saveTool;
            $scope.editTool = scope.editTool;
            $scope.saveWorkInstruction = scope.saveWorkInstruction;
            $scope.editInstruction = scope.editInstruction;
            $scope.saveProductComponent = scope.saveProductComponent;
            $scope.showAssociatedModels = scope.showAssociatedModels;
            $scope.simpleModelList = scope.simpleModelList;
            $scope.promptToReleaseAssociatedModels = scope.promptToReleaseAssociatedModels;
            $scope.archiveTool = scope.archiveTool;
            $scope.initModelStates = scope.initModelStates;
            $scope.modelStates = scope.modelStates;
            $scope.querySearchReferencePromise = scope.querySearchReferencePromise;
            $scope.closeErrorContainer = scope.closeErrorContainer;
            $scope.dialogActionResult = scope.dialogActionResult;
            $scope.addRemoveFromComponentConfigs = scope.addRemoveFromComponentConfigs;
            $scope.isInConfig = scope.isInConfig;
            $scope.test = scope.test;

            $scope.workInstructions = "scope.workInstructions";

            $scope.files = scope.files;
            if (scope.productComponent != null) {
                $scope.productComponent = scope.productComponent;
                $scope.tooling = scope.productComponent.Tooling;

                $scope.InstructionImage = "placeholder.png";

                if ($scope.productComponent.InstructionImage != null && $scope.productComponent.InstructionImage != "") {
                    $scope.InstructionImage = $scope.productComponent.InstructionImage;
                }
            }
        }],
        templateUrl: template,
        targetEvent: ev,
        clickOutsideToClose: false,
        fullscreen: useFullScreen,
        locals: {
            productComponent: $scope.productComponent,
            tooling: $scope.tooling,
            workInstructions: $scope.workInstructions,
            InstructionImage: $scope.InstructionImage,
            modelStates: $scope.modelStates,
            isSaving: $scope.$parent.isSaving
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

function CleanupDates(product) {
    product.ProductComponents.forEach(function (component, index, array) {
        component.WorkInstructions.forEach(function (instruction, index, array) {
            if (angular.isDate(instruction.CreationDate)) {
                instruction.CreationDate = new Date(parseInt(instruction.CreationDate.substr(6)));
            }

            if (angular.isDate(instruction.ModificationDate)) {
                instruction.ModificationDate = new Date(parseInt(instruction.ModificationDate.substr(6)));
            }
        });
    });

    product.Tooling.forEach(function (tool, index, array) {
        if (angular.isDate(tool.CreationDate)) {
            tool.CreationDate = new Date(parseInt(tool.CreationDate.substr(6)));
        }

        if (angular.isDate(tool.ModificationDate)) {
            tool.ModificationDate = new Date(parseInt(tool.ModificationDate.substr(6)));
        }
    });
}

function CloneDialogController($scope, $mdDialog, cloneModel, cloneModelFunction) {
    $scope.cloneModel = cloneModel;
    $scope.cloneModelFunction = cloneModelFunction;

    $scope.hide = function () {
        $mdDialog.hide();
    };
    $scope.cancel = function () {
        $mdDialog.cancel();
    };
    $scope.answer = function (answer, cloneModel) {
        if (answer === "clone") {
            $scope.cloneModelFunction(cloneModel);
        }
        $mdDialog.hide(answer);
    };
}