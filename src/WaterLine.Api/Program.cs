using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using WaterLine.Api.Configuracao;
using WaterLine.Api.Dados;
using WaterLine.Api.Dados.Seed;
using WaterLine.Api.Erros;
using WaterLine.Domain.Entidades;

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
builder.Services.AddInfraestrutura(builder.Configuration);
builder.Services.AddAutenticacao(builder.Configuration);
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

    // Botão "Authorize" do Swagger: cole o token devolvido por POST /auth/login.
    var esquemaBearer = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Token JWT devolvido por POST /auth/login. Cole apenas o token, sem a palavra Bearer.",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = JwtBearerDefaults.AuthenticationScheme,
        BearerFormat = "JWT",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = JwtBearerDefaults.AuthenticationScheme }
    };
    options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, esquemaBearer);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement { [esquemaBearer] = Array.Empty<string>() });
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
    var hasher = escopo.ServiceProvider.GetRequiredService<IPasswordHasher<Agente>>();
    var senhaDemonstracao = app.Configuration["Seed:SenhaDemonstracao"];

    try
    {
        await SeedDados.ExecutarAsync(db, logger, hasher, senhaDemonstracao);
    }
    catch (Exception ex) when (ex is Npgsql.NpgsqlException or InvalidOperationException or TimeoutException)
    {
        // A API sobe mesmo sem banco (o Swagger continua acessível); as rotas que usam o banco responderão 500.
        logger.LogError(ex, "Não foi possível conectar ao PostgreSQL para o seed. Suba o banco com 'docker compose up -d' (veja o README).");
    }
}

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await app.RunAsync();
