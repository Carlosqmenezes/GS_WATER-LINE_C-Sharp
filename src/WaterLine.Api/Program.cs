using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using WaterLine.Api.Configuracao;
using WaterLine.Api.Dados;
using WaterLine.Api.Dados.Seed;
using WaterLine.Api.Erros;

// Ponto de entrada da API WaterLine.
var builder = WebApplication.CreateBuilder(args);

// ---------- Banco de dados ----------
var connectionString = builder.Configuration.GetConnectionString("WaterLine");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "A connection string 'WaterLine' não foi configurada. Veja appsettings.Development.json ou o README.");
}

builder.Services.AddDbContext<WaterLineDbContext>(options => options.UseNpgsql(connectionString));

// ---------- Regras de negócio e serviços de aplicação ----------
builder.Services.AddRegrasDeNegocio(builder.Configuration);
builder.Services.AddServicosDeAplicacao();

// ---------- Web API ----------
builder.Services
    .AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<TratamentoErrosHandler>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "WaterLine API",
        Version = "v1",
        Description = "API principal do WaterLine: recebe leituras das bocas de lobo, diagnostica, " +
                      "abre chamados, despacha o agente certo mais próximo e gera alertas."
    });

    var arquivoXml = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
    options.IncludeXmlComments(arquivoXml);
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Configuration.GetValue("Swagger:Habilitado", true))
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "WaterLine API v1");
        options.DocumentTitle = "WaterLine API";
    });
}

if (app.Configuration.GetValue("Seed:Habilitado", false))
{
    await using var escopo = app.Services.CreateAsyncScope();
    var db = escopo.ServiceProvider.GetRequiredService<WaterLineDbContext>();
    var logger = escopo.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Seed");
    await SeedDados.ExecutarAsync(db, logger);
}

app.MapControllers();

await app.RunAsync();
