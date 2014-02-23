﻿using System;
using System.Collections.Generic;
using System.Linq;
using Marr.Data.QGen;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.Tv
{
    public interface IEpisodeRepository : IBasicRepository<Episode>
    {
        Episode Find(int seriesId, int season, int episodeNumber);
        Episode Find(int seriesId, int absoluteEpisodeNumber);
        Episode Get(int seriesId, String date);
        Episode Find(int seriesId, String date);
        List<Episode> GetEpisodes(int seriesId);
        List<Episode> GetEpisodes(int seriesId, int seasonNumber);
        List<Episode> GetEpisodeByFileId(int fileId);
        PagingSpec<Episode> EpisodesWithoutFiles(PagingSpec<Episode> pagingSpec, bool includeSpecials);
        PagingSpec<Episode> EpisodesWhereCutoffUnmet(PagingSpec<Episode> pagingSpec, List<QualitiesBelowCutoff> qualitiesBelowCutoff, bool includeSpecials);
        Episode FindEpisodeBySceneNumbering(int seriesId, int seasonNumber, int episodeNumber);
        List<Episode> EpisodesBetweenDates(DateTime startDate, DateTime endDate);
        void SetMonitoredFlat(Episode episode, bool monitored);
        void SetMonitoredBySeason(int seriesId, int seasonNumber, bool monitored);
        void SetFileId(int episodeId, int fileId);
    }

    public class EpisodeRepository : BasicRepository<Episode>, IEpisodeRepository
    {
        private readonly IDatabase _database;

        public EpisodeRepository(IDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
            _database = database;
        }

        public Episode Find(int seriesId, int season, int episodeNumber)
        {
            return Query.Where(s => s.SeriesId == seriesId)
                               .AndWhere(s => s.SeasonNumber == season)
                               .AndWhere(s => s.EpisodeNumber == episodeNumber)
                               .SingleOrDefault();
        }

        public Episode Find(int seriesId, int absoluteEpisodeNumber)
        {
            return Query.Where(s => s.SeriesId == seriesId)
                        .AndWhere(s => s.AbsoluteEpisodeNumber == absoluteEpisodeNumber)
                        .SingleOrDefault();
        }

        public Episode Get(int seriesId, String date)
        {
            return Query.Where(s => s.SeriesId == seriesId)
                        .AndWhere(s => s.AirDate == date)
                        .Single();
        }

        public Episode Find(int seriesId, String date)
        {
            return Query.Where(s => s.SeriesId == seriesId)
                        .AndWhere(s => s.AirDate == date)
                        .SingleOrDefault();
        }

        public List<Episode> GetEpisodes(int seriesId)
        {
            return Query.Where(s => s.SeriesId == seriesId).ToList();
        }

        public List<Episode> GetEpisodes(int seriesId, int seasonNumber)
        {
            return Query.Where(s => s.SeriesId == seriesId)
                        .AndWhere(s => s.SeasonNumber == seasonNumber)
                        .ToList();
        }

        public List<Episode> GetEpisodeByFileId(int fileId)
        {
            return Query.Where(e => e.EpisodeFileId == fileId).ToList();
        }

        public PagingSpec<Episode> EpisodesWithoutFiles(PagingSpec<Episode> pagingSpec, bool includeSpecials)
        {
            var currentTime = DateTime.UtcNow;
            var startingSeasonNumber = 1;

            if (includeSpecials)
            {
                startingSeasonNumber = 0;
            }

            pagingSpec.TotalRecords = GetMissingEpisodesQuery(pagingSpec, currentTime, startingSeasonNumber).GetRowCount();
            pagingSpec.Records = GetMissingEpisodesQuery(pagingSpec, currentTime, startingSeasonNumber).ToList();

            return pagingSpec;
        }

        public PagingSpec<Episode> EpisodesWhereCutoffUnmet(PagingSpec<Episode> pagingSpec, List<QualitiesBelowCutoff> qualitiesBelowCutoff, bool includeSpecials)
        {
            var currentTime = DateTime.UtcNow;
            var startingSeasonNumber = 1;

            if (includeSpecials)
            {
                startingSeasonNumber = 0;
            }

            pagingSpec.TotalRecords = EpisodesWhereCutoffUnmetQuery(pagingSpec, currentTime, qualitiesBelowCutoff, startingSeasonNumber).GetRowCount();
            pagingSpec.Records = EpisodesWhereCutoffUnmetQuery(pagingSpec, currentTime, qualitiesBelowCutoff, startingSeasonNumber).ToList();

            return pagingSpec;
        }

        public Episode FindEpisodeBySceneNumbering(int seriesId, int seasonNumber, int episodeNumber)
        {
            return Query.Where(s => s.SeriesId == seriesId)
                        .AndWhere(s => s.SceneSeasonNumber == seasonNumber)
                        .AndWhere(s => s.SceneEpisodeNumber == episodeNumber)
                        .SingleOrDefault();
        }

        public List<Episode> EpisodesBetweenDates(DateTime startDate, DateTime endDate)
        {
            return Query.Join<Episode, Series>(JoinType.Inner, e => e.Series, (e, s) => e.SeriesId == s.Id)
                        .Where<Episode>(e => e.AirDateUtc >= startDate)
                        .AndWhere(e => e.AirDateUtc <= endDate)
                        .AndWhere(e => e.Monitored)
                        .AndWhere(e => e.Series.Monitored)
                        .ToList();
        }

        public void SetMonitoredFlat(Episode episode, bool monitored)
        {
            episode.Monitored = monitored;
            SetFields(episode, p => p.Monitored);
        }

        public void SetMonitoredBySeason(int seriesId, int seasonNumber, bool monitored)
        {
            var mapper = _database.GetDataMapper();

            mapper.AddParameter("seriesId", seriesId);
            mapper.AddParameter("seasonNumber", seasonNumber);
            mapper.AddParameter("monitored", monitored);

            const string sql = "UPDATE Episodes " +
                               "SET Monitored = @monitored " +
                               "WHERE SeriesId = @seriesId " +
                               "AND SeasonNumber = @seasonNumber";

            mapper.ExecuteNonQuery(sql);
        }

        public void SetFileId(int episodeId, int fileId)
        {
            SetFields(new Episode { Id = episodeId, EpisodeFileId = fileId }, episode => episode.EpisodeFileId);
        }

        private SortBuilder<Episode> GetMissingEpisodesQuery(PagingSpec<Episode> pagingSpec, DateTime currentTime, int startingSeasonNumber)
        {
            return Query.Join<Episode, Series>(JoinType.Inner, e => e.Series, (e, s) => e.SeriesId == s.Id)
                            .Where(pagingSpec.FilterExpression)
                            .AndWhere(e => e.EpisodeFileId == 0)
                            .AndWhere(e => e.SeasonNumber >= startingSeasonNumber)
                            .AndWhere(e => e.AirDateUtc <= currentTime)
                            .OrderBy(pagingSpec.OrderByClause(), pagingSpec.ToSortDirection())
                            .Skip(pagingSpec.PagingOffset())
                            .Take(pagingSpec.PageSize);
        }

        private SortBuilder<Episode> EpisodesWhereCutoffUnmetQuery(PagingSpec<Episode> pagingSpec, DateTime currentTime, List<QualitiesBelowCutoff> qualitiesBelowCutoff, int startingSeasonNumber)
        {
            return Query.Join<Episode, Series>(JoinType.Inner, e => e.Series, (e, s) => e.SeriesId == s.Id)
                             .Join<Episode, EpisodeFile>(JoinType.Left, e => e.EpisodeFile, (e, s) => e.EpisodeFileId == s.Id)
                             .Where(pagingSpec.FilterExpression)
                             .AndWhere(e => e.EpisodeFileId != 0)
                             .AndWhere(e => e.SeasonNumber >= startingSeasonNumber)
                             .AndWhere(e => e.AirDateUtc <= currentTime)
                             .AndWhere(BuildQualityCutoffWhereClause(qualitiesBelowCutoff))
                             .OrderBy(pagingSpec.OrderByClause(), pagingSpec.ToSortDirection())
                             .Skip(pagingSpec.PagingOffset())
                             .Take(pagingSpec.PageSize);
        }

        private string BuildQualityCutoffWhereClause(List<QualitiesBelowCutoff> qualitiesBelowCutoff)
        {
            var clauses = new List<String>();

            foreach (var profile in qualitiesBelowCutoff)
            {
                foreach (var belowCutoff in profile.QualityIds)
                {
                    clauses.Add(String.Format("([t1].[QualityProfileId] = {0} AND [t2].[Quality] LIKE '%_quality_: {1},%')", profile.ProfileId, belowCutoff));
                }
            }

            return String.Format("({0})", String.Join(" OR ", clauses));
        }
    }
}
