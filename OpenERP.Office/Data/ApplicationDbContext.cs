/*
 * File: OpenERP.Office/Data/ApplicationDbContext.cs
 * Description: Entity Framework Core DbContext for the OpenERP.Office module.
 */

using Microsoft.EntityFrameworkCore;
using OpenERP.Office.Models.Entities;
using OfficeTask = OpenERP.Office.Models.Entities.Task;

namespace OpenERP.Office.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Meeting> Meetings { get; set; }
        public DbSet<OfficeTask> Tasks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Meeting>().ToTable("OF_Meeting");
            modelBuilder.Entity<OfficeTask>().ToTable("OF_Task");
        }
    }
}
