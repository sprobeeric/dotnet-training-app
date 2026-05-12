namespace DocumentTracker.Services;

public interface IPaymentReceiptNumberGenerator
{
    Task<(string ReceiptNumber, string ReferenceNumber, DateTime PaymentDateUtc)> GenerateAsync();
}
