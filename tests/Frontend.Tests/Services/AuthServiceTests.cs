using Blazored.LocalStorage;
using FluentAssertions;
using Frontend.Models.Auth;
using Frontend.Services;
using Frontend.Tests.TestHelpers;
using Moq;
using System.Net;
using Xunit;

namespace Frontend.Tests.Services;

public class AuthServiceTests
{
    // Token JWT de exemplo (header.payload.signature) com claim "role": "GESTOR"
    // e "exp" apontando para um futuro distante, gerado apenas para fins de teste
    // (assinatura não é validada pelo JwtSecurityTokenHandler.ReadJwtToken).
    private const string TokenFakeComRoleGestor =
        "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9." +
        "eyJyb2xlIjoiR0VTVE9SIiwiZXhwIjo0ODAwMDAwMDAwfQ." +
        "assinatura-fake";

    private static (AuthService Sut, FakeHttpMessageHandler Handler, Mock<ILocalStorageService> LocalStorage) CriarSut()
    {
        var handler = new FakeHttpMessageHandler();
        var http = FakeHttpMessageHandler.CriarHttpClient(handler);
        var localStorage = new Mock<ILocalStorageService>();
        var sut = new AuthService(http, localStorage.Object);
        return (sut, handler, localStorage);
    }

    [Fact]
    public async Task LoginAsync_ComCredenciaisValidas_RetornaSucessoEPersisteUsuario()
    {
        var (sut, handler, localStorage) = CriarSut();
        var guid = Guid.NewGuid();

        var responseJson = $$"""
        {
            "token": "{{TokenFakeComRoleGestor}}",
            "expiracao": "2020-01-01T00:00:00Z",
            "guid": "{{guid}}",
            "email": "usuario@teste.com",
            "status": "ATIVO"
        }
        """;
        handler.EnfileirarResposta(HttpStatusCode.OK, responseJson);

        var (sucesso, erros) = await sut.LoginAsync(new LoginRequest("usuario@teste.com", "SenhaForte123"));

        sucesso.Should().BeTrue();
        erros.Should().BeEmpty();

        // A expiração real deve vir do claim "exp" do JWT, não do campo "expiracao" da API
        // (propositalmente diferente e no passado no JSON acima, para provar a precedência).
        localStorage.Verify(l => l.SetItemAsync(
            "cs_usuario",
            It.Is<UsuarioLogado>(u =>
                u.Email == "usuario@teste.com" &&
                u.Perfil == "GESTOR" &&
                u.Expiracao > DateTime.UtcNow),
            It.IsAny<CancellationToken>()), Times.Once);

        localStorage.Verify(l => l.SetItemAsync(
            "cs_token", TokenFakeComRoleGestor, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_ComCredenciaisInvalidas_RetornaErrosDaApi()
    {
        var (sut, handler, _) = CriarSut();
        var erroJson = """{ "title": "Credenciais inválidas.", "status": 401, "errors": null, "traceId": "x" }""";
        handler.EnfileirarResposta(HttpStatusCode.Unauthorized, erroJson);

        var (sucesso, erros) = await sut.LoginAsync(new LoginRequest("usuario@teste.com", "senhaErrada"));

        sucesso.Should().BeFalse();
        erros.Should().ContainSingle().Which.Should().Be("Credenciais inválidas.");
    }

    [Fact]
    public async Task LoginAsync_QuandoRequisicaoLancaExcecao_RetornaErroDeConexao()
    {
        var (sut, handler, _) = CriarSut();
        handler.EnfileirarExcecao(new HttpRequestException("Falha de rede"));

        var (sucesso, erros) = await sut.LoginAsync(new LoginRequest("a@a.com", "123"));

        sucesso.Should().BeFalse();
        erros.Should().ContainSingle().Which.Should().StartWith("Erro de conexão:");
    }

    [Fact]
    public async Task LogoutAsync_ComTokenPresente_EnviaRequisicaoAutenticadaELimpaLocalStorage()
    {
        var (sut, handler, localStorage) = CriarSut();
        localStorage
            .Setup(l => l.GetItemAsync<string>("cs_token", It.IsAny<CancellationToken>()))
            .ReturnsAsync("meu-token");
        handler.EnfileirarResposta(HttpStatusCode.OK);

        await sut.LogoutAsync();

        handler.UltimaRequisicao!.Headers.Authorization!.Parameter.Should().Be("meu-token");
        localStorage.Verify(l => l.RemoveItemAsync("cs_token", It.IsAny<CancellationToken>()), Times.Once);
        localStorage.Verify(l => l.RemoveItemAsync("cs_usuario", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LogoutAsync_SemTokenPresente_NaoEnviaRequisicaoMasLimpaLocalStorage()
    {
        var (sut, handler, localStorage) = CriarSut();
        localStorage
            .Setup(l => l.GetItemAsync<string>("cs_token", It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        await sut.LogoutAsync();

        handler.Requisicoes.Should().BeEmpty();
        localStorage.Verify(l => l.RemoveItemAsync("cs_token", It.IsAny<CancellationToken>()), Times.Once);
        localStorage.Verify(l => l.RemoveItemAsync("cs_usuario", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LogoutAsync_QuandoRequisicaoFalha_AindaAssimLimpaLocalStorage()
    {
        var (sut, handler, localStorage) = CriarSut();
        localStorage
            .Setup(l => l.GetItemAsync<string>("cs_token", It.IsAny<CancellationToken>()))
            .ReturnsAsync("token-qualquer");
        handler.EnfileirarExcecao(new HttpRequestException("timeout"));

        await sut.LogoutAsync();

        localStorage.Verify(l => l.RemoveItemAsync("cs_token", It.IsAny<CancellationToken>()), Times.Once);
        localStorage.Verify(l => l.RemoveItemAsync("cs_usuario", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EstaLogadoAsync_ComUsuarioValido_RetornaTrue()
    {
        var (sut, _, localStorage) = CriarSut();
        localStorage
            .Setup(l => l.GetItemAsync<UsuarioLogado>("cs_usuario", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioLogado(Guid.NewGuid(), "a@a.com", "ATIVO", "tok", DateTime.UtcNow.AddHours(1), "DOADOR"));

        (await sut.EstaLogadoAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task EstaLogadoAsync_ComUsuarioExpirado_RetornaFalse()
    {
        var (sut, _, localStorage) = CriarSut();
        localStorage
            .Setup(l => l.GetItemAsync<UsuarioLogado>("cs_usuario", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioLogado(Guid.NewGuid(), "a@a.com", "ATIVO", "tok", DateTime.UtcNow.AddHours(-1), "DOADOR"));

        (await sut.EstaLogadoAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task EstaLogadoAsync_SemUsuario_RetornaFalse()
    {
        var (sut, _, localStorage) = CriarSut();
        localStorage
            .Setup(l => l.GetItemAsync<UsuarioLogado>("cs_usuario", It.IsAny<CancellationToken>()))
            .ReturnsAsync((UsuarioLogado?)null);

        (await sut.EstaLogadoAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task CadastrarDoadorAsync_ComSucesso_RetornaMensagemComDadosDoUsuario()
    {
        var (sut, handler, _) = CriarSut();
        var responseJson = """
        {
            "isSuccess": true,
            "value": { "guid": "11111111-1111-1111-1111-111111111111", "nomeCompleto": "Ana Souza", "cpf": "12345678900", "email": "ana@teste.com", "perfil": "DOADOR", "status": "ATIVO" },
            "error": null,
            "errorCode": null
        }
        """;
        handler.EnfileirarResposta(HttpStatusCode.Created, responseJson);

        var (sucesso, mensagens) = await sut.CadastrarDoadorAsync(
            new CadastroDoadorRequest("Ana Souza", "ana@teste.com", "12345678900", "SenhaForte123"));

        sucesso.Should().BeTrue();
        mensagens.Should().ContainSingle().Which.Should().Contain("Ana Souza").And.Contain("DOADOR");
    }

    [Fact]
    public async Task CadastrarDoadorAsync_ComSucessoMasSemEnvelopeReconhecivel_RetornaMensagemGenerica()
    {
        var (sut, handler, _) = CriarSut();
        handler.EnfileirarResposta(HttpStatusCode.Created, "{}");

        var (sucesso, mensagens) = await sut.CadastrarDoadorAsync(
            new CadastroDoadorRequest("Ana Souza", "ana@teste.com", "12345678900", "SenhaForte123"));

        sucesso.Should().BeTrue();
        mensagens.Should().ContainSingle().Which.Should().Be("Cadastro realizado com sucesso!");
    }

    [Fact]
    public async Task CadastrarDoadorAsync_ComEmailDuplicado_RetornaErrosDaApi()
    {
        var (sut, handler, _) = CriarSut();
        var erroJson = """
        { "title": "Erro de validação", "status": 400, "errors": { "Email": ["E-mail já cadastrado."] }, "traceId": "x" }
        """;
        handler.EnfileirarResposta(HttpStatusCode.BadRequest, erroJson);

        var (sucesso, mensagens) = await sut.CadastrarDoadorAsync(
            new CadastroDoadorRequest("Ana Souza", "ana@teste.com", "12345678900", "SenhaForte123"));

        sucesso.Should().BeFalse();
        mensagens.Should().ContainSingle().Which.Should().Be("E-mail já cadastrado.");
    }

    [Fact]
    public async Task AlterarSenhaAsync_ComSucesso_RetornaMensagemDeConfirmacao()
    {
        var (sut, handler, localStorage) = CriarSut();
        localStorage
            .Setup(l => l.GetItemAsync<string>("cs_token", It.IsAny<CancellationToken>()))
            .ReturnsAsync("token-atual");
        handler.EnfileirarResposta(HttpStatusCode.OK, "{}");

        var (sucesso, mensagens) = await sut.AlterarSenhaAsync("senhaAtual123", "senhaNova456");

        sucesso.Should().BeTrue();
        mensagens.Should().Contain("Senha alterada com sucesso!");
        handler.UltimaRequisicao!.Method.Should().Be(HttpMethod.Put);
        handler.UltimaRequisicao.Headers.Authorization!.Parameter.Should().Be("token-atual");
    }

    [Fact]
    public async Task AlterarSenhaAsync_ComSenhaAtualIncorreta_RetornaErroDaApi()
    {
        var (sut, handler, localStorage) = CriarSut();
        localStorage
            .Setup(l => l.GetItemAsync<string>("cs_token", It.IsAny<CancellationToken>()))
            .ReturnsAsync("token-atual");
        var erroJson = """{ "title": "Senha atual incorreta.", "status": 400, "errors": null, "traceId": "x" }""";
        handler.EnfileirarResposta(HttpStatusCode.BadRequest, erroJson);

        var (sucesso, mensagens) = await sut.AlterarSenhaAsync("senhaErrada", "senhaNova456");

        sucesso.Should().BeFalse();
        mensagens.Should().Contain("Senha atual incorreta.");
    }
}
