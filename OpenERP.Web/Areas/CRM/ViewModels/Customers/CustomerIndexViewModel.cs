namespace OpenERP.Web.Areas.CRM.ViewModels.Customers
{
    /// <summary>
    /// 客户资料列表页模型（包含客户主表列表与当前客户联络人列表）。
    /// </summary>
    public class CustomerIndexViewModel
    {
        /// <summary>
        /// 查询范围（全部、客户编号、客户名称、联系人、备注等）。
        /// </summary>
        public string Scope { get; set; } = "全部";

        /// <summary>
        /// 查询关键字（用于过滤客户主表记录）。
        /// </summary>
        public string? Keyword { get; set; }

        /// <summary>
        /// 当前选中客户ID（对应 CRM_Customer 主键）。
        /// </summary>
        public int? SelectedCustomerId { get; set; }

        /// <summary>
        /// 当前选中客户名称（用于联络人区域标题显示）。
        /// </summary>
        public string SelectedCustomerName { get; set; } = "未选择客户";

        /// <summary>
        /// 客户主表列表。
        /// </summary>
        public List<CustomerListItemViewModel> Customers { get; set; } = [];

        /// <summary>
        /// 当前客户联络人列表。
        /// </summary>
        public List<CustomerContactListItemViewModel> Contacts { get; set; } = [];
    }

    /// <summary>
    /// 客户资料列表行模型。
    /// </summary>
    public class CustomerListItemViewModel
    {
        /// <summary>
        /// 客户ID（对应 CRM_Customer 主键）。
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 序号（当前列表显示顺序）。
        /// </summary>
        public int SequenceNo { get; set; }

        /// <summary>
        /// 客户编号（业务唯一编码）。
        /// </summary>
        public string CustomerCode { get; set; } = string.Empty;

        /// <summary>
        /// 客户名称（客户正式名称）。
        /// </summary>
        public string CustomerName { get; set; } = string.Empty;

        /// <summary>
        /// 状态（合作中、暂停、终止等）。
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 档案按钮显示文字（对应通用文档管理入口）。
        /// </summary>
        public string ArchiveLabel { get; set; } = "档案";

        /// <summary>
        /// 别名或简称（客户常用简称）。
        /// </summary>
        public string AliasName { get; set; } = string.Empty;

        /// <summary>
        /// 客户性质（企业、个人等）。
        /// </summary>
        public string CustomerNature { get; set; } = string.Empty;

        /// <summary>
        /// 企业类型（客户企业属性）。
        /// </summary>
        public string EnterpriseType { get; set; } = string.Empty;

        /// <summary>
        /// 地区（国家/地区、城市、县区组合文本）。
        /// </summary>
        public string Region { get; set; } = string.Empty;

        /// <summary>
        /// 联系人（默认联络人姓名）。
        /// </summary>
        public string ContactName { get; set; } = string.Empty;

        /// <summary>
        /// 电话（客户或默认联络人的主要电话）。
        /// </summary>
        public string Phone { get; set; } = string.Empty;

        /// <summary>
        /// 传真（客户传真）。
        /// </summary>
        public string Fax { get; set; } = string.Empty;

        /// <summary>
        /// 电邮（客户或默认联络人的邮箱）。
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// 备注（客户补充说明）。
        /// </summary>
        public string Remarks { get; set; } = string.Empty;

        /// <summary>
        /// 最后修改人（用于审计显示）。
        /// </summary>
        public string LastModifiedBy { get; set; } = string.Empty;

        /// <summary>
        /// 最后修改时间（用于审计显示）。
        /// </summary>
        public string LastModifiedAt { get; set; } = string.Empty;
    }

    /// <summary>
    /// 客户联络人列表行模型。
    /// </summary>
    public class CustomerContactListItemViewModel
    {
        /// <summary>
        /// 联络人ID（对应 CRM_CustomerContact 主键）。
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 序号（当前联络人显示顺序）。
        /// </summary>
        public int SequenceNo { get; set; }

        /// <summary>
        /// 姓名（联络人姓名）。
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 名片显示文字（列表中名片栏使用）。
        /// </summary>
        public string BusinessCardText { get; set; } = "查看名片";

        /// <summary>
        /// 称呼（先生、小姐、女士等）。
        /// </summary>
        public string Salutation { get; set; } = string.Empty;

        /// <summary>
        /// 状态（在职、离职等）。
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 类型（账单联系人、送货联系人等）。
        /// </summary>
        public string ContactType { get; set; } = string.Empty;

        /// <summary>
        /// 职位（联络人岗位）。
        /// </summary>
        public string Position { get; set; } = string.Empty;

        /// <summary>
        /// 手机（联络人移动电话）。
        /// </summary>
        public string Mobile { get; set; } = string.Empty;

        /// <summary>
        /// 电话（联络人固定电话）。
        /// </summary>
        public string Phone { get; set; } = string.Empty;

        /// <summary>
        /// 传真（联络人传真）。
        /// </summary>
        public string Fax { get; set; } = string.Empty;

        /// <summary>
        /// 电邮（联络人邮箱）。
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// 备注（联络人补充说明）。
        /// </summary>
        public string Remarks { get; set; } = string.Empty;
    }
}
