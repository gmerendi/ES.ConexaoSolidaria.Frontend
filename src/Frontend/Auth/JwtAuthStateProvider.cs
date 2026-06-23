using Blazored.LocalStorage;
using Frontend.Models.Auth;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace Frontend.Auth;

/// <summary>
/// Fornece o estado de autenticação para o Blazor com base no token
/// armazenado no localStorage — sem depender de cookies ou sessão de servidor.
/// </summary>
public class JwtAuthStateProvider : AuthenticationStateProvider
{
    private readonly ILocalStorageService _localStorage;
    private static readonly AuthenticationState _anonimo =
        new(new ClaimsPrincipal(new ClaimsIdentity()));

    private const string UsuarioKey = "cs_usuario";

    public JwtAuthStateProvider(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var usuario = await _localStorage.GetItemAsync<UsuarioLogado>(UsuarioKey);

            if (usuario is null || usuario.Expiracao <= DateTime.UtcNow)
                return _anonimo;

            var identity = CriarIdentidade(usuario);
            return new AuthenticationState(new ClaimsPrincipal(identity));
        }
        catch
        {
            return _anonimo;
        }
    }



    /// <summary>
    /// Notifica o Blazor que o estado de auth mudou (login ou logout).
    /// Deve ser chamado pelo AuthService após persistir/remover o token.
    /// </summary>
    public void NotificarMudancaEstado()
        => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

    private static ClaimsIdentity CriarIdentidade(UsuarioLogado usuario)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Guid.ToString()),
            new Claim(ClaimTypes.Email,          usuario.Email),
            new Claim(ClaimTypes.Role,           usuario.Perfil),
            new Claim("status",                  usuario.Status),
        };

        return new ClaimsIdentity(claims, authenticationType: "jwt");
    }
}
