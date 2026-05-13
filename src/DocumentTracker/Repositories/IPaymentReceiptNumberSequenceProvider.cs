namespace DocumentTracker.Repositories;

public interface IPaymentReceiptNumberSequenceProvider
{
    Task<int> GetNextReceiptSequenceAsync();
}
