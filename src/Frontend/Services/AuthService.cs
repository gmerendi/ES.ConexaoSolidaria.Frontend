using Blazored.LocalStorage;
using Frontend.Models.Auth;
using System.Net.Http.Json;

namespace Frontend.Services;

public class AuthService
{
    private readonly HttpClient _http;
    private readonly ILocalStorageService _localStorage;

    private const string TokenKey   = "cs_token";
    private const string UsuarioKey = "cs_usuario";

    public AuthService(HttpClient http, ILocalStorageService localStorage)
    {
        _http         = http;
        _localStorage = localStorage;
    }

    public async Task<(bool Sucesso, string? Erro)> LoginAsync(LoginRequest request)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/api/v1/auth/login", request);

            if (!response.IsSuccessStatusCode)
            {
                var erro = await response.Content.ReadAsStringAsync();
                return (false, string.IsNullOrWhiteSpace(erro) ? "Credenciais inválidas." : erro);
            }

            var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();

            if (loginResponse is null)
                return (false, "Resposta inválida do servidor.");

            // Extrai o perfil do JWT para saber se é GESTOR ou DOADOR
            var perfil = ExtrairPerfil(loginResponse.Token);

            var usuario = new UsuarioLogado(
                loginResponse.Guid,
                loginResponse.Email,
                loginResponse.Status,
                loginResponse.Token,
                loginResponse.Expiracao,
                perfil);

            await _localStorage.SetItemAsync(TokenKey,   loginResponse.Token);
            await _localStorage.SetItemAsync(UsuarioKey, usuario);

            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, $"Erro de conexão: {ex.Message}");
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            var token = await _localStorage.GetItemAsync<string>(TokenKey);
            if (!string.IsNullOrEmpty(token))
            {
                _http.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                await _http.PostAsync("/api/v1/auth/logout", null);
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

    private static string ExtrairPerfil(string token)
    {
        try
        {
            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var jwt     = handler.ReadJwtToken(token);

            // Claim de perfil/role
            var perfil = jwt.Claims
                .FirstOrDefault(c => c.Type is "role"
                    or "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
                ?.Value;

            return perfil ?? "DOADOR";
        }
        catch
        {
            return "DOADOR";
        }
    }
}
