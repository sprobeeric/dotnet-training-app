using DocumentTracker.Services;
using DocumentTracker.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace DocumentTracker.Controllers;

public class DocumentsController : Controller
{
    private readonly IDocumentService _documentService;
    private readonly ICurrentUserRoleProvider _roleProvider;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(
        IDocumentService documentService,
        ICurrentUserRoleProvider roleProvider,
        ILogger<DocumentsController> logger)
    {
        _documentService = documentService;
        _roleProvider = roleProvider;
        _logger = logger;
    }

    public async Task<IActionResult> Index(string? searchTerm)
    {
        var documents = await _documentService.SearchAsync(searchTerm);
        return View(new DocumentSearchViewModel
        {
            SearchTerm = searchTerm,
            Documents = documents
        });
    }

    public async Task<IActionResult> Details(int id)
    {
        var document = await _documentService.GetDetailsAsync(id);
        return document is null ? NotFound() : View(document);
    }

    public IActionResult Create()
    {
        return View(new DocumentCreateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DocumentCreateViewModel viewModel)
    {
        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        var result = await _documentService.CreateAsync(viewModel);
        if (!result.Succeeded)
        {
            AddServiceErrors(result);
            return View(viewModel);
        }

        return RedirectToAction(nameof(Details), new { id = result.Value });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var result = await _documentService.GetEditAsync(id);
        if (!result.Succeeded || result.Value is null)
        {
            AddServiceErrors(result);
            return View("ErrorMessage");
        }

        return View(result.Value);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, DocumentEditViewModel viewModel)
    {
        if (id != viewModel.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        var result = await _documentService.UpdateAsync(viewModel);
        if (!result.Succeeded)
        {
            AddServiceErrors(result);
            return View(viewModel);
        }

        return RedirectToAction(nameeof(Create));
       // return RedirectToAction(nameof(Details), new { id = viewModel.Id });
    }

    public async Task<IActionResult> Delete(int id)
    {
        var document = await _documentService.GetDeleteAsync(id);
        return document is null ? NotFound() : View(document);
    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var result = await _documentService.SoftDeleteAsync(id, _roleProvider.GetCurrentRole());
        if (!result.Succeeded)
        {
            _logger.LogWarning("Soft delete was rejected for document id {DocumentId}.", id);
            AddServiceErrors(result);

            var document = await _documentService.GetDeleteAsync(id);
            return document is null ? View("ErrorMessage") : View(document);
        }

        return RedirectToAction(nameof(Index));
    }

    private void AddServiceErrors(ServiceResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(error.Key, error.Message);
        }
    }
}
