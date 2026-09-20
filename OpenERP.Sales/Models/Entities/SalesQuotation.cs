/*
 * File: OpenERP.Sales/Models/Entities/SalesQuotation.cs
 * Description: 销售报价单实体（承载客户报价主表信息）。
 */

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenERP.Sales.Models.Entities
{
    /// <summary>
    /// 销售报价单（承载客户报价主表信息）。
    /// </summary>
    public class SalesQuotation : BaseEntity
    {
        /// <summary>
        /// 报价单号（业务唯一编码，如 SQ2401170001）。
        /// </summary>
        [Required]
        [StringLength(30)]
        public string QuotationNumber { get; set; } = string.Empty;

        /// <summary>
        /// 报价日期（报价单开立日期）。
        /// </summary>
        public DateTime QuotationDate { get; set; } = DateTime.Today;

        /// <summary>
        /// 客户编号（对应客户资料中的编号）。
        /// </summary>
        [StringLength(30)]
        public string? CustomerCode { get; set; }

        /// <summary>
        /// 客户名称（报价客户全称）。
        /// </summary>
        [Required]
        [StringLength(200)]
        public string CustomerName { get; set; } = string.Empty;

        /// <summary>
        /// 状态（正常、作废、失效）。
        /// </summary>
        [StringLength(20)]
        public string Status { get; set; } = "正常";

        /// <summary>
        /// 有效截止日期（报价有效期的最后一天）。
        /// </summary>
        public DateTime? ValidUntil { get; set; }

        /// <summary>
        /// 账单联系人（客户方对接人员姓名）。
        /// </summary>
        [StringLength(50)]
        public string? BillingContact { get; set; }

        /// <summary>
        /// 电话（联系人电话）。
        /// </summary>
        [StringLength(30)]
        public string? Phone { get; set; }

        /// <summary>
        /// 电邮（联系人邮箱）。
        /// </summary>
        [StringLength(100)]
        public string? Email { get; set; }

        /// <summary>
        /// 备注（报价单补充说明）。
        /// </summary>
        [StringLength(500)]
        public string? Remarks { get; set; }

        /// <summary>
        /// 报价单货品明细集合。
        /// </summary>
        public virtual ICollection<SalesQuotationItem> Items { get; set; } = new List<SalesQuotationItem>();
    }
}
