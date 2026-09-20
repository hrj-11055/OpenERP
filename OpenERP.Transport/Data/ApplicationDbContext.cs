/*
 * File: OpenERP.Transport/Data/ApplicationDbContext.cs
 * Description: Entity Framework Core DbContext for the OpenERP.Transport module.
 */

using Microsoft.EntityFrameworkCore;
using OpenERP.Transport.Models.Entities;

namespace OpenERP.Transport.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<TransportRequest> TransportRequests { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Vehicle>().ToTable("TR_Vehicle");
            modelBuilder.Entity<TransportRequest>().ToTable("TR_TransportRequest");

            // 软删除查询过滤器（列表/详情查询自动排除已删除数据）。
            modelBuilder.Entity<Vehicle>().HasQueryFilter(vehicle => !vehicle.IsDeleted);
            modelBuilder.Entity<TransportRequest>().HasQueryFilter(request => !request.IsDeleted);
        }
    }
}
