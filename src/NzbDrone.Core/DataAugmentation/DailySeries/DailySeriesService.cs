using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Common.Cache;

namespace NzbDrone.Core.DataAugmentation.DailySeries
{
    public interface IDailySeriesService
    {
        bool IsDailySeries(int tvdbid);
    }

    public class DailySeriesService : IDailySeriesService
    {
        private readonly IDailySeriesDataProxy _proxy;

        private readonly ICached<List<int>> _cache;

        public DailySeriesService(IDailySeriesDataProxy proxy, ICached<List<int>> cache)
        {
            _proxy = proxy;
            _cache = cache;
        }

        public bool IsDailySeries(int tvdbid)
        {
            var dailySeries = _cache.Get("all", () => _proxy.GetDailySeriesIds().ToList(), TimeSpan.FromHours(1));
            return dailySeries.Any(i => i == tvdbid);
        }
    }
}