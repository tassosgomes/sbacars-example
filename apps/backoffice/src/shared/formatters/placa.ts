/**
 * Normaliza e formata placa brasileira (padrão antigo ABC-1234 ou Mercosul ABC1D23).
 */
export function normalizarPlaca(placa?: string | null): string {
  if (!placa) return '';
  return placa.toUpperCase().replace(/[^A-Z0-9]/g, '');
}

export function formatarPlaca(placa?: string | null): string {
  const limpa = normalizarPlaca(placa);
  if (!limpa) return '—';

  // Padrão antigo: ABC1234 -> ABC-1234
  if (/^[A-Z]{3}\d{4}$/.test(limpa)) {
    return `${limpa.slice(0, 3)}-${limpa.slice(3)}`;
  }

  // Padrão Mercosul: ABC1D23
  return limpa;
}
