﻿using System;
using System.Collections.Generic;
using System.Linq;
using Ninject;
using NLog;
using NzbDrone.Core.Model;
using NzbDrone.Core.Repository;
using PetaPoco;

namespace NzbDrone.Core.Providers
{
    public class HistoryProvider
    {
        private readonly IDatabase _database;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();


        [Inject]
        public HistoryProvider(IDatabase database)
        {
            _database = database;
        }

        public HistoryProvider()
        {
        }

        public virtual List<History> AllItems()
        {
            return _database.Fetch<History>("");
        }

        public virtual List<History> AllItemsWithRelationships()
        {
            return _database.Fetch<History, Episode>(@"
                            SELECT History.*, Series.Title as SeriesTitle, Episodes.* FROM History 
		                    INNER JOIN Series ON History.SeriesId = Series.SeriesId
                            INNER JOIN Episodes ON History.EpisodeId = Episodes.EpisodeId
                        ");
        }

        public virtual void Purge()
        {
            _database.Delete<History>("");
            Logger.Info("History has been Purged");
        }

        public virtual void Trim()
        {
            _database.Delete<History>("WHERE Date < @0", DateTime.Now.AddDays(-30).Date);
            Logger.Info("History has been trimmed, items older than 30 days have been removed");
        }

        public virtual void Add(History item)
        {
            _database.Insert(item);
            Logger.Debug("Item added to history: {0}", item.NzbTitle);
        }

        public virtual Quality GetBestQualityInHistory(long episodeId)
        {
            var history = AllItems().Where(c => c.EpisodeId == episodeId).ToList().Select(d => new Quality(d.Quality, d.IsProper)).OrderBy(c => c);

            return history.FirstOrDefault();
        }

        public virtual void Delete(int historyId)
        {
            _database.Delete<History>(historyId);
        }

        public virtual bool IsBlacklisted(string nzbTitle)
        {
            return _database.Exists<History>("WHERE Blacklisted = 1 AND NzbTitle = @0", nzbTitle);
        }

        public virtual bool IsBlacklisted(int newzbinId)
        {
            if (newzbinId <= 0)
                throw new ArgumentException("Newzbin ID must be greater than 0");

            return _database.Exists<History>("WHERE Blacklisted = 1 AND NewzbinId = @0", newzbinId);
        }

        public virtual void SetBlacklist(int historyId, bool toggle)
        {
            if (historyId <= 0)
                throw new ArgumentException("HistoryId must be greater than 0");

            _database.Execute("UPDATE History SET Blacklisted = @0 WHERE HistoryId = @1", toggle, historyId);
        }
    }
}