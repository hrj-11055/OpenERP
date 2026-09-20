using System.ComponentModel.DataAnnotations;

namespace OpenERP.CRM.Models.Entities
{
    /// <summary>
    /// 客户联络人资料从表（记录客户下的联系人、联系方式与默认联系人标记）。
    /// </summary>
    public class CustomerContact : BaseEntity
    {
        /// <summary>
        /// 客户资料ID（对应 CRM_Customer 主表）。
        /// </summary>
        public int CustomerId { get; set; }

        /// <summary>
        /// 客户资料实体（对应所属客户主记录）。
        /// </summary>
        public Customer? Customer { get; set; }

        /// <summary>
        /// 姓名（联络人姓名）。
        /// </summary>
        [Required]
        [StringLength(80)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 类型（例如账单联系人、送货联系人、会计联系人）。
        /// </summary>
        [StringLength(50)]
        public string? ContactType { get; set; }

        /// <summary>
        /// 称呼（例如先生、小姐、女士）。
        /// </summary>
        [StringLength(30)]
        public string? Salutation { get; set; }

        /// <summary>
        /// 职位（联络人在客户单位中的岗位）。
        /// </summary>
        [StringLength(80)]
        public string? Position { get; set; }

        /// <summary>
        /// 手机（联络人移动电话）。
        /// </summary>
        [StringLength(80)]
        public string? Mobile { get; set; }

        /// <summary>
        /// 电话（联络人固定电话）。
        /// </summary>
        [StringLength(80)]
        public string? Phone { get; set; }

        /// <summary>
        /// 传真（联络人传真号码）。
        /// </summary>
        [StringLength(80)]
        public string? Fax { get; set; }

        /// <summary>
        /// 电邮（联络人邮箱地址）。
        /// </summary>
        [EmailAddress]
        [StringLength(150)]
        public string? Email { get; set; }

        /// <summary>
        /// 名片说明（名片资料显示文本，后续可接入通用文档管理）。
        /// </summary>
        [StringLength(120)]
        public string? BusinessCardNote { get; set; }

        /// <summary>
        /// 状态（例如在职、离职、停用）。
        /// </summary>
        [StringLength(30)]
        public string? Status { get; set; } = "在职";

        /// <summary>
        /// 备注（联络人补充说明）。
        /// </summary>
        [StringLength(500)]
        public string? Remarks { get; set; }

        /// <summary>
        /// 默认联络人（同一客户下用于列表显示的主联系人）。
        /// </summary>
        public bool IsDefault { get; set; }

        /// <summary>
        /// 排序号（控制联络人在客户明细中的显示顺序）。
        /// </summary>
        public int SortOrder { get; set; }
    }
}
