import { describe, expect, it } from 'vitest';
import { centavosParaBrl, brlParaCentavos } from './moeda';

describe('moeda formatters', () => {
  it('centavosParaBrl converte centavos para BRL corretamente', () => {
    // Note: pt-BR non-breaking space \u00a0 or standard space
    const formatted = centavosParaBrl(8790000);
    expect(formatted.replace(/\u00a0/g, ' ')).toBe('R$ 87.900,00');
    expect(centavosParaBrl(0).replace(/\u00a0/g, ' ')).toBe('R$ 0,00');
    expect(centavosParaBrl(null)).toBe('—');
    expect(centavosParaBrl(undefined)).toBe('—');
  });

  it('brlParaCentavos converte formatos de texto para centavos', () => {
    expect(brlParaCentavos('R$ 87.900,00')).toBe(8790000);
    expect(brlParaCentavos('87900,00')).toBe(8790000);
    expect(brlParaCentavos('87.900')).toBe(8790000);
    expect(brlParaCentavos('1500,50')).toBe(150050);
    expect(brlParaCentavos('')).toBe(0);
    expect(brlParaCentavos(87900)).toBe(8790000);
  });
});
