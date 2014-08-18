﻿using System;
using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Download;
using NzbDrone.Core.HealthCheck.Checks;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Indexers.Wombles;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.HealthCheck.Checks
{
    [TestFixture]
    public class IndexerCheckFixture : CoreTest<IndexerCheck>
    {
        private IIndexer _indexer;

        private void GivenIndexer(Boolean supportsRss, Boolean supportsSearch)
        {
            var _indexer = Mocker.GetMock<IIndexer>();
            _indexer.SetupGet(s => s.SupportsRss).Returns(supportsRss);
            _indexer.SetupGet(s => s.SupportsSearch).Returns(supportsSearch);

            Mocker.GetMock<IIndexerFactory>()
                  .Setup(s => s.GetAvailableProviders())
                  .Returns(new List<IIndexer> { _indexer.Object });

            Mocker.GetMock<IIndexerFactory>()
                  .Setup(s => s.RssEnabled())
                  .Returns(new List<IIndexer>());

            Mocker.GetMock<IIndexerFactory>()
                  .Setup(s => s.SearchEnabled())
                  .Returns(new List<IIndexer>());
        }

        private void GivenRssEnabled()
        {
            Mocker.GetMock<IIndexerFactory>()
                  .Setup(s => s.RssEnabled())
                  .Returns(new List<IIndexer> { _indexer });
        }

        private void GivenSearchEnabled()
        {
            Mocker.GetMock<IIndexerFactory>()
                  .Setup(s => s.SearchEnabled())
                  .Returns(new List<IIndexer> { _indexer });
        }

        [Test]
        public void should_return_error_when_not_indexers_are_enabled()
        {
            Mocker.GetMock<IIndexerFactory>()
                  .Setup(s => s.GetAvailableProviders())
                  .Returns(new List<IIndexer>());

            Subject.Check().ShouldBeError();
        }

        [Test]
        public void should_return_warning_when_only_enabled_indexer_doesnt_support_search()
        {
            GivenIndexer(true, false);

            Subject.Check().ShouldBeWarning();
        }

        [Test]
        public void should_return_warning_when_only_enabled_indexer_doesnt_support_rss()
        {
            GivenIndexer(false, true);

            Subject.Check().ShouldBeWarning();
        }

        [Test]
        public void should_return_ok_when_multiple_indexers_are_enabled()
        {
            GivenRssEnabled();
            GivenSearchEnabled();

            var indexer1 = Mocker.GetMock<IIndexer>();
            indexer1.SetupGet(s => s.SupportsRss).Returns(true);
            indexer1.SetupGet(s => s.SupportsSearch).Returns(true);

            var indexer2 = Mocker.GetMock<Wombles>();
            indexer2.SetupGet(s => s.SupportsRss).Returns(true);
            indexer2.SetupGet(s => s.SupportsSearch).Returns(false);

            Mocker.GetMock<IIndexerFactory>()
                  .Setup(s => s.GetAvailableProviders())
                  .Returns(new List<IIndexer> { indexer1.Object, indexer2.Object });

            Subject.Check().ShouldBeOk();
        }

        [Test]
        public void should_return_ok_when_indexer_supports_rss_and_search()
        {
            GivenIndexer(true, true);
            GivenRssEnabled();
            GivenSearchEnabled();

            Subject.Check().ShouldBeOk();
        }

        [Test]
        public void should_return_warning_if_rss_is_supported_but_disabled()
        {
            GivenIndexer(true, true);
            GivenSearchEnabled();

            Subject.Check().ShouldBeWarning();
        }

        [Test]
        public void should_return_warning_if_search_is_supported_but_disabled()
        {
            GivenIndexer(true, true);
            GivenRssEnabled();

            Subject.Check().ShouldBeWarning();
        }
    }
}
