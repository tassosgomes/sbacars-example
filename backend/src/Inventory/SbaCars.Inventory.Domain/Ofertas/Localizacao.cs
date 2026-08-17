using SbaCars.BuildingBlocks.Domain;

namespace SbaCars.Inventory.Domain.Ofertas;

/// <summary>Known location of the vehicle. All fields are optional while preparing a listing.</summary>
public sealed class Localizacao : ValueObject
{
    private Localizacao()
    {
    }

    public Localizacao(string? cep, string? cidade, string? uf)
    {
        Cep = NormalizeCep(cep);
        Cidade = Normalize(cidade);
        Uf = Normalize(uf)?.ToUpperInvariant();
    }

    public string? Cep { get; private set; }

    public string? Cidade { get; private set; }

    public string? Uf { get; private set; }

    public bool EstaCompleta => Cep is { Length: 8 } cep &&
                                cep.All(char.IsDigit) &&
                                !string.IsNullOrWhiteSpace(Cidade) &&
                                Uf is { Length: 2 };

    public static Localizacao Vazia() => new(null, null, null);

    internal Localizacao ComAlteracao(LocalizacaoPatch? patch)
    {
        if (patch is null)
        {
            return Vazia();
        }

        return new Localizacao(
            patch.CepInformado ? patch.Cep : Cep,
            patch.CidadeInformada ? patch.Cidade : Cidade,
            patch.UfInformada ? patch.Uf : Uf);
    }

    internal void Aplicar(Localizacao localizacao)
    {
        ArgumentNullException.ThrowIfNull(localizacao);
        Cep = localizacao.Cep;
        Cidade = localizacao.Cidade;
        Uf = localizacao.Uf;
    }

    protected override IEnumerable<object?> GetEqualityComponents() => [Cep, Cidade, Uf];

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? NormalizeCep(string? value)
    {
        var normalized = Normalize(value);
        return normalized?.Replace("-", string.Empty, StringComparison.Ordinal);
    }
}
