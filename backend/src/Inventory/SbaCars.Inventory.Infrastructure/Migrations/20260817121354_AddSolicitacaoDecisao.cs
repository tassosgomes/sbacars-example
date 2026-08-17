using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SbaCars.Inventory.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddSolicitacaoDecisao : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "decidida_em",
            schema: "inventory",
            table: "solicitacao",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "decidida_por_em",
            schema: "inventory",
            table: "solicitacao",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "decidida_por_nome",
            schema: "inventory",
            table: "solicitacao",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "decidida_por_usuario_id",
            schema: "inventory",
            table: "solicitacao",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "decisao_justificativa",
            schema: "inventory",
            table: "solicitacao",
            type: "character varying(1000)",
            maxLength: 1000,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "decisao_status",
            schema: "inventory",
            table: "solicitacao",
            type: "character varying(20)",
            maxLength: 20,
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "decidida_em",
            schema: "inventory",
            table: "solicitacao");

        migrationBuilder.DropColumn(
            name: "decidida_por_em",
            schema: "inventory",
            table: "solicitacao");

        migrationBuilder.DropColumn(
            name: "decidida_por_nome",
            schema: "inventory",
            table: "solicitacao");

        migrationBuilder.DropColumn(
            name: "decidida_por_usuario_id",
            schema: "inventory",
            table: "solicitacao");

        migrationBuilder.DropColumn(
            name: "decisao_justificativa",
            schema: "inventory",
            table: "solicitacao");

        migrationBuilder.DropColumn(
            name: "decisao_status",
            schema: "inventory",
            table: "solicitacao");
    }
}
