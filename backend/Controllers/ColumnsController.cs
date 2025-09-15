using Microsoft.AspNetCore.Mvc;
using Backend.Services;
using Backend.DTOs;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ColumnsController : ControllerBase
{
    private readonly IColumnService _columnService;
    
    public ColumnsController(IColumnService columnService)
    {
        _columnService = columnService;
    }

    /// <summary>
    /// Get all columns
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ColumnResponse>>> GetColumns()
    {
        var columns = await _columnService.GetAllColumnsAsync();
        return Ok(columns);
    }

    /// <summary>
    /// Get a specific column by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ColumnResponse>> GetColumn(int id)
    {
        var column = await _columnService.GetColumnByIdAsync(id);
        if (column == null)
        {
            return NotFound();
        }
        return Ok(column);
    }

    /// <summary>
    /// Create a new column
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ColumnResponse>> CreateColumn(CreateColumnRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var column = await _columnService.CreateColumnAsync(request);
        return CreatedAtAction(nameof(GetColumn), new { id = column.Id }, column);
    }

    /// <summary>
    /// Delete a column
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteColumn(int id)
    {
        try
        {
            var success = await _columnService.DeleteColumnAsync(id);
            if (!success)
            {
                return NotFound();
            }
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
