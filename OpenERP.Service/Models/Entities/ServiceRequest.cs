/*
 * File: OpenERP.Service/Models/Entities/ServiceRequest.cs
 * Description: Domain entity definition for ServiceRequest in the OpenERP.Service module.
 */

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenERP.Service.Models.Entities
{
    public class ServiceRequest : BaseEntity
    {
        [Required]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(30)]
        public string Status { get; set; } = "Open"; // Open, In Progress, Closed

        public int? ServiceContractId { get; set; }

        [ForeignKey("ServiceContractId")]
        public virtual ServiceContract? ServiceContract { get; set; }
    }
}
