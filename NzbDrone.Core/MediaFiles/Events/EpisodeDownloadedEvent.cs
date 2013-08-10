﻿using NzbDrone.Common.Messaging;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.Events
{
    public class EpisodeDownloadedEvent : IEvent
    {
        public LocalEpisode Episode { get; private set; }

        public EpisodeDownloadedEvent(LocalEpisode episode)
        {
            Episode = episode;
        }
    }
}