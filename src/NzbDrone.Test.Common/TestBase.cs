﻿using System;
using System.IO;
using System.Threading;
using FluentAssertions;
using Moq;
using NLog;
using NUnit.Framework;
using NzbDrone.Common.Cache;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Messaging;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Test.Common.AutoMoq;

namespace NzbDrone.Test.Common
{
    public abstract class TestBase<TSubject> : TestBase where TSubject : class
    {

        private TSubject _subject;

        [SetUp]
        public void CoreTestSetup()
        {
            _subject = null;

        }

        protected TSubject Subject
        {
            get
            {
                if (_subject == null)
                {
                    _subject = Mocker.Resolve<TSubject>();
                }

                return _subject;
            }

        }

    }

    public abstract class TestBase : LoggingTest
    {

        private static readonly Random _random = new Random();

        private AutoMoqer _mocker;
        protected AutoMoqer Mocker
        {
            get
            {
                if (_mocker == null)
                {
                    _mocker = new AutoMoqer();
                }

                return _mocker;
            }
        }


        protected int RandomNumber
        {
            get
            {
                Thread.Sleep(1);
                return _random.Next(0, int.MaxValue);
            }
        }

        private string VirtualPath
        {
            get
            {
                var virtualPath = Path.Combine(TempFolder, "VirtualNzbDrone");
                if (!Directory.Exists(virtualPath)) Directory.CreateDirectory(virtualPath);

                return virtualPath;
            }
        }

        protected string TempFolder { get; private set; }

        [SetUp]
        public void TestBaseSetup()
        {

            GetType().IsPublic.Should().BeTrue("All Test fixtures should be public to work in mono.");

            Mocker.SetConstant<ICacheManager>(new CacheManager());

            Mocker.SetConstant(LogManager.GetLogger("TestLogger"));

            Mocker.SetConstant<IStartupContext>(new StartupContext(new string[0]));

            LogManager.ReconfigExistingLoggers();

            TempFolder = Path.Combine(Directory.GetCurrentDirectory(), "_temp_" + DateTime.Now.Ticks);

            Directory.CreateDirectory(TempFolder);
        }

        [TearDown]
        public void TestBaseTearDown()
        {
            _mocker = null;

            try
            {
                if (Directory.Exists(TempFolder))
                {
                    Directory.Delete(TempFolder, true);
                }
            }
            catch (Exception)
            {
            }
        }


        protected IAppFolderInfo TestFolderInfo { get; private set; }

        protected void WindowsOnly()
        {
            if (OsInfo.IsMono)
            {
                throw new IgnoreException("windows specific test");
            }
        }


        protected void MonoOnly()
        {
            if (!OsInfo.IsMono)
            {
                throw new IgnoreException("linux specific test");
            }
        }

        protected void WithTempAsAppPath()
        {
            Mocker.GetMock<IAppFolderInfo>()
                .SetupGet(c => c.AppDataFolder)
                .Returns(VirtualPath);

            TestFolderInfo = Mocker.GetMock<IAppFolderInfo>().Object;
        }

        protected string GetTestFilePath(string fileName)
        {
            return Path.Combine(SandboxFolder, fileName);
        }

        protected string GetTestFilePath()
        {
            return GetTestFilePath(Path.GetRandomFileName());
        }

        protected string SandboxFolder
        {
            get
            {
                var folder = Path.Combine(Directory.GetCurrentDirectory(), "Files");
                Directory.CreateDirectory(folder);
                return folder;
            }

        }
        protected void VerifyEventPublished<TEvent>() where TEvent : class, IEvent
        {
            VerifyEventPublished<TEvent>(Times.Once());
        }

        protected void VerifyEventPublished<TEvent>(Times times) where TEvent : class, IEvent
        {
            Mocker.GetMock<IEventAggregator>().Verify(c => c.PublishEvent(It.IsAny<TEvent>()), times);
        }

        protected void VerifyEventNotPublished<TEvent>() where TEvent : class, IEvent
        {
            Mocker.GetMock<IEventAggregator>().Verify(c => c.PublishEvent(It.IsAny<TEvent>()), Times.Never());
        }
    }
}
