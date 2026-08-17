/**
 * Formata string ISO para DD/MM/AAAA.
 */
export function formatarData(isoString?: string | null): string {
  if (!isoString) return '—';
  try {
    const d = new Date(isoString);
    if (Number.isNaN(d.getTime())) return '—';
    return d.toLocaleDateString('pt-BR', { timeZone: 'UTC' });
  } catch {
    return '—';
  }
}

/**
 * Formata string ISO para DD/MM/AAAA às HH:mm.
 */
export function formatarDataHora(isoString?: string | null): string {
  if (!isoString) return '—';
  try {
    const d = new Date(isoString);
    if (Number.isNaN(d.getTime())) return '—';
    const data = d.toLocaleDateString('pt-BR');
    const hora = d.toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' });
    return `${data} às ${hora}`;
  } catch {
    return '—';
  }
}

/**
 * Formata idade relativa amigável para exibição (ex: "4h", "1d 6h").
 */
export function formatarIdadeRelativa(isoString?: string | null): string {
  if (!isoString) return '—';
  try {
    const d = new Date(isoString);
    if (Number.isNaN(d.getTime())) return '—';
    const agora = Date.now();
    const diffMs = agora - d.getTime();
    if (diffMs < 0) return 'agora';

    const diffMinutos = Math.floor(diffMs / (1000 * 60));
    if (diffMinutos < 60) {
      return `${diffMinutos}m`;
    }

    const diffHoras = Math.floor(diffMinutos / 60);
    if (diffHoras < 24) {
      return `${diffHoras}h`;
    }

    const diffDias = Math.floor(diffHoras / 24);
    const horasRestantes = diffHoras % 24;
    if (horasRestantes > 0) {
      return `${diffDias}d ${horasRestantes}h`;
    }
    return `${diffDias}d`;
  } catch {
    return '—';
  }
}
