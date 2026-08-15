namespace SbaCars.BuildingBlocks.Observability.Sanitization;

/// <summary>
/// Marks a property of a DTO, log payload or integration event as personal/sensitive data (CPF,
/// address, declared income, documents, the buyer's name and contact details — §5.7 of the
/// foundation plan). <see cref="SensitiveDataSanitizer"/> and the processors in this namespace
/// redact any property carrying this attribute before the object reaches the OTLP exporter, a
/// structured log line, or an integration event payload.
/// </summary>
/// <remarks>
/// An attribute, not a marker interface, on purpose — the mirror image of the choice made for
/// <c>ISensitiveDataEntity</c> in <c>BuildingBlocks.Domain</c>. There the concern was "is this
/// whole entity sensitive", answered once per type with a plain type test on the hot query path.
/// Here the concern is "is this one property, among possibly many others on an otherwise
/// unremarkable DTO, sensitive", which is inherently a per-property, reflection-driven question —
/// exactly what attributes exist for. A DTO is also not owned by Domain the way an entity is: the
/// same shape (a request DTO, a log payload, an integration event record) needs to say which of
/// its fields are sensitive without being coupled to any base type or interface hierarchy at all.
/// </remarks>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class SensitiveDataAttribute : Attribute
{
}
