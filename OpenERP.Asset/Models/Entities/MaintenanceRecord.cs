/*
 * File: OpenERP.Asset/Models/Entities/MaintenanceRecord.cs
 * Description: Domain entity definition for MaintenanceRecord in the OpenERP.Asset module.
 */

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenERP.Asset.Models.Entities
{
    public class MaintenanceRecord : BaseEntity
    {
        [Required]
        [StringLength(150)]
        public string Description { get; set; } = string.Empty;

        public DateTime MaintenanceDate { get; set; }

        [Range(0.01, 1000000000.00)]
        public decimal Cost { get; set; }

        public int AssetId { get; set; }

        [ForeignKey("AssetId")]
        public virtual Asset? Asset { get; set; }
    }
}
