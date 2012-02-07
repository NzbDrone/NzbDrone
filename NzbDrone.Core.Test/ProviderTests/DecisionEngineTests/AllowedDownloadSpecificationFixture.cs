﻿// ReSharper disable RedundantUsingDirective

using System.Linq;
using System;
using System.Collections.Generic;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Model;
using NzbDrone.Core.Providers;
using NzbDrone.Core.Providers.DecisionEngine;
using NzbDrone.Core.Repository;
using NzbDrone.Core.Repository.Quality;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.ProviderTests.DecisionEngineTests
{
    [TestFixture]
    // ReSharper disable InconsistentNaming
    public class AllowedDownloadSpecificationFixture : CoreTest
    {
        private AllowedDownloadSpecification spec;
        private EpisodeParseResult parseResult;

        [SetUp]
        public void Setup()
        {
            spec = Mocker.Resolve<AllowedDownloadSpecification>();
            parseResult = new EpisodeParseResult();

            Mocker.GetMock<QualityAllowedByProfileSpecification>()
                    .Setup(c => c.IsSatisfiedBy(It.IsAny<EpisodeParseResult>()))
                    .Returns(true);

            Mocker.GetMock<AcceptableSizeSpecification>()
                    .Setup(c => c.IsSatisfiedBy(It.IsAny<EpisodeParseResult>()))
                    .Returns(true);

            Mocker.GetMock<UpgradeDiskSpecification>()
                    .Setup(c => c.IsSatisfiedBy(It.IsAny<EpisodeParseResult>()))
                    .Returns(true);

            Mocker.GetMock<AlreadyInQueueSpecification>()
                    .Setup(c => c.IsSatisfiedBy(It.IsAny<EpisodeParseResult>()))
                    .Returns(false);
        }

        private void WithProfileNotAllowed()
        {
            Mocker.GetMock<QualityAllowedByProfileSpecification>()
                    .Setup(c => c.IsSatisfiedBy(It.IsAny<EpisodeParseResult>()))
                    .Returns(false);
        }

        private void WithNotAcceptableSize()
        {
            Mocker.GetMock<AcceptableSizeSpecification>()
                    .Setup(c => c.IsSatisfiedBy(It.IsAny<EpisodeParseResult>()))
                    .Returns(false);
        }

        private void WithNoDiskUpgrade()
        {
            Mocker.GetMock<UpgradeDiskSpecification>()
                    .Setup(c => c.IsSatisfiedBy(It.IsAny<EpisodeParseResult>()))
                    .Returns(false);
        }

        private void WithEpisodeAlreadyInQueue()
        {
            Mocker.GetMock<AlreadyInQueueSpecification>()
                    .Setup(c => c.IsSatisfiedBy(It.IsAny<EpisodeParseResult>()))
                    .Returns(true);
        }

        [Test]
        public void should_be_allowed_if_all_conditions_are_met()
        {
            spec.IsSatisfiedBy(parseResult).Should().BeTrue();
        }

        [Test]
        public void should_not_be_allowed_if_profile_is_not_allowed()
        {
            WithProfileNotAllowed();
            spec.IsSatisfiedBy(parseResult).Should().BeFalse();
        }

        [Test]
        public void should_not_be_allowed_if_size_is_not_allowed()
        {
            WithNotAcceptableSize();
            spec.IsSatisfiedBy(parseResult).Should().BeFalse();
        }

        [Test]
        public void should_not_be_allowed_if_disk_is_not_upgrade()
        {
            WithNoDiskUpgrade();
            spec.IsSatisfiedBy(parseResult).Should().BeFalse();
        }

        [Test]
        public void should_not_be_allowed_if_episode_is_already_in_queue()
        {
            WithEpisodeAlreadyInQueue();
            spec.IsSatisfiedBy(parseResult).Should().BeFalse();
        }

        [Test]
        public void should_not_be_allowed_if_none_of_conditions_are_met()
        {
            WithNoDiskUpgrade();
            WithNotAcceptableSize();
            WithProfileNotAllowed();

            spec.IsSatisfiedBy(parseResult).Should().BeFalse();
        }
    }
}