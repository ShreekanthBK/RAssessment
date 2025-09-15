using Microsoft.AspNetCore.Mvc;
using Backend.Services;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AttachmentsController : ControllerBase
{
    private readonly IAttachmentService _attachmentService;
    
    public AttachmentsController(IAttachmentService attachmentService)
    {
        _attachmentService = attachmentService;
    }

    /// <summary>
    /// Upload an attachment to a task
    /// </summary>
    [HttpPost("tasks/{taskId}")]
    public async Task<ActionResult<DTOs.AttachmentResponse>> UploadAttachment(int taskId, IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded");
        }

        try
        {
            var attachment = await _attachmentService.UploadAttachmentAsync(taskId, file);
            return Ok(attachment);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Download an attachment
    /// </summary>
    [HttpGet("{id}/download")]
    public async Task<IActionResult> DownloadAttachment(int id)
    {
        var result = await _attachmentService.DownloadAttachmentAsync(id);
        if (result == null)
        {
            return NotFound();
        }

        var (stream, contentType, fileName) = result.Value;
        return File(stream, contentType, fileName);
    }

    /// <summary>
    /// Delete an attachment
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAttachment(int id)
    {
        var success = await _attachmentService.DeleteAttachmentAsync(id);
        if (!success)
        {
            return NotFound();
        }
        return NoContent();
    }
}
