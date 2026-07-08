using Frontend.Models.Campanhas;
using System.Net.Http.Json;

namespace Frontend.Services;

public class CampanhaPublicaService
{
    private readonly HttpClient _http;

    public CampanhaPublicaService(HttpClient http)
    {
        _http = http;
    }

    /// <summary>
    /// Busca campanhas ativas via rota pública do Gateway
    /// (não exige autenticação — AuthorizationPolicy: public).
    /// </summary>
    public async Task<List<CampanhaPublicaResponse>> ObterCampanhasAtivasAsync(
        int pagina = 1, int tamanhoPagina = 6)
    {
        try
        {
            var url = $"api/v1/Campanhas/todas?Pagina={pagina}&TamanhoPagina={tamanhoPagina}";
            var response = await _http.GetFromJsonAsync<ObterTodasCampanhasResponse>(url);
            return response?.Campanhas?.ToList() ?? new List<CampanhaPublicaResponse>();
        }
        catch
        {
            return new List<CampanhaPublicaResponse>();
        }
    }
}