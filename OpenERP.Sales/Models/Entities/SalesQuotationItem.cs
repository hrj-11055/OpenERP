/*
 * File: OpenERP.Sales/Models/Entities/SalesQuotationItem.cs
 * Description: 销售报价货品明细（承载报价单中的货品行信息）。
 */

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenERP.Sales.Models.Entities
{
    /// <summary>
    /// 销售报价货品明细（承载报价单中的货品行信息）。
    /// </summary>
    public class SalesQuotationItem : BaseEntity
    {
        /// <summary>
        /// 货品编号（产品唯一编码）。
        /// </summary>
        [Required]
        [StringLength(30)]
        public string ProductCode { get; set; } = string.Empty;

        /// <summary>
        /// 货品名称（产品描述）。
        /// </summary>
        [Required]
        [StringLength(200)]
        public string ProductName { get; set; } = string.Empty;

        /// <summary>
        /// 数量（报价数量）。
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal Quantity { get; set; }

        /// <summary>
        /// 单位（计量单位，如台、个、箱）。
        /// </summary>
        [StringLength(20)]
        public string Unit { get; set; } = string.Empty;

        /// <summary>
        /// 单价（每单位含税售价）。
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// 折扣百分比（100 表示无折扣）。
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountPercent { get; set; } = 100m;

        /// <summary>
        /// 金额（数量 × 单价 × 折扣/100）。
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        /// <summary>
        /// 货币（如 RMB、USD）。
        /// </summary>
        [StringLength(10)]
        public string Currency { get; set; } = "RMB";

        /// <summary>
        /// 汇率（外币对本币的换算比率）。
        /// </summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal ExchangeRate { get; set; } = 1.0000m;

        /// <summary>
        /// 附注（货品行补充说明）。
        /// </summary>
        [StringLength(500)]
        public string? Notes { get; set; }

        /// <summary>
        /// 所属报价单ID（外键，对应 SA_SalesQuotation 主键）。
        /// </summary>
        public int SalesQuotationId { get; set; }

        /// <summary>
        /// 所属报价单导航属性。
        /// </summary>
        [ForeignKey("SalesQuotationId")]
        public virtual SalesQuotation? SalesQuotation { get; set; }
    }
}
