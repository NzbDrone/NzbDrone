﻿using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common.Http;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Tv;
using NzbDrone.Test.Common;
using NzbDrone.Test.Common.Categories;

namespace NzbDrone.Core.Test.MetadataSourceTests
{
    [TestFixture]
    [IntegrationTest]
    public class TraktProxyFixture : CoreTest<TraktProxy>
    {


        [SetUp]
        public void Setup()
        {
            UseRealHttp();
        }

        [TestCase("The Simpsons", "The Simpsons")]
        [TestCase("South Park", "South Park")]
        [TestCase("Franklin & Bash", "Franklin & Bash")]
        [TestCase("Mr. D", "Mr. D")]
        [TestCase("Rob & Big", "Rob and Big")]
        [TestCase("M*A*S*H", "M*A*S*H")]
        [TestCase("imdb:tt0436992", "Doctor Who (2005)")]
        [TestCase("imdb:0436992", "Doctor Who (2005)")]
        [TestCase("IMDB:0436992", "Doctor Who (2005)")]
        [TestCase("IMDB: 0436992 ", "Doctor Who (2005)")]
        [TestCase("tvdb:78804", "Doctor Who (2005)")]
        [TestCase("TVDB:78804", "Doctor Who (2005)")]
        [TestCase("TVDB: 78804 ", "Doctor Who (2005)")]
        public void successful_search(string title, string expected)
        {
            var result = Subject.SearchForNewSeries(title);

            result.Should().NotBeEmpty();

            result[0].Title.Should().Be(expected);
        }

        [Test]
        public void no_search_result()
        {
            var result = Subject.SearchForNewSeries(Guid.NewGuid().ToString());
            result.Should().BeEmpty();
        }

        [TestCase(75978)]
        [TestCase(83462)]
        [TestCase(266189)]
        public void should_be_able_to_get_series_detail(int tvdbId)
        {
            var details = Subject.GetSeriesInfo(tvdbId);

            ValidateSeries(details.Item1);
            ValidateEpisodes(details.Item2);
        }

        [Test]
        public void getting_details_of_invalid_series()
        {
            Assert.Throws<HttpException>(() => Subject.GetSeriesInfo(Int32.MaxValue));

            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void should_not_have_period_at_start_of_title_slug()
        {
            var details = Subject.GetSeriesInfo(79099);

            details.Item1.TitleSlug.Should().Be("dothack");
        }

        private void ValidateSeries(Series series)
        {
            series.Should().NotBeNull();
            series.Title.Should().NotBeNullOrWhiteSpace();
            series.CleanTitle.Should().Be(Parser.Parser.CleanSeriesTitle(series.Title));
            series.SortTitle.Should().Be(Parser.Parser.NormalizeEpisodeTitle(series.Title));
            series.Overview.Should().NotBeNullOrWhiteSpace();
            series.AirTime.Should().NotBeNullOrWhiteSpace();
            series.FirstAired.Should().HaveValue();
            series.FirstAired.Value.Kind.Should().Be(DateTimeKind.Utc);
            series.Images.Should().NotBeEmpty();
            series.ImdbId.Should().NotBeNullOrWhiteSpace();
            series.Network.Should().NotBeNullOrWhiteSpace();
            series.Runtime.Should().BeGreaterThan(0);
            series.TitleSlug.Should().NotBeNullOrWhiteSpace();
            series.TvRageId.Should().BeGreaterThan(0);
            series.TvdbId.Should().BeGreaterThan(0);
        }

        private void ValidateEpisodes(List<Episode> episodes)
        {
            episodes.Should().NotBeEmpty();

            episodes.GroupBy(e => e.SeasonNumber.ToString("000") + e.EpisodeNumber.ToString("000"))
                .Max(e => e.Count()).Should().Be(1);

            episodes.Should().Contain(c => c.SeasonNumber > 0);
            episodes.Should().Contain(c => !string.IsNullOrWhiteSpace(c.Overview));

            foreach (var episode in episodes)
            {
                ValidateEpisode(episode);

                //if atleast one episdoe has title it means parse it working.
                episodes.Should().Contain(c => !string.IsNullOrWhiteSpace(c.Title));
            }
        }

        private void ValidateEpisode(Episode episode)
        {
            episode.Should().NotBeNull();

            //TODO: Is there a better way to validate that episode number or season number is greater than zero?
            (episode.EpisodeNumber + episode.SeasonNumber).Should().NotBe(0);

            episode.Should().NotBeNull();

            if (episode.AirDateUtc.HasValue)
            {
                episode.AirDateUtc.Value.Kind.Should().Be(DateTimeKind.Utc);
            }
        }
    }
}
