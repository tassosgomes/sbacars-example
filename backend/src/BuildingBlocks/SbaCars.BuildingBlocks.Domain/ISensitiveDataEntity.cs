namespace SbaCars.BuildingBlocks.Domain;

/// <summary>
/// Marks an entity as carrying sensitive personal data (CPF, address, declared income,
/// supporting documents, the buyer's name and contact details — §5.7 of the foundation plan,
/// RN-12 of D04 and RN-03 of D03). Every read and write of an entity implementing this
/// interface is recorded by <c>SensitiveDataAuditInterceptor</c> in
/// <c>BuildingBlocks.Persistence</c>.
/// </summary>
/// <remarks>
/// A marker interface, not an attribute or mapping configuration, on purpose:
/// <list type="bullet">
/// <item>Sensitivity is a fact about what the entity <em>is</em>, not about how it happens to be
/// mapped or logged — it belongs next to the entity's declaration, in Domain, the one layer every
/// service's Infrastructure ultimately depends on. Configuring it in an
/// <c>IEntityTypeConfiguration&lt;T&gt;</c> would put the fact in Infrastructure instead, invisible
/// to anyone reading the entity itself, and easy to forget the day a second service maps the same
/// kind of entity.</item>
/// <item>An interceptor has to answer "is this sensitive?" once per materialized row, on the hot
/// query path. <c>instance is ISensitiveDataEntity</c> is a plain runtime type test; an
/// attribute-based equivalent needs reflection (and a cache to make it cheap) for the same
/// answer. For entity-level marking the interface is strictly less machinery for the same
/// guarantee.</item>
/// </list>
/// This is deliberately a different mechanism from <c>SensitiveDataAttribute</c> in
/// <c>BuildingBlocks.Observability</c>, which marks individual <em>properties</em> of arbitrary
/// DTOs (not necessarily entities) for log/trace/event sanitization — a field-level, reflection-driven
/// concern where an attribute is the natural fit. The two mechanisms solve different problems: this
/// one says "reading or writing this row is a sensitive-data access, audit it"; the other says
/// "this property's value must never leave the process unmasked".
/// </remarks>
public interface ISensitiveDataEntity
{
}
