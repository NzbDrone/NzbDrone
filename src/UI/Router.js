﻿'use strict';
define(
    [
        'marionette',
        'Controller'
    ], function (Marionette, Controller) {

        return Marionette.AppRouter.extend({

            controller: new Controller(),
            appRoutes : {
                'addseries'                 : 'addSeries',
                'addseries/:action(/:query)': 'addSeries',
                'calendar'                  : 'calendar',
                'settings'                  : 'settings',
                'settings/:action(/:query)' : 'settings',
                'wanted'                    : 'wanted',
                'wanted/:action'            : 'wanted',
                'history'                   : 'activity',
                'history/:action'           : 'activity',
                'activity'                  : 'activity',
                'activity/:action'          : 'activity',
                'rss'                       : 'rss',
                'system'                    : 'system',
                'system/:action'            : 'system',
                'seasonpass'                : 'seasonPass',
                'serieseditor'              : 'seriesEditor',
                ':whatever'                 : 'showNotFound'
            }
        });
    });

