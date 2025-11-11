using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarrantyRepairLedger.Migrations
{
    /// <inheritdoc />
    public partial class AddConsumerOptedForRepairFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ConsumerOptedForRepair",
                table: "Repairs",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConsumerOptedForRepair",
                table: "Repairs");
        }
    }
}
