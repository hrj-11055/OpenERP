/*
 * File: OpenERP.CRM/Models/Entities/Lead.cs
 * Description: Domain entity definition for Lead in the OpenERP.CRM module.
 */

using System.ComponentModel.DataAnnotations;

namespace OpenERP.CRM.Models.Entities
{
    public class Lead : BaseEntity
    {
        [Required]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Company { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        [Phone]
        public string? Phone { get; set; }

        [StringLength(30)]
        public string Status { get; set; } = "New"; // New, Contacted, Qualified, Lost
    }
}
