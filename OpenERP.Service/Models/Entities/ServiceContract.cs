/*
 * File: OpenERP.Service/Models/Entities/ServiceContract.cs
 * Description: Domain entity definition for ServiceContract in the OpenERP.Service module.
 */

using System.ComponentModel.DataAnnotations;

namespace OpenERP.Service.Models.Entities
{
    public class ServiceContract : BaseEntity
    {
        [Required]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [StringLength(100)]
        public string? CustomerName { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        [StringLength(30)]
        public string Status { get; set; } = "Active"; // Active, Expired, Terminated
    }
}
