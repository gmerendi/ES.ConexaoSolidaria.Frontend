using FluentAssertions;
using Frontend.Models.Campanhas;
using Xunit;

namespace Frontend.Tests.Models;

public class CampanhaPublicaResponseTests
{
    private static CampanhaPublicaResponse Criar(
        decimal meta, decimal arrecadado, string dataFim = "2099-12-31") =>
        new(
            Guid.NewGuid(),
            "Campanha Teste",
            "Descrição",
            meta,
            arrecadado,
            "2020-01-01",
            dataFim,
            "ATIVA");

    [Theory]
    [InlineData(1000, 250, 25.0)]
    [InlineData(1000, 1000, 100.0)]
    [InlineData(3000, 1000, 33.3)]
    [InlineData(1000, 0, 0.0)]
    public void PercentualArrecadado_CalculaCorretamente(decimal meta, decimal arrecadado, double esperado)
    {
        var campanha = Criar(meta, arrecadado);

        campanha.PercentualArrecadado.Should().Be((decimal)esperado);
    }

    [Fact]
    public void PercentualArrecadado_NuncaUltrapassa100PorCento_MesmoComArrecadacaoAcimaDaMeta()
    {
        var campanha = Criar(meta: 1000, arrecadado: 5000);

        campanha.PercentualArrecadado.Should().Be(100.0m);
    }

    [Fact]
    public void PercentualArrecadado_ComMetaZero_RetornaZeroSemDividirPorZero()
    {
        var campanha = Criar(meta: 0, arrecadado: 500);

        campanha.PercentualArrecadado.Should().Be(0);
    }

    [Fact]
    public void DiasRestantes_ComDataFimNoFuturo_RetornaNumeroPositivoDeDias()
    {
        var dataFim = DateTime.UtcNow.AddDays(10).ToString("yyyy-MM-dd");
        var campanha = Criar(1000, 100, dataFim);

        campanha.DiasRestantes.Should().BeInRange(9, 10);
    }

    [Fact]
    public void DiasRestantes_ComDataFimNoPassado_RetornaZeroENaoNegativo()
    {
        var dataFim = DateTime.UtcNow.AddDays(-5).ToString("yyyy-MM-dd");
        var campanha = Criar(1000, 100, dataFim);

        campanha.DiasRestantes.Should().Be(0);
    }

    [Fact]
    public void DiasRestantes_ComDataFimInvalida_RetornaZero()
    {
        var campanha = Criar(1000, 100, dataFim: "data-invalida");

        campanha.DiasRestantes.Should().Be(0);
    }
}
