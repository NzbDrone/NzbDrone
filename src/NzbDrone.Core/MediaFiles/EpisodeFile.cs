﻿using System;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.MediaFiles
{
    public class EpisodeFile : ModelBase
    {
        public int SeriesId { get; set; }
        public int SeasonNumber { get; set; }
        public string Path { get; set; }
        public long Size { get; set; }
        public DateTime DateAdded { get; set; }
        public string SceneName { get; set; }
        public string ReleaseGroup { get; set; }
        public QualityModel Quality { get; set; }
        public LazyList<Episode> Episodes { get; set; }

        public override string ToString()
        {
            return String.Format("[{0}] {1}", Id, Path);
        }
    }
}