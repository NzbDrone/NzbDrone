﻿using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common;
using NzbDrone.Common.Cache;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using Omu.ValueInjecter;

namespace NzbDrone.Core.Download.Clients.Sabnzbd
{
    public class Sabnzbd : DownloadClientBase<SabnzbdSettings>, IExecute<TestSabnzbdCommand>
    {
        private readonly IHttpProvider _httpProvider;
        private readonly IParsingService _parsingService;
        private readonly ISabnzbdProxy _proxy;
        private readonly ICached<IEnumerable<QueueItem>> _queueCache;
        private readonly Logger _logger;

        public Sabnzbd(IHttpProvider httpProvider,
                       ICacheManager cacheManager,
                       IParsingService parsingService,
                       ISabnzbdProxy proxy,
                       Logger logger)
        {
            _httpProvider = httpProvider;
            _parsingService = parsingService;
            _proxy = proxy;
            _queueCache = cacheManager.GetCache<IEnumerable<QueueItem>>(GetType(), "queue");
            _logger = logger;
        }

        public override string DownloadNzb(RemoteEpisode remoteEpisode)
        {
            var url = remoteEpisode.Release.DownloadUrl;
            var title = remoteEpisode.Release.Title;
            var category = Settings.TvCategory;
            var priority = remoteEpisode.IsRecentEpisode() ? Settings.RecentTvPriority : Settings.OlderTvPriority;

            using (var nzb = _httpProvider.DownloadStream(url))
            {
                _logger.Info("Adding report [{0}] to the queue.", title);
                var response = _proxy.DownloadNzb(nzb, title, category, priority, Settings);

                if (response != null && response.Ids.Any())
                {
                    return response.Ids.First();
                }

                return null;
            }
        }

        public override IEnumerable<QueueItem> GetQueue()
        {
            return _queueCache.Get("queue", () =>
            {
                SabnzbdQueue sabQueue;

                try
                {
                    sabQueue = _proxy.GetQueue(0, 0, Settings);
                }
                catch (DownloadClientException ex)
                {
                    _logger.ErrorException(ex.Message, ex);
                    return Enumerable.Empty<QueueItem>();
                }

                var queueItems = new List<QueueItem>();

                foreach (var sabQueueItem in sabQueue.Items)
                {
                    var queueItem = new QueueItem();
                    queueItem.Id = sabQueueItem.Id;
                    queueItem.Title = sabQueueItem.Title;
                    queueItem.Size = sabQueueItem.Size;
                    queueItem.Sizeleft = sabQueueItem.Sizeleft;
                    queueItem.Timeleft = sabQueueItem.Timeleft;
                    queueItem.Status = sabQueueItem.Status;

                    var parsedEpisodeInfo = Parser.Parser.ParseTitle(queueItem.Title.Replace("ENCRYPTED / ", ""));
                    if (parsedEpisodeInfo == null) continue;

                    var remoteEpisode = _parsingService.Map(parsedEpisodeInfo, 0);
                    if (remoteEpisode.Series == null) continue;

                    queueItem.RemoteEpisode = remoteEpisode;

                    queueItems.Add(queueItem);
                }

                return queueItems;
            }, TimeSpan.FromSeconds(10));
        }

        public override IEnumerable<HistoryItem> GetHistory(int start = 0, int limit = 10)
        {
            SabnzbdHistory sabHistory;

            try
            {
                sabHistory = _proxy.GetHistory(start, limit, Settings);
            }
            catch (DownloadClientException ex)
            {
                _logger.ErrorException(ex.Message, ex);
                return Enumerable.Empty<HistoryItem>();
            }

            var historyItems = new List<HistoryItem>();

            foreach (var sabHistoryItem in sabHistory.Items)
            {
                var historyItem = new HistoryItem();
                historyItem.Id = sabHistoryItem.Id;
                historyItem.Title = sabHistoryItem.Title;
                historyItem.Size = sabHistoryItem.Size;
                historyItem.DownloadTime = sabHistoryItem.DownloadTime;
                historyItem.Storage = sabHistoryItem.Storage;
                historyItem.Category = sabHistoryItem.Category;
                historyItem.Message = sabHistoryItem.FailMessage;
                historyItem.Status = sabHistoryItem.Status == "Failed" ? HistoryStatus.Failed : HistoryStatus.Completed;

                historyItems.Add(historyItem);
            }

            return historyItems;
        }

        public override void RemoveFromQueue(string id)
        {
            _proxy.RemoveFrom("queue", id, Settings);
        }

        public override void RemoveFromHistory(string id)
        {
            _proxy.RemoveFrom("history", id, Settings);
        }

        public override void RetryDownload(string id)
        {
            _proxy.RetryDownload(id, Settings);
        }

        public override void Test()
        {
            _proxy.GetCategories(Settings);
        }

        public void Execute(TestSabnzbdCommand message)
        {
            var settings = new SabnzbdSettings();
            settings.InjectFrom(message);

            _proxy.GetCategories(settings);
        }
    }
}