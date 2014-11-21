﻿using System;
using System.Diagnostics;
using System.IO;
using NLog;
using NLog.Config;
using NLog.Targets;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Processes;

namespace NzbDrone.Common.Instrumentation
{
    public static class LogTargets
    {
        public static void Register(IStartupContext startupContext, bool updateApp, bool inConsole)
        {
            var appFolderInfo = new AppFolderInfo(startupContext);

            LogManager.Configuration = new LoggingConfiguration();

            if (Debugger.IsAttached)
            {
                RegisterDebugger();
            }

            //Disabling for now - until its fixed or we yank it out
            //RegisterExceptron();

            if (updateApp)
            {
                RegisterUpdateFile(appFolderInfo);
            }
            else
            {
                if (inConsole && (OsInfo.IsMono || RuntimeInfoBase.IsUserInteractive))
                {
                    RegisterConsole();
                }

                RegisterAppFile(appFolderInfo);
            }

            LogManager.ReconfigExistingLoggers();
        }

        private static void RegisterDebugger()
        {
            DebuggerTarget target = new DebuggerTarget();
            target.Name = "debuggerLogger";
            target.Layout = "[${level}] [${threadid}] ${logger}: ${message} ${onexception:inner=${newline}${newline}${exception:format=ToString}${newline}}";

            var loggingRule = new LoggingRule("*", LogLevel.Trace, target);
            LogManager.Configuration.AddTarget("debugger", target);
            LogManager.Configuration.LoggingRules.Add(loggingRule);
        }


        private static void RegisterConsole()
        {
            var level = LogLevel.Trace;

            if (RuntimeInfoBase.IsProduction)
            {
                level = LogLevel.Info;
            }

            var coloredConsoleTarget = new ColoredConsoleTarget();

            coloredConsoleTarget.Name = "consoleLogger";
            coloredConsoleTarget.Layout = "[${level}] ${logger}: ${message} ${onexception:inner=${newline}${newline}${exception:format=ToString}${newline}}";

            var loggingRule = new LoggingRule("*", level, coloredConsoleTarget);

            LogManager.Configuration.AddTarget("console", coloredConsoleTarget);
            LogManager.Configuration.LoggingRules.Add(loggingRule);
        }

        const string FILE_LOG_LAYOUT = @"${date:format=yy-M-d HH\:mm\:ss.f}|${level}|${logger}|${message}${onexception:inner=${newline}${newline}${exception:format=ToString}${newline}}";

        private static void RegisterAppFile(IAppFolderInfo appFolderInfo)
        {
            var fileTarget = new NzbDroneFileTarget();

            fileTarget.Name = "rollingFileLogger";
            fileTarget.FileName = Path.Combine(appFolderInfo.GetLogFolder(), "nzbdrone.txt");
            fileTarget.AutoFlush = true;
            fileTarget.KeepFileOpen = false;
            fileTarget.ConcurrentWrites = false;
            fileTarget.ConcurrentWriteAttemptDelay = 50;
            fileTarget.ConcurrentWriteAttempts = 10;
            fileTarget.ArchiveAboveSize = 1024000;
            fileTarget.MaxArchiveFiles = 5;
            fileTarget.EnableFileDelete = true;
            fileTarget.ArchiveNumbering = ArchiveNumberingMode.Rolling;
            fileTarget.Layout = FILE_LOG_LAYOUT;

            var loggingRule = new LoggingRule("*", LogLevel.Trace, fileTarget);

            LogManager.Configuration.AddTarget("appfile", fileTarget);
            LogManager.Configuration.LoggingRules.Add(loggingRule);
        }

        private static void RegisterUpdateFile(IAppFolderInfo appFolderInfo)
        {
            var fileTarget = new FileTarget();

            fileTarget.Name = "updateFileLogger";
            fileTarget.FileName = Path.Combine(appFolderInfo.GetUpdateLogFolder(), DateTime.Now.ToString("yyyy.MM.dd-HH.mm") + ".txt");
            fileTarget.AutoFlush = true;
            fileTarget.KeepFileOpen = false;
            fileTarget.ConcurrentWrites = false;
            fileTarget.ConcurrentWriteAttemptDelay = 50;
            fileTarget.ConcurrentWriteAttempts = 100;
            fileTarget.Layout = FILE_LOG_LAYOUT;

            var loggingRule = new LoggingRule("*", LogLevel.Trace, fileTarget);

            LogManager.Configuration.AddTarget("updateFile", fileTarget);
            LogManager.Configuration.LoggingRules.Add(loggingRule);
        }

        private static void RegisterExceptron()
        {
            var exceptronTarget = new ExceptronTarget();
            var rule = new LoggingRule("*", LogLevel.Warn, exceptronTarget);

            LogManager.Configuration.AddTarget("ExceptronTarget", exceptronTarget);
            LogManager.Configuration.LoggingRules.Add(rule);
        }
       
    }
}