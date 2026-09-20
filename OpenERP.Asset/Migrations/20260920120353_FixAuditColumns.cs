/*
 * File: OpenERP.Asset/Migrations/20260920120353_FixAuditColumns.cs
 * Description: 为资产模块两张表补齐 BaseEntity 审计列（CreatedBy/UpdatedBy）。
 *              注:本迁移手工裁剪过 —— 生成器基于旧快照(无前缀表名)产出的 RenameTable/DropPK
 *              与实际数据库(AS_ 前缀表已存在)不符,已移除;仅保留数据库真正缺失的审计列。
 */
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenERP.Asset.Migrations
{
    /// <inheritdoc />
    public partial class FixAuditColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "AS_Asset",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "AS_Asset",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "AS_MaintenanceRecord",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "AS_MaintenanceRecord",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "AS_Asset");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "AS_Asset");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "AS_MaintenanceRecord");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "AS_MaintenanceRecord");
        }
    }
}
