using Microsoft.EntityFrameworkCore;
using WaterLine.Domain.Entidades;

namespace WaterLine.Api.Dados;

/// <summary>
/// Contexto do Entity Framework Core. Os mapeamentos de cada entidade ficam em
/// classes <see cref="IEntityTypeConfiguration{TEntity}"/> na pasta Dados/Configuracoes.
/// </summary>
public class WaterLineDbContext : DbContext
{
    public WaterLineDbContext(DbContextOptions<WaterLineDbContext> options) : base(options)
    {
    }

    public DbSet<BocaDeLobo> BocasDeLobo => Set<BocaDeLobo>();

    public DbSet<Leitura> Leituras => Set<Leitura>();

    public DbSet<Agente> Agentes => Set<Agente>();

    public DbSet<Gari> Garis => Set<Gari>();

    public DbSet<EquipeGaleria> EquipesGaleria => Set<EquipeGaleria>();

    public DbSet<Chamado> Chamados => Set<Chamado>();

    public DbSet<PrevisaoChuva> PrevisoesChuva => Set<PrevisaoChuva>();

    public DbSet<Alerta> Alertas => Set<Alerta>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WaterLineDbContext).Assembly);
    }
}
