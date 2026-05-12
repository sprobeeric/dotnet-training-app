using DocumentTracker.Repositories;

namespace DocumentTracker.Tests.Repositories;

public class InvoiceSqlTests
{
    [Fact]
    public void InvoiceSearchSql_UsesParametersForSearchInput()
    {
        Assert.Contains("@SearchTerm", InvoiceSql.CountInvoices);
        Assert.Contains("@SearchPattern", InvoiceSql.CountInvoices);
        Assert.Contains("@SearchTerm", InvoiceSql.SearchInvoicesPage);
        Assert.Contains("@SearchPattern", InvoiceSql.SearchInvoicesPage);
        Assert.DoesNotContain("ILIKE '%", InvoiceSql.CountInvoices);
        Assert.DoesNotContain("ILIKE '%", InvoiceSql.SearchInvoicesPage);
        Assert.DoesNotContain("string.Concat", InvoiceSql.CountInvoices);
        Assert.DoesNotContain("string.Concat", InvoiceSql.SearchInvoicesPage);
    }

    [Fact]
    public void SearchInvoicesPage_UsesPaginationParameters()
    {
        Assert.Contains("LIMIT @PageSize", InvoiceSql.SearchInvoicesPage);
        Assert.Contains("OFFSET @Offset", InvoiceSql.SearchInvoicesPage);
    }
}