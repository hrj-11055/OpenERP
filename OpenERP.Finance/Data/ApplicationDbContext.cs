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

        /// <summary>
        /// 账户档案（科目/账户主数据，对应表 FIN_Account）。
        /// </summary>
        public DbSet<Account> Accounts { get; set; }

        /// <summary>
        /// 财务交易流水（账户收支记录，对应表 FIN_Transaction）。
        /// </summary>
        public DbSet<Transaction> Transactions { get; set; }

        /// <summary>
        /// 模型配置（FIN_ 表名前缀、软删除查询过滤器与常用查询索引）。
        /// </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Account>(entity =>
            {
                entity.ToTable("FIN_Account");
                entity.HasQueryFilter(account => !account.IsDeleted);
                entity.HasIndex(account => account.Code).IsUnique().HasFilter("[IsDeleted] = 0");
                entity.HasIndex(account => account.Name);
            });

            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.ToTable("FIN_Transaction");
                entity.HasQueryFilter(transaction => !transaction.IsDeleted);
                entity.HasIndex(transaction => transaction.AccountId);
                entity.HasIndex(transaction => transaction.DocDate);
                entity.Property(transaction => transaction.Amount).HasPrecision(18, 4);
            });
        }
    }
}
