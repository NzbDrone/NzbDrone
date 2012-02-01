﻿using System;

using FizzWare.NBuilder;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Model;
using NzbDrone.Core.Providers;
using NzbDrone.Core.Repository;
using NzbDrone.Core.Repository.Quality;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common.AutoMoq;

// ReSharper disable InconsistentNaming

namespace NzbDrone.Core.Test.ProviderTests
{
    [TestFixture]
    public class DownloadProviderTest : CoreTest
    {
        [Test]
        public void Download_report_should_send_to_sab_add_to_history_mark_as_grabbed()
        {
            WithStrictMocker();
            var parseResult = Builder<EpisodeParseResult>.CreateNew()
                .With(c => c.Quality = new Quality(QualityTypes.DVD, false))
                .Build();

            var episodes = Builder<Episode>.CreateListOfSize(2)
                                            .TheFirst(1).With(s => s.EpisodeId = 12)
                                            .TheNext(1).With(s => s.EpisodeId = 99)
                                            .All().With(s => s.SeriesId = 5)
                                            .Build();


            const string sabTitle = "My fake sab title";
            Mocker.GetMock<SabProvider>()
                .Setup(s => s.IsInQueue(It.IsAny<EpisodeParseResult>()))
                .Returns(false);

            Mocker.GetMock<SabProvider>()
                .Setup(s => s.AddByUrl(parseResult, sabTitle))
                .Returns(true);

            Mocker.GetMock<SabProvider>()
                .Setup(s => s.GetSabTitle(parseResult))
                .Returns(sabTitle);

            Mocker.GetMock<HistoryProvider>()
                .Setup(s => s.Add(It.Is<History>(h => h.EpisodeId == 12 && h.SeriesId == 5)));
            Mocker.GetMock<HistoryProvider>()
                .Setup(s => s.Add(It.Is<History>(h => h.EpisodeId == 99 && h.SeriesId == 5)));

            Mocker.GetMock<EpisodeProvider>()
                .Setup(c => c.GetEpisodesByParseResult(It.IsAny<EpisodeParseResult>(), false)).Returns(episodes);

            Mocker.GetMock<EpisodeProvider>()
                .Setup(c => c.MarkEpisodeAsFetched(12));

            Mocker.GetMock<EpisodeProvider>()
                .Setup(c => c.MarkEpisodeAsFetched(99));

            Mocker.GetMock<ExternalNotificationProvider>()
                .Setup(c => c.OnGrab(It.IsAny<string>()));

            Mocker.Resolve<DownloadProvider>().DownloadReport(parseResult);

            Mocker.VerifyAllMocks();
        }
    }
}