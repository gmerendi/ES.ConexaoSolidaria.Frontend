using System.Net;

namespace Frontend.Tests.TestHelpers;

/// <summary>
/// HttpMessageHandler de teste que permite programar a resposta (ou uma sequência de
/// respostas) que o HttpClient deve receber, sem nenhuma chamada de rede real.
/// Também guarda a última requisição enviada para permitir assertions sobre
/// método, URL, headers e corpo.
/// </summary>
public class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<Func<HttpRequestMessage, HttpResponseMessage>> _respostas = new();
    private readonly List<HttpRequestMessage> _requisicoes = new();

    public IReadOnlyList<HttpRequestMessage> Requisicoes => _requisicoes;

    public HttpRequestMessage? UltimaRequisicao => _requisicoes.Count > 0 ? _requisicoes[^1] : null;

    /// <summary>Enfileira uma resposta fixa para a próxima chamada.</summary>
    public FakeHttpMessageHandler EnfileirarResposta(HttpStatusCode statusCode, string? conteudoJson = null)
    {
        _respostas.Enqueue(_ =>
        {
            var response = new HttpResponseMessage(statusCode);
            if (conteudoJson is not null)
                response.Content = new StringContent(conteudoJson, System.Text.Encoding.UTF8, "application/json");
            return response;
        });
        return this;
    }

    /// <summary>Enfileira uma resposta calculada dinamicamente a partir da requisição recebida.</summary>
    public FakeHttpMessageHandler EnfileirarResposta(Func<HttpRequestMessage, HttpResponseMessage> fabrica)
    {
        _respostas.Enqueue(fabrica);
        return this;
    }

    /// <summary>Faz a próxima chamada lançar uma exceção (simula falha de rede/conexão).</summary>
    public FakeHttpMessageHandler EnfileirarExcecao(Exception excecao)
    {
        _respostas.Enqueue(_ => throw excecao);
        return this;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _requisicoes.Add(request);

        if (_respostas.Count == 0)
            throw new InvalidOperationException(
                "Nenhuma resposta foi programada no FakeHttpMessageHandler para esta chamada.");

        var fabrica = _respostas.Dequeue();
        return Task.FromResult(fabrica(request));
    }

    public static HttpClient CriarHttpClient(FakeHttpMessageHandler handler, string baseAddress = "https://api.teste.local/")
        => new(handler) { BaseAddress = new Uri(baseAddress) };
}
