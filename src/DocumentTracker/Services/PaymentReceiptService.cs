using System.ComponentModel.DataAnnotations;
using DocumentTracker.Models;
using DocumentTracker.Repositories;
using DocumentTracker.ViewModels;
using Npgsql;

namespace DocumentTracker.Services;

public class PaymentReceiptService : IPaymentReceiptService
{
    private const int MaxCreateAttempts = 3;
    private static readonly HashSet<string> AllowedPaymentMethods =
    [
        "Cash",
        "Bank Transfer",
        "Check",
        "Credit Card",
        "Debit Card"
    ];

    private readonly IPaymentReceiptRepository _paymentReceiptRepository;
    private readonly IInvoiceLookupRepository _invoiceLookupRepository;
    private readonly IPaymentReceiptNumberSequenceProvider _sequenceProvider;
    private readonly ILogger<PaymentReceiptService> _logger;

    public PaymentReceiptService(
        IPaymentReceiptRepository paymentReceiptRepository,
        IInvoiceLookupRepository invoiceLookupRepository,
        IPaymentReceiptNumberSequenceProvider sequenceProvider,
        ILogger<PaymentReceiptService> logger)
    {
        _paymentReceiptRepository = paymentReceiptRepository;
        _invoiceLookupRepository = invoiceLookupRepository;
        _sequenceProvider = sequenceProvider;
        _logger = logger;
    }

    public async Task<PaginatedResult<PaymentReceiptListItemViewModel>> SearchAsync(string? searchTerm, DateOnly? dateFrom, DateOnly? dateTo, string sort, string order, int page, int pageSize)
    {
        var paymentReceipts = await _paymentReceiptRepository.SearchAsync(searchTerm, dateFrom, dateTo, sort, order, page, pageSize);

        return new PaginatedResult<PaymentReceiptListItemViewModel>
        {
            Items = paymentReceipts.Items.Select(ToListItem).ToList(),
            Total = paymentReceipts.Total
        };
    }

    public async Task<ServiceResult<int>> CreateAsync(PaymentReceiptCreateViewModel viewModel)
    {
        viewModel.InvoiceNumber = viewModel.InvoiceNumber.Trim();
        viewModel.ReferenceNumber = string.IsNullOrWhiteSpace(viewModel.ReferenceNumber) ? null : viewModel.ReferenceNumber.Trim();
        viewModel.Notes = string.IsNullOrWhiteSpace(viewModel.Notes) ? null : viewModel.Notes.Trim();
        await PopulateInvoiceSummaryAsync(viewModel);

        var validationResult = ValidateCreate(viewModel);
        if (!validationResult.Succeeded)
        {
            return validationResult;
        }

        if (viewModel.InvoiceSummary is null || viewModel.InvoiceId is null || viewModel.PaymentDate is null || viewModel.AmountPaid is null)
        {
            return ServiceResult<int>.Failure(string.Empty, "The payment receipt could not be created.");
        }

        var invoiceIsActive = await _paymentReceiptRepository.InvoiceExistsAndActiveAsync(viewModel.InvoiceId.Value);
        if (!invoiceIsActive)
        {
            return ServiceResult<int>.Failure(nameof(viewModel.InvoiceNumber), "Only active invoices can receive payments.");
        }

        var now = DateTime.UtcNow;

        for (var attempt = 1; attempt <= MaxCreateAttempts; attempt++)
        {
            var receiptNumber = await GenerateReceiptNumberAsync();

            var paymentReceipt = new PaymentReceipt
            {
                ReceiptNumber = receiptNumber,
                InvoiceId = viewModel.InvoiceId.Value,
                PaymentDate = viewModel.PaymentDate.Value,
                AmountPaid = viewModel.AmountPaid.Value,
                PaymentMethod = viewModel.PaymentMethod,
                ReferenceNumber = viewModel.ReferenceNumber,
                Notes = viewModel.Notes,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

            try
            {
                var id = await _paymentReceiptRepository.CreateAsync(paymentReceipt);
                return ServiceResult<int>.Success(id);
            }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation && attempt < MaxCreateAttempts)
            {
                _logger.LogWarning(ex, "Payment receipt create hit a unique-value collision on attempt {Attempt}. Retrying.", attempt);
            }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                _logger.LogWarning(ex, "Payment receipt create failed after {AttemptCount} attempts because receipt numbering kept colliding.", attempt);
                return ServiceResult<int>.Failure(string.Empty, "The payment receipt could not be created. Please submit again.");
            }
        }

        return ServiceResult<int>.Failure(string.Empty, "The payment receipt could not be created. Please submit again.");
    }

    public async Task<PaymentReceiptDetailsViewModel?> GetDetailsAsync(int id)
    {
        var receipt = await _paymentReceiptRepository.GetByIdAsync(id);
        return receipt is null || receipt.DeletedAtUtc is not null
            ? null
            : ToDetails(receipt);
    }

    private static PaymentReceiptListItemViewModel ToListItem(PaymentReceipt paymentReceipt) => new()
    {
        Id = paymentReceipt.Id,
        ReceiptNumber = paymentReceipt.ReceiptNumber,
        InvoiceNumber = paymentReceipt.InvoiceNumber,
        PaymentDate = paymentReceipt.PaymentDate,
        AmountPaid = paymentReceipt.AmountPaid,
        PaymentMethod = paymentReceipt.PaymentMethod,
        ReferenceNumber = paymentReceipt.ReferenceNumber,
        Notes = paymentReceipt.Notes
    };

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

    private static PaymentReceiptDetailsViewModel ToDetails(PaymentReceipt receipt) => new()
    {
        Id = receipt.Id,
        ReceiptNumber = receipt.ReceiptNumber,
        InvoiceNumber = receipt.InvoiceNumber,
        PaymentDate = receipt.PaymentDate,
        AmountPaid = receipt.AmountPaid,
        PaymentMethod = receipt.PaymentMethod,
        ReferenceNumber = receipt.ReferenceNumber,
        Notes = receipt.Notes,
        CreatedAtUtc = receipt.CreatedAtUtc,
        UpdatedAtUtc = receipt.UpdatedAtUtc
    };

    private async Task PopulateInvoiceSummaryAsync(PaymentReceiptCreateViewModel viewModel)
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

    private async Task<string> GenerateReceiptNumberAsync()
    {
        var nextSequence = await _sequenceProvider.GetNextReceiptSequenceAsync();
        return $"PR-{nextSequence:D4}";
    }

    private static ServiceResult<int> ValidateCreate(PaymentReceiptCreateViewModel viewModel)
    {
        var result = new ServiceResult<int>();
        AddValidationErrors(result, viewModel);

        if (viewModel.InvoiceSummary is null || viewModel.InvoiceId != viewModel.InvoiceSummary.InvoiceId)
        {
            result.AddError(nameof(viewModel.InvoiceNumber), "Select a valid invoice before saving the payment receipt.");
        }

        if (!string.IsNullOrWhiteSpace(viewModel.PaymentMethod) && !AllowedPaymentMethods.Contains(viewModel.PaymentMethod))
        {
            result.AddError(nameof(viewModel.PaymentMethod), "Select a valid payment method.");
        }

        if (viewModel.InvoiceSummary is not null && viewModel.AmountPaid.HasValue)
        {
            if (viewModel.InvoiceSummary.RemainingBalance <= 0)
            {
                result.AddError(nameof(viewModel.InvoiceNumber), "This invoice does not have any remaining balance.");
            }

            if (viewModel.AmountPaid.Value > viewModel.InvoiceSummary.RemainingBalance)
            {
                result.AddError(nameof(viewModel.AmountPaid), $"Amount paid cannot exceed the remaining balance of {viewModel.InvoiceSummary.RemainingBalance:N2}.");
            }
        }

        return result;
    }

    private static void AddValidationErrors(ServiceResult<int> result, object instance)
    {
        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(instance);

        Validator.TryValidateObject(instance, context, validationResults, validateAllProperties: true);

        foreach (var validationResult in validationResults)
        {
            var key = validationResult.MemberNames.FirstOrDefault() ?? string.Empty;
            result.AddError(key, validationResult.ErrorMessage ?? "The value is invalid.");
        }
    }
}
