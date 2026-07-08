using Frontend.Models.Auth;
using Frontend.Services.Common;
using System.Net.Http.Headers;

namespace Frontend.Services;

/// <summary>
/// Consome as rotas autenticadas de usuário (GET /api/v1/usuario e PUT /api/v1/usuario/alterar),
/// sempre anexando o token Bearer do usuário logado.
/// </summary>
public class UsuarioService
{
    private readonly HttpClient _http;
    private readonly AuthService _authSvc;

    public UsuarioService(HttpClient http, AuthService authSvc)
    {
        _http = http;
        _authSvc = authSvc;
    }

    /// <summary>
    /// Busca os dados do usuário pelo e-mail. Para o Doador, sempre usar o e-mail logado.
    /// </summary>
    public async Task<(bool Sucesso, UsuarioDto? Usuario, List<string> Erros)> ObterPorEmailAsync(string email)
    {
        try
        {
            var url = $"api/v1/usuario?Email={Uri.EscapeDataString(email)}";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            await AnexarTokenAsync(request);

            var response = await _http.SendAsync(request);
            var conteudo = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return (false, null, ApiResponseHelper.ExtrairErros(conteudo));

            // GET retorna o objeto do usuário direto, sem envelope isSuccess/value.
            var usuario = ApiResponseHelper.ExtrairObjetoDireto<UsuarioDto>(conteudo);

            return usuario is not null
                ? (true, usuario, new List<string>())
                : (false, null, new List<string> { "Não foi possível ler os dados do usuário." });
        }
        catch (Exception ex)
        {
            return (false, null, new List<string> { $"Erro de conexão: {ex.Message}" });
        }
    }

    /// <summary>
    /// Altera nome completo e CPF do usuário logado.
    /// </summary>
    public async Task<(bool Sucesso, UsuarioDto? Usuario, List<string> Erros)> AlterarAsync(string nomeCompleto, string cpf)
    {
        try
        {
            var url = $"api/v1/usuario/alterar?NomeCompleto={Uri.EscapeDataString(nomeCompleto)}&Cpf={Uri.EscapeDataString(cpf)}";
            using var request = new HttpRequestMessage(HttpMethod.Put, url);
            await AnexarTokenAsync(request);

            var response = await _http.SendAsync(request);
            var conteudo = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return (false, null, ApiResponseHelper.ExtrairErros(conteudo));

            // Aceita tanto o formato com envelope (isSuccess/value) quanto o objeto direto.
            var usuario = ApiResponseHelper.ExtrairValor<UsuarioDto>(conteudo)
                          ?? ApiResponseHelper.ExtrairObjetoDireto<UsuarioDto>(conteudo);

            return usuario is not null
                ? (true, usuario, new List<string>())
                : (false, null, new List<string> { "Não foi possível confirmar a alteração." });
        }
        catch (Exception ex)
        {
            return (false, null, new List<string> { $"Erro de conexão: {ex.Message}" });
        }
    }

    /// <summary>
    /// Exclui a conta do usuário pelo e-mail (LGPD — dados de doações são mantidos
    /// separadamente em cumprimento à norma da Receita Federal).
    /// </summary>
    public async Task<(bool Sucesso, List<string> Mensagens)> ExcluirAsync(string email)
    {
        try
        {
            var url = $"api/v1/usuario?Email={Uri.EscapeDataString(email)}";
            using var request = new HttpRequestMessage(HttpMethod.Delete, url);
            await AnexarTokenAsync(request);

            var response = await _http.SendAsync(request);
            var conteudo = await response.Content.ReadAsStringAsync();

            return response.IsSuccessStatusCode
                ? (true, new List<string> { "Conta excluída com sucesso." })
                : (false, ApiResponseHelper.ExtrairErros(conteudo));
        }
        catch (Exception ex)
        {
            return (false, new List<string> { $"Erro de conexão: {ex.Message}" });
        }
    }

    public async Task<(bool Sucesso, List<string> Mensagens)> SuspenderAsync(string email) =>
        await ExecutarAcaoAdminAsync($"api/v1/usuario/suspender?Email={Uri.EscapeDataString(email)}", HttpMethod.Put);

    public async Task<(bool Sucesso, List<string> Mensagens)> AtivarAsync(string email) =>
        await ExecutarAcaoAdminAsync($"api/v1/usuario/ativar?Email={Uri.EscapeDataString(email)}", HttpMethod.Put);

    public async Task<(bool Sucesso, List<string> Mensagens)> AlterarParaGestorAsync(string email) =>
        await ExecutarAcaoAdminAsync($"api/v1/usuario/alterar-para-gestor?Email={Uri.EscapeDataString(email)}", HttpMethod.Put);

    public async Task<(bool Sucesso, List<string> Mensagens)> AlterarParaDoadorAsync(string email) =>
        await ExecutarAcaoAdminAsync($"api/v1/usuario/alterar-para-doador?Email={Uri.EscapeDataString(email)}", HttpMethod.Put);

    private async Task<(bool Sucesso, List<string> Mensagens)> ExecutarAcaoAdminAsync(string url, HttpMethod metodo)
    {
        try
        {
            using var request = new HttpRequestMessage(metodo, url);
            await AnexarTokenAsync(request);

            var response = await _http.SendAsync(request);
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