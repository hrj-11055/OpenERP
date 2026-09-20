using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using OpenERP.Web.Data.HR;
using OpenERP.Web.Printing;

namespace OpenERP.Web.Controllers;

/// <summary>
/// 公司组织打印模板接口控制器（提供 LOGO 预览、上传、删除和打印模板读取接口）。
/// </summary>
[ApiController]
[Route("api/companyorganizationprinttemplate")]
[Authorize]
public class CompanyOrganizationPrintTemplateApiController : ControllerBase
{
    /// <summary>
    /// 人资仓储（校验公司组织是否存在）。
    /// </summary>
    private readonly IHrRepository _hrRepository;

    /// <summary>
    /// 公司打印模板服务（处理 LOGO、页头、页脚公共打印配置）。
    /// </summary>
    private readonly ICompanyPrintTemplateService _companyPrintTemplateService;

    /// <summary>
    /// 初始化公司组织打印模板接口控制器。
    /// </summary>
    public CompanyOrganizationPrintTemplateApiController(
        IHrRepository hrRepository,
        ICompanyPrintTemplateService companyPrintTemplateService)
    {
        _hrRepository = hrRepository;
        _companyPrintTemplateService = companyPrintTemplateService;
    }

    /// <summary>
    /// 获取公司组织打印模板配置。
    /// </summary>
    [HttpGet("{companyOrganizationId:int}")]
    public async Task<IActionResult> Get(int companyOrganizationId)
    {
        if (companyOrganizationId <= 0)
        {
            return BadRequest(new { message = "未指定有效的公司组织。" });
        }

        var organization = await _hrRepository.GetCompanyOrganizationByIdAsync(companyOrganizationId);
        if (organization is null)
        {
            return NotFound(new { message = "未找到对应的公司组织资料。" });
        }

        var settings = await _companyPrintTemplateService.GetSettingsAsync(companyOrganizationId);
        return Ok(new
        {
            companyOrganizationId,
            organization.OrganizationCode,
            organization.OrganizationName,
            hasLogo = settings?.HasLogo ?? false,
            logoUrl = settings?.LogoUrl,
            logoStoragePath = settings?.LogoStoragePath,
            printHeaderContent = organization.PrintHeaderContent,
            printFooterContent = organization.PrintFooterContent
        });
    }

    /// <summary>
    /// 上传公司组织 LOGO 图片。
    /// </summary>
    [HttpPost("{companyOrganizationId:int}/logo")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> UploadLogo(int companyOrganizationId, IFormFile? file)
    {
        if (companyOrganizationId <= 0)
        {
            return BadRequest(new { message = "未指定有效的公司组织。" });
        }

        var organization = await _hrRepository.GetCompanyOrganizationByIdAsync(companyOrganizationId);
        if (organization is null)
        {
            return NotFound(new { message = "未找到对应的公司组织资料。" });
        }

        try
        {
            var settings = await _companyPrintTemplateService.SaveLogoAsync(companyOrganizationId, file!);
            return Ok(new
            {
                message = "LOGO 已上传。",
                hasLogo = settings?.HasLogo ?? false,
                logoUrl = settings?.LogoUrl,
                logoStoragePath = settings?.LogoStoragePath
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// 删除公司组织 LOGO 图片。
    /// </summary>
    [HttpPost("{companyOrganizationId:int}/logo/delete")]
    public async Task<IActionResult> DeleteLogo(int companyOrganizationId)
    {
        if (companyOrganizationId <= 0)
        {
            return BadRequest(new { message = "未指定有效的公司组织。" });
        }

        var organization = await _hrRepository.GetCompanyOrganizationByIdAsync(companyOrganizationId);
        if (organization is null)
        {
            return NotFound(new { message = "未找到对应的公司组织资料。" });
        }

        var deleted = await _companyPrintTemplateService.DeleteLogoAsync(companyOrganizationId);
        return Ok(new
        {
            message = deleted ? "LOGO 已删除。" : "当前公司未上传 LOGO。",
            hasLogo = false,
            logoUrl = (string?)null,
            logoStoragePath = (string?)null
        });
    }

    /// <summary>
    /// 输出公司组织 LOGO 图片文件。
    /// </summary>
    [HttpGet("logo/{organizationCode}")]
    [ResponseCache(Duration = 600, Location = ResponseCacheLocation.Client)]
    public async Task<IActionResult> Logo(string organizationCode)
    {
        var file = await _companyPrintTemplateService.GetLogoFileAsync(organizationCode);
        if (file is null || !System.IO.File.Exists(file.FilePath))
        {
            return NotFound();
        }

        return PhysicalFile(file.FilePath, file.ContentType, enableRangeProcessing: false);
    }
}
