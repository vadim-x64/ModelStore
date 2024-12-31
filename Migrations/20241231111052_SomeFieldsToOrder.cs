using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ModelStore.Migrations
{
    /// <inheritdoc />
    public partial class SomeFieldsToOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProductDescription",
                table: "OrderItems",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ProductName",
                table: "OrderItems",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<byte[]>(
                name: "ProductPhoto",
                table: "OrderItems",
                type: "longblob",
                nullable: false);

            migrationBuilder.AddColumn<float>(
                name: "ProductPrice",
                table: "OrderItems",
                type: "float",
                nullable: false,
                defaultValue: 0f);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProductDescription",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "ProductName",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "ProductPhoto",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "ProductPrice",
                table: "OrderItems");
        }
    }
}
