using Blazored.LocalStorage;
using FluentAssertions;
using Frontend.Services;
using Frontend.Tests.TestHelpers;
using Moq;
using System.Net;
using Xunit;

namespace Frontend.Tests.Services;

public class UsuarioServiceTests
{
    private static (UsuarioService Sut, FakeHttpMessageHandler Handler) CriarSut()
    {
        var handler = new FakeHttpMessageHandler();
        var http = FakeHttpMessageHandler.CriarHttpClient(handler);

        var localStorage = new Mock<ILocalStorageService>();
        localStorage
            .Setup(l => l.GetItemAsync<string>("cs_token", It.IsAny<CancellationToken>()))
            .ReturnsAsync("token-usuario");

        var authSvc = new AuthService(http, localStorage.Object);
        return (new UsuarioService(http, authSvc), handler);
    }

    [Fact]
    public async Task ObterPorEmailAsync_ComUsuarioExistente_RetornaDadosDoUsuario()
    {
        var (sut, handler) = CriarSut();
        var json = """{ "guid": "11111111-1111-1111-1111-111111111111", "nomeCompleto": "Ana", "cpf": "123", "email": "ana@teste.com", "perfil": "DOADOR", "status": "ATIVO" }""";
        handler.EnfileirarResposta(HttpStatusCode.OK, json);

        var (sucesso, usuario, erros) = await sut.ObterPorEmailAsync("ana@teste.com");

        sucesso.Should().BeTrue();
        usuario!.NomeCompleto.Should().Be("Ana");
        handler.UltimaRequisicao!.RequestUri!.PathAndQuery.Should().Be("/api/v1/usuario?Email=ana%40teste.com");
    }

    [Fact]
    public async Task ObterPorEmailAsync_ComUsuarioInexistente_RetornaErro()
    {
        var (sut, handler) = CriarSut();
        handler.EnfileirarResposta(HttpStatusCode.NotFound, """{ "title": "Usuário não encontrado.", "status": 404, "errors": null, "traceId": "x" }""");

        var (sucesso, usuario, erros) = await sut.ObterPorEmailAsync("naoexiste@teste.com");

        sucesso.Should().BeFalse();
        usuario.Should().BeNull();
        erros.Should().Contain("Usuário não encontrado.");
    }

    [Fact]
    public async Task AlterarAsync_ComRespostaComEnvelope_RetornaUsuarioAtualizado()
    {
        var (sut, handler) = CriarSut();
        var json = """
        {
            "isSuccess": true,
            "value": { "guid": "11111111-1111-1111-1111-111111111111", "nomeCompleto": "Ana Nova", "cpf": "999", "email": "ana@teste.com", "perfil": "DOADOR", "status": "ATIVO" },
            "error": null,
            "errorCode": null
        }
        """;
        handler.EnfileirarResposta(HttpStatusCode.OK, json);

        var (sucesso, usuario, erros) = await sut.AlterarAsync("Ana Nova", "999");

        sucesso.Should().BeTrue();
        usuario!.NomeCompleto.Should().Be("Ana Nova");
    }

    [Fact]
    public async Task AlterarAsync_ComRespostaSemEnvelope_RetornaUsuarioAtualizado()
    {
        var (sut, handler) = CriarSut();
        var json = """{ "guid": "11111111-1111-1111-1111-111111111111", "nomeCompleto": "Ana Nova", "cpf": "999", "email": "ana@teste.com", "perfil": "DOADOR", "status": "ATIVO" }""";
        handler.EnfileirarResposta(HttpStatusCode.OK, json);

        var (sucesso, usuario, erros) = await sut.AlterarAsync("Ana Nova", "999");

        sucesso.Should().BeTrue();
        usuario!.Cpf.Should().Be("999");
    }

    [Fact]
    public async Task AlterarAsync_MontaUrlComParametrosEscapados()
    {
        var (sut, handler) = CriarSut();
        handler.EnfileirarResposta(HttpStatusCode.OK, """{ "guid": "11111111-1111-1111-1111-111111111111", "nomeCompleto": "João da Silva", "cpf": "111.222.333-44", "email": "a@a.com", "perfil": "DOADOR", "status": "ATIVO" }""");

        await sut.AlterarAsync("João da Silva", "111.222.333-44");

        handler.UltimaRequisicao!.RequestUri!.PathAndQuery
            .Should().Be("/api/v1/usuario/alterar?NomeCompleto=Jo%C3%A3o%20da%20Silva&Cpf=111.222.333-44");
    }

    [Fact]
    public async Task ExcluirAsync_ComSucesso_RetornaMensagemDeConfirmacao()
    {
        var (sut, handler) = CriarSut();
        handler.EnfileirarResposta(HttpStatusCode.OK, "{}");

        var (sucesso, mensagens) = await sut.ExcluirAsync("ana@teste.com");

        sucesso.Should().BeTrue();
        mensagens.Should().Contain("Conta excluída com sucesso.");
        handler.UltimaRequisicao!.Method.Should().Be(HttpMethod.Delete);
    }

    [Theory]
    [InlineData("SuspenderAsync", "suspender")]
    [InlineData("AtivarAsync", "ativar")]
    [InlineData("AlterarParaGestorAsync", "alterar-para-gestor")]
    [InlineData("AlterarParaDoadorAsync", "alterar-para-doador")]
    public async Task AcoesAdministrativas_ComSucesso_ChamamRotaCorretaComMetodoPut(string metodo, string segmentoUrl)
    {
        var (sut, handler) = CriarSut();
        handler.EnfileirarResposta(HttpStatusCode.OK, "{}");

        var tarefa = metodo switch
        {
            "SuspenderAsync" => sut.SuspenderAsync("ana@teste.com"),
            "AtivarAsync" => sut.AtivarAsync("ana@teste.com"),
            "AlterarParaGestorAsync" => sut.AlterarParaGestorAsync("ana@teste.com"),
            "AlterarParaDoadorAsync" => sut.AlterarParaDoadorAsync("ana@teste.com"),
            _ => throw new InvalidOperationException()
        };

        var (sucesso, mensagens) = await tarefa;

        sucesso.Should().BeTrue();
        mensagens.Should().Contain("Operação realizada com sucesso!");
        handler.UltimaRequisicao!.Method.Should().Be(HttpMethod.Put);
        handler.UltimaRequisicao.RequestUri!.PathAndQuery.Should().Be($"/api/v1/usuario/{segmentoUrl}?Email=ana%40teste.com");
    }

    [Fact]
    public async Task SuspenderAsync_ComErroDaApi_RetornaMensagensDeErro()
    {
        var (sut, handler) = CriarSut();
        handler.EnfileirarResposta(HttpStatusCode.BadRequest, """{ "title": "Usuário já está suspenso.", "status": 400, "errors": null, "traceId": "x" }""");

        var (sucesso, mensagens) = await sut.SuspenderAsync("ana@teste.com");

        sucesso.Should().BeFalse();
        mensagens.Should().Contain("Usuário já está suspenso.");
    }
}
