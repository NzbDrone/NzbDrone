﻿using System.Linq;
using NLog;
using NzbDrone.Common;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles
{
    public interface IUpgradeMediaFiles
    {
        EpisodeFile UpgradeEpisodeFile(EpisodeFile episodeFile, LocalEpisode localEpisode);
    }

    public class UpgradeMediaFileService : IUpgradeMediaFiles
    {
        private readonly IRecycleBinProvider _recycleBinProvider;
        private readonly IMediaFileService _mediaFileService;
        private readonly IMoveEpisodeFiles _episodeFileMover;
        private readonly IDiskProvider _diskProvider;
        private readonly Logger _logger;

        public UpgradeMediaFileService(IRecycleBinProvider recycleBinProvider,
                                       IMediaFileService mediaFileService,
                                       IMoveEpisodeFiles episodeFileMover,
                                       IDiskProvider diskProvider,
                                       Logger logger)
        {
            _recycleBinProvider = recycleBinProvider;
            _mediaFileService = mediaFileService;
            _episodeFileMover = episodeFileMover;
            _diskProvider = diskProvider;
            _logger = logger;
        }

        public EpisodeFile UpgradeEpisodeFile(EpisodeFile episodeFile, LocalEpisode localEpisode)
        {
            var existingFiles = localEpisode.Episodes
                                            .Where(e => e.EpisodeFileId > 0)
                                            .Select(e => e.EpisodeFile.Value)
                                            .GroupBy(e => e.Id);

            episodeFile = _episodeFileMover.PreMoveEpisodeFile(episodeFile, localEpisode);

            foreach (var existingFile in existingFiles)
            {
                var file = existingFile.First();

                if (_diskProvider.FileExists(file.Path))
                {
                    _logger.Trace("Removing existing episode file: {0}", file);
                    _recycleBinProvider.DeleteFile(file.Path);
                }

                _mediaFileService.Delete(file, true);
            }

            _logger.Trace("Moving episode file: {0}", episodeFile);
            return _episodeFileMover.MoveEpisodeFile(episodeFile, localEpisode);
        }
    }
}
