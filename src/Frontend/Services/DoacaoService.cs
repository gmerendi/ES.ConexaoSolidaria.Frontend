using Frontend.Models.Doacoes;
using Frontend.Services.Common;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Frontend.Services;

public class DoacaoService
{
    private readonly HttpClient _http;
    private readonly AuthService _authSvc;

    public DoacaoService(HttpClient http, AuthService authSvc)
    {
        _http = http;
        _authSvc = authSvc;
    }

    public async Task<(bool Sucesso, List<DoacaoCampanhaDto> Doacoes, List<string> Erros)> ObterPorUsuarioAsync(Guid guidUsuario)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get,
                $"/api/v1/Doacoes/usuario?GuidUsuario={guidUsuario}");

            var token = await _authSvc.ObterTokenAsync();
            if (!string.IsNullOrEmpty(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _http.SendAsync(request);
            var conteudo = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return (false, new(), ApiResponseHelper.ExtrairErros(conteudo));

            var resultado = ApiResponseHelper.ExtrairObjetoDireto<ObterDoacoesCampanhaResponse>(conteudo);
            var doacoes = resultado?.Doacoes?.ToList() ?? new List<DoacaoCampanhaDto>();

            return (true, doacoes, new());
        }
        catch (Exception ex)
        {
            return (false, new(), new List<string> { $"Erro de conexão: {ex.Message}" });
        }
    }

    public async Task<(bool Sucesso, List<DoacaoCampanhaDto> Doacoes, List<string> Erros)> ObterPorCampanhaAsync(Guid guidCampanha)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get,
                $"/api/v1/Doacoes/campanha?GuidCampanha={guidCampanha}");

            var token = await _authSvc.ObterTokenAsync();
            if (!string.IsNullOrEmpty(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _http.SendAsync(request);
            var conteudo = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return (false, new(), ApiResponseHelper.ExtrairErros(conteudo));

            var resultado = ApiResponseHelper.ExtrairObjetoDireto<ObterDoacoesCampanhaResponse>(conteudo);
            var doacoes = resultado?.Doacoes?.ToList() ?? new List<DoacaoCampanhaDto>();

            return (true, doacoes, new());
        }
        catch (Exception ex)
        {
            return (false, new(), new List<string> { $"Erro de conexão: {ex.Message}" });
        }
    }

    public async Task<(bool Sucesso, List<DoacaoSelfDto> Doacoes, List<string> Erros)> ObterMinhasDoacoesAsync()
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/Doacoes/self");

            var token = await _authSvc.ObterTokenAsync();
            if (!string.IsNullOrEmpty(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _http.SendAsync(request);
            var conteudo = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return (false, new List<DoacaoSelfDto>(), ApiResponseHelper.ExtrairErros(conteudo));

            var resultado = ApiResponseHelper.ExtrairObjetoDireto<ObterDoacoesSelfResponse>(conteudo);
            var doacoes = resultado?.Doacoes?.ToList() ?? new List<DoacaoSelfDto>();

            return (true, doacoes, new List<string>());
        }
        catch (Exception ex)
        {
            return (false, new List<DoacaoSelfDto>(), new List<string> { $"Erro de conexão: {ex.Message}" });
        }
    }

    public async Task<(bool Sucesso, DoacaoRealizadaDto? Doacao, List<string> Erros)> CriarDoacaoAsync(Guid guidCampanha, decimal valor)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/Doacoes")
            {
                Content = JsonContent.Create(new CriarDoacaoRequest(guidCampanha, valor))
            };

            var token = await _authSvc.ObterTokenAsync();
            if (!string.IsNullOrEmpty(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _http.SendAsync(request);
            var conteudo = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return (false, null, ApiResponseHelper.ExtrairErros(conteudo));

            var doacao = ApiResponseHelper.ExtrairValor<DoacaoRealizadaDto>(conteudo);

            return doacao is not null
                ? (true, doacao, new List<string>())
                : (false, null, new List<string> { "Não foi possível confirmar a doação." });
        }
        catch (Exception ex)
        {
            return (false, null, new List<string> { $"Erro de conexão: {ex.Message}" });
        }
    }
}