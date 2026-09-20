/*
 * File: OpenERP.Production/Data/ApplicationDbContext.cs
 * Description: Entity Framework Core DbContext for the OpenERP.Production module.
 */

using Microsoft.EntityFrameworkCore;
using OpenERP.Production.Models.Entities;

namespace OpenERP.Production.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<WorkCenter> WorkCenters { get; set; }
        public DbSet<ProductionOrder> ProductionOrders { get; set; }
        public DbSet<ProductionOrderItem> ProductionOrderItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<WorkCenter>().ToTable("PRD_WorkCenter");
            modelBuilder.Entity<ProductionOrder>().ToTable("PRD_ProductionOrder");
            modelBuilder.Entity<ProductionOrderItem>().ToTable("PRD_ProductionOrderItem");

            // 软删除查询过滤器（列表/详情查询自动排除已删除数据）。
            modelBuilder.Entity<WorkCenter>().HasQueryFilter(workCenter => !workCenter.IsDeleted);
            modelBuilder.Entity<ProductionOrder>().HasQueryFilter(order => !order.IsDeleted);
            modelBuilder.Entity<ProductionOrderItem>().HasQueryFilter(item => !item.IsDeleted);
        }
    }
}
