'use strict';
define(
    [
        'vent',
        'AppLayout',
        'marionette',
        'Series/Edit/EditSeriesView',
        'Series/Delete/DeleteSeriesView',
        'Episode/EpisodeDetailsLayout',
        'Activity/History/Details/HistoryDetailsLayout',
        'System/Logs/Table/Details/LogDetailsView',
        'Rename/RenamePreviewLayout'
    ], function (vent, AppLayout, Marionette, EditSeriesView, DeleteSeriesView, EpisodeDetailsLayout, HistoryDetailsLayout, LogDetailsView, RenamePreviewLayout) {

        return Marionette.AppRouter.extend({

            initialize: function () {
                vent.on(vent.Commands.OpenModalCommand, this._openModal, this);
                vent.on(vent.Commands.CloseModalCommand, this._closeModal, this);
                vent.on(vent.Commands.EditSeriesCommand, this._editSeries, this);
                vent.on(vent.Commands.DeleteSeriesCommand, this._deleteSeries, this);
                vent.on(vent.Commands.ShowEpisodeDetails, this._showEpisode, this);
                vent.on(vent.Commands.ShowHistoryDetails, this._showHistory, this);
                vent.on(vent.Commands.ShowLogDetails, this._showLogDetails, this);
                vent.on(vent.Commands.ShowRenamePreview, this._showRenamePreview, this);
            },

            _openModal: function (view) {
                AppLayout.modalRegion.show(view);
            },

            _closeModal: function () {
                AppLayout.modalRegion.closeModal();
            },

            _editSeries: function (options) {
                var view = new EditSeriesView({ model: options.series });
                AppLayout.modalRegion.show(view);
            },

            _deleteSeries: function (options) {
                var view = new DeleteSeriesView({ model: options.series });
                AppLayout.modalRegion.show(view);
            },

            _showEpisode: function (options) {
                var view = new EpisodeDetailsLayout({ model: options.episode, hideSeriesLink: options.hideSeriesLink, openingTab: options.openingTab });
                AppLayout.modalRegion.show(view);
            },

            _showHistory: function (options) {
                var view = new HistoryDetailsLayout({ model: options.model });
                AppLayout.modalRegion.show(view);
            },

            _showLogDetails: function (options) {
                var view = new LogDetailsView({ model: options.model });
                AppLayout.modalRegion.show(view);
            },

            _showRenamePreview: function (options) {
                var view = new RenamePreviewLayout(options);
                AppLayout.modalRegion.show(view);
            }
        });
    });
