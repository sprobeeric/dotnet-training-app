using DocumentTracker.Repositories;

namespace DocumentTracker.Tests.Repositories;

public class InvoiceSqlTests
{
    [Fact]
    public void InvoiceSearchSql_UsesParametersForSearchInput()
    {
        var searchInvoicesPageSql = InvoiceSql.SearchInvoicesPage("invoice_number ASC, id ASC");

        Assert.Contains("@SearchTerm", InvoiceSql.CountInvoices);
        Assert.Contains("@SearchPattern", InvoiceSql.CountInvoices);
        Assert.Contains("@SearchTerm", searchInvoicesPageSql);
        Assert.Contains("@SearchPattern", searchInvoicesPageSql);
        Assert.DoesNotContain("ILIKE '%", InvoiceSql.CountInvoices);
        Assert.DoesNotContain("ILIKE '%", searchInvoicesPageSql);
        Assert.DoesNotContain("string.Concat", InvoiceSql.CountInvoices);
        Assert.DoesNotContain("string.Concat", searchInvoicesPageSql);
    }

    [Fact]
    public void SearchInvoicesPage_UsesPaginationParameters()
    {
        var searchInvoicesPageSql = InvoiceSql.SearchInvoicesPage("invoice_number ASC, id ASC");

        Assert.Contains("LIMIT @PageSize", searchInvoicesPageSql);
        Assert.Contains("OFFSET @Offset", searchInvoicesPageSql);
    }
}