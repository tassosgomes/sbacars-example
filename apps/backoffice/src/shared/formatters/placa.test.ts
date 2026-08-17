import { describe, expect, it } from 'vitest';
import { formatarPlaca, normalizarPlaca } from './placa';

describe('placa formatters', () => {
  it('normalizarPlaca remove caracteres especiais e transforma em maiúsculas', () => {
    expect(normalizarPlaca('abc-1234')).toBe('ABC1234');
    expect(normalizarPlaca('abc1d23')).toBe('ABC1D23');
    expect(normalizarPlaca('')).toBe('');
    expect(normalizarPlaca(null)).toBe('');
  });

  it('formatarPlaca formata padrão antigo e Mercosul', () => {
    expect(formatarPlaca('ABC1234')).toBe('ABC-1234');
    expect(formatarPlaca('abc1234')).toBe('ABC-1234');
    expect(formatarPlaca('ABC1D23')).toBe('ABC1D23');
    expect(formatarPlaca(null)).toBe('—');
  });
});
