using DocumentTracker.Models;
using DocumentTracker.Repositories;
using DocumentTracker.Services;
using Moq;

namespace DocumentTracker.Tests.Services;

public class PaymentReceiptServiceTests
{
    private readonly Mock<IPaymentReceiptRepository> _repository = new();

    [Fact]
    public async Task GetDeleteAsync_WhenPaymentReceiptIsDeleted_ReturnsNull()
    {
        var service = CreateService();

        _repository
            .Setup(repository => repository.GetByIdAsync(5))
            .ReturnsAsync(new PaymentReceipt
            {
                Id = 5,
                ReceiptNumber = "RCP-20260512-000001",
                DeletedAtUtc = DateTime.UtcNow
            });

        var result = await service.GetDeleteAsync(5);

        Assert.Null(result);
    }

    [Fact]
    public async Task SoftDeleteAsync_WithoutDocumentAdminRole_ReturnsRoleError()
    {
        var service = CreateService();

        var result = await service.SoftDeleteAsync(1, "Reviewer");

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Message.Contains("DocumentAdmin", StringComparison.Ordinal));
        _repository.Verify(repository => repository.SoftDeleteAsync(It.IsAny<int>(), It.IsAny<DateTime>()), Times.Never);
    }

    [Fact]
    public async Task SoftDeleteAsync_WhenPaymentReceiptIsMissing_ReturnsNotFoundError()
    {
        var service = CreateService();

        _repository
            .Setup(repository => repository.GetByIdAsync(9))
            .ReturnsAsync((PaymentReceipt?)null);

        var result = await service.SoftDeleteAsync(9, DocumentRoles.DocumentAdmin);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Message.Contains("not found", StringComparison.OrdinalIgnoreCase));
        _repository.Verify(repository => repository.SoftDeleteAsync(It.IsAny<int>(), It.IsAny<DateTime>()), Times.Never);
    }

    [Fact]
    public async Task SoftDeleteAsync_WhenPaymentReceiptIsAlreadyDeleted_ReturnsDeletedError()
    {
        var service = CreateService();

        _repository
            .Setup(repository => repository.GetByIdAsync(9))
            .ReturnsAsync(new PaymentReceipt
            {
                Id = 9,
                ReceiptNumber = "RCP-20260512-000001",
                DeletedAtUtc = DateTime.UtcNow
            });

        var result = await service.SoftDeleteAsync(9, DocumentRoles.DocumentAdmin);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Message.Contains("already been deleted", StringComparison.OrdinalIgnoreCase));
        _repository.Verify(repository => repository.SoftDeleteAsync(It.IsAny<int>(), It.IsAny<DateTime>()), Times.Never);
    }

    [Fact]
    public async Task SoftDeleteAsync_WithDocumentAdminRole_SoftDeletesPaymentReceipt()
    {
        var service = CreateService();

        _repository
            .Setup(repository => repository.GetByIdAsync(9))
            .ReturnsAsync(new PaymentReceipt
            {
                Id = 9,
                ReceiptNumber = "RCP-20260512-000001"
            });

        _repository
            .Setup(repository => repository.SoftDeleteAsync(9, It.IsAny<DateTime>()))
            .ReturnsAsync(true);

        var result = await service.SoftDeleteAsync(9, DocumentRoles.DocumentAdmin);

        Assert.True(result.Succeeded);
        _repository.Verify(repository => repository.SoftDeleteAsync(9, It.Is<DateTime>(deletedAtUtc =>
            deletedAtUtc.Kind == DateTimeKind.Utc)), Times.Once);
    }

    private PaymentReceiptService CreateService() => new(_repository.Object);
}
