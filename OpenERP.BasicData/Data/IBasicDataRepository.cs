using OpenERP.BasicData.Models;

namespace OpenERP.BasicData.Data;

/// <summary>
/// 基础数据仓储接口（负责基础数据类型与选项的读写）。
/// </summary>
public interface IBasicDataRepository
{
    /// <summary>
    /// 初始化基础数据表结构和默认字典。
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// 获取全部基础数据类型。
    /// </summary>
    Task<List<BasicDataType>> GetTypesAsync();

    /// <summary>
    /// 按类型ID获取基础数据类型。
    /// </summary>
    Task<BasicDataType?> GetTypeByIdAsync(int id);

    /// <summary>
    /// 按类型编码获取基础数据类型（用于模块入口按业务编码定位）。
    /// </summary>
    Task<BasicDataType?> GetTypeByCodeAsync(string typeCode);

    /// <summary>
    /// 新增基础数据类型。
    /// </summary>
    Task CreateTypeAsync(BasicDataType type, string? userName = null);

    /// <summary>
    /// 更新基础数据类型。
    /// </summary>
    Task<bool> UpdateTypeAsync(BasicDataType type, string? userName = null);

    /// <summary>
    /// 删除基础数据类型（逻辑删除）。
    /// </summary>
    Task<bool> DeleteTypeAsync(int id, string? userName = null);

    /// <summary>
    /// 获取基础数据选项（可按类型过滤）。
    /// </summary>
    Task<List<BasicDataItem>> GetItemsAsync(int? typeId = null);

    /// <summary>
    /// 按类型ID获取基础数据选项。
    /// </summary>
    Task<List<BasicDataItem>> GetItemsByTypeIdAsync(int typeId);

    /// <summary>
    /// 按选项ID获取基础数据选项。
    /// </summary>
    Task<BasicDataItem?> GetItemByIdAsync(int id);

    /// <summary>
    /// 新增基础数据选项。
    /// </summary>
    Task CreateItemAsync(BasicDataItem item, string? userName = null);

    /// <summary>
    /// 更新基础数据选项。
    /// </summary>
    Task<bool> UpdateItemAsync(BasicDataItem item, string? userName = null);

    /// <summary>
    /// 删除基础数据选项（逻辑删除）。
    /// </summary>
    Task<bool> DeleteItemAsync(int id, string? userName = null);
}
