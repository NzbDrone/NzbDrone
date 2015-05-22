﻿using System;
using System.Data.SQLite;
using Marr.Data;
using Marr.Data.Reflection;
using NzbDrone.Common.Composition;
using NzbDrone.Core.Datastore.Migration.Framework;
using NzbDrone.Core.Instrumentation;
using NzbDrone.Core.Messaging.Events;


namespace NzbDrone.Core.Datastore
{
    public interface IDbFactory
    {
        IDatabase Create(MigrationType migrationType = MigrationType.Main, Action<NzbDroneMigrationBase> beforeMigration = null);
    }

    public class DbFactory : IDbFactory
    {
        private readonly IMigrationController _migrationController;
        private readonly IConnectionStringFactory _connectionStringFactory;

        static DbFactory()
        {
            MapRepository.Instance.ReflectionStrategy = new SimpleReflectionStrategy();
            TableMapping.Map();
        }

        public static void RegisterDatabase(IContainer container)
        {
            var mainDb = new MainDatabase(container.Resolve<IDbFactory>().Create());

            container.Register<IMainDatabase>(mainDb);

            var logDb = new LogDatabase(container.Resolve<IDbFactory>().Create(MigrationType.Log));

            container.Register<ILogDatabase>(logDb);
        }

        public DbFactory(IMigrationController migrationController, IConnectionStringFactory connectionStringFactory)
        {
            _migrationController = migrationController;
            _connectionStringFactory = connectionStringFactory;
        }

        public IDatabase Create(MigrationType migrationType = MigrationType.Main, Action<NzbDroneMigrationBase> beforeMigration = null)
        {
            string connectionString;


            switch (migrationType)
            {
                case MigrationType.Main:
                    {
                        connectionString = _connectionStringFactory.MainDbConnectionString;
                        break;
                    }
                case MigrationType.Log:
                    {
                        connectionString = _connectionStringFactory.LogDbConnectionString;
                        break;
                    }
                default:
                    {
                        throw new ArgumentException("Invalid MigrationType");
                    }
            }

            _migrationController.MigrateToLatest(connectionString, migrationType, beforeMigration);

            var db = new Database(migrationType.ToString(), () =>
                {
                    var dataMapper = new DataMapper(SQLiteFactory.Instance, connectionString)
                    {
                        SqlMode = SqlModes.Text,
                    };

                    return dataMapper;
                });

            return db;
        }
    }
}
