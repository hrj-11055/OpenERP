/*
 * File: OpenERP.Purchasing/Data/ApplicationDbContext.cs
 * Description: Entity Framework Core DbContext for the OpenERP.Purchasing module.
 */

using Microsoft.EntityFrameworkCore;
using OpenERP.Purchasing.Models.Entities;

namespace OpenERP.Purchasing.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
        public DbSet<PurchaseOrderItem> PurchaseOrderItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 软删除查询过滤器（列表/详情查询自动排除已删除数据）。
            modelBuilder.Entity<Supplier>().HasQueryFilter(supplier => !supplier.IsDeleted);
            modelBuilder.Entity<PurchaseOrder>().HasQueryFilter(order => !order.IsDeleted);
            modelBuilder.Entity<PurchaseOrderItem>().HasQueryFilter(item => !item.IsDeleted);
        }
    }
}
