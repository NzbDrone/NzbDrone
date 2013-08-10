﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using NzbDrone.Common;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.MetadataSource.Trakt;
using NzbDrone.Core.Tv;
using RestSharp;
using Episode = NzbDrone.Core.Tv.Episode;

namespace NzbDrone.Core.MetadataSource
{
    public class TraktProxy : ISearchForNewSeries, IProvideSeriesInfo
    {
        public List<Series> SearchForNewSeries(string title)
        {
            var client = BuildClient("search", "shows");
            var restRequest = new RestRequest(title.ToSearchTerm());
            var response = client.Execute<List<Show>>(restRequest);

            CheckForError(response);

            return response.Data.Select(MapSeries).ToList();
        }

        public Tuple<Series, List<Episode>> GetSeriesInfo(int tvDbSeriesId)
        {
            var client = BuildClient("show", "summary");
            var restRequest = new RestRequest(tvDbSeriesId.ToString() + "/extended");
            var response = client.Execute<Show>(restRequest);

            CheckForError(response);

            var episodes = response.Data.seasons.SelectMany(c => c.episodes).Select(MapEpisode).ToList();
            var series = MapSeries(response.Data);

            return new Tuple<Series, List<Episode>>(series, episodes);
        }

        private static IRestClient BuildClient(string resource, string method)
        {
            return new RestClient(string.Format("http://api.trakt.tv/{0}/{1}.json/bc3c2c460f22cbb01c264022b540e191", resource, method));
        }

        private static Series MapSeries(Show show)
        {
            var series = new Series();
            series.TvdbId = show.tvdb_id;
            series.TvRageId = show.tvrage_id;
            series.ImdbId = show.imdb_id;
            series.Title = show.title;
            series.CleanTitle = Parser.Parser.CleanSeriesTitle(show.title);
            series.FirstAired = FromIso(show.first_aired_iso);
            series.Overview = show.overview;
            series.Runtime = show.runtime;
            series.Network = show.network;
            series.AirTime = show.air_time_utc;
            series.TitleSlug = show.url.ToLower().Replace("http://trakt.tv/show/", "");
            series.Status = GetSeriesStatus(show.status);

            series.Images.Add(new MediaCover.MediaCover { CoverType = MediaCoverTypes.Banner, Url = show.images.banner });
            series.Images.Add(new MediaCover.MediaCover { CoverType = MediaCoverTypes.Poster, Url = GetPosterThumbnailUrl(show.images.poster) });
            series.Images.Add(new MediaCover.MediaCover { CoverType = MediaCoverTypes.Fanart, Url = show.images.fanart });
            return series;
        }

        private static Episode MapEpisode(Trakt.Episode traktEpisode)
        {
            var episode = new Episode();
            episode.Overview = traktEpisode.overview;
            episode.SeasonNumber = traktEpisode.season;
            episode.EpisodeNumber = traktEpisode.episode;
            episode.EpisodeNumber = traktEpisode.number;
            episode.TvDbEpisodeId = traktEpisode.tvdb_id;
            episode.Title = traktEpisode.title;
            episode.AirDate = FromIsoToString(traktEpisode.first_aired_iso);
            episode.AirDateUtc = FromIso(traktEpisode.first_aired_iso);

            return episode;
        }

        private static string GetPosterThumbnailUrl(string posterUrl)
        {
            if (posterUrl.Contains("poster-small.jpg")) return posterUrl;

            var extension = Path.GetExtension(posterUrl);
            var withoutExtension = posterUrl.Substring(0, posterUrl.Length - extension.Length);
            return withoutExtension + "-300" + extension;
        }

        private static SeriesStatusType GetSeriesStatus(string status)
        {
            if (string.IsNullOrWhiteSpace(status)) return SeriesStatusType.Continuing;
            if (status.Equals("Ended", StringComparison.InvariantCultureIgnoreCase)) return SeriesStatusType.Ended;
            return SeriesStatusType.Continuing;
        }

        private static DateTime? FromEpoch(long ticks)
        {
            if (ticks == 0) return null;

            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Unspecified).AddSeconds(ticks);
        }

        private static DateTime? FromIso(string iso)
        {
            DateTime result;

            if (!DateTime.TryParse(iso, out result))
                return null;

            return result.ToUniversalTime();
        }

        private static string FromIsoToString(string iso)
        {
            if (String.IsNullOrWhiteSpace(iso)) return null;

            var match = Regex.Match(iso, @"^\d{4}\W\d{2}\W\d{2}");

            if (!match.Success) return null;

            return match.Captures[0].Value;
        }

        private void CheckForError(IRestResponse response)
        {
            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new TraktCommunicationException(response.ErrorMessage, response.ErrorException);
            }
        }
    }
}