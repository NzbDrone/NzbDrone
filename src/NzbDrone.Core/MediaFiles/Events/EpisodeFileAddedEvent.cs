﻿using NzbDrone.Common.Messaging;
using NzbDrone.Core.MediaFiles.Series;

namespace NzbDrone.Core.MediaFiles.Events
{
    public class EpisodeFileAddedEvent : IEvent
    {
        public EpisodeFile EpisodeFile { get; private set; }

        public EpisodeFileAddedEvent(EpisodeFile episodeFile)
        {
            EpisodeFile = episodeFile;
        }
    }
}