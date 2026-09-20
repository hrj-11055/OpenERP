using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenERP.Web.Documents;

namespace OpenERP.Web.Controllers;

/// <summary>
/// 通用文档管理 API 控制器（提供文档查询、上传、打开、下载与删除能力）。
/// </summary>
[ApiController]
[Route("api/documents")]
[Authorize]
public class DocumentsApiController : ControllerBase
{
    /// <summary>
    /// 通用文档管理服务（负责文档元数据与物理文件）。
    /// </summary>
    private readonly ICommonDocumentService _documentService;

    public DocumentsApiController(ICommonDocumentService documentService)
    {
        _documentService = documentService;
    }

    /// <summary>
    /// 查询指定业务记录关联的文档列表。
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetList(string featureCode, int entityId)
    {
        if (string.IsNullOrWhiteSpace(featureCode) || entityId <= 0)
        {
            return BadRequest(new { message = "文档关联的业务记录无效。" });
        }

        var documents = await _documentService.GetDocumentsAsync(featureCode, entityId);
        return Ok(new
        {
            items = documents.Select(ToDocumentDto)
        });
    }

    /// <summary>
    /// 上传一个或多个文档并关联到指定业务记录。
    /// </summary>
    [HttpPost("upload")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload([FromForm] string featureCode, [FromForm] int entityId, [FromForm] List<IFormFile> files)
    {
        if (string.IsNullOrWhiteSpace(featureCode) || entityId <= 0)
        {
            return BadRequest(new { message = "文档关联的业务记录无效。" });
        }

        if (files.Count == 0)
        {
            return BadRequest(new { message = "请选择要上传的文档。" });
        }

        var operatorDisplayName = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue("erp:account") ?? "System";
        var savedDocuments = new List<ManagedDocument>();
        try
        {
            foreach (var file in files)
            {
                savedDocuments.Add(await _documentService.SaveDocumentAsync(featureCode, entityId, file, operatorDisplayName));
            }
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        return Ok(new
        {
            message = "文档已上传。",
            items = savedDocuments.Select(ToDocumentDto)
        });
    }

    /// <summary>
    /// 在浏览器中打开文档（尽量使用 inline 方式交给浏览器预览）。
    /// </summary>
    [HttpGet("{id:int}/open")]
    public async Task<IActionResult> Open(int id)
    {
        var document = await _documentService.GetDocumentAsync(id);
        if (document is null || !System.IO.File.Exists(document.StoragePath))
        {
            return NotFound(new { message = "未找到该文档。" });
        }

        Response.Headers.ContentDisposition = $"inline; filename*=UTF-8''{Uri.EscapeDataString(document.OriginalFileName)}";
        var stream = System.IO.File.OpenRead(document.StoragePath);
        return File(stream, document.ContentType, enableRangeProcessing: true);
    }

    /// <summary>
    /// 下载文档（使用原始文档名作为下载文件名）。
    /// </summary>
    [HttpGet("{id:int}/download")]
    public async Task<IActionResult> Download(int id)
    {
        var document = await _documentService.GetDocumentAsync(id);
        if (document is null || !System.IO.File.Exists(document.StoragePath))
        {
            return NotFound(new { message = "未找到该文档。" });
        }

        var stream = System.IO.File.OpenRead(document.StoragePath);
        return File(stream, document.ContentType, document.OriginalFileName, enableRangeProcessing: true);
    }

    /// <summary>
    /// 删除指定文档。
    /// </summary>
    [HttpDelete("{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var operatorDisplayName = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue("erp:account") ?? "System";
        var deleted = await _documentService.DeleteDocumentAsync(id, operatorDisplayName);
        return deleted
            ? Ok(new { message = "文档已删除。" })
            : NotFound(new { message = "未找到可删除的文档。" });
    }

    /// <summary>
    /// 转换文档前端显示资料。
    /// </summary>
    private static object ToDocumentDto(ManagedDocument document)
        => new
        {
            document.Id,
            document.OriginalFileName,
            document.ContentType,
            document.FileExtension,
            document.FileSizeBytes,
            isImage = IsImageDocument(document),
            fileSizeLabel = FormatFileSize(document.FileSizeBytes),
            uploadedAt = document.UploadedAt.ToString("yyyy-MM-dd HH:mm:ss"),
            document.UploadedBy,
            openUrl = $"/api/documents/{document.Id}/open",
            downloadUrl = $"/api/documents/{document.Id}/download"
        };

    /// <summary>
    /// 判断文档是否为前端可展示的图片类型。
    /// </summary>
    private static bool IsImageDocument(ManagedDocument document)
    {
        var contentType = document.ContentType.ToLowerInvariant();
        var extension = document.FileExtension.ToLowerInvariant();
        return contentType is "image/jpeg" or "image/png" or "image/webp" or "image/gif"
            || extension is ".jpg" or ".jpeg" or ".png" or ".webp" or ".gif";
    }

    /// <summary>
    /// 格式化文档空间大小。
    /// </summary>
    private static string FormatFileSize(long fileSizeBytes)
    {
        string[] units = ["B", "KB", "MB", "GB"];
        var value = Convert.ToDouble(fileSizeBytes);
        var unitIndex = 0;
        while (value >= 1024D && unitIndex < units.Length - 1)
        {
            value /= 1024D;
            unitIndex += 1;
        }

        return unitIndex == 0 ? $"{fileSizeBytes} {units[unitIndex]}" : $"{value:0.##} {units[unitIndex]}";
    }
}
