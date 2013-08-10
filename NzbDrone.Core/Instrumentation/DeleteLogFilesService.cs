﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Common;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Messaging;
using NzbDrone.Core.Instrumentation.Commands;

namespace NzbDrone.Core.Instrumentation
{
    public interface IDeleteLogFilesService
    {
    }

    public class DeleteLogFilesService : IDeleteLogFilesService, IExecute<DeleteLogFilesCommand>
    {
        private readonly IDiskProvider _diskProvider;
        private readonly IAppFolderInfo _appFolderInfo;
        private readonly Logger _logger;

        public DeleteLogFilesService(IDiskProvider diskProvider, IAppFolderInfo appFolderInfo, Logger logger)
        {
            _diskProvider = diskProvider;
            _appFolderInfo = appFolderInfo;
            _logger = logger;
        }

        public void Execute(DeleteLogFilesCommand message)
        {
            var logFiles = _diskProvider.GetFiles(_appFolderInfo.GetLogFolder(), SearchOption.TopDirectoryOnly);

            foreach (var logFile in logFiles)
            {
                _diskProvider.DeleteFile(logFile);
            }
        }
    }
}
