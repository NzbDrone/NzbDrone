﻿using System;
using NzbDrone.Common;
using NzbDrone.Common.Messaging;

namespace NzbDrone.Core.Notifications.Xbmc
{
    public class TestXbmcCommand : ICommand
    {
        public String CommandId { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public int DisplayTime { get; set; }

        public TestXbmcCommand()
        {
            CommandId = HashUtil.GenerateCommandId();
        }
    }
}
