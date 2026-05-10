using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Saasy.Metering.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialMeteringSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "metering");

            migrationBuilder.CreateTable(
                name: "events",
                schema: "metering",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    integrator_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_external_ref = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    event_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    dimension_code = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    value = table.Column<decimal>(type: "numeric", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: true),
                    ingested_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_events", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_events_integrator_id_idempotency_key",
                schema: "metering",
                table: "events",
                columns: new[] { "integrator_id", "idempotency_key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "events",
                schema: "metering");
        }
    }
}
