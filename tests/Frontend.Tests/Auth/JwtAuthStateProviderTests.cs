using Blazored.LocalStorage;
using FluentAssertions;
using Frontend.Auth;
using Frontend.Models.Auth;
using Microsoft.AspNetCore.Components.Authorization;
using Moq;
using System.Security.Claims;
using Xunit;

namespace Frontend.Tests.Auth;

public class JwtAuthStateProviderTests
{
    private static UsuarioLogado CriarUsuario(DateTime expiracao, string perfil = "DOADOR") =>
        new(
            Guid.NewGuid(),
            "usuario@teste.com",
            "ATIVO",
            "token-fake",
            expiracao,
            perfil);

    [Fact]
    public async Task GetAuthenticationStateAsync_SemUsuarioNoLocalStorage_RetornaAnonimo()
    {
        var localStorage = new Mock<ILocalStorageService>();
        localStorage
            .Setup(l => l.GetItemAsync<UsuarioLogado>("cs_usuario", It.IsAny<CancellationToken>()))
            .ReturnsAsync((UsuarioLogado?)null);

        var sut = new JwtAuthStateProvider(localStorage.Object);

        var estado = await sut.GetAuthenticationStateAsync();

        estado.User.Identity!.IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_ComUsuarioValido_RetornaUsuarioAutenticado()
    {
        var usuario = CriarUsuario(DateTime.UtcNow.AddHours(1), perfil: "GESTOR");

        var localStorage = new Mock<ILocalStorageService>();
        localStorage
            .Setup(l => l.GetItemAsync<UsuarioLogado>("cs_usuario", It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);

        var sut = new JwtAuthStateProvider(localStorage.Object);

        var estado = await sut.GetAuthenticationStateAsync();

        estado.User.Identity!.IsAuthenticated.Should().BeTrue();
        estado.User.FindFirst(ClaimTypes.Email)!.Value.Should().Be(usuario.Email);
        estado.User.FindFirst(ClaimTypes.Role)!.Value.Should().Be("GESTOR");
        estado.User.FindFirst(ClaimTypes.NameIdentifier)!.Value.Should().Be(usuario.Guid.ToString());
        estado.User.FindFirst("status")!.Value.Should().Be("ATIVO");
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_ComTokenExpirado_RetornaAnonimoELimpaLocalStorage()
    {
        var usuario = CriarUsuario(DateTime.UtcNow.AddMinutes(-5));

        var localStorage = new Mock<ILocalStorageService>();
        localStorage
            .Setup(l => l.GetItemAsync<UsuarioLogado>("cs_usuario", It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);

        var sut = new JwtAuthStateProvider(localStorage.Object);

        var estado = await sut.GetAuthenticationStateAsync();

        estado.User.Identity!.IsAuthenticated.Should().BeFalse();
        localStorage.Verify(l => l.RemoveItemAsync("cs_token", It.IsAny<CancellationToken>()), Times.Once);
        localStorage.Verify(l => l.RemoveItemAsync("cs_usuario", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_QuandoLocalStorageLancaExcecao_RetornaAnonimo()
    {
        var localStorage = new Mock<ILocalStorageService>();
        localStorage
            .Setup(l => l.GetItemAsync<UsuarioLogado>("cs_usuario", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("localStorage indisponível (ex.: pré-renderização)"));

        var sut = new JwtAuthStateProvider(localStorage.Object);

        var estado = await sut.GetAuthenticationStateAsync();

        estado.User.Identity!.IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public void NotificarMudancaEstado_DisparaEventoAuthenticationStateChanged()
    {
        var localStorage = new Mock<ILocalStorageService>();
        localStorage
            .Setup(l => l.GetItemAsync<UsuarioLogado>("cs_usuario", It.IsAny<CancellationToken>()))
            .ReturnsAsync((UsuarioLogado?)null);

        var sut = new JwtAuthStateProvider(localStorage.Object);

        Task<AuthenticationState>? tarefaRecebida = null;
        sut.AuthenticationStateChanged += tarefa => tarefaRecebida = tarefa;

        sut.NotificarMudancaEstado();

        tarefaRecebida.Should().NotBeNull();
    }
}
