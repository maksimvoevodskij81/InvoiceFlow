using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvoiceFlow.Api.Migrations
{
    /// <inheritdoc />
    public partial class StoreMatchReasonsAsJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasNewBankDetails",
                table: "UploadedInvoices",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MatchReasons",
                table: "UploadedInvoices",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SupplierAddressLine",
                table: "UploadedInvoices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupplierBankAccount",
                table: "UploadedInvoices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupplierBicCode",
                table: "UploadedInvoices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupplierCity",
                table: "UploadedInvoices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupplierCountry",
                table: "UploadedInvoices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupplierPostcode",
                table: "UploadedInvoices",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasNewBankDetails",
                table: "UploadedInvoices");

            migrationBuilder.DropColumn(
                name: "MatchReasons",
                table: "UploadedInvoices");

            migrationBuilder.DropColumn(
                name: "SupplierAddressLine",
                table: "UploadedInvoices");

            migrationBuilder.DropColumn(
                name: "SupplierBankAccount",
                table: "UploadedInvoices");

            migrationBuilder.DropColumn(
                name: "SupplierBicCode",
                table: "UploadedInvoices");

            migrationBuilder.DropColumn(
                name: "SupplierCity",
                table: "UploadedInvoices");

            migrationBuilder.DropColumn(
                name: "SupplierCountry",
                table: "UploadedInvoices");

            migrationBuilder.DropColumn(
                name: "SupplierPostcode",
                table: "UploadedInvoices");
        }
    }
}
