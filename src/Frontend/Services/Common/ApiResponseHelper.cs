using Frontend.Models.Common;
using System.Text.Json;

namespace Frontend.Services.Common;

/// <summary>
/// Extrai valores e mensagens de erro das respostas da API, suportando tanto o formato
/// com envelope (Result pattern: isSuccess/value/error) quanto o objeto retornado direto,
/// e o formato padrão de erro de validação (ProblemDetails: title/status/errors/traceId).
/// </summary>
public static class ApiResponseHelper
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    /// <summary>Tenta extrair o valor de uma resposta com envelope { isSuccess, value, ... }.</summary>
    public static T? ExtrairValor<T>(string conteudo) where T : class
    {
        try
        {
            return JsonSerializer.Deserialize<ApiResult<T>>(conteudo, JsonOpts)?.Value;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Tenta desserializar a resposta diretamente como T (sem envelope).</summary>
    public static T? ExtrairObjetoDireto<T>(string conteudo) where T : class
    {
        try
        {
            return JsonSerializer.Deserialize<T>(conteudo, JsonOpts);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Extrai a lista de mensagens de erro de uma resposta 4xx/5xx (uma mensagem por linha na UI).
    /// </summary>
    public static List<string> ExtrairErros(string conteudo)
    {
        try
        {
            var erro = JsonSerializer.Deserialize<ErroValidacaoApi>(conteudo, JsonOpts);

            if (erro?.Errors is { Count: > 0 })
            {
                var mensagens = erro.Errors
                    .SelectMany(campo => campo.Value)
                    .Where(m => !string.IsNullOrWhiteSpace(m));

                return mensagens.ToList();
            }

            if (!string.IsNullOrWhiteSpace(erro?.Title))
                return new List<string> { erro.Title! };
        }
        catch { /* cai no fallback abaixo */ }

        return new List<string>
        {
            string.IsNullOrWhiteSpace(conteudo) ? "Ocorreu um erro inesperado." : conteudo
        };
    }
}