using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SbaCars.Inventory.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddOferta : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "inventory");

        migrationBuilder.CreateTable(
            name: "oferta",
            schema: "inventory",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tipo_veiculo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                placa = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                chassi = table.Column<string>(type: "character varying(17)", maxLength: 17, nullable: true),
                marca = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                modelo = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                versao = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                ano_fabricacao = table.Column<int>(type: "integer", nullable: true),
                ano_modelo = table.Column<int>(type: "integer", nullable: true),
                quilometragem = table.Column<int>(type: "integer", nullable: true),
                cor = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                combustivel = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                cambio = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                cep = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                cidade = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                uf = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                situacao = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                motivo_suspensao = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                suspensa_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                fato_origem_tipo = table.Column<string>(type: "text", nullable: false),
                fato_origem_indisponivel = table.Column<bool>(type: "boolean", nullable: false),
                fato_origem_descricao = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                fato_origem_fonte = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                fato_origem_limitacao = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                fato_origem_atualizado_por_usuario_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                fato_origem_atualizado_por_nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                fato_origem_atualizado_por_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                fato_condicao_tipo = table.Column<string>(type: "text", nullable: false),
                fato_condicao_indisponivel = table.Column<bool>(type: "boolean", nullable: false),
                fato_condicao_descricao = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                fato_condicao_fonte = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                fato_condicao_limitacao = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                fato_condicao_atualizado_por_usuario_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                fato_condicao_atualizado_por_nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                fato_condicao_atualizado_por_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                fato_historico_tipo = table.Column<string>(type: "text", nullable: false),
                fato_historico_indisponivel = table.Column<bool>(type: "boolean", nullable: false),
                fato_historico_descricao = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                fato_historico_fonte = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                fato_historico_limitacao = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                fato_historico_atualizado_por_usuario_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                fato_historico_atualizado_por_nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                fato_historico_atualizado_por_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                preco_valor_centavos = table.Column<long>(type: "bigint", nullable: true),
                preco_moeda = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                preco_definido_por_usuario_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                preco_definido_por_nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                preco_definido_por_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                disponibilidade_estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                disponibilidade_desde = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                disponibilidade_alterada_por_usuario_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                disponibilidade_alterada_por_nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                disponibilidade_alterada_por_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                disponibilidade_estado_conhecido = table.Column<bool>(type: "boolean", nullable: false),
                criada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                atualizado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                criada_por_usuario_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                criada_por_nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                criada_por_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                atualizada_por_usuario_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                atualizada_por_nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                atualizada_por_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_oferta", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_oferta_situacao_atualizado_em",
            schema: "inventory",
            table: "oferta",
            columns: new[] { "situacao", "atualizado_em" });

        migrationBuilder.CreateIndex(
            name: "ux_oferta_placa_ativa",
            schema: "inventory",
            table: "oferta",
            column: "placa",
            unique: true,
            filter: "situacao <> 'Retirada'");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "oferta",
            schema: "inventory");
    }
}
