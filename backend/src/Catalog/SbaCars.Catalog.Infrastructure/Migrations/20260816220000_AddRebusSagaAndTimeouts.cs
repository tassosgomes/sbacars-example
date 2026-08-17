using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SbaCars.Catalog.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddRebusSagaAndTimeouts : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TABLE catalog.sagas
            (
              "id" UUID NOT NULL,
              "revision" INTEGER NOT NULL,
              "data" BYTEA NOT NULL,
              PRIMARY KEY ("id")
            );

            CREATE TABLE catalog.saga_index
            (
              "saga_type" TEXT NOT NULL,
              "key" TEXT NOT NULL,
              "value" TEXT NOT NULL,
              "saga_id" UUID NOT NULL,
              PRIMARY KEY ("key", "value", "saga_type")
            );
            CREATE INDEX ON catalog.saga_index ("saga_id");

            CREATE TABLE catalog.timeouts
            (
              "id" BIGSERIAL NOT NULL,
              "due_time" TIMESTAMP WITH TIME ZONE NOT NULL,
              "headers" TEXT NULL,
              "body" BYTEA NULL,
              PRIMARY KEY ("id")
            );
            CREATE INDEX ON catalog.timeouts ("due_time");
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP TABLE catalog.timeouts;
            DROP TABLE catalog.saga_index;
            DROP TABLE catalog.sagas;
            """);
    }
}
