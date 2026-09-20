/*
 * File: OpenERP.Logistics/Models/Entities/Shipment.cs
 * Description: Domain entity definition for Shipment in the OpenERP.Logistics module.
 */

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenERP.Logistics.Models.Entities
{
    public class Shipment : BaseEntity
    {
        [Required]
        [StringLength(30)]
        public string ShipmentNumber { get; set; } = string.Empty;

        public DateTime ShipmentDate { get; set; } = DateTime.Today;

        public int? CarrierId { get; set; }

        [ForeignKey("CarrierId")]
        public virtual Carrier? Carrier { get; set; }

        [StringLength(30)]
        public string Status { get; set; } = "Pending";

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalWeight { get; set; } = 0m;
    }
}
