﻿using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Datastore.Extentions;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Test.Datastore.PagingSpecExtenstionsTests
{
    public class ToSortDirectionFixture
    {
        [Test]
        public void should_convert_ascending_to_asc()
        {
            var pagingSpec = new PagingSpec<Episode>
                {
                    Page = 1,
                    PageSize = 10,
                    SortDirection = SortDirection.Ascending,
                    SortKey = "AirDate"
                };

            pagingSpec.ToSortDirection().Should().Be(Marr.Data.QGen.SortDirection.Asc);
        }

        [Test]
        public void should_convert_descending_to_desc()
        {
            var pagingSpec = new PagingSpec<Episode>
            {
                Page = 1,
                PageSize = 10,
                SortDirection = SortDirection.Descending,
                SortKey = "AirDate"
            };

            pagingSpec.ToSortDirection().Should().Be(Marr.Data.QGen.SortDirection.Desc);
        }
    }
}
