using Blazored.LocalStorage;
using FluentAssertions;
using Frontend.Services;
using Frontend.Tests.TestHelpers;
using Moq;
using System.Net;
using Xunit;

namespace Frontend.Tests.Services;

public class DoacaoServiceTests
{
    private static (DoacaoService Sut, FakeHttpMessageHandler Handler) CriarSut(string? token = "token-fake")
    {
        var handler = new FakeHttpMessageHandler();
        var http = FakeHttpMessageHandler.CriarHttpClient(handler);

        var localStorage = new Mock<ILocalStorageService>();
        localStorage
            .Setup(l => l.GetItemAsync<string>("cs_token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        var authSvc = new AuthService(http, localStorage.Object);
        return (new DoacaoService(http, authSvc), handler);
    }

    [Fact]
    public async Task ObterPorUsuarioAsync_ComSucesso_RetornaDoacoesEAnexaToken()
    {
        var (sut, handler) = CriarSut();
        var json = """
        {
            "doacoes": [
                { "guidUsuario": "11111111-1111-1111-1111-111111111111", "nomeUsuario": "Ana", "emailUsuario": "ana@teste.com", "cpfUsuario": "123", "guidCampanha": "22222222-2222-2222-2222-222222222222", "tituloCampanha": "Campanha X", "valorDoacao": 100, "dataDoacao": "2024-01-01" }
            ]
        }
        """;
        handler.EnfileirarResposta(HttpStatusCode.OK, json);

        var (sucesso, doacoes, erros) = await sut.ObterPorUsuarioAsync("ana@teste.com");

        sucesso.Should().BeTrue();
        doacoes.Should().ContainSingle();
        erros.Should().BeEmpty();
        handler.UltimaRequisicao!.Headers.Authorization!.Parameter.Should().Be("token-fake");
        handler.UltimaRequisicao.RequestUri!.PathAndQuery.Should().Be("/api/v1/Doacoes/usuario?Email=ana%40teste.com");
    }

    [Fact]
    public async Task ObterPorUsuarioAsync_ComErroDaApi_RetornaErros()
    {
        var (sut, handler) = CriarSut();
        handler.EnfileirarResposta(HttpStatusCode.NotFound, """{ "title": "Usuário não encontrado.", "status": 404, "errors": null, "traceId": "x" }""");

        var (sucesso, doacoes, erros) = await sut.ObterPorUsuarioAsync("naoexiste@teste.com");

        sucesso.Should().BeFalse();
        doacoes.Should().BeEmpty();
        erros.Should().Contain("Usuário não encontrado.");
    }

    [Fact]
    public async Task ObterMinhasDoacoesAsync_SemToken_NaoAnexaHeaderDeAutorizacao()
    {
        var (sut, handler) = CriarSut(token: null);
        handler.EnfileirarResposta(HttpStatusCode.OK, """{ "doacoes": [] }""");

        await sut.ObterMinhasDoacoesAsync();

        handler.UltimaRequisicao!.Headers.Authorization.Should().BeNull();
    }

    [Fact]
    public async Task CriarDoacaoAsync_ComSucesso_RetornaDoacaoRealizada()
    {
        var (sut, handler) = CriarSut();
        var guidCampanha = Guid.NewGuid();
        var json = $$"""
        {
            "isSuccess": true,
            "value": { "guidCampanha": "{{guidCampanha}}", "tituloCampanha": "Campanha X", "nomeUsuario": "Ana", "emailUsuario": "ana@teste.com", "valor": 250.50 },
            "error": null,
            "errorCode": null
        }
        """;
        handler.EnfileirarResposta(HttpStatusCode.Created, json);

        var (sucesso, doacao, erros) = await sut.CriarDoacaoAsync(guidCampanha, 250.50m);

        sucesso.Should().BeTrue();
        doacao.Should().NotBeNull();
        doacao!.Valor.Should().Be(250.50m);
        erros.Should().BeEmpty();
        handler.UltimaRequisicao!.Method.Should().Be(HttpMethod.Post);
    }

    [Fact]
    public async Task CriarDoacaoAsync_ComRespostaSemEnvelopeReconhecivel_RetornaErroAmigavel()
    {
        var (sut, handler) = CriarSut();
        handler.EnfileirarResposta(HttpStatusCode.OK, "{}");

        var (sucesso, doacao, erros) = await sut.CriarDoacaoAsync(Guid.NewGuid(), 100m);

        sucesso.Should().BeFalse();
        doacao.Should().BeNull();
        erros.Should().Contain("Não foi possível confirmar a doação.");
    }

    [Fact]
    public async Task CriarDoacaoAsync_ComValorAbaixoDoMinimo_RetornaErrosDeValidacaoDaApi()
    {
        var (sut, handler) = CriarSut();
        var json = """{ "title": "Erro de validação", "status": 400, "errors": { "Valor": ["O valor mínimo de doação é R$ 5,00."] }, "traceId": "x" }""";
        handler.EnfileirarResposta(HttpStatusCode.BadRequest, json);

        var (sucesso, doacao, erros) = await sut.CriarDoacaoAsync(Guid.NewGuid(), 1m);

        sucesso.Should().BeFalse();
        doacao.Should().BeNull();
        erros.Should().Contain("O valor mínimo de doação é R$ 5,00.");
    }

    [Fact]
    public async Task ObterPorCampanhaAsync_QuandoRequisicaoLancaExcecao_RetornaErroDeConexao()
    {
        var (sut, handler) = CriarSut();
        handler.EnfileirarExcecao(new HttpRequestException("falha de rede"));

        var (sucesso, doacoes, erros) = await sut.ObterPorCampanhaAsync(Guid.NewGuid());

        sucesso.Should().BeFalse();
        doacoes.Should().BeEmpty();
        erros.Should().ContainSingle().Which.Should().StartWith("Erro de conexão:");
    }
}
