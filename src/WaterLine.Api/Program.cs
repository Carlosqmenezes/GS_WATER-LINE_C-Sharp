// Ponto de entrada da API WaterLine. A configuração completa é adicionada nas próximas etapas.
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

app.Run();
