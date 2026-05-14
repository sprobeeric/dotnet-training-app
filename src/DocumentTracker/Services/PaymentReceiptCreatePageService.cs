using DocumentTracker.Models;
using DocumentTracker.Repositories;
using DocumentTracker.ViewModels;

namespace DocumentTracker.Services;

public class PaymentReceiptCreatePageService : IPaymentReceiptCreatePageService
{
    private readonly IInvoiceLookupRepository _invoiceLookupRepository;

    public PaymentReceiptCreatePageService(IInvoiceLookupRepository invoiceLookupRepository)
    {
        _invoiceLookupRepository = invoiceLookupRepository;
    }

    public async Task<PaymentReceiptCreateViewModel> BuildAsync(string? invoiceNumber = null)
    {
        var viewModel = new PaymentReceiptCreateViewModel
        {
            InvoiceNumber = invoiceNumber?.Trim() ?? string.Empty,
            PaymentDate = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        await PopulateInvoiceSummaryAsync(viewModel);
        return viewModel;
    }

    public async Task PopulateInvoiceSummaryAsync(PaymentReceiptFormViewModel viewModel)
    {
        if (string.IsNullOrWhiteSpace(viewModel.InvoiceNumber))
        {
            viewModel.InvoiceSummary = null;
            viewModel.InvoiceId = null;
            return;
        }

        var summary = await _invoiceLookupRepository.GetPaymentReceiptSummaryByNumberAsync(viewModel.InvoiceNumber.Trim());
        viewModel.InvoiceSummary = summary is null ? null : ToInvoiceSummary(summary);
        viewModel.InvoiceId = viewModel.InvoiceSummary?.InvoiceId;
    }

    private static PaymentReceiptInvoiceSummaryViewModel ToInvoiceSummary(InvoicePaymentSummary summary) => new()
    {
        InvoiceId = summary.InvoiceId,
        InvoiceNumber = summary.InvoiceNumber,
        CustomerName = summary.CustomerName,
        InvoiceDate = summary.InvoiceDate,
        DueDate = summary.DueDate,
        Status = summary.Status,
        InvoiceTotal = summary.TotalAmount,
        PreviouslyPaid = summary.PreviouslyPaid,
        RemainingBalance = summary.RemainingBalance,
        Notes = summary.Notes
    };
}
