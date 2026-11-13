using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WaterLine.Domain.Entidades;

namespace WaterLine.Api.Dados.Configuracoes;

public class AlertaConfiguration : IEntityTypeConfiguration<Alerta>
{
    public void Configure(EntityTypeBuilder<Alerta> builder)
    {
        builder.ToTable("Alertas");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Nivel).HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.Mensagem).HasMaxLength(Alerta.TamanhoMaximoMensagem).IsRequired();

        builder.HasIndex(a => new { a.BocaDeLoboId, a.CriadoEm });
    }
}
