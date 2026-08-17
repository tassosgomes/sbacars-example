using System.Text.Json.Serialization;
using SbaCars.Inventory.Application.Ofertas.AlterarDisponibilidade;
using SbaCars.Inventory.Application.Ofertas.AtualizarVeiculo;
using SbaCars.Inventory.Application.Ofertas.CadastrarVeiculo;
using SbaCars.Inventory.Application.Ofertas.DefinirPrecoInicial;
using SbaCars.Inventory.Application.Ofertas.SubstituirFatos;
using SbaCars.Inventory.Domain.Ofertas;

namespace SbaCars.Inventory.Api.Contracts;

public sealed record CadastrarVeiculoRequest
{
    public string? Placa { get; init; }

    public string? Chassi { get; init; }

    public string? TipoVeiculo { get; init; }

    public string? Marca { get; init; }

    public string? Modelo { get; init; }

    public string? Versao { get; init; }

    public int? AnoFabricacao { get; init; }

    public int? AnoModelo { get; init; }

    public int? Quilometragem { get; init; }

    public string? Cor { get; init; }

    public string? Combustivel { get; init; }

    public string? Cambio { get; init; }

    public LocalizacaoRequest? Localizacao { get; init; }

    public CadastrarVeiculoCommand ToCommand() => new()
    {
        Placa = Placa,
        Chassi = Chassi,
        TipoVeiculo = TipoVeiculo,
        Marca = Marca,
        Modelo = Modelo,
        Versao = Versao,
        AnoFabricacao = AnoFabricacao,
        AnoModelo = AnoModelo,
        Quilometragem = Quilometragem,
        Cor = Cor,
        Combustivel = Combustivel,
        Cambio = Cambio,
        Localizacao = Localizacao is null
            ? null
            : new LocalizacaoInput(Localizacao.Cep, Localizacao.Cidade, Localizacao.Uf),
    };
}

public sealed record LocalizacaoRequest
{
    public string? Cep { get; init; }

    public string? Cidade { get; init; }

    public string? Uf { get; init; }
}

/// <summary>Request for the first official price, expressed as positive BRL cents.</summary>
public sealed record DefinirPrecoInicialRequest
{
    public long ValorCentavos { get; init; }

    public DefinirPrecoInicialCommand ToCommand(Guid ofertaId) => new()
    {
        OfertaId = ofertaId,
        ValorCentavos = ValorCentavos,
    };
}

/// <summary>Request for one explicit availability transition.</summary>
public sealed record AlterarDisponibilidadeRequest
{
    public string? NovoEstado { get; init; }

    public string? Observacao { get; init; }

    public AlterarDisponibilidadeCommand ToCommand(Guid ofertaId) => new()
    {
        OfertaId = ofertaId,
        NovoEstado = EstadoDisponibilidadeExtensions.Parse(NovoEstado),
        Observacao = Observacao,
    };
}

/// <summary>Replaces all three known-fact blocks in one request.</summary>
public sealed record SubstituirFatosRequest
{
    public BlocoFatoRequest? Origem { get; init; }

    public BlocoFatoRequest? Condicao { get; init; }

    public BlocoFatoRequest? Historico { get; init; }

    public bool ConfirmaSuspensao { get; init; }

    public SubstituirFatosCommand ToCommand(Guid ofertaId) => new()
    {
        OfertaId = ofertaId,
        Origem = Origem?.ToInput(),
        Condicao = Condicao?.ToInput(),
        Historico = Historico?.ToInput(),
        ConfirmaSuspensao = ConfirmaSuspensao,
    };
}

/// <summary>Content or an explicit limitation for one known-fact block.</summary>
public sealed record BlocoFatoRequest
{
    public bool Indisponivel { get; init; }

    public string? Descricao { get; init; }

    public string? Fonte { get; init; }

    public Guid? EvidenciaId { get; init; }

    public string? LimitacaoDeclarada { get; init; }

    public BlocoFatoInput ToInput() => new()
    {
        Indisponivel = Indisponivel,
        Descricao = Descricao,
        Fonte = Fonte,
        EvidenciaId = EvidenciaId,
        LimitacaoDeclarada = LimitacaoDeclarada,
    };
}

/// <summary>
/// PATCH body that preserves the distinction between an omitted property and an explicit JSON
/// null. The distinction is required by the API contract: omitted values stay unchanged, while
/// null clears the corresponding vehicle field.
/// </summary>
public sealed class AtualizarVeiculoRequest
{
    private string? _tipoVeiculo;
    private string? _placa;
    private string? _chassi;
    private string? _marca;
    private string? _modelo;
    private string? _versao;
    private int? _anoFabricacao;
    private int? _anoModelo;
    private int? _quilometragem;
    private string? _cor;
    private string? _combustivel;
    private string? _cambio;
    private LocalizacaoPatchRequest? _localizacao;

    public string? TipoVeiculo
    {
        get => _tipoVeiculo;
        init
        {
            _tipoVeiculo = value;
            TipoVeiculoInformado = true;
        }
    }

    [JsonIgnore]
    public bool TipoVeiculoInformado { get; private set; }

    public string? Placa
    {
        get => _placa;
        init
        {
            _placa = value;
            PlacaInformada = true;
        }
    }

    [JsonIgnore]
    public bool PlacaInformada { get; private set; }

    public string? Chassi
    {
        get => _chassi;
        init
        {
            _chassi = value;
            ChassiInformado = true;
        }
    }

    [JsonIgnore]
    public bool ChassiInformado { get; private set; }

    public string? Marca
    {
        get => _marca;
        init
        {
            _marca = value;
            MarcaInformada = true;
        }
    }

    [JsonIgnore]
    public bool MarcaInformada { get; private set; }

    public string? Modelo
    {
        get => _modelo;
        init
        {
            _modelo = value;
            ModeloInformado = true;
        }
    }

    [JsonIgnore]
    public bool ModeloInformado { get; private set; }

    public string? Versao
    {
        get => _versao;
        init
        {
            _versao = value;
            VersaoInformada = true;
        }
    }

    [JsonIgnore]
    public bool VersaoInformada { get; private set; }

    public int? AnoFabricacao
    {
        get => _anoFabricacao;
        init
        {
            _anoFabricacao = value;
            AnoFabricacaoInformado = true;
        }
    }

    [JsonIgnore]
    public bool AnoFabricacaoInformado { get; private set; }

    public int? AnoModelo
    {
        get => _anoModelo;
        init
        {
            _anoModelo = value;
            AnoModeloInformado = true;
        }
    }

    [JsonIgnore]
    public bool AnoModeloInformado { get; private set; }

    public int? Quilometragem
    {
        get => _quilometragem;
        init
        {
            _quilometragem = value;
            QuilometragemInformada = true;
        }
    }

    [JsonIgnore]
    public bool QuilometragemInformada { get; private set; }

    public string? Cor
    {
        get => _cor;
        init
        {
            _cor = value;
            CorInformada = true;
        }
    }

    [JsonIgnore]
    public bool CorInformada { get; private set; }

    public string? Combustivel
    {
        get => _combustivel;
        init
        {
            _combustivel = value;
            CombustivelInformado = true;
        }
    }

    [JsonIgnore]
    public bool CombustivelInformado { get; private set; }

    public string? Cambio
    {
        get => _cambio;
        init
        {
            _cambio = value;
            CambioInformado = true;
        }
    }

    [JsonIgnore]
    public bool CambioInformado { get; private set; }

    public LocalizacaoPatchRequest? Localizacao
    {
        get => _localizacao;
        init
        {
            _localizacao = value;
            LocalizacaoInformada = true;
        }
    }

    [JsonIgnore]
    public bool LocalizacaoInformada { get; private set; }

    public bool ConfirmaSuspensao { get; init; }

    public AtualizarVeiculoCommand ToCommand(Guid ofertaId) => new()
    {
        OfertaId = ofertaId,
        TipoVeiculoInformado = TipoVeiculoInformado,
        TipoVeiculo = TipoVeiculo,
        PlacaInformada = PlacaInformada,
        Placa = Placa,
        ChassiInformado = ChassiInformado,
        Chassi = Chassi,
        MarcaInformada = MarcaInformada,
        Marca = Marca,
        ModeloInformado = ModeloInformado,
        Modelo = Modelo,
        VersaoInformada = VersaoInformada,
        Versao = Versao,
        AnoFabricacaoInformado = AnoFabricacaoInformado,
        AnoFabricacao = AnoFabricacao,
        AnoModeloInformado = AnoModeloInformado,
        AnoModelo = AnoModelo,
        QuilometragemInformada = QuilometragemInformada,
        Quilometragem = Quilometragem,
        CorInformada = CorInformada,
        Cor = Cor,
        CombustivelInformado = CombustivelInformado,
        Combustivel = Combustivel,
        CambioInformado = CambioInformado,
        Cambio = Cambio,
        LocalizacaoInformada = LocalizacaoInformada,
        Localizacao = Localizacao is null
            ? null
            : new LocalizacaoPatch
            {
                CepInformado = Localizacao.CepInformado,
                Cep = Localizacao.Cep,
                CidadeInformada = Localizacao.CidadeInformada,
                Cidade = Localizacao.Cidade,
                UfInformada = Localizacao.UfInformado,
                Uf = Localizacao.Uf,
            },
        ConfirmaSuspensao = ConfirmaSuspensao,
    };
}

public sealed class LocalizacaoPatchRequest
{
    private string? _cep;
    private string? _cidade;
    private string? _uf;

    public string? Cep
    {
        get => _cep;
        init
        {
            _cep = value;
            CepInformado = true;
        }
    }

    [JsonIgnore]
    public bool CepInformado { get; private set; }

    public string? Cidade
    {
        get => _cidade;
        init
        {
            _cidade = value;
            CidadeInformada = true;
        }
    }

    [JsonIgnore]
    public bool CidadeInformada { get; private set; }

    public string? Uf
    {
        get => _uf;
        init
        {
            _uf = value;
            UfInformado = true;
        }
    }

    [JsonIgnore]
    public bool UfInformado { get; private set; }
}
