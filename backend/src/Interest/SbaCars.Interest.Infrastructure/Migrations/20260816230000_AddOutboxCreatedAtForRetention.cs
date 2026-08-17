using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SbaCars.Interest.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddOutboxCreatedAtForRetention : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE interest.outbox
              ADD COLUMN "created_at" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT now();

            CREATE INDEX ix_outbox_created_at_sent
              ON interest.outbox ("created_at")
              WHERE "Sent" = TRUE;

            CREATE INDEX ix_inbox_message_processed_at
              ON interest.inbox_message (processed_at);
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP INDEX interest.ix_inbox_message_processed_at;
            DROP INDEX interest.ix_outbox_created_at_sent;
            ALTER TABLE interest.outbox DROP COLUMN "created_at";
            """);
    }
}
