using Microsoft.AspNetCore.Http;

namespace OpenERP.Web.Printing;

/// <summary>
/// 公司打印模板服务接口（统一处理公司 LOGO、页头、页脚和打印公共模板配置）。
/// </summary>
public interface ICompanyPrintTemplateService
{
    /// <summary>
    /// 按公司组织ID获取打印模板设置。
    /// </summary>
    Task<CompanyPrintTemplateSettings?> GetSettingsAsync(int companyOrganizationId);

    /// <summary>
    /// 按公司组织编号获取打印模板设置（供共享布局按当前公司自动套用打印模板）。
    /// </summary>
    Task<CompanyPrintTemplateSettings?> GetSettingsByOrganizationCodeAsync(string? organizationCode);

    /// <summary>
    /// 保存公司 LOGO 文件（文件名固定为组织编号，存入系统安装目录）。
    /// </summary>
    Task<CompanyPrintTemplateSettings?> SaveLogoAsync(int companyOrganizationId, IFormFile logoFile);

    /// <summary>
    /// 删除公司 LOGO 文件（仅删除当前组织编号对应的 JPG/JPEG/PNG 文件）。
    /// </summary>
    Task<bool> DeleteLogoAsync(int companyOrganizationId);

    /// <summary>
    /// 当组织编号变更时同步重命名 LOGO 文件。
    /// </summary>
    Task<bool> RenameLogoAsync(string? oldOrganizationCode, string? newOrganizationCode);

    /// <summary>
    /// 按组织编号读取 LOGO 文件信息（供图片接口输出图片流）。
    /// </summary>
    Task<CompanyPrintLogoFileResult?> GetLogoFileAsync(string organizationCode);
}
