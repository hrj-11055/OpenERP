namespace OpenERP.Web.Printing;

/// <summary>
/// 公司打印模板设置（供打印页、共享布局和单据打印功能统一读取公司级页眉页脚配置）。
/// </summary>
public class CompanyPrintTemplateSettings
{
    /// <summary>
    /// 公司组织ID（对应 CompanyOrganization 主表记录）。
    /// </summary>
    public int CompanyOrganizationId { get; set; }

    /// <summary>
    /// 公司组织编号（同时作为 LOGO 文件名）。
    /// </summary>
    public string OrganizationCode { get; set; } = string.Empty;

    /// <summary>
    /// 公司组织名称（供打印页眉展示公司名称）。
    /// </summary>
    public string OrganizationName { get; set; } = string.Empty;

    /// <summary>
    /// 是否已存在公司 LOGO 文件。
    /// </summary>
    public bool HasLogo { get; set; }

    /// <summary>
    /// LOGO 访问地址（供浏览器预览和打印时加载图片）。
    /// </summary>
    public string? LogoUrl { get; set; }

    /// <summary>
    /// LOGO 存储路径（系统安装目录下的实际文件路径，仅供管理页提示）。
    /// </summary>
    public string? LogoStoragePath { get; set; }

    /// <summary>
    /// 打印页头内容（公司级公共页头，多行文本）。
    /// </summary>
    public string? PrintHeaderContent { get; set; }

    /// <summary>
    /// 打印页脚内容（公司级公共页脚，多行文本）。
    /// </summary>
    public string? PrintFooterContent { get; set; }
}
