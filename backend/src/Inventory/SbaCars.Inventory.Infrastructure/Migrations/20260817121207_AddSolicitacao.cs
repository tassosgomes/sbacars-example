using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SbaCars.Inventory.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddSolicitacao : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "solicitacao",
            schema: "inventory",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                oferta_id = table.Column<Guid>(type: "uuid", nullable: false),
                tipo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                novo_preco_centavos = table.Column<long>(type: "bigint", nullable: true),
                justificativa = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                aberta_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                aberta_por_usuario_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                aberta_por_nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                aberta_por_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_solicitacao", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_solicitacao_status_aberta_em",
            schema: "inventory",
            table: "solicitacao",
            columns: new[] { "status", "aberta_em" });

        migrationBuilder.CreateIndex(
            name: "ux_solicitacao_oferta_tipo_pendente",
            schema: "inventory",
            table: "solicitacao",
            columns: new[] { "oferta_id", "tipo" },
            unique: true,
            filter: "status = 'Pendente'");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "solicitacao",
            schema: "inventory");
    }
}
