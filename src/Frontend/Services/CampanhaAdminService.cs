using Frontend.Models.Campanhas;
using Frontend.Models.Common;
using Frontend.Services.Common;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Frontend.Services;

/// <summary>
/// Consome as rotas autenticadas de campanhas para o Gestor:
/// POST /api/v1/Campanhas, GET /api/v1/Campanhas/busca, GET /api/v1/Campanhas?Guid=
/// </summary>
public class CampanhaAdminService
{
    private readonly HttpClient _http;
    private readonly AuthService _authSvc;

    public CampanhaAdminService(HttpClient http, AuthService authSvc)
    {
        _http = http;
        _authSvc = authSvc;
    }

    public async Task<(bool Sucesso, List<string> Mensagens)> CriarAsync(CriarCampanhaRequest request)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/Campanhas")
            {
                Content = JsonContent.Create(request)
            };
            await AnexarTokenAsync(req);

            var response = await _http.SendAsync(req);
            var conteudo = await response.Content.ReadAsStringAsync();

            return response.IsSuccessStatusCode
                ? (true, new List<string> { "Campanha criada com sucesso!" })
                : (false, ApiResponseHelper.ExtrairErros(conteudo));
        }
        catch (Exception ex)
        {
            return (false, new List<string> { $"Erro de conexão: {ex.Message}" });
        }
    }

    public async Task<(bool Sucesso, List<CampanhaAdminDto> Campanhas, List<string> Erros)> BuscarAsync(string termo)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get,
                $"/api/v1/Campanhas/busca?Termo={Uri.EscapeDataString(termo)}");
            await AnexarTokenAsync(req);

            var response = await _http.SendAsync(req);
            var conteudo = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return (false, new(), ApiResponseHelper.ExtrairErros(conteudo));

            // Resposta: { isSuccess, value: { campanhas: [...] } }
            var resultado = ApiResponseHelper.ExtrairValor<BuscaCampanhaValue>(conteudo);
            var lista = resultado?.Campanhas?.ToList() ?? new List<CampanhaAdminDto>();

            return (true, lista, new());
        }
        catch (Exception ex)
        {
            return (false, new(), new List<string> { $"Erro de conexão: {ex.Message}" });
        }
    }

    public async Task<(bool Sucesso, CampanhaAdminDto? Campanha, List<string> Erros)> ObterPorGuidAsync(Guid guid)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get,
                $"/api/v1/Campanhas?Guid={guid}");
            await AnexarTokenAsync(req);

            var response = await _http.SendAsync(req);
            var conteudo = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return (false, null, ApiResponseHelper.ExtrairErros(conteudo));

            // Resposta direta (sem envelope)
            var campanha = ApiResponseHelper.ExtrairObjetoDireto<CampanhaAdminDto>(conteudo);

            return campanha is not null
                ? (true, campanha, new())
                : (false, null, new List<string> { "Não foi possível ler os dados da campanha." });
        }
        catch (Exception ex)
        {
            return (false, null, new List<string> { $"Erro de conexão: {ex.Message}" });
        }
    }

    public async Task<(bool Sucesso, List<string> Mensagens)> CancelarAsync(Guid guid) =>
        await ExecutarAcaoAsync($"/api/v1/Campanhas/cancel?Guid={guid}", HttpMethod.Put);

    public async Task<(bool Sucesso, List<string> Mensagens)> ConcluirAsync(Guid guid) =>
        await ExecutarAcaoAsync($"/api/v1/Campanhas/concluir?Guid={guid}", HttpMethod.Put);

    public async Task<(bool Sucesso, List<string> Mensagens)> AlterarAsync(AlterarCampanhaRequest request)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Put, "/api/v1/Campanhas")
            {
                Content = JsonContent.Create(request)
            };
            await AnexarTokenAsync(req);

            var response = await _http.SendAsync(req);
            var conteudo = await response.Content.ReadAsStringAsync();

            return response.IsSuccessStatusCode
                ? (true, new List<string> { "Campanha alterada com sucesso!" })
                : (false, ApiResponseHelper.ExtrairErros(conteudo));
        }
        catch (Exception ex)
        {
            return (false, new List<string> { $"Erro de conexão: {ex.Message}" });
        }
    }

    private async Task<(bool Sucesso, List<string> Mensagens)> ExecutarAcaoAsync(string url, HttpMethod metodo)
    {
        try
        {
            using var req = new HttpRequestMessage(metodo, url);
            await AnexarTokenAsync(req);

            var response = await _http.SendAsync(req);
            var conteudo = await response.Content.ReadAsStringAsync();

            return response.IsSuccessStatusCode
                ? (true, new List<string> { "Operação realizada com sucesso!" })
                : (false, ApiResponseHelper.ExtrairErros(conteudo));
        }
        catch (Exception ex)
        {
            return (false, new List<string> { $"Erro de conexão: {ex.Message}" });
        }
    }

    private async Task AnexarTokenAsync(HttpRequestMessage request)
    {
        var token = await _authSvc.ObterTokenAsync();
        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}