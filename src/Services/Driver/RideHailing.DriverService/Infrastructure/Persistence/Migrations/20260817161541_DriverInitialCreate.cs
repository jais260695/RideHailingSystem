using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RideHailing.DriverService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DriverInitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "drivers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    LicenseNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Rating = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_drivers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "vehicles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DriverId = table.Column<Guid>(type: "uuid", nullable: false),
                    Make = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Color = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LicensePlate = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ManufacturingYear = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehicles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_vehicles_drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_drivers_Email",
                table: "drivers",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_drivers_LicenseNumber",
                table: "drivers",
                column: "LicenseNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_drivers_PhoneNumber",
                table: "drivers",
                column: "PhoneNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vehicles_DriverId",
                table: "vehicles",
                column: "DriverId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vehicles_LicensePlate",
                table: "vehicles",
                column: "LicensePlate",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "vehicles");

            migrationBuilder.DropTable(
                name: "drivers");
        }
    }
}
