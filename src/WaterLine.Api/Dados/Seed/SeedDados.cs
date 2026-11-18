using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WaterLine.Domain.Entidades;

namespace WaterLine.Api.Dados.Seed;

/// <summary>
/// Popula o banco com dados FICTÍCIOS para demonstração: bocas de lobo e agentes.
/// Nenhum nome, matrícula ou endereço corresponde a pessoas ou lugares reais.
/// </summary>
/// <remarks>
/// O seed roda na inicialização em vez de usar HasData porque, no PostgreSQL, HasData grava
/// Ids explícitos sem avançar a sequência da coluna identity, o que faria o primeiro POST falhar.
/// Aqui os Ids são gerados pelo banco e as entidades passam pelas validações do domínio.
/// </remarks>
public static class SeedDados
{
    public static async Task ExecutarAsync(
        WaterLineDbContext db,
        ILogger logger,
        IPasswordHasher<Agente> hasher,
        string? senhaDemonstracao,
        CancellationToken cancellationToken = default)
    {
        var migracoesPendentes = await db.Database.GetPendingMigrationsAsync(cancellationToken);
        if (migracoesPendentes.Any())
        {
            logger.LogWarning(
                "Seed ignorado: existem {Quantidade} migration(s) pendente(s). Rode 'dotnet tool run dotnet-ef database update' (veja o README).",
                migracoesPendentes.Count());
            return;
        }

        if (await db.BocasDeLobo.AnyAsync(cancellationToken) || await db.Agentes.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Seed ignorado: o banco já possui dados.");
        }
        else
        {
            await CriarDadosIniciaisAsync(db, logger, cancellationToken);
        }

        await DefinirSenhasDeDemonstracaoAsync(db, logger, hasher, senhaDemonstracao, cancellationToken);
    }

    private static async Task CriarDadosIniciaisAsync(WaterLineDbContext db, ILogger logger, CancellationToken cancellationToken)
    {

        // Grava um registro por vez: o EF Core não garante a ordem dos inserts dentro de um mesmo
        // SaveChanges, e o README depende dos Ids (bocas 1 a 5; garis 1 a 3; equipes de galeria 4 e 5).
        await using var transacao = await db.Database.BeginTransactionAsync(cancellationToken);

        foreach (var boca in CriarBocasDeLobo())
        {
            db.BocasDeLobo.Add(boca);
            await db.SaveChangesAsync(cancellationToken);
        }

        foreach (var agente in CriarAgentes())
        {
            db.Agentes.Add(agente);
            await db.SaveChangesAsync(cancellationToken);
        }

        await transacao.CommitAsync(cancellationToken);

        logger.LogInformation("Seed concluído: bocas de lobo e agentes fictícios criados.");
    }

    /// <summary>
    /// Dá a senha de demonstração (Seed:SenhaDemonstracao) aos agentes que ainda não têm senha.
    /// Também cobre bancos criados antes de existir o login. Só o hash é gravado.
    /// </summary>
    private static async Task DefinirSenhasDeDemonstracaoAsync(
        WaterLineDbContext db,
        ILogger logger,
        IPasswordHasher<Agente> hasher,
        string? senhaDemonstracao,
        CancellationToken cancellationToken)
    {
        var semSenha = await db.Agentes.Where(a => a.SenhaHash == string.Empty).ToListAsync(cancellationToken);
        if (semSenha.Count == 0)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(senhaDemonstracao))
        {
            logger.LogWarning(
                "{Quantidade} agente(s) sem senha: configure Seed:SenhaDemonstracao para que consigam entrar no app.",
                semSenha.Count);
            return;
        }

        foreach (var agente in semSenha)
        {
            agente.DefinirSenhaHash(hasher.HashPassword(agente, senhaDemonstracao));
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Senha de demonstração definida para {Quantidade} agente(s).", semSenha.Count);
    }

    /// <summary>
    /// Cinco bocas de lobo próximas entre si (as quatro primeiras a menos de 300 m umas das outras)
    /// e uma mais distante, para demonstrar o despacho por raio.
    /// </summary>
    private static IEnumerable<BocaDeLobo> CriarBocasDeLobo() => new[]
    {
        new BocaDeLobo("BL-0001", "Rua Fictícia das Águas, 100 - Bairro Exemplo", -23.55000, -46.64000, 80, 50),
        new BocaDeLobo("BL-0002", "Rua Fictícia das Águas, 350 - Bairro Exemplo", -23.55120, -46.64090, 80, 50),
        new BocaDeLobo("BL-0003", "Travessa Imaginária, 20 - Bairro Exemplo", -23.55180, -46.63900, 90, 60),
        new BocaDeLobo("BL-0004", "Avenida Modelo, 1200 - Bairro Exemplo", -23.54900, -46.63850, 100, 60),
        new BocaDeLobo("BL-0005", "Rua Demonstração, 45 - Vila Teste", -23.56000, -46.64800, 80, 50)
    };

    /// <summary>Agentes começam fora de serviço e, portanto, sem localização.</summary>
    private static IEnumerable<Agente> CriarAgentes() => new Agente[]
    {
        new Gari("Gari Fictício Alfa", "GAR-0001"),
        new Gari("Gari Fictício Beta", "GAR-0002"),
        new Gari("Gari Fictício Gama", "GAR-0003"),
        new EquipeGaleria("Equipe de Galeria Fictícia Norte", "GAL-0001"),
        new EquipeGaleria("Equipe de Galeria Fictícia Sul", "GAL-0002")
    };
}
