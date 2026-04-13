using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvoiceFlow.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierAndBankAccountMappings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CanCreateSupplier",
                table: "UploadedInvoices",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "BankAccountMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Fingerprint = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ExactSupplierId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankAccountMappings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SupplierMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Fingerprint = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ExactSupplierId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierMappings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BankAccountMappings_Fingerprint",
                table: "BankAccountMappings",
                column: "Fingerprint",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierMappings_Fingerprint",
                table: "SupplierMappings",
                column: "Fingerprint",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BankAccountMappings");

            migrationBuilder.DropTable(
                name: "SupplierMappings");

            migrationBuilder.DropColumn(
                name: "CanCreateSupplier",
                table: "UploadedInvoices");
        }
    }
}
