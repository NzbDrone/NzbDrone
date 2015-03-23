﻿using System;
using System.Diagnostics;
using System.IO;
using NzbDrone.Common.EnvironmentInfo;

namespace NzbDrone.Common
{
    public interface IConsoleService
    {
        void PrintHelp();
        void PrintServiceAlreadyExist();
        void PrintServiceDoesNotExist();
    }

    public class ConsoleService : IConsoleService
    {
        public static bool IsConsoleAvailable
        {
            get { return Console.In != StreamReader.Null; }
        }

        public void PrintHelp()
        {
            Console.WriteLine();
            Console.WriteLine("     Usage: {0} <command> ", Process.GetCurrentProcess().MainModule.ModuleName);
            Console.WriteLine("     Commands:");
            Console.WriteLine("                 /{0} Install the application as a Windows Service ({1}).", StartupContext.INSTALL_SERVICE, ServiceProvider.NZBDRONE_SERVICE_NAME);
            Console.WriteLine("                 /{0} Uninstall already installed Windows Service ({1}).", StartupContext.UNINSTALL_SERVICE, ServiceProvider.NZBDRONE_SERVICE_NAME);
            Console.WriteLine("                 /{0} Don't open NzbDrone in a browser", StartupContext.NO_BROWSER);
            if (!OsInfo.IsWindows)
            {
                Console.WriteLine("                 /{0}=PATH Path of file to write process identifier to", StartupContext.PID_FILE);
            }
            Console.WriteLine("                 <No Arguments>  Run application in console mode.");
        }

        public void PrintServiceAlreadyExist()
        {
            Console.WriteLine("A service with the same name ({0}) already exists. Aborting installation", ServiceProvider.NZBDRONE_SERVICE_NAME);
        }

        public void PrintServiceDoesNotExist()
        {
            Console.WriteLine("Can't find service ({0})", ServiceProvider.NZBDRONE_SERVICE_NAME);
        }
    }
}