namespace Frontend.Models.Auth;

public record LoginRequest(string Email, string Password);

public record LoginResponse(
    string Token,
    DateTime Expiracao,
    Guid Guid,
    string Email,
    string Status);

public record UsuarioLogado(
    Guid Guid,
    string Email,
    string Status,
    string Token,
    DateTime Expiracao,
    string Perfil);
