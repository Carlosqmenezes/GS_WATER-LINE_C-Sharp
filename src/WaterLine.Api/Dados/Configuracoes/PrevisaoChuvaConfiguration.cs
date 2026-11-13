using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WaterLine.Domain.Entidades;

namespace WaterLine.Api.Dados.Configuracoes;

public class PrevisaoChuvaConfiguration : IEntityTypeConfiguration<PrevisaoChuva>
{
    public void Configure(EntityTypeBuilder<PrevisaoChuva> builder)
    {
        builder.ToTable("PrevisoesChuva");
        builder.HasKey(p => p.Id);
        builder.HasIndex(p => p.GeradaEm);
    }
}
