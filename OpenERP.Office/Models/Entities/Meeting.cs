/*
 * File: OpenERP.Office/Models/Entities/Meeting.cs
 * Description: Domain entity definition for Meeting in the OpenERP.Office module.
 */

using System.ComponentModel.DataAnnotations;

namespace OpenERP.Office.Models.Entities
{
    public class Meeting : BaseEntity
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        [StringLength(500)]
        public string? Location { get; set; }

        [StringLength(1000)]
        public string? Agenda { get; set; }
    }
}
