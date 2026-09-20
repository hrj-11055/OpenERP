/*
 * File: OpenERP.Logistics/Data/ApplicationDbContext.cs
 * Description: Entity Framework Core DbContext for the OpenERP.Logistics module.
 */

using Microsoft.EntityFrameworkCore;
using OpenERP.Logistics.Models.Entities;

namespace OpenERP.Logistics.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Carrier> Carriers { get; set; }
        public DbSet<Shipment> Shipments { get; set; }
        public DbSet<ShipmentItem> ShipmentItems { get; set; }

        /// <summary>
        /// 共享资料主表集合（产品、材料、辅料、资产资料、办公用品共用）。
        /// </summary>
        public DbSet<MaterialItem> MaterialItems { get; set; }

        /// <summary>
        /// 产品 BOM 明细集合（对应共享资料主表）。
        /// </summary>
        public DbSet<MaterialItemBomLine> MaterialItemBomLines { get; set; }

        /// <summary>
        /// 产品售价设置明细集合（对应共享资料主表）。
        /// </summary>
        public DbSet<MaterialItemPriceLine> MaterialItemPriceLines { get; set; }

        /// <summary>
        /// 车间仓库资料主表集合。
        /// </summary>
        public DbSet<WorkshopWarehouse> WorkshopWarehouses { get; set; }

        /// <summary>
        /// 仓库库位资料从表集合。
        /// </summary>
        public DbSet<WarehouseLocation> WarehouseLocations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<WorkshopWarehouse>(entity =>
            {
                entity.ToTable("LG_WorkshopWarehouse");
                entity.HasQueryFilter(warehouse => !warehouse.IsDeleted);
                entity.HasIndex(warehouse => warehouse.WarehouseCode)
                    .IsUnique()
                    .HasFilter("[IsDeleted] = 0");
            });

            modelBuilder.Entity<WarehouseLocation>(entity =>
            {
                entity.ToTable("LG_WarehouseLocation");
                entity.HasQueryFilter(location => !location.IsDeleted);
                entity.HasIndex(location => new { location.WorkshopWarehouseId, location.LocationCode })
                    .IsUnique()
                    .HasFilter("[IsDeleted] = 0");
                entity.HasOne(location => location.WorkshopWarehouse)
                    .WithMany(warehouse => warehouse.Locations)
                    .HasForeignKey(location => location.WorkshopWarehouseId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<MaterialItem>(entity =>
            {
                entity.ToTable("BD_ItemMaster");
                entity.HasQueryFilter(item => !item.IsDeleted);
                entity.HasIndex(item => new { item.ItemCategory, item.ItemCode })
                    .IsUnique()
                    .HasFilter("[IsDeleted] = 0");
                entity.Property(item => item.PurchaseUnitRate).HasPrecision(18, 4);
                entity.Property(item => item.InventoryUnitRate).HasPrecision(18, 4);
                entity.Property(item => item.SalesUnitRate).HasPrecision(18, 4);
                entity.Property(item => item.InventoryQuantity).HasPrecision(18, 4);
                entity.Property(item => item.InventoryCapacity).HasPrecision(18, 4);
                entity.Property(item => item.CurrentCost).HasPrecision(18, 4);
                entity.Property(item => item.SuggestedPrice).HasPrecision(18, 4);
                entity.Property(item => item.SinglePackageQuantity).HasPrecision(18, 4);
                entity.Property(item => item.InnerPackageQuantity).HasPrecision(18, 4);
                entity.Property(item => item.CartonPackageQuantity).HasPrecision(18, 4);
                entity.Property(item => item.OtherPackageQuantity).HasPrecision(18, 4);
                entity.Property(item => item.SingleCbm).HasPrecision(18, 4);
                entity.Property(item => item.InnerCbm).HasPrecision(18, 4);
                entity.Property(item => item.CartonCbm).HasPrecision(18, 4);
                entity.Property(item => item.OtherCbm).HasPrecision(18, 4);
                entity.Property(item => item.SingleNetWeight).HasPrecision(18, 4);
                entity.Property(item => item.InnerNetWeight).HasPrecision(18, 4);
                entity.Property(item => item.CartonNetWeight).HasPrecision(18, 4);
                entity.Property(item => item.OtherNetWeight).HasPrecision(18, 4);
                entity.Property(item => item.SingleGrossWeight).HasPrecision(18, 4);
                entity.Property(item => item.InnerGrossWeight).HasPrecision(18, 4);
                entity.Property(item => item.CartonGrossWeight).HasPrecision(18, 4);
                entity.Property(item => item.OtherGrossWeight).HasPrecision(18, 4);
            });

            modelBuilder.Entity<MaterialItemBomLine>(entity =>
            {
                entity.ToTable("BD_ItemBomLine");
                entity.HasQueryFilter(line => !line.IsDeleted);
                entity.HasIndex(line => new { line.MaterialItemId, line.SortOrder });
                entity.Property(line => line.Quantity).HasPrecision(18, 4);
                entity.Property(line => line.LossRatePercent).HasPrecision(18, 4);
                entity.Property(line => line.QuantityWithLoss).HasPrecision(18, 4);
                entity.Property(line => line.CostPrice).HasPrecision(18, 4);
                entity.Property(line => line.CostAmount).HasPrecision(18, 4);
                entity.HasOne(line => line.MaterialItem)
                    .WithMany(item => item.BomLines)
                    .HasForeignKey(line => line.MaterialItemId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<MaterialItemPriceLine>(entity =>
            {
                entity.ToTable("BD_ItemPriceLine");
                entity.HasQueryFilter(line => !line.IsDeleted);
                entity.HasIndex(line => new { line.MaterialItemId, line.SortOrder });
                entity.Property(line => line.Quantity).HasPrecision(18, 4);
                entity.Property(line => line.Price).HasPrecision(18, 4);
                entity.HasOne(line => line.MaterialItem)
                    .WithMany(item => item.PriceLines)
                    .HasForeignKey(line => line.MaterialItemId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

        }
    }
}
