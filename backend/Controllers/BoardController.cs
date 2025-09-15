using Microsoft.AspNetCore.Mvc;
using Backend.Services;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BoardController : ControllerBase
{
    private readonly IColumnService _columnService;
    
    public BoardController(IColumnService columnService)
    {
        _columnService = columnService;
    }

    /// <summary>
    /// Get the complete board with all columns and tasks
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<DTOs.BoardResponse>> GetBoard()
    {
        var board = await _columnService.GetBoardAsync();
        return Ok(board);
    }
}
