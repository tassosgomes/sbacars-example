using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SbaCars.Inventory.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddOutboxCreatedAtForRetention : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE inventory.outbox
              ADD COLUMN "created_at" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT now();

            CREATE INDEX ix_outbox_created_at_sent
              ON inventory.outbox ("created_at")
              WHERE "Sent" = TRUE;

            CREATE INDEX ix_inbox_message_processed_at
              ON inventory.inbox_message (processed_at);
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP INDEX inventory.ix_inbox_message_processed_at;
            DROP INDEX inventory.ix_outbox_created_at_sent;
            ALTER TABLE inventory.outbox DROP COLUMN "created_at";
            """);
    }
}
