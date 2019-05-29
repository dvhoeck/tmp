(function(){"use strict";angular
  .module('material.components.expansionPanels')
  .directive('mdExpansionPanelIcon', mdExpansionPanelIconDirective);



/**
 * @ngdoc directive
 * @name mdExpansionPanelIcon
 * @module material.components.expansionPanels
 *
 * @restrict E
 *
 * @description
 * `mdExpansionPanelIcon` can be used in both `md-expansion-panel-collapsed` and `md-expansion-panel-header` as the first or last element.
 * Adding this will provide a animated arrow for expanded and collapsed states
 **/
function mdExpansionPanelIconDirective() {
  var directive = {
    restrict: 'E',
    template: '<ng-md-icon icon="mode_edit" class="md-hue-2" Size="48" aria-label="Edit product component"></ng-md-icon>',
    replace: true
  };
  return directive;
}
}());