using Dapper;
using DocumentTracker.Models;
using DocumentTracker.Repositories;
using Microsoft.Extensions.Logging;
using Moq;
using Npgsql;

namespace DocumentTracker.Tests.Repositories;

public class DocumentRepositoryIntegrationTests
{
    [Fact]
    public async Task CreateAndSearchAsync_WithPostgreSqlConnection_CanRoundTripDocument()
    {
        var connectionString = Environment.GetEnvironmentVariable("DOCUMENTTRACKER_TEST_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        await using var dataSource = NpgsqlDataSource.Create(connectionString);
        await using var connection = await dataSource.OpenConnectionAsync();

        var root = FindRepositoryRoot();
        var schemaSql = await File.ReadAllTextAsync(Path.Combine(root, "database", "schema.sql"));

        await connection.ExecuteAsync(schemaSql);
        await connection.ExecuteAsync("DELETE FROM documents;");

        var repository = new DocumentRepository(dataSource, new Mock<ILogger<DocumentRepository>>().Object);
        var now = DateTime.UtcNow;

        var id = await repository.CreateAsync(new Document
        {
            Title = "Integration Test Plan",
            DocumentNumber = $"TEST-{Guid.NewGuid():N}"[..13],
            Department = "Quality",
            OwnerName = "Test Runner",
            Status = DocumentStatus.Review,
            Description = "Created by the repository integration test.",
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });

        var documents = await repository.SearchAsync("Integration");

        Assert.Contains(documents, document => document.Id == id && document.Title == "Integration Test Plan");
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "DocumentTracker.slnx")))
        {
            directory = directory.Parent;
        }

        if (directory is null)
        {
            throw new DirectoryNotFoundException("Could not locate the repository root.");
        }

        return directory.FullName;
    }
}
