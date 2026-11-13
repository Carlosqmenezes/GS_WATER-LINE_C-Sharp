using Microsoft.EntityFrameworkCore;
using WaterLine.Api.Dados;
using WaterLine.Api.Dados.Seed;

// Ponto de entrada da API WaterLine.
var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("WaterLine");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "A connection string 'WaterLine' não foi configurada. Veja appsettings.Development.json ou o README.");
}

builder.Services.AddDbContext<WaterLineDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddControllers();

var app = builder.Build();

if (builder.Configuration.GetValue("Seed:Habilitado", false))
{
    await using var escopo = app.Services.CreateAsyncScope();
    var db = escopo.ServiceProvider.GetRequiredService<WaterLineDbContext>();
    var logger = escopo.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Seed");
    await SeedDados.ExecutarAsync(db, logger);
}

app.MapControllers();

await app.RunAsync();
