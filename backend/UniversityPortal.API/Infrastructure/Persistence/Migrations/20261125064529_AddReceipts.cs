using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace UniversityPortal.API.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReceipts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReceiptIssues",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ReceiptNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    StudentNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CopyNumber = table.Column<int>(type: "integer", nullable: false),
                    IssuedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IssuedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceiptIssues", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRequests_ReceiptNumber",
                table: "PaymentRequests",
                column: "ReceiptNumber");

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptIssues_ReceiptNumber_StudentNumber_CopyNumber",
                table: "ReceiptIssues",
                columns: new[] { "ReceiptNumber", "StudentNumber", "CopyNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReceiptIssues");

            migrationBuilder.DropIndex(
                name: "IX_PaymentRequests_ReceiptNumber",
                table: "PaymentRequests");
        }
    }
}
