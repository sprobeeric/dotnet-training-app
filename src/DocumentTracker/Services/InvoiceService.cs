using DocumentTracker.Models;
using DocumentTracker.Repositories;
using DocumentTracker.ViewModels;

namespace DocumentTracker.Services;

public class InvoiceService : IInvoiceService
{
    private readonly IInvoiceRepository _repository;

    public InvoiceService(IInvoiceRepository repository)
    {
        _repository = repository;
    }

    public async Task<InvoiceDeleteViewModel?> GetDeleteAsync(int id)
    {
        var invoice = await _repository.GetByIdAsync(id);
        return invoice is null || invoice.DeletedAtUtc is not null ? null : ToDelete(invoice);
    }

    public async Task<ServiceResult> SoftDeleteAsync(int id, string? currentRole)
    {
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
