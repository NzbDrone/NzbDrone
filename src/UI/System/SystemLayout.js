﻿'use strict';
define(
    [
        'jquery',
        'backbone',
        'marionette',
        'System/Info/SystemInfoLayout',
        'System/Logs/LogsLayout',
        'System/Update/UpdateLayout',
        'System/Backup/BackupLayout',
        'Shared/Messenger'
    ], function ($,
                 Backbone,
                 Marionette,
                 SystemInfoLayout,
                 LogsLayout,
                 UpdateLayout,
                 BackupLayout,
                 Messenger) {
        return Marionette.Layout.extend({
            template: 'System/SystemLayoutTemplate',

            regions: {
                info    : '#info',
                logs    : '#logs',
                updates : '#updates',
                backup  : '#backup'
            },

            ui: {
                infoTab    : '.x-info-tab',
                logsTab    : '.x-logs-tab',
                updatesTab : '.x-updates-tab',
                backupTab  : '.x-backup-tab'
            },

            events: {
                'click .x-info-tab'   : '_showInfo',
                'click .x-logs-tab'   : '_showLogs',
                'click .x-updates-tab': '_showUpdates',
                'click .x-backup-tab': '_showBackup',
                'click .x-shutdown'   : '_shutdown',
                'click .x-restart'    : '_restart'
            },

            initialize: function (options) {
                if (options.action) {
                    this.action = options.action.toLowerCase();
                }
            },

            onShow: function () {
                switch (this.action) {
                    case 'logs':
                        this._showLogs();
                        break;
                    case 'updates':
                        this._showUpdates();
                        break;
                    case 'backup':
                        this._showBackup();
                        break;
                    default:
                        this._showInfo();
                }
            },

            _navigate:function(route){
                Backbone.history.navigate(route, { trigger: true, replace: true });
            },

            _showInfo: function (e) {
                if (e) {
                    e.preventDefault();
                }

                this.info.show(new SystemInfoLayout());
                this.ui.infoTab.tab('show');
                this._navigate('system/info');
            },

            _showLogs: function (e) {
                if (e) {
                    e.preventDefault();
                }

                this.logs.show(new LogsLayout());
                this.ui.logsTab.tab('show');
                this._navigate('system/logs');
            },

            _showUpdates: function (e) {
                if (e) {
                    e.preventDefault();
                }

                this.updates.show(new UpdateLayout());
                this.ui.updatesTab.tab('show');
                this._navigate('system/updates');
            },

            _showBackup: function (e) {
                if (e) {
                    e.preventDefault();
                }

                this.backup.show(new BackupLayout());
                this.ui.backupTab.tab('show');
                this._navigate('system/backup');
            },

            _shutdown: function () {
                $.ajax({
                    url: window.NzbDrone.ApiRoot + '/system/shutdown',
                    type: 'POST'
                });

                Messenger.show({
                    message: 'NzbDrone will shutdown shortly',
                    type: 'info'
                });
            },

            _restart: function () {
                $.ajax({
                    url: window.NzbDrone.ApiRoot + '/system/restart',
                    type: 'POST'
                });

                Messenger.show({
                    message: 'NzbDrone will restart shortly',
                    type: 'info'
                });
            }
        });
    });

