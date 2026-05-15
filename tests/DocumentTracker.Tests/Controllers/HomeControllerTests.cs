using DocumentTracker.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DocumentTracker.Tests.Controllers;

public class HomeControllerTests
{
    [Fact]
    public void Index_RedirectsToInvoices()
    {
        var controller = new HomeController();

        var result = controller.Index();

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("Invoices", redirectResult.ControllerName);
    }

    [Fact]
    public void StatusCode_When404_ReturnsNotFoundView()
    {
        var controller = new HomeController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var result = controller.StatusCode(StatusCodes.Status404NotFound);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("NotFound", viewResult.ViewName);
        Assert.Equal(StatusCodes.Status404NotFound, controller.Response.StatusCode);
    }

    [Fact]
    public void StatusCode_WhenNon404_ReturnsErrorView()
    {
        var controller = new HomeController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var result = controller.StatusCode(StatusCodes.Status500InternalServerError);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("Error", viewResult.ViewName);
        Assert.Equal(StatusCodes.Status500InternalServerError, controller.Response.StatusCode);
    }
}