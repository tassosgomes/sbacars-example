using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SbaCars.Catalog.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddOutboxCreatedAtForRetention : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE catalog.outbox
              ADD COLUMN "created_at" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT now();

            CREATE INDEX ix_outbox_created_at_sent
              ON catalog.outbox ("created_at")
              WHERE "Sent" = TRUE;

            CREATE INDEX ix_inbox_message_processed_at
              ON catalog.inbox_message (processed_at);
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP INDEX catalog.ix_inbox_message_processed_at;
            DROP INDEX catalog.ix_outbox_created_at_sent;
            ALTER TABLE catalog.outbox DROP COLUMN "created_at";
            """);
    }
}
