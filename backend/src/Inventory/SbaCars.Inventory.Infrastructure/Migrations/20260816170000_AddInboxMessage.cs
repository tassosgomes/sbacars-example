using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SbaCars.Inventory.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddInboxMessage : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TABLE inventory.inbox_message
            (
              message_id text NOT NULL,
              consumer text NOT NULL,
              processed_at timestamp with time zone NOT NULL,
              PRIMARY KEY (message_id, consumer)
            );
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE inventory.inbox_message;");
    }
}
