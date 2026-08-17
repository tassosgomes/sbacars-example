using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SbaCars.Purchase.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddOutboxCreatedAtForRetention : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE purchase.outbox
              ADD COLUMN "created_at" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT now();

            CREATE INDEX ix_outbox_created_at_sent
              ON purchase.outbox ("created_at")
              WHERE "Sent" = TRUE;

            CREATE INDEX ix_inbox_message_processed_at
              ON purchase.inbox_message (processed_at);
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP INDEX purchase.ix_inbox_message_processed_at;
            DROP INDEX purchase.ix_outbox_created_at_sent;
            ALTER TABLE purchase.outbox DROP COLUMN "created_at";
            """);
    }
}
