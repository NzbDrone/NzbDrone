﻿using System;
using System.IO;

namespace NzbDrone.Common
{
    public static class PathExtentions
    {
        private const string WEB_FOLDER = "NzbDrone.Web\\";
        private const string APP_DATA = "App_Data\\";
        public const string IIS_FOLDER = "IISExpress";
        public const string IIS_EXE = "iisexpress.exe";


        private const string LOG_CONFIG_FILE = "log.config";
        private const string APP_CONFIG_FILE = "config.xml";

        public const string NZBDRONE_DB_FILE = "nzbdrone.sdf";
        public const string LOG_DB_FILE = "log.sdf";

        private const string BACKUP_ZIP_FILE = "NzbDrone_Backup.zip";

        private const string UPDATE_SANDBOX_FOLDER_NAME = "nzbdrone_update\\";
        private const string UPDATE_PACKAGE_FOLDER_NAME = "nzbdrone\\";
        private const string UPDATE_BACKUP_FOLDER_NAME = "nzbdrone_backup\\";
        private const string UPDATE_CLIENT_EXE = "nzbdrone.update.exe";
        private const string UPDATE_CLIENT_FOLDER_NAME = "NzbDrone.Update\\";
        private const string UPDATE_LOG_FOLDER_NAME = "UpdateLogs\\";

        public static string NormalizePath(this string path)
        {
            if (String.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path can not be null or empty");

            var info = new FileInfo(path);

            if (info.FullName.StartsWith(@"\\")) //UNC
            {
                return info.FullName.TrimEnd('/', '\\', ' ');
            }

            return info.FullName.Trim('/', '\\', ' ');
        }


        public static string GetIISFolder(this EnviromentProvider enviromentProvider)
        {
            return Path.Combine(enviromentProvider.ApplicationPath, IIS_FOLDER);
        }

        public static string GetIISExe(this EnviromentProvider enviromentProvider)
        {
            return Path.Combine(enviromentProvider.GetIISFolder(), IIS_EXE);
        }

        public static string GetIISConfigPath(this EnviromentProvider enviromentProvider)
        {
            return Path.Combine(enviromentProvider.GetIISFolder(), "AppServer", "applicationhost.config");
        }

        public static string GetWebRoot(this EnviromentProvider enviromentProvider)
        {
            return Path.Combine(enviromentProvider.ApplicationPath, WEB_FOLDER);
        }

        public static string GetAppDataPath(this EnviromentProvider enviromentProvider)
        {
            return Path.Combine(enviromentProvider.GetWebRoot(), APP_DATA);
        }

        public static string GetNlogConfigPath(this EnviromentProvider enviromentProvider)
        {
            return Path.Combine(enviromentProvider.GetWebRoot(), LOG_CONFIG_FILE);
        }

        public static string GetConfigPath(this EnviromentProvider enviromentProvider)
        {
            return Path.Combine(enviromentProvider.ApplicationPath, APP_CONFIG_FILE);
        }

        public static string GetNzbDronoeDbFile(this EnviromentProvider enviromentProvider)
        {
            return Path.Combine(enviromentProvider.GetAppDataPath(), NZBDRONE_DB_FILE);
        }

        public static string GetLogDbFileDbFile(this EnviromentProvider enviromentProvider)
        {
            return Path.Combine(enviromentProvider.GetAppDataPath(), LOG_DB_FILE);
        }

        public static string GetMediaCoverPath(this EnviromentProvider enviromentProvider)
        {
            return Path.Combine(enviromentProvider.GetWebRoot(), "MediaCover");
        }

        public static string GetBannerPath(this EnviromentProvider enviromentProvider)
        {
            return Path.Combine(enviromentProvider.GetMediaCoverPath(), "Banners");
        }

        public static string GetFanArthPath(this EnviromentProvider enviromentProvider)
        {
            return Path.Combine(enviromentProvider.GetMediaCoverPath(), "Fanarts");
        }

        public static string GetCacheFolder(this EnviromentProvider enviromentProvider)
        {
            return Path.Combine(enviromentProvider.GetWebRoot(), "Cache");
        }

        public static string GetUpdateLogFolder(this EnviromentProvider enviromentProvider)
        {
            return Path.Combine(enviromentProvider.ApplicationPath, UPDATE_LOG_FOLDER_NAME);
        }

        public static string GetUpdateSandboxFolder(this EnviromentProvider enviromentProvider)
        {
            return Path.Combine(enviromentProvider.SystemTemp, UPDATE_SANDBOX_FOLDER_NAME);
        }

        public static string GetUpdateBackUpFolder(this EnviromentProvider enviromentProvider)
        {
            return Path.Combine(enviromentProvider.GetUpdateSandboxFolder(), UPDATE_BACKUP_FOLDER_NAME);
        }

        public static string GetUpdatePackageFolder(this EnviromentProvider enviromentProvider)
        {
            return Path.Combine(enviromentProvider.GetUpdateSandboxFolder(), UPDATE_PACKAGE_FOLDER_NAME);
        }

        public static string GetUpdateClientFolder(this EnviromentProvider enviromentProvider)
        {
            return Path.Combine(enviromentProvider.GetUpdatePackageFolder(), UPDATE_CLIENT_FOLDER_NAME);
        }

        public static string GetUpdateClientExePath(this EnviromentProvider enviromentProvider)
        {
            return Path.Combine(enviromentProvider.GetUpdateSandboxFolder(), UPDATE_CLIENT_EXE);
        }

        public static string GetSandboxLogFolder(this EnviromentProvider enviromentProvider)
        {
            return Path.Combine(enviromentProvider.GetUpdateSandboxFolder(), UPDATE_LOG_FOLDER_NAME);
        }

        public static string GetLogFileName(this EnviromentProvider enviromentProvider)
        {
            return Path.Combine(enviromentProvider.ApplicationPath, "nzbdrone.log.txt");
        }

        public static string GetArchivedLogFileName(this EnviromentProvider enviromentProvider)
        {
            return Path.Combine(enviromentProvider.ApplicationPath, "nzbdrone.log.0.txt");
        }

        public static string GetConfigBackupFile(this EnviromentProvider enviromentProvider)
        {
            return Path.Combine(enviromentProvider.GetAppDataPath(), BACKUP_ZIP_FILE);
        }
    }
}