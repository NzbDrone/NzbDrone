﻿using System;
using System.Linq;
using NzbDrone.Api.Episodes;
using NzbDrone.Api.Extensions;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Tv;

namespace NzbDrone.Api.Wanted
{
    public class CutoffModule : EpisodeModuleWithSignalR
    {
        private readonly IEpisodeCutoffService _episodeCutoffService;
        private readonly ISeriesRepository _seriesRepository;

        public CutoffModule(IEpisodeService episodeService, IEpisodeCutoffService episodeCutoffService, ISeriesRepository seriesRepository, ICommandExecutor commandExecutor)
            :base(episodeService, commandExecutor, "wanted/cutoff")
        {
            _episodeCutoffService = episodeCutoffService;
            _seriesRepository = seriesRepository;
            GetResourcePaged = GetCutoffUnmetEpisodes;
        }

        private PagingResource<EpisodeResource> GetCutoffUnmetEpisodes(PagingResource<EpisodeResource> pagingResource)
        {
            var pagingSpec = new PagingSpec<Episode>
            {
                Page = pagingResource.Page,
                PageSize = pagingResource.PageSize,
                SortKey = pagingResource.SortKey,
                SortDirection = pagingResource.SortDirection
            };

            //This is a hack to deal with backgrid setting the sortKey to the column name instead of sortValue
            if (pagingSpec.SortKey.Equals("series", StringComparison.InvariantCultureIgnoreCase))
            {
                pagingSpec.SortKey = "series.sortTitle";
            }

            if (pagingResource.FilterKey == "monitored" && pagingResource.FilterValue == "false")
            {
                pagingSpec.FilterExpression = v => v.Monitored == false || v.Series.Monitored == false;
            }
            else
            {
                pagingSpec.FilterExpression = v => v.Monitored == true && v.Series.Monitored == true;
            }

            PagingResource<EpisodeResource> resource = ApplyToPage(_episodeCutoffService.EpisodesWhereCutoffUnmet, pagingSpec);

            resource.Records = resource.Records.LoadSubtype(e => e.SeriesId, _seriesRepository).ToList();

            return resource;
        }
    }
}