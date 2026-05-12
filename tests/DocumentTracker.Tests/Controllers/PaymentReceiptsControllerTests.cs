using DocumentTracker.Controllers;
using DocumentTracker.Services;
using DocumentTracker.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace DocumentTracker.Tests.Controllers;

public class PaymentReceiptsControllerTests
{
    [Fact]
    public async Task Delete_WhenPaymentReceiptIsMissing_ReturnsNotFound()
    {
        var service = new Mock<IPaymentReceiptService>();
        var roleProvider = new Mock<ICurrentUserRoleProvider>();
        var logger = new Mock<ILogger<PaymentReceiptsController>>();
        var controller = new PaymentReceiptsController(service.Object, roleProvider.Object, logger.Object);

        service
            .Setup(paymentReceiptService => paymentReceiptService.GetDeleteAsync(7))
            .ReturnsAsync((PaymentReceiptDeleteViewModel?)null);

        var result = await controller.Delete(7);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_Post_WithSuccessfulSoftDelete_RedirectsToDocumentsIndex()
    {
        var service = new Mock<IPaymentReceiptService>();
        var roleProvider = new Mock<ICurrentUserRoleProvider>();
        var logger = new Mock<ILogger<PaymentReceiptsController>>();
        var controller = new PaymentReceiptsController(service.Object, roleProvider.Object, logger.Object);

        roleProvider
            .Setup(provider => provider.GetCurrentRole())
            .Returns(DocumentRoles.DocumentAdmin);

        service
            .Setup(paymentReceiptService => paymentReceiptService.SoftDeleteAsync(7, DocumentRoles.DocumentAdmin))
            .ReturnsAsync(ServiceResult.Success());

        var result = await controller.DeleteConfirmed(7);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Documents", redirect.ControllerName);
    }
}
