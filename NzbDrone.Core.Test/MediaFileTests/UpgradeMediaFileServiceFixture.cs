﻿using System;
using System.Linq;
using FizzWare.NBuilder;
using Marr.Data;
using Moq;
using NUnit.Framework;
using NzbDrone.Common;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Test.MediaFileTests
{
    public class UpgradeMediaFileServiceFixture : CoreTest<UpgradeMediaFileService>
    {
        private EpisodeFile _episodeFile;
        private LocalEpisode _localEpisode;

        [SetUp]
        public void Setup()
        {
            _localEpisode = new LocalEpisode();

            _episodeFile = Builder<EpisodeFile>
                .CreateNew()
                .Build();


            Mocker.GetMock<IDiskProvider>()
                .Setup(c => c.FileExists(It.IsAny<string>()))
                .Returns(true);
        }

        private void GivenSingleEpisodeWithSingleEpisodeFile()
        {
            _localEpisode.Episodes = Builder<Episode>.CreateListOfSize(1)
                                                     .All()
                                                     .With(e => e.EpisodeFileId = 1)
                                                     .With(e => e.EpisodeFile = new LazyLoaded<EpisodeFile>(
                                                                                new EpisodeFile
                                                                                {
                                                                                    Id = 1,
                                                                                    Path = @"C:\Test\30 Rock\Season 01\30.rock.s01e01.avi",
                                                                                }))
                                                     .Build()
                                                     .ToList();
        }

        private void GivenMultipleEpisodesWithSingleEpisodeFile()
        {
            _localEpisode.Episodes = Builder<Episode>.CreateListOfSize(2)
                                                     .All()
                                                     .With(e => e.EpisodeFileId = 1)
                                                     .With(e => e.EpisodeFile = new LazyLoaded<EpisodeFile>(
                                                                                new EpisodeFile
                                                                                {
                                                                                    Id = 1,
                                                                                    Path = @"C:\Test\30 Rock\Season 01\30.rock.s01e01.avi",
                                                                                }))
                                                     .Build()
                                                     .ToList();
        }

        private void GivenMultipleEpisodesWithMultipleEpisodeFiles()
        {
            _localEpisode.Episodes = Builder<Episode>.CreateListOfSize(2)
                                                     .TheFirst(1)
                                                     .With(e => e.EpisodeFile = new LazyLoaded<EpisodeFile>(
                                                                                new EpisodeFile
                                                                                {
                                                                                    Id = 1,
                                                                                    Path = @"C:\Test\30 Rock\Season 01\30.rock.s01e01.avi",
                                                                                }))
                                                     .TheNext(1)
                                                     .With(e => e.EpisodeFile = new LazyLoaded<EpisodeFile>(
                                                                                new EpisodeFile
                                                                                {
                                                                                    Id = 2,
                                                                                    Path = @"C:\Test\30 Rock\Season 01\30.rock.s01e02.avi",
                                                                                }))
                                                     .Build()
                                                     .ToList();
        }

        [Test]
        public void should_delete_single_episode_file_once()
        {
            GivenSingleEpisodeWithSingleEpisodeFile();

            Subject.UpgradeEpisodeFile(_episodeFile, _localEpisode);

            Mocker.GetMock<IRecycleBinProvider>().Verify(v => v.DeleteFile(It.IsAny<string>()), Times.Once());
        }

        [Test]
        public void should_delete_the_same_episode_file_only_once()
        {
            GivenMultipleEpisodesWithSingleEpisodeFile();

            Subject.UpgradeEpisodeFile(_episodeFile, _localEpisode);

            Mocker.GetMock<IRecycleBinProvider>().Verify(v => v.DeleteFile(It.IsAny<string>()), Times.Once());
        }

        [Test]
        public void should_delete_multiple_different_episode_files()
        {
            GivenMultipleEpisodesWithMultipleEpisodeFiles();

            Subject.UpgradeEpisodeFile(_episodeFile, _localEpisode);

            Mocker.GetMock<IRecycleBinProvider>().Verify(v => v.DeleteFile(It.IsAny<string>()), Times.Exactly(2));
        }

        [Test]
        public void should_delete_episode_file_from_database()
        {
            GivenSingleEpisodeWithSingleEpisodeFile();

            Subject.UpgradeEpisodeFile(_episodeFile, _localEpisode);

            Mocker.GetMock<IMediaFileService>().Verify(v => v.Delete(It.IsAny<EpisodeFile>(), true), Times.Once());
        }

        [Test]
        public void should_delete_existing_file_fromdb_if_file_doesnt_exist()
        {
            GivenSingleEpisodeWithSingleEpisodeFile();

            Mocker.GetMock<IDiskProvider>()
                .Setup(c => c.FileExists(It.IsAny<string>()))
                .Returns(false);

            Subject.UpgradeEpisodeFile(_episodeFile, _localEpisode);

            Mocker.GetMock<IMediaFileService>().Verify(v => v.Delete(_localEpisode.Episodes.Single().EpisodeFile.Value, true), Times.Once());
        }

        [Test]
        public void should_not_try_to_recyclebin_existing_file_if_file_doesnt_exist()
        {
            GivenSingleEpisodeWithSingleEpisodeFile();

            Mocker.GetMock<IDiskProvider>()
                .Setup(c => c.FileExists(It.IsAny<string>()))
                .Returns(false);

            Subject.UpgradeEpisodeFile(_episodeFile, _localEpisode);

            Mocker.GetMock<IRecycleBinProvider>().Verify(v => v.DeleteFile(It.IsAny<string>()), Times.Never());
        }

        [Test]
        public void should_not_delete_file_if_pre_move_fails()
        {
            GivenSingleEpisodeWithSingleEpisodeFile();

            Mocker.GetMock<IMoveEpisodeFiles>()
                .Setup(s => s.PreMoveEpisodeFile(It.IsAny<EpisodeFile>(), It.IsAny<LocalEpisode>()))
                .Throws<InvalidOperationException>();

            Assert.Throws<InvalidOperationException>(() => Subject.UpgradeEpisodeFile(_episodeFile, _localEpisode));

            Mocker.GetMock<IRecycleBinProvider>().Verify(v => v.DeleteFile(It.IsAny<string>()), Times.Never());
        }

        [Test]
        public void should_move_after_premove()
        {
            GivenSingleEpisodeWithSingleEpisodeFile();

            Subject.UpgradeEpisodeFile(_episodeFile, _localEpisode);

            Mocker.GetMock<IMoveEpisodeFiles>().Verify(v => v.PreMoveEpisodeFile(It.IsAny<EpisodeFile>(), It.IsAny<LocalEpisode>()), Times.Once());
            Mocker.GetMock<IMoveEpisodeFiles>().Verify(v => v.MoveEpisodeFile(It.IsAny<EpisodeFile>(), It.IsAny<LocalEpisode>()), Times.Once());
        }
    }
}
