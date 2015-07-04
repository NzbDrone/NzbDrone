﻿using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Indexers
{
    public class IndexerStatus : ModelBase
    {
        public int IndexerId { get; set; }

        public DateTime? FirstFailure { get; set; }
        public DateTime? LastFailure { get; set; }
        public int FailureEscalation { get; set; }
        public DateTime? DisabledTill { get; set; }

        public DateTime? LastContinuousRssSync { get; set; }
        public ReleaseInfo LastRssSyncReleaseInfo { get; set; }
    }
}
