using DocumentTracker.Controllers;
using DocumentTracker.Services;
using DocumentTracker.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace DocumentTracker.Tests.Controllers;

public class PaymentReceiptsControllerTests
{
    private static PaymentReceiptsController CreateController(
        Mock<IPaymentReceiptService> service,
        Mock<IPaymentReceiptCreatePageService> createPageService)
    {
        return new PaymentReceiptsController(
            service.Object,
            createPageService.Object,
            Mock.Of<ICurrentUserRoleProvider>(),
            Mock.Of<ILogger<PaymentReceiptsController>>());
    }

    [Fact]
    public async Task Details_WhenReceiptIsMissing_ReturnsNotFound()
    {
        var service = new Mock<IPaymentReceiptService>();
        var createPageService = new Mock<IPaymentReceiptCreatePageService>();
        var controller = CreateController(service, createPageService);

        service
            .Setup(paymentReceiptService => paymentReceiptService.GetDetailsAsync(5))
            .ReturnsAsync((PaymentReceiptDetailsViewModel?)null);

        var result = await controller.Details(5);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Details_WhenReceiptExists_ReturnsDetailsView()
    {
        var service = new Mock<IPaymentReceiptService>();
        var createPageService = new Mock<IPaymentReceiptCreatePageService>();
        var controller = CreateController(service, createPageService);
        var details = new PaymentReceiptDetailsViewModel
        {
            Id = 5,
            ReceiptNumber = "PR-1001",
            InvoiceNumber = "INV-1001",
            CustomerName = "Northwind Traders"
        };

        service
            .Setup(paymentReceiptService => paymentReceiptService.GetDetailsAsync(5))
            .ReturnsAsync(details);

        var result = await controller.Details(5);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Same(details, viewResult.Model);
    }

    [Fact]
    public async Task Delete_WhenReceiptIsMissing_ReturnsNotFound()
    {
        var service = new Mock<IPaymentReceiptService>();
        var createPageService = new Mock<IPaymentReceiptCreatePageService>();
        var controller = CreateController(service, createPageService);

        service
            .Setup(paymentReceiptService => paymentReceiptService.GetDeleteAsync(20))
            .ReturnsAsync((PaymentReceiptDeleteViewModel?)null);

        var result = await controller.Delete(20);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_WhenReceiptExists_ReturnsDeleteView()
    {
        var service = new Mock<IPaymentReceiptService>();
        var createPageService = new Mock<IPaymentReceiptCreatePageService>();
        var controller = CreateController(service, createPageService);
        var deleteModel = new PaymentReceiptDeleteViewModel
        {
            Id = 20,
            ReceiptNumber = "PR-1020",
            InvoiceNumber = "INV-1020",
            CustomerName = "Northwind Traders"
        };

        service
            .Setup(paymentReceiptService => paymentReceiptService.GetDeleteAsync(20))
            .ReturnsAsync(deleteModel);

        var result = await controller.Delete(20);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Same(deleteModel, viewResult.Model);
    }
    
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

        var controller = CreateController(service, createPageService);

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
        var controller = CreateController(service, createPageService);
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
        var controller = CreateController(service, createPageService);
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
        var search = new PaymentReceiptSearchViewModel
        {
            Page = 0,
            PageSize = 0
        };
        service
            .Setup(paymentReceiptService => paymentReceiptService.SearchAsync(search))
            .ReturnsAsync(new PaginatedResult<PaymentReceiptListItemViewModel>());

        var controller = CreateController(service, new Mock<IPaymentReceiptCreatePageService>());

        var result = await controller.Index(search);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<PaymentReceiptSearchViewModel>(viewResult.Model);
        Assert.Equal(1, model.Page);
        Assert.Equal(10, model.PageSize);
        service.Verify(paymentReceiptService => paymentReceiptService.SearchAsync(search), Times.Once);
    }

    [Fact]
    public async Task Index_WithSearchAndDateFilter_ReturnsFilteredReceipts()
    {
        var service = new Mock<IPaymentReceiptService>();
        var search = new PaymentReceiptSearchViewModel
        {
            SearchTerm = "Northwind",
            DateFrom = new DateOnly(2026, 5, 6),
            DateTo = new DateOnly(2026, 5, 8),
            Sort = "receipt_number",
            Order = "asc",
            Page = 1,
            PageSize = 10
        };
        var receipts = new[]
        {
            new PaymentReceiptListItemViewModel
            {
                Id = 6,
                ReceiptNumber = "PR-1006",
                InvoiceNumber = "INV-1001",
                CustomerName = "Northwind Traders"
            },
            new PaymentReceiptListItemViewModel
            {
                Id = 8,
                ReceiptNumber = "PR-1008",
                InvoiceNumber = "INV-1001",
                CustomerName = "Northwind Traders"
            }
        };

        service
            .Setup(paymentReceiptService => paymentReceiptService.SearchAsync(search))
            .ReturnsAsync(new PaginatedResult<PaymentReceiptListItemViewModel>
            {
                Total = receipts.Length,
                Items = receipts
            });

        var controller = CreateController(service, new Mock<IPaymentReceiptCreatePageService>());

        var result = await controller.Index(search);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<PaymentReceiptSearchViewModel>(viewResult.Model);
        Assert.Equal("Northwind", model.SearchTerm);
        Assert.Equal(new DateOnly(2026, 5, 6), model.DateFrom);
        Assert.Equal(new DateOnly(2026, 5, 8), model.DateTo);
        Assert.Equal("receipt_number", model.Sort);
        Assert.Equal("asc", model.Order);
        Assert.Equal(2, model.Total);
        Assert.Same(receipts, model.Receipts);
        service.Verify(paymentReceiptService => paymentReceiptService.SearchAsync(search), Times.Once);
    }
}
