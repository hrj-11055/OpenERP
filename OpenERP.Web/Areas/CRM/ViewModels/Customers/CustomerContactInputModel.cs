using System.ComponentModel.DataAnnotations;

namespace OpenERP.Web.Areas.CRM.ViewModels.Customers
{
    /// <summary>
    /// 客户联络人明细输入模型（用于客户详情页表格编辑）。
    /// </summary>
    public class CustomerContactInputModel
    {
        /// <summary>
        /// 联络人ID（对应 CRM_CustomerContact 主键；新增行为空）。
        /// </summary>
        public int? Id { get; set; }

        /// <summary>
        /// 是否删除（提交时用于软删除已有联络人）。
        /// </summary>
        public bool IsDeleted { get; set; }

        /// <summary>
        /// 姓名（联络人姓名）。
        /// </summary>
        [StringLength(80, ErrorMessage = "姓名不能超过 80 个字符。")]
        public string? Name { get; set; }

        /// <summary>
        /// 类型（账单联系人、送货联系人等）。
        /// </summary>
        public string? ContactType { get; set; }

        /// <summary>
        /// 称呼（先生、小姐、女士等）。
        /// </summary>
        public string? Salutation { get; set; }

        /// <summary>
        /// 职位（联络人在客户单位中的岗位）。
        /// </summary>
        public string? Position { get; set; }

        /// <summary>
        /// 手机（联络人移动电话）。
        /// </summary>
        public string? Mobile { get; set; }

        /// <summary>
        /// 电话（联络人固定电话）。
        /// </summary>
        public string? Phone { get; set; }

        /// <summary>
        /// 传真（联络人传真）。
        /// </summary>
        public string? Fax { get; set; }

        /// <summary>
        /// 电邮（联络人邮箱）。
        /// </summary>
        [EmailAddress(ErrorMessage = "请输入有效的电邮地址。")]
        public string? Email { get; set; }

        /// <summary>
        /// 名片说明（名片资料显示文本）。
        /// </summary>
        public string? BusinessCardNote { get; set; }

        /// <summary>
        /// 状态（在职、离职、停用）。
        /// </summary>
        public string? Status { get; set; } = "在职";

        /// <summary>
        /// 备注（联络人补充说明）。
        /// </summary>
        public string? Remarks { get; set; }

        /// <summary>
        /// 默认联络人（用于客户列表显示）。
        /// </summary>
        public bool IsDefault { get; set; }
    }
}
