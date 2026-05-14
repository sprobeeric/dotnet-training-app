using System.ComponentModel.DataAnnotations;
using DocumentTracker.Models;
using DocumentTracker.Repositories;
using DocumentTracker.ViewModels;
namespace DocumentTracker.Services;

public class PaymentReceiptService : IPaymentReceiptService
{
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
    private readonly ILogger<PaymentReceiptService> _logger;

    public PaymentReceiptService(
        IPaymentReceiptRepository paymentReceiptRepository,
        IInvoiceLookupRepository invoiceLookupRepository,
        ILogger<PaymentReceiptService> logger)
    {
        _paymentReceiptRepository = paymentReceiptRepository;
        _invoiceLookupRepository = invoiceLookupRepository;
        _logger = logger;
    }

    public async Task<PaginatedResult<PaymentReceiptListItemViewModel>> SearchAsync(PaymentReceiptSearchViewModel model)
    {
        var criteria = new PaymentReceiptSearchCriteria
        {
            SearchTerm = model.SearchTerm,
            DateFrom = model.DateFrom,
            DateTo = model.DateTo,
            Sort = model.Sort,
            Order = model.Order,
            Page = model.Page,
            PageSize = model.PageSize
        };

        var receipts = await _paymentReceiptRepository.SearchAsync(criteria);

        return new PaginatedResult<PaymentReceiptListItemViewModel>
        {
            Items = receipts.Items.Select(ToListItem).ToList(),
            Total = receipts.Total
        };
    }

    public async Task<ServiceResult<int>> CreateAsync(PaymentReceiptCreateViewModel viewModel)
    {
        viewModel.ReceiptNumber = viewModel.ReceiptNumber.Trim();
        viewModel.InvoiceNumber = viewModel.InvoiceNumber.Trim();
        viewModel.ReferenceNumber = string.Equals(viewModel.PaymentMethod, "Cash", StringComparison.Ordinal)
            ? null
            : string.IsNullOrWhiteSpace(viewModel.ReferenceNumber) ? null : viewModel.ReferenceNumber.Trim();
        viewModel.Notes = string.IsNullOrWhiteSpace(viewModel.Notes) ? null : viewModel.Notes.Trim();
        await PopulateInvoiceSummaryAsync(viewModel);

        var validationResult = Validate(viewModel);
        if (!validationResult.Succeeded)
        {
            return validationResult;
        }

        if (viewModel.InvoiceSummary is null || viewModel.InvoiceId is null || viewModel.PaymentDate is null || viewModel.AmountPaid is null)
        {
            return ServiceResult<int>.Failure(string.Empty, "The payment receipt could not be created.");
        }

        var invoiceIsPending = await _paymentReceiptRepository.InvoiceExistsAndPendingAsync(viewModel.InvoiceId.Value);
        if (!invoiceIsPending)
        {
            return ServiceResult<int>.Failure(nameof(viewModel.InvoiceNumber), "Only invoices in Pending status can receive payments.");
        }

        var receiptNumberExists = await _paymentReceiptRepository.ReceiptNumberExistsAsync(viewModel.ReceiptNumber);
        if (receiptNumberExists)
        {
            return ServiceResult<int>.Failure(nameof(viewModel.ReceiptNumber), "A payment receipt with this receipt number already exists.");
        }

        if (!string.IsNullOrWhiteSpace(viewModel.ReferenceNumber))
        {
            var referenceNumberExists = await _paymentReceiptRepository.ReferenceNumberExistsAsync(viewModel.ReferenceNumber);
            if (referenceNumberExists)
            {
                return ServiceResult<int>.Failure(nameof(viewModel.ReferenceNumber), "A payment receipt with this reference number already exists.");
            }
        }

        var now = DateTime.UtcNow;
        var paymentReceipt = new PaymentReceipt
        {
            ReceiptNumber = viewModel.ReceiptNumber,
            InvoiceId = viewModel.InvoiceId.Value,
            PaymentDate = viewModel.PaymentDate.Value,
            AmountPaid = viewModel.AmountPaid.Value,
            PaymentMethod = viewModel.PaymentMethod,
            ReferenceNumber = viewModel.ReferenceNumber,
            Notes = viewModel.Notes,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        var id = await _paymentReceiptRepository.CreateAsync(paymentReceipt);
        return ServiceResult<int>.Success(id);
    }

    public async Task<PaymentReceiptDetailsViewModel?> GetDetailsAsync(int id)
    {
        var receipt = await _paymentReceiptRepository.GetByIdAsync(id);
        return receipt is null || receipt.DeletedAtUtc is not null
            ? null
            : ToDetails(receipt);
    }

    public async Task<PaymentReceiptDeleteViewModel?> GetDeleteAsync(int id)
    {
        var receipt = await _paymentReceiptRepository.GetByIdAsync(id);
        return receipt is null || receipt.DeletedAtUtc is not null ? null : ToDelete(receipt);
    }

    public async Task<ServiceResult> SoftDeleteAsync(int id, string? currentRole)
    {
        if (!string.Equals(currentRole, PaymentReceiptRoles.ReceiptAdmin, StringComparison.Ordinal))
        {
            return ServiceResult.Failure(string.Empty, "Only users in the ReceiptAdmin role can delete payment receipts.");
        }

        var existing = await _paymentReceiptRepository.GetByIdAsync(id);
        if (existing is null)
        {
            return ServiceResult.Failure(string.Empty, "The payment receipt was not found.");
        }

        if (existing.DeletedAtUtc is not null)
        {
            return ServiceResult.Failure(string.Empty, "This payment receipt has already been deleted.");
        }

        var deleted = await _paymentReceiptRepository.SoftDeleteAsync(id, DateTime.UtcNow);
        return deleted
            ? ServiceResult.Success()
            : ServiceResult.Failure(string.Empty, "The payment receipt could not be deleted. It may have been changed by another user.");
    }

    public async Task<ServiceResult<PaymentReceiptEditViewModel>> GetEditAsync(int id)
    {
        var receipt = await _paymentReceiptRepository.GetByIdAsync(id);
        if (receipt is null)
        {
            return ServiceResult<PaymentReceiptEditViewModel>.Failure(string.Empty, "The payment receipt was not found.");
        }

        if (receipt.DeletedAtUtc is not null)
        {
            return ServiceResult<PaymentReceiptEditViewModel>.Failure(string.Empty, "Deleted payment receipts cannot be edited.");
        }

        var viewModel = ToEdit(receipt);
        await PopulateInvoiceSummaryAsync(viewModel);
        return ServiceResult<PaymentReceiptEditViewModel>.Success(viewModel);
    }

    public async Task<ServiceResult> UpdateAsync(PaymentReceiptEditViewModel viewModel)
    {
        var existing = await _paymentReceiptRepository.GetByIdAsync(viewModel.Id);
        if (existing is null)
        {
            return ServiceResult.Failure(string.Empty, "The payment receipt was not found.");
        }

        if (existing.DeletedAtUtc is not null)
        {
            return ServiceResult.Failure(string.Empty, "Deleted payment receipts cannot be edited.");
        }

        viewModel.ReceiptNumber = viewModel.ReceiptNumber.Trim();
        viewModel.InvoiceNumber = viewModel.InvoiceNumber.Trim();
        viewModel.ReferenceNumber = string.Equals(viewModel.PaymentMethod, "Cash", StringComparison.Ordinal)
            ? null
            : string.IsNullOrWhiteSpace(viewModel.ReferenceNumber) ? null : viewModel.ReferenceNumber.Trim();
        viewModel.Notes = string.IsNullOrWhiteSpace(viewModel.Notes) ? null : viewModel.Notes.Trim();
        await PopulateInvoiceSummaryAsync(viewModel);

        var validationResult = Validate(viewModel, existing);
        if (!validationResult.Succeeded)
        {
            return validationResult;
        }

        if (viewModel.InvoiceSummary is null || viewModel.InvoiceId is null || viewModel.PaymentDate is null || viewModel.AmountPaid is null)
        {
            return ServiceResult.Failure(string.Empty, "The payment receipt could not be updated.");
        }

        var receiptNumberExists = await _paymentReceiptRepository.ReceiptNumberExistsAsync(viewModel.ReceiptNumber, viewModel.Id);
        if (receiptNumberExists)
        {
            return ServiceResult.Failure(nameof(viewModel.ReceiptNumber), "A payment receipt with this receipt number already exists.");
        }

        if (!string.IsNullOrWhiteSpace(viewModel.ReferenceNumber))
        {
            var referenceNumberExists = await _paymentReceiptRepository.ReferenceNumberExistsAsync(viewModel.ReferenceNumber, viewModel.Id);
            if (referenceNumberExists)
            {
                return ServiceResult.Failure(nameof(viewModel.ReferenceNumber), "A payment receipt with this reference number already exists.");
            }
        }

        var paymentReceipt = new PaymentReceipt
        {
            Id = viewModel.Id,
            ReceiptNumber = viewModel.ReceiptNumber,
            InvoiceId = viewModel.InvoiceId.Value,
            PaymentDate = viewModel.PaymentDate.Value,
            AmountPaid = viewModel.AmountPaid.Value,
            PaymentMethod = viewModel.PaymentMethod,
            ReferenceNumber = viewModel.ReferenceNumber,
            Notes = viewModel.Notes,
            UpdatedAtUtc = DateTime.UtcNow
        };

        var updated = await _paymentReceiptRepository.UpdateAsync(paymentReceipt);
        return updated
            ? ServiceResult.Success()
            : ServiceResult.Failure(string.Empty, "The payment receipt could not be updated. It may have been changed by another user.");
    }

    private static PaymentReceiptListItemViewModel ToListItem(PaymentReceipt paymentReceipt) => new()
    {
        Id = paymentReceipt.Id,
        ReceiptNumber = paymentReceipt.ReceiptNumber,
        InvoiceNumber = paymentReceipt.InvoiceNumber,
        CustomerName = paymentReceipt.CustomerName,
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
        InvoiceId = receipt.InvoiceId,
        InvoiceNumber = receipt.InvoiceNumber,
        CustomerName = receipt.CustomerName,
        PaymentDate = receipt.PaymentDate,
        AmountPaid = receipt.AmountPaid,
        PaymentMethod = receipt.PaymentMethod,
        ReferenceNumber = receipt.ReferenceNumber,
        Notes = receipt.Notes,
        CreatedAtUtc = receipt.CreatedAtUtc,
        UpdatedAtUtc = receipt.UpdatedAtUtc
    };

    private static PaymentReceiptEditViewModel ToEdit(PaymentReceipt receipt) => new()
    {
        Id = receipt.Id,
        ExistingInvoiceId = receipt.InvoiceId,
        ReceiptNumber = receipt.ReceiptNumber,
        InvoiceId = receipt.InvoiceId,
        InvoiceNumber = receipt.InvoiceNumber,
        PaymentDate = receipt.PaymentDate,
        AmountPaid = receipt.AmountPaid,
        PaymentMethod = receipt.PaymentMethod,
        ReferenceNumber = receipt.ReferenceNumber,
        Notes = receipt.Notes
    };

    private async Task PopulateInvoiceSummaryAsync(PaymentReceiptFormViewModel viewModel)
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

    private static PaymentReceiptDeleteViewModel ToDelete(PaymentReceipt paymentReceipt) => new()
    {
        Id = paymentReceipt.Id,
        ReceiptNumber = paymentReceipt.ReceiptNumber,
        InvoiceNumber = paymentReceipt.InvoiceNumber,
        CustomerName = paymentReceipt.CustomerName,
        PaymentDate = paymentReceipt.PaymentDate,
        AmountPaid = paymentReceipt.AmountPaid,
        PaymentMethod = paymentReceipt.PaymentMethod,
        ReferenceNumber = paymentReceipt.ReferenceNumber,
        Notes = paymentReceipt.Notes
    };

    private static ServiceResult<int> Validate(PaymentReceiptFormViewModel viewModel)
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

        AddPaymentDateValidation(result, viewModel);

        if (viewModel.InvoiceSummary is not null)
        {
            if (!viewModel.InvoiceSummary.AllowsPayment)
            {
                result.AddError(nameof(viewModel.InvoiceNumber), "Only invoices in Pending status can receive payments.");
            }

            if (viewModel.AmountPaid.HasValue && viewModel.AmountPaid.Value > viewModel.InvoiceSummary.RemainingBalance)
            {
                result.AddError(nameof(viewModel.AmountPaid), $"Amount paid cannot exceed the remaining balance of {viewModel.InvoiceSummary.RemainingBalance:N2}.");
            }
        }

        return result;
    }

    private static ServiceResult Validate(PaymentReceiptEditViewModel viewModel, PaymentReceipt existing)
    {
        var result = new ServiceResult();
        AddValidationErrors(result, viewModel);

        if (viewModel.InvoiceSummary is null || viewModel.InvoiceId != viewModel.InvoiceSummary.InvoiceId)
        {
            result.AddError(nameof(viewModel.InvoiceNumber), "Select a valid invoice before saving the payment receipt.");
        }

        if (!string.IsNullOrWhiteSpace(viewModel.PaymentMethod) && !AllowedPaymentMethods.Contains(viewModel.PaymentMethod))
        {
            result.AddError(nameof(viewModel.PaymentMethod), "Select a valid payment method.");
        }

        AddPaymentDateValidation(result, viewModel);

        if (viewModel.InvoiceSummary is not null)
        {
            var isSameInvoice = viewModel.InvoiceId == existing.InvoiceId;
            if (!isSameInvoice && !viewModel.InvoiceSummary.AllowsPayment)
            {
                result.AddError(nameof(viewModel.InvoiceNumber), "Only invoices in Pending status can receive payments.");
            }

            var maximumAmount = viewModel.InvoiceSummary.RemainingBalance + (isSameInvoice ? existing.AmountPaid : 0m);
            if (viewModel.AmountPaid.HasValue && viewModel.AmountPaid.Value > maximumAmount)
            {
                result.AddError(nameof(viewModel.AmountPaid), $"Amount paid cannot exceed the available balance of {maximumAmount:N2}.");
            }
        }

        return result;
    }

    private static void AddPaymentDateValidation(ServiceResult result, PaymentReceiptFormViewModel viewModel)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (viewModel.PaymentDate.HasValue && viewModel.PaymentDate.Value > today)
        {
            result.AddError(nameof(viewModel.PaymentDate), "Payment date cannot be in the future.");
        }
    }

    private static void AddValidationErrors(ServiceResult result, object instance)
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
