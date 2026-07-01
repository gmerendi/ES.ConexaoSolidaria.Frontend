namespace Frontend.Models.Common;

/// <summary>Formato padrão de sucesso (Result pattern) usado por vários endpoints da API.</summary>
public record ApiResult<T>(
    bool IsSuccess,
    T? Value,
    string? Error,
    string? ErrorCode);

/// <summary>Formato padrão de erro de validação (ProblemDetails) retornado pela API.</summary>
public record ErroValidacaoApi(
    string? Title,
    int? Status,
    Dictionary<string, string[]>? Errors,
    string? TraceId);