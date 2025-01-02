using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ModelStore.Migrations
{
    /// <inheritdoc />
    public partial class OrderChangedForMore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Orders",
                newName: "FirstName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FirstName",
                table: "Orders",
                newName: "Name");
        }
    }
}
