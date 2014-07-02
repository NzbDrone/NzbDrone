﻿using System;
using System.Collections.Generic;
using System.Text;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.SeriesStats
{
    public interface ISeriesStatisticsRepository
    {
        List<SeriesStatistics> SeriesStatistics();
        SeriesStatistics SeriesStatistics(int seriesId);
    }

    public class SeriesStatisticsRepository : ISeriesStatisticsRepository
    {
        private readonly IDatabase _database;

        public SeriesStatisticsRepository(IDatabase database)
        {
            _database = database;
        }

        public List<SeriesStatistics> SeriesStatistics()
        {
            var mapper = _database.GetDataMapper();

            mapper.AddParameter("currentDate", DateTime.UtcNow);

            var sb = new StringBuilder();
            sb.AppendLine(GetSelectClause());
            sb.AppendLine(GetGroupByClause());
            var queryText = sb.ToString();

            return mapper.Query<SeriesStatistics>(queryText);
        }

        public SeriesStatistics SeriesStatistics(int seriesId)
        {
            var mapper = _database.GetDataMapper();

            mapper.AddParameter("currentDate", DateTime.UtcNow);
            mapper.AddParameter("seriesId", seriesId);

            var sb = new StringBuilder();
            sb.AppendLine(GetSelectClause());
            sb.AppendLine("WHERE SeriesId = @seriesId");
            sb.AppendLine(GetGroupByClause());
            var queryText = sb.ToString();

            return mapper.Find<SeriesStatistics>(queryText);
        }

        private string GetSelectClause()
        {
            return @"SELECT
                     SeriesId,
                     SUM(CASE WHEN (Monitored = 1 AND AirdateUtc <= @currentDate) OR EpisodeFileId > 0 THEN 1 ELSE 0 END) AS EpisodeCount,
                     SUM(CASE WHEN EpisodeFileId > 0 THEN 1 ELSE 0 END) AS EpisodeFileCount,
                     MIN(CASE WHEN AirDateUtc < @currentDate OR EpisodeFileId > 0 OR Monitored = 0 THEN NULL ELSE AirDateUtc END) AS NextAiringString,
                     MAX(CASE WHEN AirDateUtc >= @currentDate OR EpisodeFileId = 0 AND Monitored = 0 THEN NULL ELSE AirDateUtc END) AS PreviousAiringString
                     FROM Episodes";
        }

        private string GetGroupByClause()
        {
            return "GROUP BY SeriesId";
        }
    }
}
