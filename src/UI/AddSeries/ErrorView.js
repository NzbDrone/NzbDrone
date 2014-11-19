﻿'use strict';

define(
    [
        'marionette'
    ], function (Marionette) {

        return Marionette.CompositeView.extend({
            template: 'AddSeries/ErrorViewTemplate',

            initialize: function (options) {
                this.options = options;
            },

            templateHelpers: function () {
                return  this.options;
            }

        });
    });
