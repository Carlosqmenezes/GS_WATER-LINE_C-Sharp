using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WaterLine.Domain.Entidades;

namespace WaterLine.Api.Dados.Configuracoes;

/// <summary>
/// Mapeia a herança Agente → Gari / EquipeGaleria numa única tabela (TPH),
/// com a coluna discriminadora TipoAgente.
/// </summary>
public class AgenteConfiguration : IEntityTypeConfiguration<Agente>
{
    public const string ColunaDiscriminadora = "TipoAgente";

    public void Configure(EntityTypeBuilder<Agente> builder)
    {
        builder.ToTable("Agentes");
        builder.HasKey(a => a.Id);

        builder.HasDiscriminator<string>(ColunaDiscriminadora)
            .HasValue<Gari>(nameof(Gari))
            .HasValue<EquipeGaleria>(nameof(EquipeGaleria));

        builder.Property<string>(ColunaDiscriminadora).HasMaxLength(20);

        builder.Property(a => a.Nome).HasMaxLength(Agente.TamanhoMaximoNome).IsRequired();
        builder.Property(a => a.Matricula).HasMaxLength(Agente.TamanhoMaximoMatricula).IsRequired();
        builder.HasIndex(a => a.Matricula).IsUnique();
        builder.Property(a => a.SenhaHash).HasMaxLength(Agente.TamanhoMaximoSenhaHash).IsRequired().HasDefaultValue(string.Empty);

        // Propriedades calculadas (polimorfismo e regras), não são colunas.
        builder.Ignore(a => a.TiposDeChamadoAtendidos);
        builder.Ignore(a => a.Funcao);
        builder.Ignore(a => a.TemLocalizacao);
        builder.Ignore(a => a.TemSenha);

        builder.HasMany(a => a.Chamados)
            .WithOne(c => c.Agente)
            .HasForeignKey(c => c.AgenteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(a => a.Chamados).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
