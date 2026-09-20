/*
 * File: OpenERP.Sales/Data/ApplicationDbContext.cs
 * Description: Entity Framework Core DbContext for the OpenERP.Sales module.
 */

using Microsoft.EntityFrameworkCore;
using OpenERP.Sales.Models.Entities;

namespace OpenERP.Sales.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Customer> Customers { get; set; }
        public DbSet<SalesOrder> SalesOrders { get; set; }
        public DbSet<SalesOrderItem> SalesOrderItems { get; set; }
        public DbSet<SalesQuotation> SalesQuotations { get; set; }
        public DbSet<SalesQuotationItem> SalesQuotationItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<SalesQuotation>().ToTable("SA_SalesQuotation");
            modelBuilder.Entity<SalesQuotationItem>().ToTable("SA_SalesQuotationItem");
        }
    }
}
