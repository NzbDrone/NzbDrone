﻿'use strict';
define(
    [
        'Shared/NzbDroneController',
        'AppLayout',
        'Series/SeriesCollection',
        'Series/Index/SeriesIndexLayout',
        'Series/Details/SeriesDetailsLayout'
    ], function (NzbDroneController, AppLayout, SeriesCollection, SeriesIndexLayout, SeriesDetailsLayout) {

        return NzbDroneController.extend({

            _originalInit: NzbDroneController.prototype.initialize,

            initialize: function () {
                this.route('', this.series);
                this.route('series', this.series);
                this.route('series/:query', this.seriesDetails);

                this._originalInit.apply(this, arguments);
            },

            series: function () {
                this.setTitle('Sonarr');
                this.showMainRegion(new SeriesIndexLayout());
            },

            seriesDetails: function (query) {
                var series = SeriesCollection.where({titleSlug: query});

                if (series.length !== 0) {
                    var targetSeries = series[0];
                    this.setTitle(targetSeries.get('title'));
                    this.showMainRegion(new SeriesDetailsLayout({ model: targetSeries }));
                }
                else {
                    this.showNotFound();
                }
            }
        });
    });

