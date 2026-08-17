using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SbaCars.Inventory.Domain.Ofertas;

namespace SbaCars.Inventory.Infrastructure.Ofertas;

public sealed class EvidenciaConfiguration : IEntityTypeConfiguration<Evidencia>
{
    public void Configure(EntityTypeBuilder<Evidencia> builder)
    {
        builder.ToTable("evidencia");
        builder.HasKey(evidencia => evidencia.Id);
        builder.Property(evidencia => evidencia.Id).HasColumnName("id");
        builder.Property(evidencia => evidencia.OfertaId)
            .HasColumnName("oferta_id")
            .IsRequired();
        builder.Property(evidencia => evidencia.ObjectKey)
            .HasMaxLength(500)
            .HasColumnName("object_key")
            .IsRequired();
        builder.Property(evidencia => evidencia.NomeArquivo)
            .HasMaxLength(255)
            .HasColumnName("nome_arquivo")
            .IsRequired();
        builder.Property(evidencia => evidencia.TipoConteudo)
            .HasMaxLength(100)
            .HasColumnName("tipo_conteudo")
            .IsRequired();
        builder.Property(evidencia => evidencia.TamanhoBytes)
            .HasColumnName("tamanho_bytes")
            .IsRequired();
        builder.Property(evidencia => evidencia.Checksum)
            .HasMaxLength(128)
            .HasColumnName("checksum");
        builder.Property(evidencia => evidencia.EnviadaEm)
            .HasColumnName("enviada_em")
            .IsRequired();

        builder.OwnsOne(evidencia => evidencia.EnviadaPor, autoria =>
        {
            autoria.Property(item => item.UsuarioId)
                .HasMaxLength(200)
                .HasColumnName("enviada_por_usuario_id")
                .IsRequired();
            autoria.Property(item => item.Nome)
                .HasMaxLength(200)
                .HasColumnName("enviada_por_nome")
                .IsRequired();
            autoria.Property(item => item.Em)
                .HasColumnName("enviada_por_em")
                .IsRequired();
        });

        builder.HasIndex(evidencia => evidencia.OfertaId)
            .HasDatabaseName("ix_evidencia_oferta_id");
    }
}
