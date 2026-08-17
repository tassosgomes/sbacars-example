/**
 * Converte valor inteiro em centavos de BRL para string formatada em Real.
 * Exemplo: 8790000 -> "R$ 87.900,00"
 */
export function centavosParaBrl(centavos: number | null | undefined): string {
  if (centavos === null || centavos === undefined || Number.isNaN(centavos)) {
    return '—';
  }
  const valor = centavos / 100;
  return valor.toLocaleString('pt-BR', {
    style: 'currency',
    currency: 'BRL',
  });
}

/**
 * Converte string formatada em BRL (ou número em texto) para inteiro em centavos.
 * Exemplo: "R$ 87.900,00" -> 8790000
 */
export function brlParaCentavos(valorBrl: string | number): number {
  if (typeof valorBrl === 'number') {
    return Math.round(valorBrl * 100);
  }
  if (!valorBrl || typeof valorBrl !== 'string') {
    return 0;
  }
  const limpo = valorBrl.trim().replace(/[^\d.,]/g, '');
  if (!limpo) return 0;

  if (limpo.includes(',')) {
    const semPontos = limpo.replace(/\./g, '');
    const comPonto = semPontos.replace(',', '.');
    const num = parseFloat(comPonto);
    return Number.isNaN(num) ? 0 : Math.round(num * 100);
  }

  if (limpo.includes('.')) {
    const partes = limpo.split('.');
    if (partes.length > 2 || (partes.length === 2 && partes[1].length === 3)) {
      const num = parseFloat(limpo.replace(/\./g, ''));
      return Number.isNaN(num) ? 0 : Math.round(num * 100);
    }
  }

  const num = parseFloat(limpo);
  return Number.isNaN(num) ? 0 : Math.round(num * 100);
}
