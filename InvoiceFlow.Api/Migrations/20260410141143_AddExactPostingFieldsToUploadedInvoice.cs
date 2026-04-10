using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvoiceFlow.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddExactPostingFieldsToUploadedInvoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExactDocumentId",
                table: "UploadedInvoices",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExactPostingError",
                table: "UploadedInvoices",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExactPostingStatus",
                table: "UploadedInvoices",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PostedToExactAtUtc",
                table: "UploadedInvoices",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExactDocumentId",
                table: "UploadedInvoices");

            migrationBuilder.DropColumn(
                name: "ExactPostingError",
                table: "UploadedInvoices");

            migrationBuilder.DropColumn(
                name: "ExactPostingStatus",
                table: "UploadedInvoices");

            migrationBuilder.DropColumn(
                name: "PostedToExactAtUtc",
                table: "UploadedInvoices");
        }
    }
}
