/*
 * File: OpenERP.Asset/Data/ApplicationDbContext.cs
 * Description: Entity Framework Core DbContext for the OpenERP.Asset module.
 */

using Microsoft.EntityFrameworkCore;
using OpenERP.Asset.Models.Entities;

namespace OpenERP.Asset.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Models.Entities.Asset> Assets { get; set; }
        public DbSet<MaintenanceRecord> MaintenanceRecords { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Models.Entities.Asset>().ToTable("AS_Asset");
            modelBuilder.Entity<MaintenanceRecord>().ToTable("AS_MaintenanceRecord");

            // 软删除查询过滤器（列表/详情查询自动排除已删除数据）。
            modelBuilder.Entity<Models.Entities.Asset>().HasQueryFilter(asset => !asset.IsDeleted);
            modelBuilder.Entity<MaintenanceRecord>().HasQueryFilter(record => !record.IsDeleted);
        }
    }
}
