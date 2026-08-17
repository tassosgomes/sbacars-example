using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SbaCars.Inventory.Domain.Ofertas;

namespace SbaCars.Inventory.Infrastructure.Ofertas;

public sealed class OfertaConfiguration : IEntityTypeConfiguration<Oferta>
{
    public void Configure(EntityTypeBuilder<Oferta> builder)
    {
        builder.ToTable("oferta");
        builder.HasKey(oferta => oferta.Id);
        builder.Property(oferta => oferta.Id).HasColumnName("id");
        builder.Property(oferta => oferta.Situacao)
            .HasConversion<string>()
            .HasMaxLength(30)
            .HasColumnName("situacao")
            .IsRequired();
        builder.Property(oferta => oferta.MotivoSuspensao)
            .HasMaxLength(1000)
            .HasColumnName("motivo_suspensao");
        builder.Property(oferta => oferta.SuspensaEm)
            .HasColumnName("suspensa_em");
        builder.Property(oferta => oferta.CriadaEm).HasColumnName("criada_em").IsRequired();
        builder.Property(oferta => oferta.AtualizadoEm).HasColumnName("atualizado_em").IsRequired();

        ConfigureAutoria(builder, oferta => oferta.CriadaPor, "criada_por");
        ConfigureAutoria(builder, oferta => oferta.AtualizadaPor, "atualizada_por");

        builder.OwnsOne(oferta => oferta.Veiculo, vehicle =>
        {
            vehicle.Property(item => item.TipoVeiculo)
                .HasConversion<string>()
                .HasMaxLength(30)
                .HasColumnName("tipo_veiculo")
                .IsRequired();
            vehicle.Property(item => item.Placa).HasMaxLength(10).HasColumnName("placa");
            vehicle.Property(item => item.Chassi).HasMaxLength(17).HasColumnName("chassi");
            vehicle.Property(item => item.Marca).HasMaxLength(120).HasColumnName("marca");
            vehicle.Property(item => item.Modelo).HasMaxLength(120).HasColumnName("modelo");
            vehicle.Property(item => item.Versao).HasMaxLength(120).HasColumnName("versao");
            vehicle.Property(item => item.AnoFabricacao).HasColumnName("ano_fabricacao");
            vehicle.Property(item => item.AnoModelo).HasColumnName("ano_modelo");
            vehicle.Property(item => item.Quilometragem).HasColumnName("quilometragem");
            vehicle.Property(item => item.Cor).HasMaxLength(80).HasColumnName("cor");
            vehicle.Property(item => item.Combustivel).HasMaxLength(40).HasColumnName("combustivel");
            vehicle.Property(item => item.Cambio).HasMaxLength(40).HasColumnName("cambio");

            vehicle.OwnsOne(item => item.Localizacao, location =>
            {
                location.Property(value => value.Cep).HasMaxLength(8).HasColumnName("cep");
                location.Property(value => value.Cidade).HasMaxLength(120).HasColumnName("cidade");
                location.Property(value => value.Uf).HasMaxLength(2).HasColumnName("uf");
            });

            vehicle.HasIndex(item => item.Placa)
                .IsUnique()
                .HasDatabaseName("ux_oferta_placa_ativa")
                .HasFilter("situacao <> 'Retirada'");
        });

        builder.OwnsOne(oferta => oferta.Disponibilidade, availability =>
        {
            availability.Property(value => value.Estado)
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasColumnName("disponibilidade_estado")
                .IsRequired();
            availability.Property(value => value.Desde)
                .HasColumnName("disponibilidade_desde")
                .IsRequired();
            availability.Property(value => value.EstadoConhecido)
                .HasColumnName("disponibilidade_estado_conhecido")
                .IsRequired();
            availability.OwnsOne(value => value.AlteradaPor, autoria =>
            {
                autoria.Property(item => item.UsuarioId).HasMaxLength(200).HasColumnName("disponibilidade_alterada_por_usuario_id");
                autoria.Property(item => item.Nome).HasMaxLength(200).HasColumnName("disponibilidade_alterada_por_nome");
                autoria.Property(item => item.Em).HasColumnName("disponibilidade_alterada_por_em");
            });
        });

        builder.OwnsOne(oferta => oferta.Fatos, facts =>
        {
            ConfigureFact(facts, value => value.Origem, "origem");
            ConfigureFact(facts, value => value.Condicao, "condicao");
            ConfigureFact(facts, value => value.Historico, "historico");
        });

        builder.OwnsOne(oferta => oferta.PrecoOficial, price =>
        {
            price.Property(value => value.ValorCentavos).HasColumnName("preco_valor_centavos");
            price.Property(value => value.Moeda).HasMaxLength(3).HasColumnName("preco_moeda");
            price.OwnsOne(value => value.DefinidoPor, autoria =>
            {
                autoria.Property(item => item.UsuarioId).HasMaxLength(200).HasColumnName("preco_definido_por_usuario_id");
                autoria.Property(item => item.Nome).HasMaxLength(200).HasColumnName("preco_definido_por_nome");
                autoria.Property(item => item.Em).HasColumnName("preco_definido_por_em");
            });
        });

        builder.HasIndex(oferta => new { oferta.Situacao, oferta.AtualizadoEm })
            .HasDatabaseName("ix_oferta_situacao_atualizado_em");
        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .IsRowVersion()
            .ValueGeneratedOnAddOrUpdate();
    }

    private static void ConfigureAutoria(
        EntityTypeBuilder<Oferta> builder,
        System.Linq.Expressions.Expression<Func<Oferta, Autoria?>> property,
        string prefix)
    {
        builder.OwnsOne(property, autoria =>
        {
            autoria.Property(value => value.UsuarioId).HasMaxLength(200).HasColumnName($"{prefix}_usuario_id");
            autoria.Property(value => value.Nome).HasMaxLength(200).HasColumnName($"{prefix}_nome");
            autoria.Property(value => value.Em).HasColumnName($"{prefix}_em");
        });
    }

    private static void ConfigureFact(
        OwnedNavigationBuilder<Oferta, FatosConhecidos> owner,
        System.Linq.Expressions.Expression<Func<FatosConhecidos, BlocoFato?>> property,
        string prefix)
    {
        owner.OwnsOne(property, fact =>
        {
            fact.Property(value => value.Tipo).HasConversion<string>().HasColumnName($"fato_{prefix}_tipo");
            fact.Property(value => value.Indisponivel).HasColumnName($"fato_{prefix}_indisponivel");
            fact.Property(value => value.Descricao).HasMaxLength(2000).HasColumnName($"fato_{prefix}_descricao");
            fact.Property(value => value.Fonte).HasMaxLength(300).HasColumnName($"fato_{prefix}_fonte");
            fact.Property(value => value.LimitacaoDeclarada).HasMaxLength(1000).HasColumnName($"fato_{prefix}_limitacao");
            fact.Property(value => value.EvidenciaId).HasColumnName($"fato_{prefix}_evidencia_id");
            fact.OwnsOne(value => value.AtualizadoPor, autoria =>
            {
                autoria.Property(item => item.UsuarioId).HasMaxLength(200).HasColumnName($"fato_{prefix}_atualizado_por_usuario_id");
                autoria.Property(item => item.Nome).HasMaxLength(200).HasColumnName($"fato_{prefix}_atualizado_por_nome");
                autoria.Property(item => item.Em).HasColumnName($"fato_{prefix}_atualizado_por_em");
            });
        });
    }
}
