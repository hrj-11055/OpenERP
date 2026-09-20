/*
 * File: OpenERP.Office/Models/Entities/Task.cs
 * Description: Domain entity definition for Task in the OpenERP.Office module.
 */

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenERP.Office.Models.Entities
{
    public class Task : BaseEntity
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        public DateTime DueDate { get; set; }

        [StringLength(30)]
        public string Status { get; set; } = "Pending"; // Pending, In Progress, Completed

        [StringLength(50)]
        public string? AssignedTo { get; set; }
    }
}
