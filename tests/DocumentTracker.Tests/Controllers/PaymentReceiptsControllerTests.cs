using DocumentTracker.Controllers;
using DocumentTracker.Services;
using DocumentTracker.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace DocumentTracker.Tests.Controllers;

public class PaymentReceiptControllerTests
{
    [Fact]
    public async Task Create_Get_WithInvoiceNumber_LoadsCreateViewForThatInvoice()
    {
        var service = new Mock<IPaymentReceiptService>();
        var createPageService = new Mock<IPaymentReceiptCreatePageService>();
        createPageService
            .Setup(paymentReceiptService => paymentReceiptService.BuildAsync("INV-1001"))
            .ReturnsAsync(new PaymentReceiptCreateViewModel
            {
                InvoiceNumber = "INV-1001"
            });

        var controller = new PaymentReceiptsController(service.Object, createPageService.Object);

        var result = await controller.Create("INV-1001");

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<PaymentReceiptCreateViewModel>(viewResult.Model);
        Assert.Equal("INV-1001", model.InvoiceNumber);
        createPageService.Verify(paymentReceiptService => paymentReceiptService.BuildAsync("INV-1001"), Times.Once);
    }

    [Fact]
    public async Task Create_Post_WithInvalidModelState_ReturnsCreateView()
    {
        var service = new Mock<IPaymentReceiptService>();
        var createPageService = new Mock<IPaymentReceiptCreatePageService>();
        var controller = new PaymentReceiptsController(service.Object, createPageService.Object);
        var viewModel = new PaymentReceiptCreateViewModel
        {
            InvoiceNumber = "INV-1001",
            PaymentDate = new DateOnly(2026, 5, 13),
            AmountPaid = 100m,
            PaymentMethod = "Cash"
        };

        controller.ModelState.AddModelError(nameof(PaymentReceiptCreateViewModel.AmountPaid), "The Amount Paid field is required.");

        var result = await controller.Create(viewModel);

        var viewResult = Assert.IsType<ViewResult>(result);
        var resultModel = Assert.IsType<PaymentReceiptCreateViewModel>(viewResult.Model);
        Assert.Equal("INV-1001", resultModel.InvoiceNumber);
        Assert.Equal(100m, resultModel.AmountPaid);
        Assert.Equal("Cash", resultModel.PaymentMethod);
        createPageService.Verify(paymentReceiptService => paymentReceiptService.PopulateInvoiceSummaryAsync(viewModel), Times.Once);
        service.Verify(paymentReceiptService => paymentReceiptService.CreateAsync(It.IsAny<PaymentReceiptCreateViewModel>()), Times.Never);
    }

    [Fact]
    public async Task Create_Post_WithDuplicateReceiptNumber_AddsFieldError()
    {
        var service = new Mock<IPaymentReceiptService>();
        var createPageService = new Mock<IPaymentReceiptCreatePageService>();
        var controller = new PaymentReceiptsController(service.Object, createPageService.Object);
        var viewModel = new PaymentReceiptCreateViewModel
        {
            ReceiptNumber = "RCT-0001",
            InvoiceNumber = "INV-1001",
            PaymentDate = new DateOnly(2026, 5, 13),
            AmountPaid = 100m,
            PaymentMethod = "Cash"
        };

        service
            .Setup(paymentReceiptService => paymentReceiptService.CreateAsync(viewModel))
            .ReturnsAsync(ServiceResult<int>.Failure(nameof(PaymentReceiptCreateViewModel.ReceiptNumber), "A payment receipt with this receipt number already exists."));

        var result = await controller.Create(viewModel);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<PaymentReceiptCreateViewModel>(viewResult.Model);
        Assert.False(controller.ModelState.IsValid);
        Assert.Contains(controller.ModelState[nameof(PaymentReceiptCreateViewModel.ReceiptNumber)]!.Errors,
            error => error.ErrorMessage == "A payment receipt with this receipt number already exists.");
        createPageService.Verify(paymentReceiptService => paymentReceiptService.PopulateInvoiceSummaryAsync(viewModel), Times.Once);
    }

    [Fact]
    public async Task Index_WithoutPagingParameters_UsesDefaultPagingValues()
    {
        var service = new Mock<IPaymentReceiptService>();
        service
            .Setup(paymentReceiptService => paymentReceiptService.SearchAsync(null, null, null, "payment_date", "desc", 1, 10))
            .ReturnsAsync(new PaginatedResult<PaymentReceiptListItemViewModel>());

        var controller = new PaymentReceiptsController(service.Object, new Mock<IPaymentReceiptCreatePageService>().Object);

        var result = await controller.Index(null, null, null);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<PaymentReceiptSearchViewModel>(viewResult.Model);
        Assert.Equal(1, model.Page);
        Assert.Equal(10, model.PageSize);
        service.Verify(paymentReceiptService => paymentReceiptService.SearchAsync(null, null, null, "payment_date", "desc", 1, 10), Times.Once);
    }
}
