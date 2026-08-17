using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SbaCars.Inventory.Domain.Ofertas;
using SbaCars.Inventory.Domain.Solicitacoes;

namespace SbaCars.Inventory.Infrastructure.Solicitacoes;

public sealed class SolicitacaoConfiguration : IEntityTypeConfiguration<Solicitacao>
{
    public const string PendingTypeUniqueIndexName = "ux_solicitacao_oferta_tipo_pendente";

    public void Configure(EntityTypeBuilder<Solicitacao> builder)
    {
        builder.ToTable("solicitacao");
        builder.HasKey(solicitacao => solicitacao.Id);
        builder.Property(solicitacao => solicitacao.Id).HasColumnName("id");
        builder.Property(solicitacao => solicitacao.OfertaId)
            .HasColumnName("oferta_id")
            .IsRequired();
        builder.Property(solicitacao => solicitacao.Tipo)
            .HasConversion<string>()
            .HasMaxLength(30)
            .HasColumnName("tipo")
            .IsRequired();
        builder.Property(solicitacao => solicitacao.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasColumnName("status")
            .IsRequired();
        builder.Property(solicitacao => solicitacao.NovoPrecoCentavos)
            .HasColumnName("novo_preco_centavos");
        builder.Property(solicitacao => solicitacao.Justificativa)
            .HasMaxLength(1000)
            .HasColumnName("justificativa")
            .IsRequired();
        builder.Property(solicitacao => solicitacao.AbertaEm)
            .HasColumnName("aberta_em")
            .IsRequired();

        builder.OwnsOne(solicitacao => solicitacao.AbertaPor, autoria =>
        {
            autoria.Property(item => item.UsuarioId)
                .HasMaxLength(200)
                .HasColumnName("aberta_por_usuario_id")
                .IsRequired();
            autoria.Property(item => item.Nome)
                .HasMaxLength(200)
                .HasColumnName("aberta_por_nome")
                .IsRequired();
            autoria.Property(item => item.Em)
                .HasColumnName("aberta_por_em")
                .IsRequired();
        });

        builder.OwnsOne(solicitacao => solicitacao.Decisao, decisao =>
        {
            decisao.Property(item => item.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasColumnName("decisao_status");
            decisao.Property(item => item.DecididaEm)
                .HasColumnName("decidida_em");
            decisao.Property(item => item.Justificativa)
                .HasMaxLength(1000)
                .HasColumnName("decisao_justificativa");
            decisao.OwnsOne(item => item.DecididaPor, autoria =>
            {
                autoria.Property(value => value.UsuarioId)
                    .HasMaxLength(200)
                    .HasColumnName("decidida_por_usuario_id")
                    .IsRequired();
                autoria.Property(value => value.Nome)
                    .HasMaxLength(200)
                    .HasColumnName("decidida_por_nome")
                    .IsRequired();
                autoria.Property(value => value.Em)
                    .HasColumnName("decidida_por_em")
                    .IsRequired();
            });
        });

        builder.HasIndex(solicitacao => new { solicitacao.OfertaId, solicitacao.Tipo })
            .IsUnique()
            .HasDatabaseName(PendingTypeUniqueIndexName)
            .HasFilter("status = 'Pendente'");

        builder.HasIndex(solicitacao => new { solicitacao.Status, solicitacao.AbertaEm })
            .HasDatabaseName("ix_solicitacao_status_aberta_em");

        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .IsRowVersion()
            .ValueGeneratedOnAddOrUpdate();
    }
}
