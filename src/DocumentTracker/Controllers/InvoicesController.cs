using DocumentTracker.Services;
using DocumentTracker.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace DocumentTracker.Controllers
{
    public class InvoicesController : Controller
    {
        private readonly IInvoiceService _invoiceService;

        public InvoicesController(
            IInvoiceService invoiceService)
        {
            _invoiceService = invoiceService;
        }


            // GET: InvoiceController
        public async Task<IActionResult> Edit(int id)
        {
            var result = await _invoiceService.GetEditAsync(id);
            if (!result.Succeeded || result.Value is null)
            {
                AddServiceErrors(result);
                return View("ErrorMessage");
            }

            return View(result.Value);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, InvoiceEditViewModel viewModel)
        {
            if (id != viewModel.Id)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return View(viewModel);
            }

            var result = await _invoiceService.UpdateAsync(viewModel);
            if (!result.Succeeded)
            {
                AddServiceErrors(result);
                return View(viewModel);
            }

            return RedirectToAction(nameof(Edit), new { id = viewModel.Id });
        }

        
        private void AddServiceErrors(ServiceResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(error.Key, error.Message);
            }
        }
    }
}
