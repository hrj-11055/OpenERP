using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenERP.CRM.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCustomerType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomerType",
                table: "CRM_Customer");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomerType",
                table: "CRM_Customer",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }
    }
}
