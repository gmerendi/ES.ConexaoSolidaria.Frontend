namespace Frontend.Models.Auth;

public record LoginRequest(string Email, string Password);

public record CadastroDoadorRequest(
    string NomeCompleto,
    string Email,
    string Cpf,
    string Password);

/// <summary>Dados de usuário retornados pela API (cadastro, consulta e alteração de perfil).</summary>
public record UsuarioDto(
    Guid Guid,
    string NomeCompleto,
    string Cpf,
    string Email,
    string Perfil,
    string Status);

public record ResetSenhaRequest(string PasswordAtual, string PasswordNovo);

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