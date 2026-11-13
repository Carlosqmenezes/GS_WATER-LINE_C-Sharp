using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WaterLine.Domain.Entidades;

namespace WaterLine.Api.Dados.Configuracoes;

public class ChamadoConfiguration : IEntityTypeConfiguration<Chamado>
{
    public void Configure(EntityTypeBuilder<Chamado> builder)
    {
        builder.ToTable("Chamados");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Tipo).HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.FotoAntes).HasMaxLength(Chamado.TamanhoMaximoCaminhoFoto);
        builder.Property(c => c.FotoDepois).HasMaxLength(Chamado.TamanhoMaximoCaminhoFoto);

        builder.Ignore(c => c.EstaAtivo);
        builder.Ignore(c => c.EhDeGari);

        // Consultas mais frequentes: chamados ativos de uma boca de lobo e chamados por status.
        builder.HasIndex(c => new { c.BocaDeLoboId, c.Status });
        builder.HasIndex(c => c.Status);
    }
}
