/*
 * File: OpenERP.Service/Data/ApplicationDbContext.cs
 * Description: Entity Framework Core DbContext for the OpenERP.Service module.
 */

using Microsoft.EntityFrameworkCore;
using OpenERP.Service.Models.Entities;

namespace OpenERP.Service.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<ServiceContract> ServiceContracts { get; set; }
        public DbSet<ServiceRequest> ServiceRequests { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<ServiceContract>().ToTable("SV_ServiceContract");
            modelBuilder.Entity<ServiceRequest>().ToTable("SV_ServiceRequest");
        }
    }
}
