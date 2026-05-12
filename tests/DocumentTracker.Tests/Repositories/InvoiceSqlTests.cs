using DocumentTracker.Repositories;

namespace DocumentTracker.Tests.Repositories;

public class InvoiceSqlTests
{
    [Fact]
    public void SearchInvoices_UsesParametersForSearchInput()
    {
        Assert.Contains("@SearchTerm", InvoiceSql.SearchInvoices);
        Assert.Contains("@SearchPattern", InvoiceSql.SearchInvoices);
        Assert.DoesNotContain("ILIKE '%", InvoiceSql.SearchInvoices);
        Assert.DoesNotContain("string.Concat", InvoiceSql.SearchInvoices);
    }
}