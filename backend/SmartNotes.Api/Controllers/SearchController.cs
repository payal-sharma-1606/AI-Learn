using Microsoft.AspNetCore.Mvc;
using SmartNotes.Api.Exceptions;
using SmartNotes.Api.Interfaces;

namespace SmartNotes.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SearchController : ControllerBase
{
    private readonly ISearchIndexService _searchIndex;
    private readonly ILogger<SearchController> _logger;

    public SearchController(ISearchIndexService searchIndex, ILogger<SearchController> logger)
    {
        _searchIndex = searchIndex;
        _logger = logger;
    }

    /// <summary>
    /// Creates the vector index, or updates it to match the current schema. Run once after
    /// configuring the search resource; re-running is harmless.
    /// </summary>
    [HttpPost("create-index")]
    public async Task<IActionResult> CreateIndex(CancellationToken cancellationToken)
    {
        try
        {
            await _searchIndex.EnsureIndexAsync(cancellationToken);
            return NoContent();
        }
        catch (AiServiceException ex)
        {
            _logger.LogError(ex, "Could not create the search index.");
            return Problem(
                title: "Index unavailable",
                detail: ex.Message,
                statusCode: ex.IsTransient
                    ? StatusCodes.Status503ServiceUnavailable
                    : StatusCodes.Status502BadGateway);
        }
    }
}
