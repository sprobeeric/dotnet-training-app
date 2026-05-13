using DocumentTracker.Models;
using DocumentTracker.Repositories;
using DocumentTracker.ViewModels;
using Npgsql;

namespace DocumentTracker.Services;

public class PaymentReceiptService : IPaymentReceiptService
{
    private const int MaxCreateAttempts = 3;
    private readonly IPaymentReceiptRepository _paymentReceiptRepository;
    private readonly IPaymentReceiptNumberGenerator _numberGenerator;
    private readonly IProductRepository _productRepository;
    private readonly IPaymentReceiptValidator _validator;
    private readonly ILogger<PaymentReceiptService> _logger;

    public PaymentReceiptService(
        IPaymentReceiptRepository paymentReceiptRepository,
        IPaymentReceiptNumberGenerator numberGenerator,
        IProductRepository productRepository,
        IPaymentReceiptValidator validator,
        ILogger<PaymentReceiptService> logger)
    {
        _paymentReceiptRepository = paymentReceiptRepository;
        _numberGenerator = numberGenerator;
        _productRepository = productRepository;
        _validator = validator;
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

    public async Task<PaymentReceiptCreateViewModel> GetCreateAsync()
    {
        return new PaymentReceiptCreateViewModel
        {
            Products = await BuildProductInputsAsync()
        };
    }

    public async Task<ServiceResult<int>> CreateAsync(PaymentReceiptCreateViewModel viewModel)
    {
        await HydrateProductsAsync(viewModel);

        var selectedProducts = viewModel.Products
            .Where(product => product.Quantity > 0)
            .Select(product => new PaymentReceiptProduct
            {
                ProductId = product.ProductId,
                Quantity = product.Quantity,
                UnitPrice = product.UnitPrice,
                LineTotal = product.UnitPrice * product.Quantity
            })
            .ToList();

        var validationResult = _validator.ValidateCreate(viewModel, selectedProducts);
        if (!validationResult.Succeeded)
        {
            return validationResult;
        }

        var totalAmount = selectedProducts.Sum(product => product.LineTotal);

        for (var attempt = 1; attempt <= MaxCreateAttempts; attempt++)
        {
            var generatedNumbers = await _numberGenerator.GenerateAsync();

            var paymentReceipt = new PaymentReceipt
            {
                ReceiptNumber = generatedNumbers.ReceiptNumber,
                PaymentDateUtc = generatedNumbers.PaymentDateUtc,
                ReferenceNumber = generatedNumbers.ReferenceNumber,
                TotalAmount = totalAmount,
                Received = viewModel.Received,
                ChangeAmount = viewModel.Received - totalAmount,
                CreatedAtUtc = generatedNumbers.PaymentDateUtc,
                UpdatedAtUtc = generatedNumbers.PaymentDateUtc
            };

            try
            {
                var id = await _paymentReceiptRepository.CreateAsync(paymentReceipt, selectedProducts);
                return ServiceResult<int>.Success(id);
            }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation && attempt < MaxCreateAttempts)
            {
                _logger.LogWarning(ex, "Payment receipt create hit a unique-value collision on attempt {Attempt}. Retrying.", attempt);
            }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                _logger.LogWarning(ex, "Payment receipt create failed after {AttemptCount} attempts because receipt numbering kept colliding.", attempt);
                return ServiceResult<int>.Failure(string.Empty, "The payment receipt could not be created. Please submit the purchase again.");
            }
        }

        return ServiceResult<int>.Failure(string.Empty, "The payment receipt could not be created. Please submit the purchase again.");
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

    private async Task<List<PaymentReceiptProductInputViewModel>> BuildProductInputsAsync()
    {
        var products = await _productRepository.ListActiveAsync();
        return products.Select(product => new PaymentReceiptProductInputViewModel
        {
            ProductId = product.Id,
            ProductName = product.Name,
            UnitPrice = product.UnitPrice,
            Quantity = 0
        }).ToList();
    }

    private async Task HydrateProductsAsync(PaymentReceiptCreateViewModel viewModel)
    {
        var products = await _productRepository.ListActiveAsync();
        var submittedQuantities = viewModel.Products.ToDictionary(product => product.ProductId, product => product.Quantity);

        viewModel.Products = products.Select(product => new PaymentReceiptProductInputViewModel
        {
            ProductId = product.Id,
            ProductName = product.Name,
            UnitPrice = product.UnitPrice,
            Quantity = submittedQuantities.TryGetValue(product.Id, out var quantity) ? quantity : 0
        }).ToList();
    }

    private static PaymentReceiptDetailsViewModel ToDetails(PaymentReceipt receipt) => new()
    {
        Id = receipt.Id,
        ReceiptNumber = receipt.ReceiptNumber,
        PaymentDateUtc = receipt.PaymentDateUtc,
        ReferenceNumber = receipt.ReferenceNumber,
        TotalAmount = receipt.TotalAmount,
        Received = receipt.Received,
        ChangeAmount = receipt.ChangeAmount,
        CreatedAtUtc = receipt.CreatedAtUtc,
        UpdatedAtUtc = receipt.UpdatedAtUtc,
        Products = receipt.PaymentReceiptProducts.Select(ToProductDetails).ToList()
    };

    private static PaymentReceiptProductDetailsViewModel ToProductDetails(PaymentReceiptProduct item) => new()
    {
        ProductId = item.ProductId,
        ProductName = item.Product.Name,
        UnitPrice = item.UnitPrice,
        Quantity = item.Quantity
    };

    private static PaymentReceiptDeleteViewModel ToDelete(PaymentReceipt paymentReceipt) => new()
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
}
