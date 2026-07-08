using Blazored.LocalStorage;
using Frontend.Models.Auth;
using Frontend.Services.Common;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Frontend.Services;

public class AuthService
{
    private readonly HttpClient _http;
    private readonly ILocalStorageService _localStorage;

    private const string TokenKey = "cs_token";
    private const string UsuarioKey = "cs_usuario";

    public AuthService(HttpClient http, ILocalStorageService localStorage)
    {
        _http = http;
        _localStorage = localStorage;
    }

    public async Task<(bool Sucesso, List<string> Erros)> LoginAsync(LoginRequest request)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/v1/auth/login", request);

            if (!response.IsSuccessStatusCode)
            {
                var conteudo = await response.Content.ReadAsStringAsync();
                return (false, ApiResponseHelper.ExtrairErros(conteudo));
            }

            var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();

            if (loginResponse is null)
                return (false, new List<string> { "Resposta inválida do servidor." });

            // Extrai perfil e expiração reais do JWT (mais confiável que o campo da API)
            var (perfil, expiracao) = ExtrairDadosToken(loginResponse.Token, loginResponse.Expiracao);

            var usuario = new UsuarioLogado(
                loginResponse.Guid,
                loginResponse.Email,
                loginResponse.Status,
                loginResponse.Token,
                expiracao,
                perfil);

            await _localStorage.SetItemAsync(TokenKey, loginResponse.Token);
            await _localStorage.SetItemAsync(UsuarioKey, usuario);

            return (true, new List<string>());
        }
        catch (Exception ex)
        {
            return (false, new List<string> { $"Erro de conexão: {ex.Message}" });
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            var token = await _localStorage.GetItemAsync<string>(TokenKey);
            if (!string.IsNullOrEmpty(token))
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/auth/logout");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                await _http.SendAsync(request);
            }
        }
        catch { /* ignora erro de rede no logout */ }
        finally
        {
            await _localStorage.RemoveItemAsync(TokenKey);
            await _localStorage.RemoveItemAsync(UsuarioKey);
        }
    }

    public async Task<UsuarioLogado?> ObterUsuarioLogadoAsync()
        => await _localStorage.GetItemAsync<UsuarioLogado>(UsuarioKey);

    public async Task<string?> ObterTokenAsync()
        => await _localStorage.GetItemAsync<string>(TokenKey);

    public async Task<bool> EstaLogadoAsync()
    {
        var usuario = await ObterUsuarioLogadoAsync();
        return usuario is not null && usuario.Expiracao > DateTime.UtcNow;
    }

    /// <summary>
    /// Altera a senha do usuário logado via rota autenticada do Gateway.
    /// </summary>
    public async Task<(bool Sucesso, List<string> Mensagens)> AlterarSenhaAsync(string senhaAtual, string senhaNova)
    {
        try
        {
            var token = await ObterTokenAsync();

            using var request = new HttpRequestMessage(HttpMethod.Put, "api/v1/auth/reset-password")
            {
                Content = JsonContent.Create(new ResetSenhaRequest(senhaAtual, senhaNova))
            };

            if (!string.IsNullOrEmpty(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _http.SendAsync(request);
            var conteudo = await response.Content.ReadAsStringAsync();

            return response.IsSuccessStatusCode
                ? (true, new List<string> { "Senha alterada com sucesso!" })
                : (false, ApiResponseHelper.ExtrairErros(conteudo));
        }
        catch (Exception ex)
        {
            return (false, new List<string> { $"Erro de conexão: {ex.Message}" });
        }
    }

    /// <summary>
    /// Cadastra um novo doador via rota pública do Gateway.
    /// Retorna sempre a mensagem derivada da resposta da API (sucesso ou erro),
    /// para exibição direta ao usuário.
    /// </summary>
    public async Task<(bool Sucesso, List<string> Mensagens)> CadastrarDoadorAsync(CadastroDoadorRequest request)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/v1/usuario", request);
            var conteudo = await response.Content.ReadAsStringAsync();

            return response.IsSuccessStatusCode
                ? (true, new List<string> { MontarMensagemSucesso(conteudo) })
                : (false, ApiResponseHelper.ExtrairErros(conteudo));
        }
        catch (Exception ex)
        {
            return (false, new List<string> { $"Erro de conexão: {ex.Message}" });
        }
    }

    /// <summary>
    /// Formato de sucesso da API: { "isSuccess": true, "value": { "guid", "nomeCompleto", "cpf", "email", "perfil", "status" }, ... }
    /// </summary>
    private static string MontarMensagemSucesso(string conteudo)
    {
        var valor = ApiResponseHelper.ExtrairValor<UsuarioDto>(conteudo);

        if (valor is not null)
        {
            return $"Cadastro realizado com sucesso! Bem-vindo(a), {valor.NomeCompleto}. " +
                   $"Seu e-mail ({valor.Email}) foi registrado com o perfil {valor.Perfil} " +
                   $"e status {valor.Status}.";
        }

        return "Cadastro realizado com sucesso!";
    }

    private static (string Perfil, DateTime Expiracao) ExtrairDadosToken(string token, DateTime expiracaoFallback)
    {
        try
        {
            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);

            // Claim de perfil/role
            var perfil = jwt.Claims
                .FirstOrDefault(c => c.Type is "role"
                    or "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
                ?.Value;

            // ValidTo é derivado do claim "exp" (timestamp Unix) e o JwtSecurityTokenHandler
            // sempre retorna em UTC corretamente — diferente do campo "Expiracao" vindo da API,
            // que pode chegar sem indicação de fuso e ser malinterpretado como UTC quando na
            // verdade é horário local, fazendo um token recém-emitido parecer já expirado.
            var expiracao = jwt.ValidTo != default ? jwt.ValidTo : expiracaoFallback;

            return (perfil ?? "DOADOR", expiracao);
        }
        catch
        {
            return ("DOADOR", expiracaoFallback);
        }
    }
}