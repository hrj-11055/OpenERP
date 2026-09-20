/*
 * File: OpenERP.CRM/Models/Entities/Opportunity.cs
 * Description: Domain entity definition for Opportunity in the OpenERP.CRM module.
 */

using System.ComponentModel.DataAnnotations;

namespace OpenERP.CRM.Models.Entities
{
    public class Opportunity : BaseEntity
    {
        [Required]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [StringLength(100)]
        public string? AccountName { get; set; }

        [StringLength(30)]
        public string Stage { get; set; } = "Prospecting"; // Prospecting, Qualification, Proposal, Negotiation, Closed Won, Closed Lost

        [Range(0.01, 1000000000.00)]
        public decimal Amount { get; set; }

        public int? LeadId { get; set; }
    }
}
