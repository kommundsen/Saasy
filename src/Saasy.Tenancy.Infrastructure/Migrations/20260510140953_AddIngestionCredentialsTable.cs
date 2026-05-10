using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Saasy.Tenancy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIngestionCredentialsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ingestion_credentials",
                schema: "tenancy",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    encrypted_connection_string = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    integrator_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ingestion_credentials", x => x.id);
                    table.ForeignKey(
                        name: "FK_ingestion_credentials_integrators_integrator_id",
                        column: x => x.integrator_id,
                        principalSchema: "tenancy",
                        principalTable: "integrators",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ingestion_credentials_integrator_id",
                schema: "tenancy",
                table: "ingestion_credentials",
                column: "integrator_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ingestion_credentials",
                schema: "tenancy");
        }
    }
}
