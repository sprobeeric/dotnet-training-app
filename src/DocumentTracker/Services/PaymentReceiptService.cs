using System.ComponentModel.DataAnnotations;
using DocumentTracker.Models;
using DocumentTracker.Repositories;
using DocumentTracker.ViewModels;
using Npgsql;

namespace DocumentTracker.Services;

public class PaymentReceiptService : IPaymentReceiptService
{
    private readonly IPaymentReceiptRepository _paymentReceiptRepository;
    private readonly IProductRepository _productRepository;
    private readonly ILogger<PaymentReceiptService> _logger;

    public PaymentReceiptService(
        IPaymentReceiptRepository paymentReceiptRepository,
        IProductRepository productRepository,
        ILogger<PaymentReceiptService> logger)
    {
        _paymentReceiptRepository = paymentReceiptRepository;
        _productRepository = productRepository;
        _logger = logger;
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

        var validationResult = Validate(viewModel);
        if (!validationResult.Succeeded)
        {
            return validationResult;
        }

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

        if (selectedProducts.Count == 0)
        {
            return ServiceResult<int>.Failure(nameof(viewModel.Products), "Select at least one coffee product.");
        }

        var totalAmount = selectedProducts.Sum(product => product.LineTotal);
        if (viewModel.Received < totalAmount)
        {
            return ServiceResult<int>.Failure(nameof(viewModel.Received), "Amount received must be greater than or equal to the total amount.");
        }

        var now = DateTime.UtcNow;
        var paymentDate = DateOnly.FromDateTime(now);
        var nextSequence = await _paymentReceiptRepository.GetNextReceiptSequenceAsync(paymentDate);
        var receiptNumber = $"RCP-{paymentDate:yyyyMMdd}-{nextSequence:D6}";
        var referenceNumber = BuildReferenceNumber(paymentDate, nextSequence);

        var paymentReceipt = new PaymentReceipt
        {
            ReceiptNumber = receiptNumber,
            PaymentDateUtc = now,
            ReferenceNumber = referenceNumber,
            TotalAmount = totalAmount,
            Received = viewModel.Received,
            ChangeAmount = viewModel.Received - totalAmount,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        try
        {
            var id = await _paymentReceiptRepository.CreateAsync(paymentReceipt, selectedProducts);
            return ServiceResult<int>.Success(id);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            _logger.LogWarning(ex, "Payment receipt create failed because receipt numbering collided.");
            return ServiceResult<int>.Failure(string.Empty, "The payment receipt could not be created. Please submit the purchase again.");
        }
    }

    public async Task<PaymentReceiptDetailsViewModel?> GetDetailsAsync(int id)
    {
        var paymentReceipt = await _paymentReceiptRepository.GetByIdAsync(id);
        if (paymentReceipt is null)
        {
            return null;
        }

        var products = await _paymentReceiptRepository.ListProductsByReceiptIdAsync(id);
        return new PaymentReceiptDetailsViewModel
        {
            Id = paymentReceipt.Id,
            ReceiptNumber = paymentReceipt.ReceiptNumber,
            ReferenceNumber = paymentReceipt.ReferenceNumber,
            PaymentDateUtc = paymentReceipt.PaymentDateUtc,
            TotalAmount = paymentReceipt.TotalAmount,
            Received = paymentReceipt.Received,
            ChangeAmount = paymentReceipt.ChangeAmount,
            Products = products.Select(product => new PaymentReceiptDetailsProductViewModel
            {
                ProductName = product.Product.Name,
                Quantity = product.Quantity,
                UnitPrice = product.UnitPrice,
                LineTotal = product.LineTotal
            }).ToList()
        };
    }

    private static ServiceResult<int> Validate(PaymentReceiptCreateViewModel viewModel)
    {
        var result = new ServiceResult<int>();
        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(viewModel);

        Validator.TryValidateObject(viewModel, context, validationResults, validateAllProperties: true);

        foreach (var validationResult in validationResults)
        {
            var key = validationResult.MemberNames.FirstOrDefault() ?? string.Empty;
            result.AddError(key, validationResult.ErrorMessage ?? "The value is invalid.");
        }

        return result;
    }

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

    private static string BuildReferenceNumber(DateOnly paymentDate, int sequence)
    {
        var suffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        return $"REF-{paymentDate:yyyyMMdd}-{sequence:D6}-{suffix}";
    }
}
