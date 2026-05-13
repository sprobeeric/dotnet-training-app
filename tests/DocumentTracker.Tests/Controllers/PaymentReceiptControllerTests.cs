using DocumentTracker.Controllers;
using DocumentTracker.Services;
using DocumentTracker.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace DocumentTracker.Tests.Controllers;

public class PaymentReceiptControllerTests
{
    [Fact]
    public async Task Create_Post_WithInvalidModelState_ReturnsCreateView()
    {
        var service = new Mock<IPaymentReceiptService>();
        var controller = new PaymentReceiptController(service.Object);
        var viewModel = new PaymentReceiptCreateViewModel
        {
            Received = 100m,
            Products =
            [
                new PaymentReceiptProductInputViewModel
                {
                    ProductId = 1,
                    ProductName = "Classic Pearl Milk Tea",
                    UnitPrice = 120m,
                    Quantity = 2
                }
            ]
        };

        controller.ModelState.AddModelError(nameof(PaymentReceiptCreateViewModel.Received), "The Amount Received field is required.");
        service.Setup(paymentReceiptService => paymentReceiptService.GetCreateAsync())
            .ReturnsAsync(new PaymentReceiptCreateViewModel
            {
                Products =
                [
                    new PaymentReceiptProductInputViewModel
                    {
                        ProductId = 1,
                        ProductName = "Classic Pearl Milk Tea",
                        UnitPrice = 120m
                    }
                ]
            });

        var result = await controller.Create(viewModel);

        var viewResult = Assert.IsType<ViewResult>(result);
        var resultModel = Assert.IsType<PaymentReceiptCreateViewModel>(viewResult.Model);
        Assert.Equal(100m, resultModel.Received);
        Assert.Single(resultModel.Products);
        Assert.Equal(2, resultModel.Products[0].Quantity);
        service.Verify(paymentReceiptService => paymentReceiptService.CreateAsync(It.IsAny<PaymentReceiptCreateViewModel>()), Times.Never);
    }

    [Fact]
    public async Task Index_WithoutPagingParameters_UsesDefaultPagingValues()
    {
        var service = new Mock<IPaymentReceiptService>();
        service
            .Setup(paymentReceiptService => paymentReceiptService.SearchAsync(null, null, null, "payment_date", "desc", 1, 10))
            .ReturnsAsync(new PaginatedResult<PaymentReceiptListItemViewModel>());

        var controller = new PaymentReceiptController(service.Object);

        var result = await controller.Index(null, null, null);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<PaymentReceiptSearchViewModel>(viewResult.Model);
        Assert.Equal(1, model.Page);
        Assert.Equal(10, model.PageSize);
        service.Verify(paymentReceiptService => paymentReceiptService.SearchAsync(null, null, null, "payment_date", "desc", 1, 10), Times.Once);
    }
}
