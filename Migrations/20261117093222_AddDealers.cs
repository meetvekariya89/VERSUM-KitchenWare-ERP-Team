
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VERSUM.KitchenWareERP.Migrations
{
    public partial class AddDealers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Dealers",
                columns: table => new
                {
                    Id = table.Column<int>(
                        type: "int",
                        nullable: false)
                        .Annotation(
                            "SqlServer:Identity", "1, 1"),

                    DealerCode = table.Column<string>(
                        type: "nvarchar(30)",
                        maxLength: 30,
                        nullable: false),

                    Name = table.Column<string>(
                        type: "nvarchar(150)",
                        maxLength: 150,
                        nullable: false),

                    ContactPerson = table.Column<string>(
                        type: "nvarchar(150)",
                        maxLength: 150,
                        nullable: true),

                    Phone = table.Column<string>(
                        type: "nvarchar(20)",
                        maxLength: 20,
                        nullable: false),

                    Email = table.Column<string>(
                        type: "nvarchar(150)",
                        maxLength: 150,
                        nullable: true),

                    Address = table.Column<string>(
                        type: "nvarchar(500)",
                        maxLength: 500,
                        nullable: true),

                    City = table.Column<string>(
                        type: "nvarchar(100)",
                        maxLength: 100,
                        nullable: false),

                    State = table.Column<string>(
                        type: "nvarchar(100)",
                        maxLength: 100,
                        nullable: true),

                    Pincode = table.Column<string>(
                        type: "nvarchar(15)",
                        maxLength: 15,
                        nullable: true),

                    GSTNumber = table.Column<string>(
                        type: "nvarchar(15)",
                        maxLength: 15,
                        nullable: true),

                    IsActive = table.Column<bool>(
                        type: "bit",
                        nullable: false),

                    CreatedAt = table.Column<DateTime>(
                        type: "datetime2",
                        nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dealers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Dealers_DealerCode",
                table: "Dealers",
                column: "DealerCode",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Dealers");
        }
    }
}
