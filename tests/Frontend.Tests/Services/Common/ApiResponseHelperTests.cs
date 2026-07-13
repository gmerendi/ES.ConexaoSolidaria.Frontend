using FluentAssertions;
using Frontend.Models.Auth;
using Frontend.Models.Common;
using Frontend.Services.Common;
using Xunit;

namespace Frontend.Tests.Services.Common;

public class ApiResponseHelperTests
{
    // ═══ ExtrairValor<T> ═══

    [Fact]
    public void ExtrairValor_ComEnvelopeValido_RetornaValor()
    {
        var json = """
        {
            "isSuccess": true,
            "value": { "guid": "11111111-1111-1111-1111-111111111111", "nomeCompleto": "Maria Silva", "cpf": "12345678900", "email": "maria@teste.com", "perfil": "DOADOR", "status": "ATIVO" },
            "error": null,
            "errorCode": null
        }
        """;

        var resultado = ApiResponseHelper.ExtrairValor<UsuarioDto>(json);

        resultado.Should().NotBeNull();
        resultado!.NomeCompleto.Should().Be("Maria Silva");
        resultado.Email.Should().Be("maria@teste.com");
    }

    [Fact]
    public void ExtrairValor_ComValueNulo_RetornaNulo()
    {
        var json = """{ "isSuccess": false, "value": null, "error": "erro", "errorCode": "X" }""";

        var resultado = ApiResponseHelper.ExtrairValor<UsuarioDto>(json);

        resultado.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("não é json")]
    [InlineData("{ campo invalido")]
    public void ExtrairValor_ComJsonInvalido_RetornaNuloSemLancarExcecao(string jsonInvalido)
    {
        var resultado = ApiResponseHelper.ExtrairValor<UsuarioDto>(jsonInvalido);

        resultado.Should().BeNull();
    }

    // ═══ ExtrairObjetoDireto<T> ═══

    [Fact]
    public void ExtrairObjetoDireto_ComObjetoValido_RetornaObjeto()
    {
        var json = """
        { "guid": "11111111-1111-1111-1111-111111111111", "nomeCompleto": "João", "cpf": "00011122233", "email": "joao@teste.com", "perfil": "GESTOR", "status": "ATIVO" }
        """;

        var resultado = ApiResponseHelper.ExtrairObjetoDireto<UsuarioDto>(json);

        resultado.Should().NotBeNull();
        resultado!.NomeCompleto.Should().Be("João");
        resultado.Perfil.Should().Be("GESTOR");
    }

    [Fact]
    public void ExtrairObjetoDireto_ComJsonInvalido_RetornaNulo()
    {
        var resultado = ApiResponseHelper.ExtrairObjetoDireto<UsuarioDto>("{ inválido");

        resultado.Should().BeNull();
    }

    // ═══ ExtrairErros ═══

    [Fact]
    public void ExtrairErros_ComErrosDeValidacaoPorCampo_RetornaTodasAsMensagens()
    {
        var json = """
        {
            "title": "Um ou mais erros de validação ocorreram.",
            "status": 400,
            "errors": {
                "Email": ["O e-mail é obrigatório.", "O e-mail informado é inválido."],
                "Cpf": ["O CPF é obrigatório."]
            },
            "traceId": "abc-123"
        }
        """;

        var erros = ApiResponseHelper.ExtrairErros(json);

        erros.Should().HaveCount(3);
        erros.Should().Contain("O e-mail é obrigatório.");
        erros.Should().Contain("O e-mail informado é inválido.");
        erros.Should().Contain("O CPF é obrigatório.");
    }

    [Fact]
    public void ExtrairErros_ComApenasTitulo_RetornaTitulo()
    {
        var json = """{ "title": "Credenciais inválidas.", "status": 401, "errors": null, "traceId": "abc" }""";

        var erros = ApiResponseHelper.ExtrairErros(json);

        erros.Should().ContainSingle().Which.Should().Be("Credenciais inválidas.");
    }

    [Fact]
    public void ExtrairErros_ComErrorsVazio_UsaTituloComoFallback()
    {
        var json = """{ "title": "Erro genérico", "status": 500, "errors": {}, "traceId": null }""";

        var erros = ApiResponseHelper.ExtrairErros(json);

        erros.Should().ContainSingle().Which.Should().Be("Erro genérico");
    }

    [Fact]
    public void ExtrairErros_ComJsonInvalido_RetornaConteudoOriginalComoFallback()
    {
        var conteudo = "Internal Server Error - texto puro não estruturado";

        var erros = ApiResponseHelper.ExtrairErros(conteudo);

        erros.Should().ContainSingle().Which.Should().Be(conteudo);
    }

    [Fact]
    public void ExtrairErros_ComConteudoVazio_RetornaMensagemPadrao()
    {
        var erros = ApiResponseHelper.ExtrairErros("");

        erros.Should().ContainSingle().Which.Should().Be("Ocorreu um erro inesperado.");
    }

    [Fact]
    public void ExtrairErros_IgnoraMensagensEmBranco()
    {
        var json = """
        {
            "title": "Erro",
            "status": 400,
            "errors": { "Campo": ["", "  ", "Mensagem válida"] },
            "traceId": null
        }
        """;

        var erros = ApiResponseHelper.ExtrairErros(json);

        erros.Should().ContainSingle().Which.Should().Be("Mensagem válida");
    }
}
