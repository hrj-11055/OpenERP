namespace OpenERP.Web.Documents;

/// <summary>
/// 通用文档元数据（按业务功能编码与业务记录ID关联上传文档）。
/// </summary>
public class ManagedDocument
{
    /// <summary>
    /// 文档ID（对应 SYS_Document 实体主键）。
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 业务功能编码（用于区分员工资料、员工图片、培训历程等业务来源）。
    /// </summary>
    public string FeatureCode { get; set; } = string.Empty;

    /// <summary>
    /// 业务记录ID（对应具体单据或资料实体主键）。
    /// </summary>
    public int EntityId { get; set; }

    /// <summary>
    /// 原始文档名（来自用户上传文件名，用于界面显示与下载命名）。
    /// </summary>
    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>
    /// 存储文档名（系统生成的唯一文件名，用于避免重名覆盖）。
    /// </summary>
    public string StoredFileName { get; set; } = string.Empty;

    /// <summary>
    /// 文档类型（MIME 类型，来自上传文件内容类型）。
    /// </summary>
    public string ContentType { get; set; } = "application/octet-stream";

    /// <summary>
    /// 文档扩展名（来自原始文件名，用于识别文件格式）。
    /// </summary>
    public string FileExtension { get; set; } = string.Empty;

    /// <summary>
    /// 文档空间大小（字节数，来自上传文件长度）。
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// 物理存储路径（系统内部使用，不直接暴露给前端）。
    /// </summary>
    public string StoragePath { get; set; } = string.Empty;

    /// <summary>
    /// 上传时间（记录文档写入系统的时间）。
    /// </summary>
    public DateTime UploadedAt { get; set; }

    /// <summary>
    /// 上传人（来自当前登录用户）。
    /// </summary>
    public string? UploadedBy { get; set; }
}
