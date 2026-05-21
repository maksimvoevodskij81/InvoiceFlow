using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvoiceFlow.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAcceptedInvoiceFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AcceptedCurrency",
                table: "UploadedInvoices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "AcceptedInvoiceDate",
                table: "UploadedInvoices",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AcceptedInvoiceNumber",
                table: "UploadedInvoices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AcceptedSupplierName",
                table: "UploadedInvoices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AcceptedTotalAmount",
                table: "UploadedInvoices",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcceptedCurrency",
                table: "UploadedInvoices");

            migrationBuilder.DropColumn(
                name: "AcceptedInvoiceDate",
                table: "UploadedInvoices");

            migrationBuilder.DropColumn(
                name: "AcceptedInvoiceNumber",
                table: "UploadedInvoices");

            migrationBuilder.DropColumn(
                name: "AcceptedSupplierName",
                table: "UploadedInvoices");

            migrationBuilder.DropColumn(
                name: "AcceptedTotalAmount",
                table: "UploadedInvoices");
        }
    }
}
