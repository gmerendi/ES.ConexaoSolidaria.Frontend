namespace Frontend.Models.Campanhas;

public record CampanhaPublicaResponse(
    Guid Guid,
    string Titulo,
    string Descricao,
    decimal MetaFinanceira,
    decimal ValorArrecadado,
    string DataInicio,   
    string DataFim,
    string StatusCampanha
)
{
    public decimal PercentualArrecadado =>
        MetaFinanceira > 0
            ? Math.Min(Math.Round(ValorArrecadado / MetaFinanceira * 100, 1), 100)
            : 0;

    public int DiasRestantes
    {
        get
        {
            if (DateTime.TryParse(DataFim, out var fim))
                return Math.Max((int)(fim - DateTime.UtcNow).TotalDays, 0);
            return 0;
        }
    }
}

public record ObterTodasCampanhasResponse(
    IEnumerable<CampanhaPublicaResponse> Campanhas,
    int TotalPaginas,
    int PaginaAtual
);
