﻿using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Processes;
using NzbDrone.Core.MediaFiles.Movies;
using NzbDrone.Core.MediaFiles.Series;
using NzbDrone.Core.Movies;
using NzbDrone.Core.Tv;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Notifications.CustomScript
{
    public interface ICustomScriptService
    {
        void OnDownload(Series series, EpisodeFile episodeFile, string sourcePath, CustomScriptSettings settings);
        void OnDownloadMovie(Movie movie, MovieFile movieFile, string sourcePath, CustomScriptSettings settings);
        void OnRename(Series series, CustomScriptSettings settings);
        void OnRenameMovie(Movie movie, CustomScriptSettings settings);
        ValidationFailure Test(CustomScriptSettings settings);
    }

    public class CustomScriptService : ICustomScriptService
    {
        private readonly IProcessProvider _processProvider;
        private readonly IDiskProvider _diskProvider;
        private readonly Logger _logger;

        public CustomScriptService(IProcessProvider processProvider, IDiskProvider diskProvider, Logger logger)
        {
            _processProvider = processProvider;
            _diskProvider = diskProvider;
            _logger = logger;
        }

        public void OnDownload(Series series, EpisodeFile episodeFile, string sourcePath, CustomScriptSettings settings)
        {
            var environmentVariables = new StringDictionary();

            environmentVariables.Add("Sonarr_EventType", "Download");
            environmentVariables.Add("Sonarr_Series_Id", series.Id.ToString());
            environmentVariables.Add("Sonarr_Series_Title", series.Title);
            environmentVariables.Add("Sonarr_Series_Path", series.Path);
            environmentVariables.Add("Sonarr_Series_TvdbId", series.TvdbId.ToString());
            environmentVariables.Add("Sonarr_EpisodeFile_Id", episodeFile.Id.ToString());
            environmentVariables.Add("Sonarr_EpisodeFile_RelativePath", episodeFile.RelativePath);
            environmentVariables.Add("Sonarr_EpisodeFile_Path", Path.Combine(series.Path, episodeFile.RelativePath));
            environmentVariables.Add("Sonarr_EpisodeFile_SeasonNumber", episodeFile.SeasonNumber.ToString());
            environmentVariables.Add("Sonarr_EpisodeFile_EpisodeNumbers", String.Join(",", episodeFile.Episodes.Value.Select(e => e.EpisodeNumber)));
            environmentVariables.Add("Sonarr_EpisodeFile_EpisodeAirDates", String.Join(",", episodeFile.Episodes.Value.Select(e => e.AirDate)));
            environmentVariables.Add("Sonarr_EpisodeFile_EpisodeAirDatesUtc", String.Join(",", episodeFile.Episodes.Value.Select(e => e.AirDateUtc)));
            environmentVariables.Add("Sonarr_EpisodeFile_Quality", episodeFile.Quality.Quality.Name);
            environmentVariables.Add("Sonarr_EpisodeFile_QualityVersion", episodeFile.Quality.Revision.Version.ToString());
            environmentVariables.Add("Sonarr_EpisodeFile_ReleaseGroup", episodeFile.ReleaseGroup ?? String.Empty);
            environmentVariables.Add("Sonarr_EpisodeFile_SceneName", episodeFile.SceneName ?? String.Empty);
            environmentVariables.Add("Sonarr_EpisodeFile_SourcePath", sourcePath);
            environmentVariables.Add("Sonarr_EpisodeFile_SourceFolder", Path.GetDirectoryName(sourcePath));

            ExecuteScript(environmentVariables, settings);
        }

        public void OnDownloadMovie(Movie movie, MovieFile movieFile, string sourcePath, CustomScriptSettings settings)
        {
            var environmentVariables = new StringDictionary();

            environmentVariables.Add("Sonarr_EventType", "DownloadMovie");
            environmentVariables.Add("Sonarr_Movie_Id", movie.Id.ToString());
            environmentVariables.Add("Sonarr_Movie_Title", movie.Title);
            environmentVariables.Add("Sonarr_Movie_Path", movie.Path);
            environmentVariables.Add("Sonarr_Movie_TmdbId", movie.TmdbId.ToString());
            environmentVariables.Add("Sonarr_MovieFile_Id", movieFile.Id.ToString());
            environmentVariables.Add("Sonarr_MovieFile_RelativePath", movieFile.RelativePath);
            environmentVariables.Add("Sonarr_MovieFile_Path", Path.Combine(movie.Path, movieFile.RelativePath));
            environmentVariables.Add("Sonarr.MovieFile_Quality", movieFile.Quality.Quality.Name);
            environmentVariables.Add("Sonarr.MovieFile_QualityVersion", movieFile.Quality.Revision.Version.ToString());
            environmentVariables.Add("Sonarr.MovieFile_ReleaseGroup", movieFile.ReleaseGroup ?? String.Empty);
            environmentVariables.Add("Sonarr.MovieFile_SceneName", movieFile.SceneName ?? String.Empty);
            environmentVariables.Add("Sonarr_MovieFile_SourcePath", sourcePath);
            environmentVariables.Add("Sonarr_MovieFile_SourceFolder", Path.GetDirectoryName(sourcePath));

            ExecuteScript(environmentVariables, settings);
        }

        public void OnRename(Series series, CustomScriptSettings settings)
        {
            var environmentVariables = new StringDictionary();

            environmentVariables.Add("Sonarr_EventType", "Rename");
            environmentVariables.Add("Sonarr_Series_Id", series.Id.ToString());
            environmentVariables.Add("Sonarr_Series_Title", series.Title);
            environmentVariables.Add("Sonarr_Series_Path", series.Path);
            environmentVariables.Add("Sonarr_Series_TvdbId", series.TvdbId.ToString());

            ExecuteScript(environmentVariables, settings);
        }

        public void OnRenameMovie(Movie movie, CustomScriptSettings settings)
        {
            var environmentVariables = new StringDictionary();

            environmentVariables.Add("Sonarr_EventType", "RenameMovie");
            environmentVariables.Add("Sonarr_Movie_Id", movie.Id.ToString());
            environmentVariables.Add("Sonarr_Movie_Title", movie.Title);
            environmentVariables.Add("Sonarr_Movie_Path", movie.Path);
            environmentVariables.Add("Sonarr_Movie_TmdbId", movie.TmdbId.ToString());

            ExecuteScript(environmentVariables, settings);
        }

        public ValidationFailure Test(CustomScriptSettings settings)
        {
            if (!_diskProvider.FileExists(settings.Path))
            {
                return new NzbDroneValidationFailure("Path", "File does not exist");
            }

            return null;
        }

        private void ExecuteScript(StringDictionary environmentVariables, CustomScriptSettings settings)
        {
            _logger.Debug("Executing external script: {0}", settings.Path);

            var process = _processProvider.StartAndCapture(settings.Path, settings.Arguments, environmentVariables);

            _logger.Debug("Executed external script: {0} - Status: {1}", settings.Path, process.ExitCode);
            _logger.Debug("Script Output: \r\n{0}", String.Join("\r\n", process.Lines));
        }
    }
}
