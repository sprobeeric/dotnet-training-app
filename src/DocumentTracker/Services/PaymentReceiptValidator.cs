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
        AddValidationErrors(result, viewModel, prefix: string.Empty);

        for (var index = 0; index < viewModel.Products.Count; index++)
        {
            AddValidationErrors(result, viewModel.Products[index], $"Products[{index}]");
        }

        return result;
    }

    private static void AddValidationErrors(ServiceResult<int> result, object instance, string prefix)
    {
        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(instance);

        Validator.TryValidateObject(instance, context, validationResults, validateAllProperties: true);

        foreach (var validationResult in validationResults)
        {
            var memberName = validationResult.MemberNames.FirstOrDefault() ?? string.Empty;
            var key = string.IsNullOrEmpty(prefix)
                ? memberName
                : string.IsNullOrEmpty(memberName)
                    ? prefix
                    : $"{prefix}.{memberName}";

            result.AddError(key, validationResult.ErrorMessage ?? "The value is invalid.");
        }
    }
}
