namespace OpenERP.Web.Printing;

/// <summary>
/// 公司打印 LOGO 文件结果（供控制器返回图片流时描述文件路径与内容类型）。
/// </summary>
public class CompanyPrintLogoFileResult
{
    /// <summary>
    /// 图片文件物理路径（位于系统安装目录下）。
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// 图片内容类型（仅支持 image/png 与 image/jpeg）。
    /// </summary>
    public string ContentType { get; set; } = "image/png";

    /// <summary>
    /// 输出文件名（组织编号加图片扩展名）。
    /// </summary>
    public string FileName { get; set; } = string.Empty;
}
