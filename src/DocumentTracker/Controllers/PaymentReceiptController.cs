using System.ComponentModel.Design;
using DocumentTracker.Services;
using Microsoft.AspNetCore.Mvc;

namespace DocumentTracker.Controllers;

public class PaymentReceiptsController : Controller
{
    private readonly IPaymentReceiptService _paymentReceiptService;

    public PaymentReceiptsController(IPaymentReceiptService paymentReceiptService)
    {
        _paymentReceiptService = paymentReceiptService;
    }

    public async Task<IActionResult> Details (int id)
    {
        var receipt = await _paymentReceiptService.GetDetailsAsync(id);
        return receipt is null ? NotFound() : View(receipt);
    }
}