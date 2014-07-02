﻿'use strict';
define(
    [
        'underscore',
        'backbone',
        'backbone.pageable',
        'Series/SeriesModel',
        'api!series',
        'Mixins/AsFilteredCollection',
        'Mixins/AsPersistedStateCollection',
        'moment'
    ], function (_, Backbone, PageableCollection, SeriesModel, SeriesData, AsFilteredCollection, AsPersistedStateCollection, Moment) {
        var Collection = PageableCollection.extend({
            url  : window.NzbDrone.ApiRoot + '/series',
            model: SeriesModel,
            tableName: 'series',

            state: {
                sortKey: 'title',
                order  : -1,
                pageSize: 100000
            },

            mode: 'client',

            save: function () {
                var self = this;

                var proxy = _.extend( new Backbone.Model(),
                    {
                        id: '',

                        url: self.url + '/editor',

                        toJSON: function()
                        {
                            return self.filter(function (model) {
                                return model.edited;
                            });
                        }
                    });

                this.listenTo(proxy, 'sync', function (proxyModel, models) {
                    this.add(models, { merge: true });
                    this.trigger('save', this);
                });

                return proxy.save();
            },

            // Filter Modes
            filterModes: {
                'all'        : [null, null],
                'continuing' : ['status', 'continuing'],
                'ended'      : ['status', 'ended'],
                'monitored'  : ['monitored', true]
            },
            
            //Sorters
            nextAiring: function (model, attr) {
                var nextAiring = model.get(attr);
                
                if (nextAiring) {
                    return Moment(nextAiring).unix();
                }
                
                var previousAiring = model.get(attr.replace('nextAiring', 'previousAiring'));
                
                if (previousAiring) {
                    return 10000000000 - Moment(previousAiring).unix();
                }

                return Number.MAX_VALUE;
            }
        });

        var FilteredCollection = AsFilteredCollection.call(Collection);
        var MixedIn = AsPersistedStateCollection.call(FilteredCollection);
        var collection = new MixedIn(SeriesData, { full: true });

        return collection;
    });
