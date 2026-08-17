namespace SbaCars.Inventory.Infrastructure.Projecoes;

/// <summary>
/// Marker namespace for read-side projections. V-06 keeps the queue projection in
/// <c>SolicitacaoRepository</c> so the query and its pagination stay together; this file makes
/// the infrastructure projection boundary explicit for the subsequent decision slice.
/// </summary>
internal static class InventoryProjectionExpressions
{
}
