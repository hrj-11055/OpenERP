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
    }
}
