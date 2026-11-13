using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WaterLine.Domain.Entidades;

namespace WaterLine.Api.Dados.Configuracoes;

public class LeituraConfiguration : IEntityTypeConfiguration<Leitura>
{
    public void Configure(EntityTypeBuilder<Leitura> builder)
    {
        builder.ToTable("Leituras");
        builder.HasKey(l => l.Id);

        // Índice para buscar rapidamente a leitura anterior e o histórico de cada boca de lobo.
        builder.HasIndex(l => new { l.BocaDeLoboId, l.DataHora });
    }
}
