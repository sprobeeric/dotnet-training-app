using DocumentTracker.Repositories;

namespace DocumentTracker.Services;

public class PaymentReceiptNumberGenerator : IPaymentReceiptNumberGenerator
{
    private readonly IPaymentReceiptRepository _paymentReceiptRepository;

    public PaymentReceiptNumberGenerator(IPaymentReceiptRepository paymentReceiptRepository)
    {
        _paymentReceiptRepository = paymentReceiptRepository;
    }

    public async Task<(string ReceiptNumber, string ReferenceNumber, DateTime PaymentDateUtc)> GenerateAsync()
    {
        var paymentDateUtc = DateTime.UtcNow;
        var paymentDate = DateOnly.FromDateTime(paymentDateUtc);
        var nextSequence = await _paymentReceiptRepository.GetNextReceiptSequenceAsync(paymentDate);
        var receiptNumber = $"PR-{paymentDate:yyyyMMdd}-{nextSequence:D6}";
        var referenceNumber = BuildReferenceNumber(paymentDate, nextSequence);

        return (receiptNumber, referenceNumber, paymentDateUtc);
    }

    private static string BuildReferenceNumber(DateOnly paymentDate, int sequence)
    {
        var suffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        return $"REF-{paymentDate:yyyyMMdd}-{sequence:D6}-{suffix}";
    }
}
