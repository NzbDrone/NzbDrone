﻿﻿'use strict';

define([
    'jquery',
    'backbone.deepmodel',
    'Shared/Messenger'
], function ($, DeepModel, Messenger) {
    return DeepModel.DeepModel.extend({

        test: function () {
            var self = this;

            this.trigger('validation:sync');

            var params = {};

            params.url = this.collection.url + '/test';
            params.contentType = 'application/json';
            params.data = JSON.stringify(this.toJSON());
            params.type = 'POST';
            params.isValidatedCall = true;

            var promise = $.ajax(params);

            Messenger.monitor({
                promise        : promise,
                successMessage : 'Testing \'{0}\' completed'.format(this.get('name')),
                errorMessage   : 'Testing \'{0}\' failed'.format(this.get('name'))
            });

            promise.fail(function (response) {
                self.trigger('validation:failed', response);
            });
        }
    });
});
