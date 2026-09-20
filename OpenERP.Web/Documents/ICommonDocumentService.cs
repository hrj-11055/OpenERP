namespace OpenERP.Web.Documents;

/// <summary>
/// 通用文档管理服务（供单据、员工资料、客户资料等页面复用）。
/// </summary>
public interface ICommonDocumentService
{
    /// <summary>
    /// 初始化通用文档管理表结构（创建 SYS_Document 表及必要索引）。
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// 查询指定业务记录关联的文档列表。
    /// </summary>
    Task<List<ManagedDocument>> GetDocumentsAsync(string featureCode, int entityId);

    /// <summary>
    /// 保存上传文档并写入文档元数据。
    /// </summary>
    Task<ManagedDocument> SaveDocumentAsync(string featureCode, int entityId, IFormFile file, string? uploadedBy);

    /// <summary>
    /// 按文档ID读取文档元数据。
    /// </summary>
    Task<ManagedDocument?> GetDocumentAsync(int documentId);

    /// <summary>
    /// 删除文档记录并移除对应物理文件。
    /// </summary>
    Task<bool> DeleteDocumentAsync(int documentId, string? deletedBy);
}
