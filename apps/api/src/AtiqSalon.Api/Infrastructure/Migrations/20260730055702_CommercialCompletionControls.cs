using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtiqSalon.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CommercialCompletionControls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VarianceApprovalNote",
                table: "TillSessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "VarianceApprovedAtUtc",
                table: "TillSessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VarianceApprovedByUserId",
                table: "TillSessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AppliedAtUtc",
                table: "DiscountApprovalRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AppliedByUserId",
                table: "DiscountApprovalRequests",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VarianceApprovalNote",
                table: "TillSessions");

            migrationBuilder.DropColumn(
                name: "VarianceApprovedAtUtc",
                table: "TillSessions");

            migrationBuilder.DropColumn(
                name: "VarianceApprovedByUserId",
                table: "TillSessions");

            migrationBuilder.DropColumn(
                name: "AppliedAtUtc",
                table: "DiscountApprovalRequests");

            migrationBuilder.DropColumn(
                name: "AppliedByUserId",
                table: "DiscountApprovalRequests");
        }
    }
}
