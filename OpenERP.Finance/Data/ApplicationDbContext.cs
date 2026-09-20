/*
 * File: OpenERP.Finance/Data/ApplicationDbContext.cs
 * Description: Entity Framework Core DbContext for the OpenERP.Finance module.
 */

using Microsoft.EntityFrameworkCore;
using OpenERP.Finance.Models.Entities;

namespace OpenERP.Finance.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Account> Accounts { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
    }
}
