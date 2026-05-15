using System.ComponentModel.DataAnnotations;
using DocumentTracker.Models;
using DocumentTracker.Repositories;
using DocumentTracker.ViewModels;
using Npgsql;

namespace DocumentTracker.Services;

public class InvoiceService : IInvoiceService
{
    private readonly IInvoiceRepository _repository;
    private readonly ILogger<InvoiceService> _logger;

    public InvoiceService(IInvoiceRepository repository, ILogger<InvoiceService> logger)
    {
        _repository = repository;
        _logger = logger;
    }
    
    public async Task<PagedResult<InvoiceListItemViewModel>> SearchAsync(
        string? searchTerm,
        DateOnly? invoiceDateFrom,
        DateOnly? invoiceDateTo,
        string? sortBy,
        string? sortDirection,
        int pageNumber,
        int pageSize)
    {
        var searchResult = await _repository.SearchAsync(
            searchTerm,
            invoiceDateFrom,
            invoiceDateTo,
            sortBy,
            sortDirection,
            pageNumber,
            pageSize);
        return new PagedResult<InvoiceListItemViewModel>
        {
            Items = searchResult.Items.Select(ToListItem).ToList(),
            TotalCount = searchResult.TotalCount
        };
    }

    public async Task<ServiceResult<InvoiceEditViewModel>> GetEditAsync(int id)
    {
        var invoice = await _repository.GetByIdAsync(id);
        if (invoice is null)
        {
            return ServiceResult<InvoiceEditViewModel>.Failure(string.Empty, "The document was not found.");
        }

        if (invoice.DeletedAtUtc is not null)
        {
            return ServiceResult<InvoiceEditViewModel>.Failure(string.Empty, "Deleted documents cannot be edited.");
        }

        return ServiceResult<InvoiceEditViewModel>.Success(ToEdit(invoice));
    }

    public async Task<InvoiceDetailsViewModel?> GetDetailsAsync(int id)
    {
        var invoice = await _repository.GetByIdAsync(id);
        return invoice is null || invoice.DeletedAtUtc is not null ? null : ToDetails(invoice);
    }

    public async Task<ServiceResult> UpdateAsync(InvoiceEditViewModel viewModel)
    {
        var validationResult = Validate(viewModel);
        if (!validationResult.Succeeded)
        {
            return validationResult;
        }

        var invoiceData = await _repository.GetByIdAsync(viewModel.Id);
        if (invoiceData is null)
        {
            return ServiceResult.Failure(string.Empty, "The invoice was not found.");
        }

        if (invoiceData.DeletedAtUtc is not null)
        {
            return ServiceResult.Failure(string.Empty, "Deleted documents cannot be edited.");
        }

        var duplicate = await _repository.GetByInvoiceNumberAsync(viewModel.InvoiceNumber);
        if (duplicate is not null && duplicate.InvoiceNumber != viewModel.InvoiceNumber)
        {
            return ServiceResult.Failure(nameof(viewModel.InvoiceNumber), "A invoice with this invoice number already exists.");
        }

        invoiceData.InvoiceNumber = viewModel.InvoiceNumber.Trim();
        invoiceData.CustomerName = viewModel.CustomerName.Trim();
        invoiceData.InvoiceDate = viewModel.InvoiceDate;
        invoiceData.DueDate = viewModel.DueDate;
        invoiceData.Status = viewModel.Status!.Value;
        invoiceData.Subtotal = viewModel.Subtotal;
        invoiceData.TaxAmount = viewModel.TaxAmount;
        invoiceData.TotalAmount = viewModel.TotalAmount;
        invoiceData.Notes = string.IsNullOrWhiteSpace(viewModel.Notes)
            ? null
            : viewModel.Notes.Trim();

        invoiceData.UpdatedAtUtc = DateTime.UtcNow;

        var updated = await _repository.UpdateAsync(invoiceData);
        return updated
            ? ServiceResult.Success()
            : ServiceResult.Failure(string.Empty, "The document could not be updated. It may have been deleted by another user.");
    }

    private static ServiceResult Validate(InvoiceEditViewModel viewModel)
    {
        var result = new ServiceResult();
        AddValidationErrors(viewModel, result);
        return result;
    }
    
    public async Task<ServiceResult<int>> CreateAsync(InvoiceCreateViewModel viewModel)
    {
        var invoiceNumberExists = await _repository.InvoiceNumberExistsAsync(viewModel.InvoiceNumber);
        if (invoiceNumberExists)
        {
            return ServiceResult<int>.Failure(nameof(viewModel.InvoiceNumber), "An invoice with this invoice number already exists.");
        }

        var now = DateTime.UtcNow;
        var invoice = new Invoice
        {
            InvoiceNumber = viewModel.InvoiceNumber.Trim(),
            CustomerName = viewModel.CustomerName.Trim(),
            InvoiceDate = viewModel.InvoiceDate,
            DueDate = viewModel.DueDate,
            Status = viewModel.Status!.Value,
            Notes = string.IsNullOrWhiteSpace(viewModel.Notes) ? null : viewModel.Notes.Trim(),

            Subtotal = viewModel.Subtotal,
            TaxAmount = viewModel.TaxAmount,
            TotalAmount = viewModel.Subtotal + viewModel.TaxAmount,

            CreatedAtUtc = now,
        };

        try
        {
            var id = await _repository.CreateAsync(invoice);
            return ServiceResult<int>.Success(id);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            _logger.LogWarning(ex, "Invoice create failed because the invoice number was not unique.");
            return ServiceResult<int>.Failure(nameof(viewModel.InvoiceNumber), "An invoice with this invoice number already exists.");
        }
    }

    private static ServiceResult<int> Validate(InvoiceCreateViewModel viewModel)
    {
        var result = new ServiceResult<int>();
        AddValidationErrors(viewModel, result);
        return result;
    }  

    private static void AddValidationErrors(object viewModel, ServiceResult result)
    {
        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(viewModel);

        Validator.TryValidateObject(viewModel, context, validationResults, validateAllProperties: true);

        foreach (var validationResult in validationResults)
        {
            var key = validationResult.MemberNames.FirstOrDefault() ?? string.Empty;
            result.AddError(key, validationResult.ErrorMessage ?? "The value is invalid.");
        }
    }

    private static InvoiceListItemViewModel ToListItem(Invoice invoice) => new()
    {
        Id = invoice.Id,
        InvoiceNumber = invoice.InvoiceNumber,
        CustomerName = invoice.CustomerName,
        InvoiceDate = invoice.InvoiceDate,
        DueDate = invoice.DueDate,
        Status = invoice.Status,
        TotalAmount = invoice.TotalAmount,
        UpdatedAtUtc = invoice.UpdatedAtUtc
    };

    private static InvoiceEditViewModel ToEdit(Invoice invoice) => new()
    {
        Id = invoice.Id,
        InvoiceNumber = invoice.InvoiceNumber,
        CustomerName = invoice.CustomerName,
        InvoiceDate = invoice.InvoiceDate,
        DueDate = invoice.DueDate,
        Status = invoice.Status,
        Subtotal = invoice.Subtotal,
        TaxAmount = invoice.TaxAmount,
        TotalAmount = invoice.TotalAmount,
        Notes = invoice.Notes,
    };
    private static InvoiceDetailsViewModel ToDetails(Invoice invoice) => new()
    {
        Id = invoice.Id,
        InvoiceNumber = invoice.InvoiceNumber,
        CustomerName = invoice.CustomerName,
        InvoiceDate = invoice.InvoiceDate,
        DueDate = invoice.DueDate,
        Status = invoice.Status,
        Subtotal = invoice.Subtotal,
        TaxAmount = invoice.TaxAmount,
        TotalAmount = invoice.TotalAmount,
        Notes = invoice.Notes,
        CreatedAtUtc = invoice.CreatedAtUtc,
        UpdatedAtUtc = invoice.UpdatedAtUtc,
        DeletedAtUtc = invoice.DeletedAtUtc
    };

    public async Task<InvoiceDeleteViewModel?> GetDeleteAsync(int id)
    {
        var invoice = await _repository.GetByIdAsync(id);
        return invoice is null || invoice.DeletedAtUtc is not null ? null : ToDelete(invoice);
    }

    public async Task<ServiceResult> SoftDeleteAsync(int id, string? currentRole)
    {
        if (!string.Equals(currentRole, InvoiceRoles.InvoiceAdmin, StringComparison.Ordinal))
        {
            return ServiceResult.Failure(string.Empty, "Only users in the InvoiceAdmin role can delete invoices.");
        }

        var existing = await _repository.GetByIdAsync(id);
        if (existing is null)
        {
            return ServiceResult.Failure(string.Empty, "The invoice was not found.");
        }

        if (existing.DeletedAtUtc is not null)
        {
            return ServiceResult.Failure(string.Empty, "This invoice has already been deleted.");
        }

        var deleted = await _repository.SoftDeleteAsync(id, DateTime.UtcNow);
        return deleted
            ? ServiceResult.Success()
            : ServiceResult.Failure(string.Empty, "The invoice could not be deleted. It may have been changed by another user.");
    }

    private static InvoiceDeleteViewModel ToDelete(Invoice invoice) => new()
    {
        Id = invoice.Id,
        InvoiceNumber = invoice.InvoiceNumber,
        CustomerName = invoice.CustomerName,
        InvoiceDate = invoice.InvoiceDate,
        DueDate = invoice.DueDate,
        Status = invoice.Status,
        TotalAmount = invoice.TotalAmount
    };
}
