using Blazored.LocalStorage;
using FluentAssertions;
using Frontend.Models.Campanhas;
using Frontend.Services;
using Frontend.Tests.TestHelpers;
using Moq;
using System.Net;
using Xunit;

namespace Frontend.Tests.Services;

public class CampanhaAdminServiceTests
{
    private static (CampanhaAdminService Sut, FakeHttpMessageHandler Handler) CriarSut()
    {
        var handler = new FakeHttpMessageHandler();
        var http = FakeHttpMessageHandler.CriarHttpClient(handler);

        var localStorage = new Mock<ILocalStorageService>();
        localStorage
            .Setup(l => l.GetItemAsync<string>("cs_token", It.IsAny<CancellationToken>()))
            .ReturnsAsync("token-gestor");

        var authSvc = new AuthService(http, localStorage.Object);
        return (new CampanhaAdminService(http, authSvc), handler);
    }

    [Fact]
    public async Task CriarAsync_ComSucesso_RetornaMensagemDeConfirmacao()
    {
        var (sut, handler) = CriarSut();
        handler.EnfileirarResposta(HttpStatusCode.Created, "{}");

        var request = new CriarCampanhaRequest("Título", "Descrição", 1000, DateTime.UtcNow, DateTime.UtcNow.AddMonths(1));
        var (sucesso, mensagens) = await sut.CriarAsync(request);

        sucesso.Should().BeTrue();
        mensagens.Should().Contain("Campanha criada com sucesso!");
        handler.UltimaRequisicao!.Method.Should().Be(HttpMethod.Post);
        handler.UltimaRequisicao.Headers.Authorization!.Parameter.Should().Be("token-gestor");
    }

    [Fact]
    public async Task CriarAsync_ComErroDeValidacao_RetornaErrosDaApi()
    {
        var (sut, handler) = CriarSut();
        var json = """{ "title": "Erro", "status": 400, "errors": { "MetaFinanceira": ["A meta deve ser maior que zero."] }, "traceId": "x" }""";
        handler.EnfileirarResposta(HttpStatusCode.BadRequest, json);

        var request = new CriarCampanhaRequest("Título", "Descrição", 0, DateTime.UtcNow, DateTime.UtcNow.AddMonths(1));
        var (sucesso, mensagens) = await sut.CriarAsync(request);

        sucesso.Should().BeFalse();
        mensagens.Should().Contain("A meta deve ser maior que zero.");
    }

    [Fact]
    public async Task BuscarAsync_ComResultados_RetornaListaDeCampanhas()
    {
        var (sut, handler) = CriarSut();
        var json = """
        {
            "isSuccess": true,
            "value": { "campanhas": [
                { "guid": "11111111-1111-1111-1111-111111111111", "titulo": "Campanha A", "descricao": "d", "metaFinanceira": 1000, "valorArrecadado": 200, "dataInicio": "2024-01-01", "dataFim": "2099-01-01", "statusCampanha": "ATIVA" }
            ] },
            "error": null,
            "errorCode": null
        }
        """;
        handler.EnfileirarResposta(HttpStatusCode.OK, json);

        var (sucesso, campanhas, erros) = await sut.BuscarAsync("Campanha");

        sucesso.Should().BeTrue();
        campanhas.Should().ContainSingle();
        erros.Should().BeEmpty();
        handler.UltimaRequisicao!.RequestUri!.PathAndQuery.Should().Be("/api/v1/Campanhas/busca?Termo=Campanha");
    }

    [Fact]
    public async Task BuscarAsync_EscapaTermoDeBuscaNaUrl()
    {
        var (sut, handler) = CriarSut();
        handler.EnfileirarResposta(HttpStatusCode.OK, """{ "isSuccess": true, "value": { "campanhas": [] }, "error": null, "errorCode": null }""");

        await sut.BuscarAsync("campanha & doação");

        handler.UltimaRequisicao!.RequestUri!.PathAndQuery
            .Should().Be("/api/v1/Campanhas/busca?Termo=campanha%20%26%20doa%C3%A7%C3%A3o");
    }

    [Fact]
    public async Task ObterPorGuidAsync_ComCampanhaExistente_RetornaCampanha()
    {
        var (sut, handler) = CriarSut();
        var guid = Guid.NewGuid();
        var json = $$"""
        { "guid": "{{guid}}", "titulo": "Campanha A", "descricao": "d", "metaFinanceira": 1000, "valorArrecadado": 200, "dataInicio": "2024-01-01", "dataFim": "2099-01-01", "statusCampanha": "ATIVA" }
        """;
        handler.EnfileirarResposta(HttpStatusCode.OK, json);

        var (sucesso, campanha, erros) = await sut.ObterPorGuidAsync(guid);

        sucesso.Should().BeTrue();
        campanha.Should().NotBeNull();
        campanha!.Guid.Should().Be(guid);
    }

    [Fact]
    public async Task ObterPorGuidAsync_ComCampanhaInexistente_RetornaErro()
    {
        var (sut, handler) = CriarSut();
        handler.EnfileirarResposta(HttpStatusCode.NotFound, """{ "title": "Campanha não encontrada.", "status": 404, "errors": null, "traceId": "x" }""");

        var (sucesso, campanha, erros) = await sut.ObterPorGuidAsync(Guid.NewGuid());

        sucesso.Should().BeFalse();
        campanha.Should().BeNull();
        erros.Should().Contain("Campanha não encontrada.");
    }

    [Fact]
    public async Task CancelarAsync_ComSucesso_EnviaRequisicaoPutParaRotaCorreta()
    {
        var (sut, handler) = CriarSut();
        var guid = Guid.NewGuid();
        handler.EnfileirarResposta(HttpStatusCode.OK, "{}");

        var (sucesso, mensagens) = await sut.CancelarAsync(guid);

        sucesso.Should().BeTrue();
        handler.UltimaRequisicao!.Method.Should().Be(HttpMethod.Put);
        handler.UltimaRequisicao.RequestUri!.PathAndQuery.Should().Be($"/api/v1/Campanhas/cancel?Guid={guid}");
    }

    [Fact]
    public async Task ConcluirAsync_ComSucesso_EnviaRequisicaoPutParaRotaCorreta()
    {
        var (sut, handler) = CriarSut();
        var guid = Guid.NewGuid();
        handler.EnfileirarResposta(HttpStatusCode.OK, "{}");

        var (sucesso, mensagens) = await sut.ConcluirAsync(guid);

        sucesso.Should().BeTrue();
        handler.UltimaRequisicao!.RequestUri!.PathAndQuery.Should().Be($"/api/v1/Campanhas/concluir?Guid={guid}");
    }

    [Fact]
    public async Task AlterarAsync_QuandoRequisicaoLancaExcecao_RetornaErroDeConexao()
    {
        var (sut, handler) = CriarSut();
        handler.EnfileirarExcecao(new HttpRequestException("sem conexão"));

        var request = new AlterarCampanhaRequest(Guid.NewGuid(), "T", "D", 1000, DateTime.UtcNow, DateTime.UtcNow.AddDays(30));
        var (sucesso, mensagens) = await sut.AlterarAsync(request);

        sucesso.Should().BeFalse();
        mensagens.Should().ContainSingle().Which.Should().StartWith("Erro de conexão:");
    }
}
