﻿using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common;
using NzbDrone.Common.Cache;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.History;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Download
{
    public interface IFailedDownloadService
    {
        void MarkAsFailed(int historyId);
    }

    public class FailedDownloadService : IFailedDownloadService, IExecute<CheckForFailedDownloadCommand>
    {
        private readonly IProvideDownloadClient _downloadClientProvider;
        private readonly IHistoryService _historyService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IConfigService _configService;
        private readonly Logger _logger;

        private readonly ICached<FailedDownload> _failedDownloads;

        private static string DOWNLOAD_CLIENT = "downloadClient";
        private static string DOWNLOAD_CLIENT_ID = "downloadClientId";

        public FailedDownloadService(IProvideDownloadClient downloadClientProvider,
                                     IHistoryService historyService,
                                     IEventAggregator eventAggregator,
                                     IConfigService configService,
                                     ICacheManager cacheManager,
                                     Logger logger)
        {
            _downloadClientProvider = downloadClientProvider;
            _historyService = historyService;
            _eventAggregator = eventAggregator;
            _configService = configService;
            _logger = logger;

            _failedDownloads = cacheManager.GetCache<FailedDownload>(GetType(), "queue");
        }

        public void MarkAsFailed(int historyId)
        {
            var item = _historyService.Get(historyId);
            PublishDownloadFailedEvent(new List<History.History> { item }, "Manually marked as failed");
        }

        private void CheckQueue(List<History.History> grabbedHistory, List<History.History> failedHistory)
        {
            var downloadClient = GetDownloadClient();

            if (downloadClient == null)
            {
                return;
            }

            var downloadClientQueue = downloadClient.GetQueue().ToList();
            var failedItems = downloadClientQueue.Where(q => q.Title.StartsWith("ENCRYPTED / ")).ToList();

            if (!failedItems.Any())
            {
                _logger.Debug("Yay! No encrypted downloads");
                return;
            }

            foreach (var failedItem in failedItems)
            {
                var failedLocal = failedItem;
                var historyItems = GetHistoryItems(grabbedHistory, failedLocal.Id);

                if (!historyItems.Any())
                {
                    _logger.Debug("Unable to find matching history item");
                    continue;
                }

                if (failedHistory.Any(h => failedLocal.Id.Equals(h.Data.GetValueOrDefault(DOWNLOAD_CLIENT_ID))))
                {
                    _logger.Debug("Already added to history as failed");
                    continue;
                }

                PublishDownloadFailedEvent(historyItems, "Encrypted download detected");

                if (_configService.RemoveFailedDownloads)
                {
                    _logger.Info("Removing encrypted download from queue: {0}", failedItem.Title.Replace("ENCRYPTED / ", ""));
                    downloadClient.RemoveFromQueue(failedItem.Id);
                }
            }
        }

        private void CheckHistory(List<History.History> grabbedHistory, List<History.History> failedHistory)
        {
            var downloadClient = GetDownloadClient();

            if (downloadClient == null)
            {
                return;
            }

            var downloadClientHistory = downloadClient.GetHistory(0, 20).ToList();
            var failedItems = downloadClientHistory.Where(h => h.Status == HistoryStatus.Failed).ToList();

            if (!failedItems.Any())
            {
                _logger.Debug("Yay! No failed downloads");
                return;
            }

            foreach (var failedItem in failedItems)
            {
                var failedLocal = failedItem;
                var historyItems = GetHistoryItems(grabbedHistory, failedLocal.Id);

                if (!historyItems.Any())
                {
                    _logger.Debug("Unable to find matching history item");
                    continue;
                }

                //TODO: Make this more configurable (ignore failure reasons) to support changes and other failures that should be ignored
                if (failedLocal.Message.Equals("Unpacking failed, write error or disk is full?",
                    StringComparison.InvariantCultureIgnoreCase))
                {
                    _logger.Debug("Failed due to lack of disk space, do not blacklist");
                    continue;
                }

                if (FailedDownloadForRecentRelease(failedItem, historyItems))
                {
                    _logger.Debug("Recent release Failed, do not blacklist");
                    continue;
                }
                
                if (failedHistory.Any(h => failedLocal.Id.Equals(h.Data.GetValueOrDefault(DOWNLOAD_CLIENT_ID))))
                {
                    _logger.Debug("Already added to history as failed");
                    continue;
                }

                PublishDownloadFailedEvent(historyItems, failedItem.Message);

                if (_configService.RemoveFailedDownloads)
                {
                    _logger.Info("Removing failed download from history: {0}", failedItem.Title);
                    downloadClient.RemoveFromHistory(failedItem.Id);
                }
            }
        }

        private List<History.History> GetHistoryItems(List<History.History> grabbedHistory, string downloadClientId)
        {
            return grabbedHistory.Where(h => downloadClientId.Equals(h.Data.GetValueOrDefault(DOWNLOAD_CLIENT_ID)))
                                 .ToList();
        }

        private void PublishDownloadFailedEvent(List<History.History> historyItems, string message)
        {
            var historyItem = historyItems.First();

            var downloadFailedEvent = new DownloadFailedEvent
                                      {
                                          SeriesId = historyItem.SeriesId,
                                          EpisodeIds = historyItems.Select(h => h.EpisodeId).ToList(),
                                          Quality = historyItem.Quality,
                                          SourceTitle = historyItem.SourceTitle,
                                          DownloadClient = historyItem.Data.GetValueOrDefault(DOWNLOAD_CLIENT),
                                          DownloadClientId = historyItem.Data.GetValueOrDefault(DOWNLOAD_CLIENT_ID),
                                          Message = message
                                      };

            downloadFailedEvent.Data = downloadFailedEvent.Data.Merge(historyItem.Data);

            _eventAggregator.PublishEvent(downloadFailedEvent);
        }

        private IDownloadClient GetDownloadClient()
        {
            var downloadClient = _downloadClientProvider.GetDownloadClient();

            if (downloadClient == null)
            {
                _logger.Debug("No download client is configured");
            }

            return downloadClient;
        }

        private bool FailedDownloadForRecentRelease(HistoryItem failedDownloadHistoryItem, List<History.History> matchingHistoryItems)
        {
            double ageHours;

            if (!Double.TryParse(matchingHistoryItems.First().Data.GetValueOrDefault("ageHours"), out ageHours))
            {
                _logger.Debug("Unable to determine age of failed download");
                return false;
            }

            if (ageHours > _configService.BlacklistGracePeriod)
            {
                _logger.Debug("Failed download is older than the grace period");
                return false;
            }

            var tracked = _failedDownloads.Get(failedDownloadHistoryItem.Id, () => new FailedDownload
                       {
                           DownloadClientHistoryItem = failedDownloadHistoryItem,
                           LastRetry = DateTime.UtcNow
                       }
            );

            if (tracked.RetryCount >= _configService.BlacklistRetryLimit)
            {
                _logger.Debug("Retry limit reached");
                return false;
            }

            if (tracked.LastRetry.AddMinutes(_configService.BlacklistRetryInterval) < DateTime.UtcNow)
            {
                _logger.Debug("Retrying failed release");
                tracked.LastRetry = DateTime.UtcNow;
                tracked.RetryCount++;

                try
                {
                    GetDownloadClient().RetryDownload(failedDownloadHistoryItem.Id);
                }

                catch (NotImplementedException ex)
                {
                    _logger.Debug("Retrying failed downloads is not supported by your download client");
                    return false;
                }
            }

            return true;
        }

        public void Execute(CheckForFailedDownloadCommand message)
        {
            if (!_configService.EnableFailedDownloadHandling)
            {
                _logger.Debug("Failed Download Handling is not enabled");
                return;
            }

            var grabbedHistory = _historyService.Grabbed();
            var failedHistory = _historyService.Failed();

            CheckQueue(grabbedHistory, failedHistory);
            CheckHistory(grabbedHistory, failedHistory);
        }
    }
}
