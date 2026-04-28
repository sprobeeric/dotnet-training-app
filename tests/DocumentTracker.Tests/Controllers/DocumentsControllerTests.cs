using DocumentTracker.Controllers;
using DocumentTracker.Services;
using DocumentTracker.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace DocumentTracker.Tests.Controllers;

public class DocumentsControllerTests
{
    [Fact]
    public async Task Create_Post_WithInvalidModelState_ReturnsCreateView()
    {
        var service = new Mock<IDocumentService>();
        var roleProvider = new Mock<ICurrentUserRoleProvider>();
        var logger = new Mock<ILogger<DocumentsController>>();
        var controller = new DocumentsController(service.Object, roleProvider.Object, logger.Object);
        var viewModel = new DocumentCreateViewModel();

        controller.ModelState.AddModelError(nameof(DocumentCreateViewModel.Title), "The Title field is required.");

        var result = await controller.Create(viewModel);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Same(viewModel, viewResult.Model);
        service.Verify(documentService => documentService.CreateAsync(It.IsAny<DocumentCreateViewModel>()), Times.Never);
    }
}
