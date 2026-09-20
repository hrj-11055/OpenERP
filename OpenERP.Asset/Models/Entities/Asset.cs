/*
 * File: OpenERP.Asset/Models/Entities/Asset.cs
 * Description: Domain entity definition for Asset in the OpenERP.Asset module.
 */

using System.ComponentModel.DataAnnotations;

namespace OpenERP.Asset.Models.Entities
{
    public class Asset : BaseEntity
    {
        // 资产名称
        [Required]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        // 资产标签
        [StringLength(100)]
        public string? AssetTag { get; set; }

        // 资产状态（如 In-Use 在用, In-Stock 在库, Retired 报废）
        [StringLength(50)]
        public string? Status { get; set; } // In-Use, In-Stock, Retired

        // 采购日期
        public DateTime PurchaseDate { get; set; }

        // 采购成本
        [Range(0.01, 1000000000.00)]
        public decimal PurchaseCost { get; set; }
    }
}
