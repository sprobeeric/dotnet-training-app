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

    public async Task<ServiceResult<int>> CreateAsync(InvoiceCreateViewModel viewModel)
    {
        var validationResult = Validate(viewModel);
        if (!validationResult.Succeeded)
        {
            return validationResult;
        }

        var existing = await _repository.GetByInvoiceNumberAsync(viewModel.InvoiceNumber);
        if (existing is not null)
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
}
