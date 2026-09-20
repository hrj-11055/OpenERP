/*
 * File: OpenERP.Transport/Models/Entities/TransportRequest.cs
 * Description: Domain entity definition for TransportRequest in the OpenERP.Transport module.
 */

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenERP.Transport.Models.Entities
{
    public class TransportRequest : BaseEntity
    {
        [Required]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(30)]
        public string Status { get; set; } = "Pending"; // Pending, In-Transit, Delivered

        public int? VehicleId { get; set; }

        [ForeignKey("VehicleId")]
        public virtual Vehicle? Vehicle { get; set; }
    }
}
