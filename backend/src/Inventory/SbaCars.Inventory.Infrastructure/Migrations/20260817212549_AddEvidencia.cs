using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SbaCars.Inventory.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddEvidencia : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "fato_condicao_evidencia_id",
            schema: "inventory",
            table: "oferta",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "fato_historico_evidencia_id",
            schema: "inventory",
            table: "oferta",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "fato_origem_evidencia_id",
            schema: "inventory",
            table: "oferta",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "evidencia",
            schema: "inventory",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                oferta_id = table.Column<Guid>(type: "uuid", nullable: false),
                object_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                nome_arquivo = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                tipo_conteudo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                tamanho_bytes = table.Column<long>(type: "bigint", nullable: false),
                checksum = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                enviada_por_usuario_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                enviada_por_nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                enviada_por_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                enviada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_evidencia", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_evidencia_oferta_id",
            schema: "inventory",
            table: "evidencia",
            column: "oferta_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "evidencia",
            schema: "inventory");

        migrationBuilder.DropColumn(
            name: "fato_condicao_evidencia_id",
            schema: "inventory",
            table: "oferta");

        migrationBuilder.DropColumn(
            name: "fato_historico_evidencia_id",
            schema: "inventory",
            table: "oferta");

        migrationBuilder.DropColumn(
            name: "fato_origem_evidencia_id",
            schema: "inventory",
            table: "oferta");
    }
}
