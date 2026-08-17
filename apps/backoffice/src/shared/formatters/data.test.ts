import { describe, expect, it } from 'vitest';
import { formatarData, formatarIdadeRelativa } from './data';

describe('data formatters', () => {
  it('formatarData formata corretamente strings ISO', () => {
    expect(formatarData('2026-08-12T14:22:05Z')).toBe('12/08/2026');
    expect(formatarData(null)).toBe('—');
    expect(formatarData(undefined)).toBe('—');
  });

  it('formatarIdadeRelativa calcula tempos amigáveis', () => {
    const agora = Date.now();
    const duasHorasAtras = new Date(agora - 2 * 60 * 60 * 1000).toISOString();
    expect(formatarIdadeRelativa(duasHorasAtras)).toBe('2h');
    expect(formatarIdadeRelativa(null)).toBe('—');
  });
});
