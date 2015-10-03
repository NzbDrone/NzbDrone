﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MediaFiles.TorrentInfo;
using NLog;
using NzbDrone.Core.Validation;
using FluentValidation.Results;
using NzbDrone.Core.Download.Clients.rTorrent;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.RemotePathMappings;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Download.Clients.RTorrent
{
    public class RTorrent : TorrentClientBase<RTorrentSettings>
    {
        private readonly IRTorrentProxy _proxy;
        private readonly IRTorrentDirectoryValidator _rTorrentDirectoryValidator;

        public RTorrent(IRTorrentProxy proxy,
                        ITorrentFileInfoReader torrentFileInfoReader,
                        IHttpClient httpClient,
                        IConfigService configService,
                        IDiskProvider diskProvider,
                        IRemotePathMappingService remotePathMappingService,
                        IRTorrentDirectoryValidator rTorrentDirectoryValidator,
                        Logger logger)
            : base(torrentFileInfoReader, httpClient, configService, diskProvider, remotePathMappingService, logger)
        {
            _proxy = proxy;
            _rTorrentDirectoryValidator = rTorrentDirectoryValidator;
        }

        protected override string AddFromMagnetLink(RemoteEpisode remoteEpisode, string hash, string magnetLink)
        {
            _proxy.AddTorrentFromUrl(magnetLink, Settings);

            // Wait until url has been resolved before returning
            var TRIES = 5;
            var RETRY_DELAY = 500; //ms
            var ready = false;

            for (var i = 0; i < TRIES; i++)
            {
                ready = _proxy.HasHashTorrent(hash, Settings);
                if (ready)
                {
                    break;
                }

                Thread.Sleep(RETRY_DELAY);
            }

            if (ready)
            {
                _proxy.SetTorrentLabel(hash, Settings.TvCategory, Settings);

                SetPriority(remoteEpisode, hash);
                SetDownloadDirectory(hash);

                _proxy.StartTorrent(hash, Settings);

                return hash;
            }
            else
            {
                _logger.Debug("Magnet {0} could not be resolved in {1} tries at {2} ms intervals.", magnetLink, TRIES, RETRY_DELAY);
                // Remove from client, since it is discarded
                RemoveItem(hash, true);

                return null;
            }
        }

        protected override string AddFromTorrentFile(RemoteEpisode remoteEpisode, string hash, string filename, byte[] fileContent)
        {
            _proxy.AddTorrentFromFile(filename, fileContent, Settings);
            _proxy.SetTorrentLabel(hash, Settings.TvCategory, Settings);

            SetPriority(remoteEpisode, hash);
            SetDownloadDirectory(hash);

            _proxy.StartTorrent(hash, Settings);

            return hash;
        }

        public override string Name
        {
            get
            {
                return "rTorrent";
            }
        }

        public override ProviderMessage Message
        {
            get
            {
                return new ProviderMessage("Sonarr is unable to remove torrents that have finished seeding when using rTorrent", ProviderMessageType.Warning);
            }
        }

        public override IEnumerable<DownloadClientItem> GetItems()
        {
            try
            {
                var torrents = _proxy.GetTorrents(Settings);

                _logger.Debug("Retrieved metadata of {0} torrents in client", torrents.Count);

                var items = new List<DownloadClientItem>();
                foreach (RTorrentTorrent torrent in torrents)
                {
                    // Don't concern ourselves with categories other than specified
                    if (torrent.Category != Settings.TvCategory) continue;

                    if (torrent.Path.StartsWith("."))
                    {
                        throw new DownloadClientException("Download paths paths must be absolute. Please specify variable \"directory\" in rTorrent.");
                    }

                    var item = new DownloadClientItem();
                    item.DownloadClient = Definition.Name;
                    item.Title = torrent.Name;
                    item.DownloadId = torrent.Hash;
                    item.OutputPath = _remotePathMappingService.RemapRemoteToLocal(Settings.Host, new OsPath(torrent.Path));
                    item.TotalSize = torrent.TotalSize;
                    item.RemainingSize = torrent.RemainingSize;
                    item.Category = torrent.Category;

                    if (torrent.DownRate > 0) {
                        var secondsLeft = torrent.RemainingSize / torrent.DownRate;
                        item.RemainingTime = TimeSpan.FromSeconds(secondsLeft);
                    } else {
                        item.RemainingTime = TimeSpan.Zero;
                    }

                    if (torrent.IsFinished) item.Status = DownloadItemStatus.Completed;
                    else if (torrent.IsActive) item.Status = DownloadItemStatus.Downloading;
                    else if (!torrent.IsActive) item.Status = DownloadItemStatus.Paused;

                    // Since we do not know the user's intent, do not let Sonarr to remove the torrent
                    item.IsReadOnly = true;

                    items.Add(item);
                }

                return items;
            }
            catch (DownloadClientException ex)
            {
                _logger.Error(ex, ex.Message);
                return Enumerable.Empty<DownloadClientItem>();
            }

        }

        public override void RemoveItem(string downloadId, bool deleteData)
        {
            if (deleteData)
            {
                DeleteItemData(downloadId);
            }

            _proxy.RemoveTorrent(downloadId, Settings);
        }

        public override DownloadClientStatus GetStatus()
        {
            // XXX: This function's correctness has not been considered

            var status = new DownloadClientStatus
            {
                IsLocalhost = Settings.Host == "127.0.0.1" || Settings.Host == "localhost"
            };

            return status;
        }

        protected override void Test(List<ValidationFailure> failures)
        {
            failures.AddIfNotNull(TestConnection());
            if (failures.Any()) return;
            failures.AddIfNotNull(TestGetTorrents());
            failures.AddIfNotNull(TestDirectory());
        }

        private ValidationFailure TestConnection()
        {
            try
            {
                var version = _proxy.GetVersion(Settings);

                if (new Version(version) < new Version("0.9.0"))
                {
                    return new ValidationFailure(string.Empty, "rTorrent version should be at least 0.9.0. Version reported is {0}", version);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, ex.Message);
                return new NzbDroneValidationFailure(string.Empty, "Unknown exception: " + ex.Message);
            }

            return null;
        }

        private ValidationFailure TestGetTorrents()
        {
            try
            {
                _proxy.GetTorrents(Settings);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, ex.Message);
                return new NzbDroneValidationFailure(string.Empty, "Failed to get the list of torrents: " + ex.Message);
            }

            return null;
        }

        private ValidationFailure TestDirectory()
        {
            var result = _rTorrentDirectoryValidator.Validate(Settings);

            if (result.IsValid)
            {
                return null;
            }

            return result.Errors.First();
        }

        private void SetPriority(RemoteEpisode remoteEpisode, string hash)
        {
            var priority = (RTorrentPriority)(remoteEpisode.IsRecentEpisode() ? Settings.RecentTvPriority : Settings.OlderTvPriority);
            _proxy.SetTorrentPriority(hash, priority, Settings);
        }

        private void SetDownloadDirectory(string hash)
        {
            if (Settings.TvDirectory.IsNotNullOrWhiteSpace())
            {
                _proxy.SetTorrentDownloadDirectory(hash, Settings.TvDirectory, Settings);
            }
        }
    }
}
