using System.Collections.Generic;
using System.Linq;
using Nancy;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.SeriesStats;
using Sonarr.Http;
using Sonarr.Http.Extensions;

namespace Sonarr.Api.V3.Series
{
    public class SeriesLookupModule : SonarrRestModule<SeriesResource>
    {
        private readonly ISearchForNewSeries _searchProxy;
        private readonly IBuildFileNames _fileNameBuilder;

        public SeriesLookupModule(ISearchForNewSeries searchProxy, IBuildFileNames fileNameBuilder)
            : base("/series/lookup")
        {
            _searchProxy = searchProxy;
            _fileNameBuilder = fileNameBuilder;
            Get("/",  x => Search());
        }

        private object Search()
        {
            var tvDbResults = _searchProxy.SearchForNewSeries((string)Request.Query.term);
            return MapToResource(tvDbResults);
        }

        private IEnumerable<SeriesResource> MapToResource(IEnumerable<NzbDrone.Core.Tv.Series> series)
        {
            foreach (var currentSeries in series)
            {
                var resource = currentSeries.ToResource();
                var poster = currentSeries.Images.FirstOrDefault(c => c.CoverType == MediaCoverTypes.Poster);

                if (poster != null)
                {
                    resource.RemotePoster = poster.Url;
                }

                resource.Folder = _fileNameBuilder.GetSeriesFolder(currentSeries);
                resource.Statistics = new SeriesStatistics().ToResource(resource.Seasons);

                yield return resource;
            }
        }
    }
}
