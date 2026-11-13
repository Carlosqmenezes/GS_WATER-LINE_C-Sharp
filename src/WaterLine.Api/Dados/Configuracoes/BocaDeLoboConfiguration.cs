using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WaterLine.Domain.Entidades;

namespace WaterLine.Api.Dados.Configuracoes;

public class BocaDeLoboConfiguration : IEntityTypeConfiguration<BocaDeLobo>
{
    public void Configure(EntityTypeBuilder<BocaDeLobo> builder)
    {
        builder.ToTable("BocasDeLobo");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Codigo).HasMaxLength(BocaDeLobo.TamanhoMaximoCodigo).IsRequired();
        builder.HasIndex(b => b.Codigo).IsUnique();

        builder.Property(b => b.Endereco).HasMaxLength(BocaDeLobo.TamanhoMaximoEndereco).IsRequired();
        builder.Property(b => b.Status).HasConversion<string>().HasMaxLength(30);

        // Relacionamentos 1:N. Restrict: o histórico nunca é apagado em cascata
        // (a exclusão de boca de lobo pela API é lógica, via Ativa = false).
        builder.HasMany(b => b.Leituras)
            .WithOne(l => l.BocaDeLobo)
            .HasForeignKey(l => l.BocaDeLoboId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(b => b.Chamados)
            .WithOne(c => c.BocaDeLobo)
            .HasForeignKey(c => c.BocaDeLoboId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(b => b.Alertas)
            .WithOne(a => a.BocaDeLobo)
            .HasForeignKey(a => a.BocaDeLoboId)
            .OnDelete(DeleteBehavior.Restrict);

        // As coleções são expostas como somente leitura; o EF usa os campos privados.
        builder.Navigation(b => b.Leituras).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(b => b.Chamados).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(b => b.Alertas).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
