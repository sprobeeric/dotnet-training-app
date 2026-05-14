using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using DocumentTracker.Models;

namespace DocumentTracker.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return RedirectToAction("Index", "Invoices");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [Route("Home/StatusCode/{statusCode:int}")]
    public IActionResult StatusCode(int statusCode)
    {
        Response.StatusCode = statusCode;

        if (statusCode == StatusCodes.Status404NotFound)
        {
            return View("NotFound");
        }

        return View("Error");
    }
}
