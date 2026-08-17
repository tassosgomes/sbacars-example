using SbaCars.BuildingBlocks.Domain;

namespace SbaCars.Inventory.Domain.Ofertas;

/// <summary>Records who changed a curated value and when.</summary>
public sealed class Autoria : ValueObject
{
    private Autoria()
    {
        UsuarioId = string.Empty;
        Nome = string.Empty;
    }

    public Autoria(string usuarioId, string? nome, DateTimeOffset em)
    {
        UsuarioId = string.IsNullOrWhiteSpace(usuarioId) ? "system" : usuarioId.Trim();
        Nome = string.IsNullOrWhiteSpace(nome) ? UsuarioId : nome.Trim();
        Em = em.ToUniversalTime();
    }

    public string UsuarioId { get; private set; }

    public string Nome { get; private set; }

    public DateTimeOffset Em { get; private set; }

    public static Autoria System(DateTimeOffset em) => new("system", "system", em);

    public Autoria Copiar() => new(UsuarioId, Nome, Em);

    protected override IEnumerable<object?> GetEqualityComponents() => [UsuarioId, Nome, Em];
}
