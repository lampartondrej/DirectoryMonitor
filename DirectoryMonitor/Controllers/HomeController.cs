using System.Diagnostics;
using DirectoryMonitor.Application.Interfaces;
using DirectoryMonitor.Models;
using Microsoft.AspNetCore.Mvc;

namespace DirectoryMonitor.Controllers;

public class HomeController : Controller
{
    private readonly IDirectoryAnalysisService _analysisService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(IDirectoryAnalysisService analysisService, ILogger<HomeController> logger)
    {
        _analysisService = analysisService;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Index() => View(new AnalyzeViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Analyze(AnalyzeViewModel model, CancellationToken cancellationToken)
    {
        var validationError = ValidateInput(model.DirectoryPath);
        if (validationError is not null)
        {
            _logger.LogWarning("Invalid directory input: {Reason} — value: {Input}", validationError, model.DirectoryPath);
            model.ErrorMessage = validationError;
            return View("Index", model);
        }

        try
        {
            model.Result = await _analysisService.AnalyzeAsync(model.DirectoryPath.Trim(), cancellationToken);
        }
        catch (DirectoryNotFoundException)
        {
            model.ErrorMessage = $"Directory not found: \"{model.DirectoryPath}\". Please verify the path exists and try again.";
        }
        catch (UnauthorizedAccessException)
        {
            model.ErrorMessage = $"Access denied: \"{model.DirectoryPath}\". The application does not have permission to read this directory.";
        }
        catch (ArgumentException ex)
        {
            model.ErrorMessage = ex.Message;
        }
        catch (OperationCanceledException)
        {
            model.ErrorMessage = "The analysis was cancelled.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error analysing {DirectoryPath}", model.DirectoryPath);
            model.ErrorMessage = "An unexpected error occurred. Please check the application logs.";
        }

        return View("Index", model);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });

    private static string? ValidateInput(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "Please enter a directory path.";

        if (path.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            return "The path contains invalid characters. Please enter a valid directory path.";

        return null;
    }
}


