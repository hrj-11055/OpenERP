/*
 * File: OpenERP.CRM/Data/ApplicationDbContext.cs
 * Description: Entity Framework Core DbContext for the OpenERP.CRM module.
 */

using Microsoft.EntityFrameworkCore;
using OpenERP.CRM.Models.Entities;

namespace OpenERP.CRM.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Lead> Leads { get; set; }
        public DbSet<Opportunity> Opportunities { get; set; }

        /// <summary>
        /// 客户资料主表集合（对应 CRM_Customer）。
        /// </summary>
        public DbSet<Customer> Customers { get; set; }

        /// <summary>
        /// 客户联络人从表集合（对应 CRM_CustomerContact）。
        /// </summary>
        public DbSet<CustomerContact> CustomerContacts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Lead>().ToTable("CRM_Lead");
            modelBuilder.Entity<Opportunity>(entity =>
            {
                entity.ToTable("CRM_Opportunity");
                entity.Property(opportunity => opportunity.Amount).HasColumnType("decimal(18,2)");
            });

            modelBuilder.Entity<Customer>(entity =>
            {
                entity.ToTable("CRM_Customer");
                entity.HasIndex(customer => new { customer.CustomerCode, customer.IsDeleted }).IsUnique();
                entity.Property(customer => customer.CreditLimit).HasPrecision(18, 3);
                entity.Property(customer => customer.MinimumOrderAmount).HasPrecision(18, 3);
            });

            modelBuilder.Entity<CustomerContact>(entity =>
            {
                entity.ToTable("CRM_CustomerContact");
                entity.HasIndex(contact => new { contact.CustomerId, contact.SortOrder });
                entity.HasOne(contact => contact.Customer)
                    .WithMany(customer => customer.Contacts)
                    .HasForeignKey(contact => contact.CustomerId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // 软删除查询过滤器（列表/详情查询自动排除已删除数据）。
            modelBuilder.Entity<Lead>().HasQueryFilter(lead => !lead.IsDeleted);
            modelBuilder.Entity<Opportunity>().HasQueryFilter(opportunity => !opportunity.IsDeleted);
            modelBuilder.Entity<Customer>().HasQueryFilter(customer => !customer.IsDeleted);
            modelBuilder.Entity<CustomerContact>().HasQueryFilter(contact => !contact.IsDeleted);
        }
    }
}
