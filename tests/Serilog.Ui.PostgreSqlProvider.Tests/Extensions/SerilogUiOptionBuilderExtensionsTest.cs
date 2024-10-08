﻿using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Postgres.Tests.Util;
using Serilog.Ui.Core;
using Serilog.Ui.Core.Extensions;
using Serilog.Ui.Core.Models.Options;
using Serilog.Ui.PostgreSqlProvider;
using Serilog.Ui.PostgreSqlProvider.Extensions;
using Serilog.Ui.PostgreSqlProvider.Models;
using Serilog.Ui.Web.Extensions;
using Xunit;

namespace Postgres.Tests.Extensions
{
    [Trait("DI-DataProvider", "Postgres")]
    public class SerilogUiOptionBuilderExtensionsTest
    {
        private readonly ServiceCollection _serviceCollection = [];

        [Theory]
#pragma warning disable CS0618 // Type or member is obsolete
        [InlineData(PostgreSqlSinkType.SerilogSinksPostgreSQL, typeof(PostgreSqlSinkColumnNames))]
#pragma warning restore CS0618 // Type or member is obsolete
        [InlineData(PostgreSqlSinkType.SerilogSinksPostgreSQLAlternative, typeof(PostgreSqlAlternativeSinkColumnNames))]
        [InlineData(null, typeof(PostgreSqlAlternativeSinkColumnNames))]
        public void It_registers_provider_and_dependencies(PostgreSqlSinkType? sink, Type type)
        {
            PostgreSqlDbOptions opts = null!;
            _serviceCollection.AddSerilogUi(builder =>
            {
                builder.UseNpgSql(opt =>
                {
                    opt
                        .WithConnectionString("https://npgsql.example.com")
                        .WithTable("my-table");
                    if (sink is not null) opt.WithSinkType((PostgreSqlSinkType)sink);

                    opts = opt;
                });
            });

            var serviceProvider = _serviceCollection.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();

            var provider = scope.ServiceProvider.GetService<IDataProvider>();
            provider.Should().NotBeNull().And.BeOfType<PostgresDataProvider>();
            opts.ColumnNames.Should().BeOfType(type);
            serviceProvider.GetRequiredService<ProvidersOptions>().DisabledSortProviderNames.Should().BeEmpty();
            serviceProvider.GetRequiredService<ProvidersOptions>().ExceptionAsStringProviderNames.Should().HaveCount(1);
        }

        [Fact]
        public void It_registers_provider_and_dependencies_with_custom_log_model()
        {
            _serviceCollection.AddSerilogUi(builder =>
            {
                builder
                    .UseNpgSql<PostgresTestModel>(opt => opt
                        .WithConnectionString("https://sqlserver.com").WithTable("table-custom"));
            });

            var serviceProvider = _serviceCollection.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();

            var provider = scope.ServiceProvider.GetService<IDataProvider>();
            provider.Should().NotBeNull().And.BeOfType<PostgresDataProvider<PostgresTestModel>>();
            serviceProvider.GetRequiredService<ProvidersOptions>().DisabledSortProviderNames.Should().BeEmpty();
            serviceProvider.GetRequiredService<ProvidersOptions>().ExceptionAsStringProviderNames.Should().HaveCount(1);
        }

        [Fact]
        public void It_registers_multiple_providers()
        {
            _serviceCollection.AddSerilogUi(builder =>
            {
                builder.UseNpgSql(opt =>
                    {
                        opt
                            .WithConnectionString("https://npgsql.example.com")
                            .WithTable("my-table");
                    })
                    .UseNpgSql(opt =>
                    {
                        opt
                            .WithConnectionString("https://npgsql.example.com")
                            .WithTable("my-table-2");
                    })
                    .UseNpgSql<PostgresTestModel>(opt =>
                    {
                        opt
                            .WithConnectionString("https://npgsql.example.com")
                            .WithTable("table-custom");
                    })
                    .UseNpgSql<PostgresTestModel>(opt =>
                    {
                        opt
                            .WithConnectionString("https://npgsql.example.com")
                            .WithTable("table-custom-2");
                    });
            });

            var serviceProvider = _serviceCollection.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();

            var providers = scope.ServiceProvider.GetServices<IDataProvider>().ToList();
            providers.Take(2).Should().AllBeOfType<PostgresDataProvider>();
            providers.Skip(2).Take(2).Should().AllBeOfType<PostgresDataProvider<PostgresTestModel>>();

            providers.Select(p => p.Name).Should().OnlyHaveUniqueItems();
            serviceProvider.GetRequiredService<ProvidersOptions>().DisabledSortProviderNames.Should().BeEmpty();
            serviceProvider.GetRequiredService<ProvidersOptions>().ExceptionAsStringProviderNames.Should().HaveCount(4);
        }

        [Fact]
        public void It_throws_on_invalid_registration()
        {
            var nullables = new List<Func<IServiceCollection>>
            {
                () => _serviceCollection.AddSerilogUi(builder => builder.UseNpgSql(opt => opt.WithConnectionString(null!).WithTable("my-table"))),
                () => _serviceCollection.AddSerilogUi(builder => builder.UseNpgSql(opt => opt.WithConnectionString(" ").WithTable("my-table"))),
                () => _serviceCollection.AddSerilogUi(builder =>
                    builder.UseNpgSql(opt => opt.WithConnectionString(string.Empty).WithTable("my-table"))),
                () => _serviceCollection.AddSerilogUi(builder => builder.UseNpgSql(opt => opt.WithConnectionString("conn").WithTable(null!))),
                () => _serviceCollection.AddSerilogUi(builder => builder.UseNpgSql(opt => opt.WithConnectionString("conn").WithTable(" "))),
                () => _serviceCollection.AddSerilogUi(builder => builder.UseNpgSql(opt => opt.WithConnectionString("conn").WithTable(string.Empty))),
                // if user sets an invalid schema, default value will be overridden an validation should fail
                () => _serviceCollection.AddSerilogUi(builder =>
                    builder.UseNpgSql(opt => opt.WithConnectionString("conn").WithTable("ok").WithSchema(null!))),
                () => _serviceCollection.AddSerilogUi(builder =>
                    builder.UseNpgSql(opt => opt.WithConnectionString("conn").WithTable("ok").WithSchema(" "))),
                () => _serviceCollection.AddSerilogUi(builder =>
                    builder.UseNpgSql(opt => opt.WithConnectionString("conn").WithTable("ok").WithSchema(string.Empty))),
            };

            foreach (var nullable in nullables)
            {
                nullable.Should().Throw<ArgumentException>();
            }
        }
    }
}