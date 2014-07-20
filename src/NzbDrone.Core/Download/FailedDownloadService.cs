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
        void MarkAsFailed(TrackedDownload trackedDownload, History.History grabbedHistory);
        void CheckForFailedItem(IDownloadClient downloadClient, TrackedDownload trackedDownload, List<History.History> grabbedHistory, List<History.History> failedHistory);
    }

    public class FailedDownloadService : IFailedDownloadService
    {
        private readonly IHistoryService _historyService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IConfigService _configService;
        private readonly Logger _logger;

        public FailedDownloadService(IHistoryService historyService,
                                     IEventAggregator eventAggregator,
                                     IConfigService configService,
                                     Logger logger)
        {
            _historyService = historyService;
            _eventAggregator = eventAggregator;
            _configService = configService;
            _logger = logger;
        }

        public void MarkAsFailed(TrackedDownload trackedDownload, History.History grabbedHistory)
        {
            if (trackedDownload != null && trackedDownload.State == TrackedDownloadState.Downloading)
            {
                trackedDownload.State = TrackedDownloadState.DownloadFailed;
            }

            PublishDownloadFailedEvent(new List<History.History> { grabbedHistory }, "Manually marked as failed");
        }

        public void CheckForFailedItem(IDownloadClient downloadClient, TrackedDownload trackedDownload, List<History.History> grabbedHistory, List<History.History> failedHistory)
        {
            if (!_configService.EnableFailedDownloadHandling)
            {
                return;
            }

            if (trackedDownload.DownloadItem.IsEncrypted && trackedDownload.State == TrackedDownloadState.Downloading)
            {
                var grabbedItems = GetHistoryItems(grabbedHistory, trackedDownload.DownloadItem.DownloadClientId);

                if (!grabbedItems.Any())
                {
                    UpdateStatusMessage(trackedDownload, LogLevel.Debug, "Download was not grabbed by drone, ignoring download");
                    return;
                }

                trackedDownload.State = TrackedDownloadState.DownloadFailed;

                var failedItems = GetHistoryItems(failedHistory, trackedDownload.DownloadItem.DownloadClientId);

                if (failedItems.Any())
                {
                    UpdateStatusMessage(trackedDownload, LogLevel.Debug, "Already added to history as failed.");
                }
                else
                {
                    PublishDownloadFailedEvent(grabbedItems, "Encrypted download detected");
                }
            }

            if (trackedDownload.DownloadItem.Status == DownloadItemStatus.Failed && trackedDownload.State == TrackedDownloadState.Downloading)
            {
                var grabbedItems = GetHistoryItems(grabbedHistory, trackedDownload.DownloadItem.DownloadClientId);

                if (!grabbedItems.Any())
                {
                    UpdateStatusMessage(trackedDownload, LogLevel.Debug, "Download wasn't grabbed by drone or not in a category, ignoring download.");
                    return;
                }

                //TODO: Make this more configurable (ignore failure reasons) to support changes and other failures that should be ignored
                if (trackedDownload.DownloadItem.Message.Equals("Unpacking failed, write error or disk is full?",
                    StringComparison.InvariantCultureIgnoreCase))
                {
                    UpdateStatusMessage(trackedDownload, LogLevel.Error, "Download failed due to lack of disk space, not blacklisting.");
                    return;
                }

                if (FailedDownloadForRecentRelease(downloadClient, trackedDownload, grabbedItems))
                {
                    _logger.Debug("[{0}] Recent release Failed, do not blacklist.", trackedDownload.DownloadItem.Title);
                    return;
                }

                trackedDownload.State = TrackedDownloadState.DownloadFailed;

                var failedItems = GetHistoryItems(failedHistory, trackedDownload.DownloadItem.DownloadClientId);

                if (failedItems.Any())
                {
                    UpdateStatusMessage(trackedDownload, LogLevel.Debug, "Already added to history as failed.");
                }
                else
                {
                    PublishDownloadFailedEvent(grabbedItems, trackedDownload.DownloadItem.Message);
                }
            }

            if (trackedDownload.DownloadItem.Status != DownloadItemStatus.Failed && trackedDownload.State == TrackedDownloadState.Downloading)
            {
                var grabbedItems = GetHistoryItems(grabbedHistory, trackedDownload.DownloadItem.DownloadClientId);
                var failedItems = GetHistoryItems(failedHistory, trackedDownload.DownloadItem.DownloadClientId);

                if (grabbedItems.Any() && failedItems.Any())
                {
                    UpdateStatusMessage(trackedDownload, LogLevel.Debug, "Already added to history as failed, updating tracked state.");
                    trackedDownload.State = TrackedDownloadState.DownloadFailed;
                }
            }

            if (_configService.RemoveFailedDownloads && trackedDownload.State == TrackedDownloadState.DownloadFailed)
            {
                try
                {
                    _logger.Debug("[{0}] Removing failed download from client.", trackedDownload.DownloadItem.Title);
                    downloadClient.RemoveItem(trackedDownload.DownloadItem.DownloadClientId);

                    trackedDownload.State = TrackedDownloadState.Removed;
                }
                catch (NotSupportedException)
                {
                    UpdateStatusMessage(trackedDownload, LogLevel.Debug, "Removing item not supported by your download client.");
                }
            }
        }

        private bool FailedDownloadForRecentRelease(IDownloadClient downloadClient, TrackedDownload trackedDownload, List<History.History> matchingHistoryItems)
        {
            double ageHours;

            if (!Double.TryParse(matchingHistoryItems.First().Data.GetValueOrDefault("ageHours"), out ageHours))
            {
                UpdateStatusMessage(trackedDownload, LogLevel.Info, "Unable to determine age of failed download.");
                return false;
            }

            if (ageHours > _configService.BlacklistGracePeriod)
            {
                UpdateStatusMessage(trackedDownload, LogLevel.Info, "Download Failed, Failed download is older than the grace period.");
                return false;
            }

            if (trackedDownload.RetryCount >= _configService.BlacklistRetryLimit)
            {
                UpdateStatusMessage(trackedDownload, LogLevel.Info, "Download Failed, Retry limit reached.");
                return false;
            }

            if (trackedDownload.LastRetry == new DateTime())
            {
                trackedDownload.LastRetry = DateTime.UtcNow;
            }

            if (trackedDownload.LastRetry.AddMinutes(_configService.BlacklistRetryInterval) < DateTime.UtcNow)
            {
                trackedDownload.LastRetry = DateTime.UtcNow;
                trackedDownload.RetryCount++;

                UpdateStatusMessage(trackedDownload, LogLevel.Info, "Download Failed, initiating retry attempt {0}/{1}.", trackedDownload.RetryCount, _configService.BlacklistRetryLimit);

                try
                {
                    var newDownloadClientId = downloadClient.RetryDownload(trackedDownload.DownloadItem.DownloadClientId);

                    if (newDownloadClientId != trackedDownload.DownloadItem.DownloadClientId)
                    {
                        var oldTrackingId = trackedDownload.TrackingId;
                        var newTrackingId = String.Format("{0}-{1}", downloadClient.Definition.Id, newDownloadClientId);

                        trackedDownload.TrackingId = newTrackingId;
                        trackedDownload.DownloadItem.DownloadClientId = newDownloadClientId;

                        _logger.Debug("[{0}] Changed id from {1} to {2}.", trackedDownload.DownloadItem.Title, oldTrackingId, newTrackingId);
                        var newHistoryData = new Dictionary<String, String>(matchingHistoryItems.First().Data);
                        newHistoryData[DownloadTrackingService.DOWNLOAD_CLIENT_ID] = newDownloadClientId;
                        _historyService.UpdateHistoryData(matchingHistoryItems.First().Id, newHistoryData);
                    }
                }
                catch (NotSupportedException)
                {
                    UpdateStatusMessage(trackedDownload, LogLevel.Debug, "Retrying failed downloads is not supported by your download client.");
                    return false;
                }
            }
            else
            {
                UpdateStatusMessage(trackedDownload, LogLevel.Warn, "Download Failed, waiting for retry interval to expire.");
            }

            return true;
        }

        private List<History.History> GetHistoryItems(List<History.History> grabbedHistory, string downloadClientId)
        {
            return grabbedHistory.Where(h => downloadClientId.Equals(h.Data.GetValueOrDefault(DownloadTrackingService.DOWNLOAD_CLIENT_ID)))
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
                DownloadClient = historyItem.Data.GetValueOrDefault(DownloadTrackingService.DOWNLOAD_CLIENT),
                DownloadClientId = historyItem.Data.GetValueOrDefault(DownloadTrackingService.DOWNLOAD_CLIENT_ID),
                Message = message
            };

            downloadFailedEvent.Data = downloadFailedEvent.Data.Merge(historyItem.Data);

            _eventAggregator.PublishEvent(downloadFailedEvent);
        }

        private void UpdateStatusMessage(TrackedDownload trackedDownload, LogLevel logLevel, String message, params object[] args)
        {
            var statusMessage = String.Format(message, args);
            var logMessage = String.Format("[{0}] {1}", trackedDownload.DownloadItem.Title, statusMessage);

            if (trackedDownload.StatusMessage != statusMessage)
            {
                trackedDownload.HasError = logLevel >= LogLevel.Warn;
                trackedDownload.StatusMessage = statusMessage;
                _logger.Log(logLevel, logMessage);
            }
            else
            {
                _logger.Debug(logMessage);
            }
        }
    }
}
