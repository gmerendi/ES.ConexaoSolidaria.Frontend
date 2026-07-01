namespace Frontend.Models.Doacoes;

public record DoacaoSelfDto(
    Guid GuidCampanha,
    string TituloCampanha,
    decimal ValorDoacao,
    string DataDoacao);

public record ObterDoacoesSelfResponse(IEnumerable<DoacaoSelfDto> Doacoes);

public record DoacaoCampanhaDto(
    Guid GuidUsuario,
    string NomeUsuario,
    string EmailUsuario,
    string CpfUsuario,
    Guid GuidCampanha,
    string TituloCampanha,
    decimal ValorDoacao,
    string DataDoacao);

public record ObterDoacoesCampanhaResponse(IEnumerable<DoacaoCampanhaDto> Doacoes);

public record CriarDoacaoRequest(Guid Guid, decimal Valor);

public record DoacaoRealizadaDto(
    Guid GuidCampanha,
    string TituloCampanha,
    string NomeUsuario,
    string EmailUsuario,
    decimal Valor);