'use strict';
define(
    [
        'marionette',
        'backgrid',
        'Logs/LogTimeCell',
        'Logs/LogLevelCell',
        'Shared/Grid/Pager',
        'Logs/Collection',
        'Shared/Toolbar/ToolbarLayout'
    ], function (Marionette, Backgrid, LogTimeCell, LogLevelCell, GridPager, LogCollection, ToolbarLayout) {
        return Marionette.Layout.extend({
            template: 'Logs/LayoutTemplate',

            regions: {
                grid   : '#x-grid',
                toolbar: '#x-toolbar',
                pager  : '#x-pager'
            },

            attributes: {
                id: 'logs-screen'
            },

            columns:
                [
                    {
                        name    : 'level',
                        label   : '',
                        sortable: true,
                        cell    : LogLevelCell
                    },
                    {
                        name    : 'logger',
                        label   : 'Component',
                        sortable: true,
                        cell    : Backgrid.StringCell.extend({
                            className: 'log-logger-cell'
                        })
                    },
                    {
                        name    : 'message',
                        label   : 'Message',
                        sortable: false,
                        cell    : Backgrid.StringCell.extend({
                            className: 'log-message-cell'
                        })
                    },
                    {
                        name : 'time',
                        label: 'Time',
                        cell : LogTimeCell
                    }
                ],

            initialize: function () {
                this.collection = new LogCollection();
                this.collection.fetch();
            },

            onShow: function () {
                this._showToolbar();
                this._showTable();
            },

            _showTable: function () {

                this.grid.show(new Backgrid.Grid({
                    row       : Backgrid.Row,
                    columns   : this.columns,
                    collection: this.collection,
                    className : 'table table-hover'
                }));

                this.pager.show(new GridPager({
                    columns   : this.columns,
                    collection: this.collection
                }));
            },

            _showToolbar: function () {
                var leftSideButtons = {
                    type      : 'default',
                        storeState: false,
                        items     :
                    [
                        {
                            title         : 'Refresh',
                            icon          : 'icon-refresh',
                            ownerContext  : this,
                            callback      : this._refreshLogs
                        },

                        {
                            title          : 'Clear Logs',
                            icon           : 'icon-trash',
                            command        : 'clearLog',
                            successMessage : 'Logs have been cleared',
                            errorMessage   : 'Failed to clear logs',
                            ownerContext   : this,
                            onSuccess: this._refreshLogs
                        },

                        {
                            title: 'Files',
                            icon : 'icon-file',
                            route: 'logs/files'
                        }
                    ]
                };

                this.toolbar.show(new ToolbarLayout({
                    left   :
                        [
                            leftSideButtons
                        ],
                    context: this
                }));
            },

            _refreshLogs: function () {
                this.collection.fetch({ reset: true });
                this._showTable();
            }
        });
    });
