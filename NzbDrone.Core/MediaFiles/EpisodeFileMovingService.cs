﻿using System;
using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Common;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.MediaFiles
{
    public interface IMoveEpisodeFiles
    {
        string MoveEpisodeFile(EpisodeFile episodeFile, Series series);
        string MoveEpisodeFile(EpisodeFile episodeFile, LocalEpisode localEpisode);
    }

    public class MoveEpisodeFiles : IMoveEpisodeFiles
    {
        private readonly IEpisodeService _episodeService;
        private readonly IBuildFileNames _buildFileNames;
        private readonly IEventAggregator _eventAggregator;
        private readonly IDiskProvider _diskProvider;
        private readonly Logger _logger;

        public MoveEpisodeFiles(IEpisodeService episodeService,
                                IBuildFileNames buildFileNames,
                                IEventAggregator eventAggregator,
                                IDiskProvider diskProvider,
                                Logger logger)
        {
            _episodeService = episodeService;
            _buildFileNames = buildFileNames;
            _eventAggregator = eventAggregator;
            _diskProvider = diskProvider;
            _logger = logger;
        }

        public string MoveEpisodeFile(EpisodeFile episodeFile, Series series)
        {
            var episodes = _episodeService.GetEpisodesByFileId(episodeFile.Id);
            var newFileName = _buildFileNames.BuildFilename(episodes, series, episodeFile);
            var filePath = _buildFileNames.BuildFilePath(series, episodes.First().SeasonNumber, newFileName, Path.GetExtension(episodeFile.Path));
            MoveFile(episodeFile, series, filePath);

            return filePath;
        }

        public string MoveEpisodeFile(EpisodeFile episodeFile, LocalEpisode localEpisode)
        {
            var newFileName = _buildFileNames.BuildFilename(localEpisode.Episodes, localEpisode.Series, episodeFile);
            var filePath = _buildFileNames.BuildFilePath(localEpisode.Series, localEpisode.SeasonNumber, newFileName, Path.GetExtension(episodeFile.Path));
            MoveFile(episodeFile, localEpisode.Series, filePath);
            
            return filePath;
        }

        private void MoveFile(EpisodeFile episodeFile, Series series, string destinationFilename)
        {
            if (!_diskProvider.FileExists(episodeFile.Path))
            {
                throw new FileNotFoundException("Episode file path does not exist", episodeFile.Path);
            }

            if (episodeFile.Path.PathEquals(destinationFilename))
            {
                throw new SameFilenameException("File not moved, source and destination are the same", episodeFile.Path);
            }

            _diskProvider.CreateFolder(new FileInfo(destinationFilename).DirectoryName);

            _logger.Debug("Moving [{0}] > [{1}]", episodeFile.Path, destinationFilename);
            _diskProvider.MoveFile(episodeFile.Path, destinationFilename);

            _logger.Trace("Setting last write time on series folder: {0}", series.Path);
            _diskProvider.SetFolderWriteTime(series.Path, episodeFile.DateAdded);

            if (series.SeasonFolder)
            {
                var seasonFolder = Path.GetDirectoryName(destinationFilename);

                _logger.Trace("Setting last write time on season folder: {0}", seasonFolder);
                _diskProvider.SetFolderWriteTime(seasonFolder, episodeFile.DateAdded);
            }

            //Wrapped in Try/Catch to prevent this from causing issues with remote NAS boxes, the move worked, which is more important.
            try
            {
                _diskProvider.InheritFolderPermissions(destinationFilename);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.Debug("Unable to apply folder permissions to: ", destinationFilename);
                _logger.TraceException(ex.Message, ex);
            }
        }
    }
}