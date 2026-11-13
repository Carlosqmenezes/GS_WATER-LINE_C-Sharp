using WaterLine.Domain.Entidades;
using WaterLine.Domain.Interfaces;

namespace WaterLine.Domain.Servicos;

/// <summary>Regra de segurança do trabalhador: cancelamento dos chamados de gari quando começa a chover.</summary>
public sealed class SegurancaTrabalhadorService : ISegurancaTrabalhadorService
{
    public IReadOnlyList<Chamado> CancelarChamadosDeGariPorChuva(Leitura leitura, IEnumerable<Chamado> chamados)
    {
        ArgumentNullException.ThrowIfNull(leitura);
        ArgumentNullException.ThrowIfNull(chamados);

        if (!leitura.Chovendo)
        {
            return Array.Empty<Chamado>();
        }

        var aCancelar = chamados
            .Where(c => c.BocaDeLoboId == leitura.BocaDeLoboId && c.EstaAtivo && c.EhDeGari)
            .ToList();

        foreach (var chamado in aCancelar)
        {
            chamado.Cancelar();
        }

        return aCancelar;
    }
}
