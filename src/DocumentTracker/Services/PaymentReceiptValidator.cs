using System.ComponentModel.DataAnnotations;
using DocumentTracker.Models;
using DocumentTracker.ViewModels;

namespace DocumentTracker.Services;

public class PaymentReceiptValidator : IPaymentReceiptValidator
{
    public ServiceResult<int> ValidateCreate(
        PaymentReceiptCreateViewModel viewModel,
        IReadOnlyList<PaymentReceiptProduct> selectedProducts)
    {
        var result = ValidateAnnotations(viewModel);
        if (!result.Succeeded)
        {
            return result;
        }

        if (selectedProducts.Count == 0)
        {
            return ServiceResult<int>.Failure(string.Empty, "Select at least one coffee product.");
        }

        var totalAmount = selectedProducts.Sum(product => product.LineTotal);
        if (viewModel.Received < totalAmount)
        {
            return ServiceResult<int>.Failure(nameof(viewModel.Received), "Amount received must be greater than or equal to the total amount.");
        }

        return ServiceResult<int>.Success(0);
    }

    private static ServiceResult<int> ValidateAnnotations(PaymentReceiptCreateViewModel viewModel)
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
}
