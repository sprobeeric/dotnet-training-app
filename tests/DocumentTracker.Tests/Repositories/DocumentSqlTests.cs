using DocumentTracker.Repositories;

namespace DocumentTracker.Tests.Repositories;

public class DocumentSqlTests
{
    [Fact]
    public void SearchDocuments_UsesParametersForSearchInput()
    {
        Assert.Contains("@SearchTerm", DocumentSql.SearchDocuments);
        Assert.Contains("@SearchPattern", DocumentSql.SearchDocuments);
        Assert.DoesNotContain("ILIKE '%", DocumentSql.SearchDocuments);
        Assert.DoesNotContain("string.Concat", DocumentSql.SearchDocuments);
    }

    [Fact]
    public void InsertUpdateAndDeleteSql_UseNamedParameters()
    {
        Assert.Contains("@DocumentNumber", DocumentSql.InsertDocument);
        Assert.Contains("@DocumentNumber", DocumentSql.UpdateDocument);
        Assert.Contains("@DeletedAtUtc", DocumentSql.SoftDeleteDocument);
        Assert.DoesNotContain("DELETE FROM documents", DocumentSql.SoftDeleteDocument, StringComparison.OrdinalIgnoreCase);
    }
}
