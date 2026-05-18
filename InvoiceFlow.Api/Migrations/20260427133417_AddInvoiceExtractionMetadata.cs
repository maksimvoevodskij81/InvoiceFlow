using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvoiceFlow.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceExtractionMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExtractionCompletedAtUtc",
                table: "UploadedInvoices",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExtractionError",
                table: "UploadedInvoices",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExtractionModel",
                table: "UploadedInvoices",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExtractionWarnings",
                table: "UploadedInvoices",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RawExtractionJson",
                table: "UploadedInvoices",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExtractionCompletedAtUtc",
                table: "UploadedInvoices");

            migrationBuilder.DropColumn(
                name: "ExtractionError",
                table: "UploadedInvoices");

            migrationBuilder.DropColumn(
                name: "ExtractionModel",
                table: "UploadedInvoices");

            migrationBuilder.DropColumn(
                name: "ExtractionWarnings",
                table: "UploadedInvoices");

            migrationBuilder.DropColumn(
                name: "RawExtractionJson",
                table: "UploadedInvoices");
        }
    }
}
