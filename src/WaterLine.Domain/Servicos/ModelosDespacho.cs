using WaterLine.Domain.Entidades;

namespace WaterLine.Domain.Servicos;

/// <summary>Chamado acompanhado da boca de lobo onde ele acontece (para calcular distâncias).</summary>
public sealed record ChamadoComBoca(Chamado Chamado, BocaDeLobo Boca);

/// <summary>Chamado dentro do raio de um ponto, com a distância calculada.</summary>
public sealed record ChamadoProximo(Chamado Chamado, BocaDeLobo Boca, double DistanciaMetros);

/// <summary>Resultado do despacho: qual agente foi designado para qual chamado.</summary>
public sealed record Designacao(Chamado Chamado, Agente Agente, double DistanciaMetros);
