﻿using Ardalis.GuardClauses;
using Raven.Client.Documents;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;
using Serilog;
using Serilog.Ui.Common.Tests.DataSamples;
using Serilog.Ui.Common.Tests.SqlUtil;
using Serilog.Ui.RavenDbProvider;
using Testcontainers.RavenDb;

namespace RavenDb.Tests.Util;

[CollectionDefinition(nameof(RavenDbDataProvider))]
public class RavenDbCollection : ICollectionFixture<RavenDbTestProvider>
{ }

public sealed class RavenDbTestProvider : DatabaseInstance
{
    private const string DbName = "TestDB";
    private readonly DocumentStore _documentStore;

    public RavenDbTestProvider()
    {
        _documentStore = new DocumentStore { Database = DbName };
        Container = new RavenDbBuilder().Build();
    }

    protected override string Name => nameof(RavenDbContainer);

    protected override Task CheckDbReadinessAsync()
    {
        Guard.Against.Null(Container);

        var container = Container as RavenDbContainer;
        _documentStore.Urls = new[] { (container)?.GetConnectionString() };
        //_documentStore.Urls = new[] { "http://localhost:8080" };
        _documentStore.Initialize();
        //_documentStore.Maintenance.Server.Send(new DeleteDatabasesOperation(DbName, true));
        _documentStore.Maintenance.Server.Send(new CreateDatabaseOperation(new DatabaseRecord(DbName)));

        return Task.CompletedTask;
    }

    protected override Task InitializeAdditionalAsync()
    {
        var serilog = new SetupSerilog(_documentStore);
        Collector = serilog.InitializeLogs();

        Provider = new RavenDbDataProvider(_documentStore, "LogEvents");

        return Task.CompletedTask;
    }

    public sealed class SetupSerilog
    {
        private readonly LoggerConfiguration _loggerConfig;

        public SetupSerilog(IDocumentStore documentStore)
        {
            _loggerConfig = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.RavenDB(documentStore);
        }

        public LogModelPropsCollector InitializeLogs()
        {
            using var logger = _loggerConfig.CreateLogger();
            return ElasticSearchLogModelFaker.Logs(logger);
        }
    }
}