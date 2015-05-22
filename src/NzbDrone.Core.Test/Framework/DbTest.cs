﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentMigrator;
using FluentMigrator.Runner;
using Marr.Data;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Datastore.Migration.Framework;
using NzbDrone.Core.Messaging.Events;


namespace NzbDrone.Core.Test.Framework
{
    public abstract class DbTest<TSubject, TModel> : DbTest
        where TSubject : class
        where TModel : ModelBase, new()
    {
        private TSubject _subject;

        protected BasicRepository<TModel> Storage { get; private set; }

        protected IList<TModel> AllStoredModels
        {
            get
            {
                return Storage.All().ToList();
            }
        }

        protected TModel StoredModel
        {
            get
            {
                return Storage.All().Single();
            }
        }

        [SetUp]
        public void CoreTestSetup()
        {
            _subject = null;
            Storage = Mocker.Resolve<BasicRepository<TModel>>();
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

    [Category("DbTest")]
    public abstract class DbTest : CoreTest
    {
        private ITestDatabase _db;

        protected virtual MigrationType MigrationType
        {
            get
            {
                return MigrationType.Main;

            }
        }

        protected ITestDatabase Db
        {
            get
            {
                if (_db == null)
                    throw new InvalidOperationException("Test object database doesn't exists. Make sure you call WithRealDb() if you intend to use an actual database.");

                return _db;
            }
        }

        protected virtual TestDatabase WithTestDb(Action<MigrationBase> beforeMigration)
        {
            var factory = Mocker.Resolve<DbFactory>();
            var database = factory.Create(MigrationType, beforeMigration);
            Mocker.SetConstant(database);

            switch (MigrationType)
            {
                case MigrationType.Main:
                    {
                        var mainDb = new MainDatabase(database);

                        Mocker.SetConstant<IMainDatabase>(mainDb);
                        break;
                    }
                case MigrationType.Log:
                    {
                        var logDb = new LogDatabase(database);

                        Mocker.SetConstant<ILogDatabase>(logDb);
                        break;
                    }
                default:
                    {
                        throw new ArgumentException("Invalid MigrationType");
                    }
            }

            var testDb = new TestDatabase(database);

            return testDb;
        }


        protected void SetupContainer()
        {
            WithTempAsAppPath();

            Mocker.SetConstant<IAnnouncer>(Mocker.Resolve<MigrationLogger>());
            Mocker.SetConstant<IConnectionStringFactory>(Mocker.Resolve<ConnectionStringFactory>());
            Mocker.SetConstant<IMigrationController>(Mocker.Resolve<MigrationController>());

            MapRepository.Instance.EnableTraceLogging = true;
        }

        [SetUp]
        public virtual void SetupDb()
        {
            SetupContainer();
            _db = WithTestDb(null);
        }

        [TearDown]
        public void TearDown()
        {
            if (TestFolderInfo != null && Directory.Exists(TestFolderInfo.AppDataFolder))
            {
                var files = Directory.GetFiles(TestFolderInfo.AppDataFolder);

                foreach (var file in files)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception)
                    {

                    }
                }
            }
        }
    }


    public interface ITestDatabase
    {
        void InsertMany<T>(IEnumerable<T> items) where T : ModelBase, new();
        T Insert<T>(T item) where T : ModelBase, new();
        List<T> All<T>() where T : ModelBase, new();
        T Single<T>() where T : ModelBase, new();
        void Update<T>(T childModel) where T : ModelBase, new();
        void Delete<T>(T childModel) where T : ModelBase, new();
    }

    public class TestDatabase : ITestDatabase
    {
        private readonly IDatabase _dbConnection;
        private IEventAggregator _eventAggregator;

        public TestDatabase(IDatabase dbConnection)
        {
            _eventAggregator = new Mock<IEventAggregator>().Object;
            _dbConnection = dbConnection;
        }

        public void InsertMany<T>(IEnumerable<T> items) where T : ModelBase, new()
        {
            new BasicRepository<T>(_dbConnection, _eventAggregator).InsertMany(items.ToList());
        }

        public T Insert<T>(T item) where T : ModelBase, new()
        {
            return new BasicRepository<T>(_dbConnection, _eventAggregator).Insert(item);
        }

        public List<T> All<T>() where T : ModelBase, new()
        {
            return new BasicRepository<T>(_dbConnection, _eventAggregator).All().ToList();
        }

        public T Single<T>() where T : ModelBase, new()
        {
            return All<T>().SingleOrDefault();
        }

        public void Update<T>(T childModel) where T : ModelBase, new()
        {
            new BasicRepository<T>(_dbConnection, _eventAggregator).Update(childModel);
        }

        public void Delete<T>(T childModel) where T : ModelBase, new()
        {
            new BasicRepository<T>(_dbConnection, _eventAggregator).Delete(childModel);
        }
    }
}