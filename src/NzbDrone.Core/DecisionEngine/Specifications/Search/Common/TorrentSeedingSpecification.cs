using NLog;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine.Specifications.Search.Common
{
    public class TorrentSeedingSpecification : IDecisionEngineSpecification
    {
        private readonly Logger _logger;

        public TorrentSeedingSpecification(Logger logger)
        {
            _logger = logger;
        }

        public RejectionType Type
        {
            get
            {
                return RejectionType.Permanent;
            }
        }


        public Decision IsSatisfiedBy(RemoteItem remoteItem, SearchCriteriaBase searchCriteria)
        {
            var torrentInfo = remoteItem.Release as TorrentInfo;

            if (torrentInfo == null)
            {
                return Decision.Accept();
            }

            if (torrentInfo.Seeders != null && torrentInfo.Seeders < 1)
            {
                _logger.Debug("Not enough seeders. ({0})", torrentInfo.Seeders);
                return Decision.Reject("Not enough seeders. ({0})", torrentInfo.Seeders);
            }

            return Decision.Accept();
        }
    }
}