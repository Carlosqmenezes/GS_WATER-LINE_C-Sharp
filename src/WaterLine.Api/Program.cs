using Microsoft.EntityFrameworkCore;
using WaterLine.Api.Dados;

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

app.MapControllers();

app.Run();
