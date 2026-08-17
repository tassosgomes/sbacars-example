using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SbaCars.Purchase.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddRebusSagaAndTimeouts : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TABLE purchase.sagas
            (
              "id" UUID NOT NULL,
              "revision" INTEGER NOT NULL,
              "data" BYTEA NOT NULL,
              PRIMARY KEY ("id")
            );

            CREATE TABLE purchase.saga_index
            (
              "saga_type" TEXT NOT NULL,
              "key" TEXT NOT NULL,
              "value" TEXT NOT NULL,
              "saga_id" UUID NOT NULL,
              PRIMARY KEY ("key", "value", "saga_type")
            );
            CREATE INDEX ON purchase.saga_index ("saga_id");

            CREATE TABLE purchase.timeouts
            (
              "id" BIGSERIAL NOT NULL,
              "due_time" TIMESTAMP WITH TIME ZONE NOT NULL,
              "headers" TEXT NULL,
              "body" BYTEA NULL,
              PRIMARY KEY ("id")
            );
            CREATE INDEX ON purchase.timeouts ("due_time");
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP TABLE purchase.timeouts;
            DROP TABLE purchase.saga_index;
            DROP TABLE purchase.sagas;
            """);
    }
}
