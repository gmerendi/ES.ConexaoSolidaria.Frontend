using FluentAssertions;
using Frontend.Services;
using Frontend.Tests.TestHelpers;
using System.Net;
using Xunit;

namespace Frontend.Tests.Services;

public class CampanhaPublicaServiceTests
{
    private static (CampanhaPublicaService Sut, FakeHttpMessageHandler Handler) CriarSut()
    {
        var handler = new FakeHttpMessageHandler();
        var http = FakeHttpMessageHandler.CriarHttpClient(handler);
        return (new CampanhaPublicaService(http), handler);
    }

    [Fact]
    public async Task ObterCampanhasAtivasAsync_ComRespostaValida_RetornaListaDeCampanhas()
    {
        var (sut, handler) = CriarSut();
        var json = """
        {
            "campanhas": [
                { "guid": "11111111-1111-1111-1111-111111111111", "titulo": "Campanha A", "descricao": "d", "metaFinanceira": 1000, "valorArrecadado": 500, "dataInicio": "2024-01-01", "dataFim": "2099-01-01", "statusCampanha": "ATIVA" }
            ],
            "totalPaginas": 1,
            "paginaAtual": 1
        }
        """;
        handler.EnfileirarResposta(HttpStatusCode.OK, json);

        var campanhas = await sut.ObterCampanhasAtivasAsync();

        campanhas.Should().ContainSingle();
        campanhas[0].Titulo.Should().Be("Campanha A");
    }

    [Fact]
    public async Task ObterCampanhasAtivasAsync_MontaUrlComPaginaETamanhoInformados()
    {
        var (sut, handler) = CriarSut();
        handler.EnfileirarResposta(HttpStatusCode.OK, """{ "campanhas": [], "totalPaginas": 0, "paginaAtual": 2 }""");

        await sut.ObterCampanhasAtivasAsync(pagina: 2, tamanhoPagina: 12);

        handler.UltimaRequisicao!.RequestUri!.PathAndQuery
            .Should().Be("/api/v1/Campanhas/todas?Pagina=2&TamanhoPagina=12");
    }

    [Fact]
    public async Task ObterCampanhasAtivasAsync_QuandoApiFalha_RetornaListaVaziaSemLancarExcecao()
    {
        var (sut, handler) = CriarSut();
        handler.EnfileirarResposta(HttpStatusCode.InternalServerError, "erro interno");

        var campanhas = await sut.ObterCampanhasAtivasAsync();

        campanhas.Should().BeEmpty();
    }

    [Fact]
    public async Task ObterCampanhasAtivasAsync_QuandoRequisicaoLancaExcecao_RetornaListaVazia()
    {
        var (sut, handler) = CriarSut();
        handler.EnfileirarExcecao(new HttpRequestException("sem conexão"));

        var campanhas = await sut.ObterCampanhasAtivasAsync();

        campanhas.Should().BeEmpty();
    }

    [Fact]
    public async Task ObterCampanhasAtivasAsync_ComListaNulaNaResposta_RetornaListaVazia()
    {
        var (sut, handler) = CriarSut();
        handler.EnfileirarResposta(HttpStatusCode.OK, """{ "campanhas": null, "totalPaginas": 0, "paginaAtual": 1 }""");

        var campanhas = await sut.ObterCampanhasAtivasAsync();

        campanhas.Should().BeEmpty();
    }
}
