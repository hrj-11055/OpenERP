using System;

namespace OpenERP.Web.Areas.HR.ViewModels.CompanyOrganizations;

/// <summary>
/// 公司组织列表项视图模型（用于组织记录表格展示）。
/// </summary>
public class CompanyOrganizationListItemViewModel
{
    /// <summary>
    /// 公司组织记录ID（对应 CompanyOrganization 实体主键）。
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 序号（列表展示序号，不入库）。
    /// </summary>
    public int SequenceNo { get; set; }

    /// <summary>
    /// 组织编号（业务唯一编码）。
    /// </summary>
    public string OrganizationCode { get; set; } = string.Empty;

    /// <summary>
    /// 组织简称（列表展示短名称，便于表格显示）。
    /// </summary>
    public string OrganizationShortName { get; set; } = string.Empty;

    /// <summary>
    /// 公司名称（组织全称，来自 CompanyOrganization.OrganizationName）。
    /// </summary>
    public string CompanyName { get; set; } = string.Empty;

    /// <summary>
    /// 状态名称（来自基础数据字典 ORG_STATUS）。
    /// </summary>
    public string StatusName { get; set; } = string.Empty;

    /// <summary>
    /// 企业类型名称（来自基础数据字典 ENTERPRISE_TYPE）。
    /// </summary>
    public string EnterpriseTypeName { get; set; } = string.Empty;

    /// <summary>
    /// 地区展示名称（地区/城市/区县组合，只读展示）。
    /// </summary>
    public string RegionDisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 负责人（组织联系人或负责人）。
    /// </summary>
    public string Principal { get; set; } = string.Empty;

    /// <summary>
    /// 电话（组织联系电话）。
    /// </summary>
    public string Phone { get; set; } = string.Empty;

    /// <summary>
    /// 传真（组织传真号码）。
    /// </summary>
    public string Fax { get; set; } = string.Empty;

    /// <summary>
    /// 邮箱（组织联系邮箱）。
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// 备注（业务补充说明）。
    /// </summary>
    public string Remarks { get; set; } = string.Empty;

    /// <summary>
    /// 档案路径（电子档案目录，只读展示）。
    /// </summary>
    public string ArchivePath { get; set; } = string.Empty;

    /// <summary>
    /// 最后修改人（优先 UpdatedBy，回退 CreatedBy）。
    /// </summary>
    public string LastModifiedBy { get; set; } = string.Empty;

    /// <summary>
    /// 最后修改时间文本（优先 UpdatedAt，回退 CreatedAt）。
    /// </summary>
    public string LastModifiedAtText { get; set; } = string.Empty;

    /// <summary>
    /// 最后修改时间（用于排序和展示）。
    /// </summary>
    public DateTime LastModifiedAt { get; set; }
}
