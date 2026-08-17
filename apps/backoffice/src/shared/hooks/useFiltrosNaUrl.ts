import { useCallback, useMemo } from 'react';
import { useSearchParams } from 'react-router-dom';

export function useFiltrosNaUrl<T extends Record<string, unknown>>(defaultFiltros: T) {
  const [searchParams, setSearchParams] = useSearchParams();

  const filtros = useMemo(() => {
    const resultado = { ...defaultFiltros } as Record<string, unknown>;
    searchParams.forEach((value, key) => {
      if (value.includes(',') && Array.isArray(defaultFiltros[key])) {
        resultado[key] = value.split(',').filter(Boolean);
      } else if (typeof defaultFiltros[key] === 'number') {
        const parsed = Number(value);
        resultado[key] = Number.isNaN(parsed) ? defaultFiltros[key] : parsed;
      } else if (typeof defaultFiltros[key] === 'boolean') {
        resultado[key] = value === 'true';
      } else {
        resultado[key] = value;
      }
    });
    return resultado as T;
  }, [searchParams, defaultFiltros]);

  const setFiltro = useCallback(
    <K extends keyof T>(chave: K, valor: T[K] | null | undefined) => {
      setSearchParams((prev) => {
        const novo = new URLSearchParams(prev);
        if (valor === null || valor === undefined || valor === '' || (Array.isArray(valor) && valor.length === 0)) {
          novo.delete(String(chave));
        } else if (Array.isArray(valor)) {
          novo.set(String(chave), valor.join(','));
        } else {
          novo.set(String(chave), String(valor));
        }
        // Ao mudar filtro que não é página, volta para página 1
        if (chave !== 'page' && novo.has('page')) {
          novo.set('page', '1');
        }
        return novo;
      });
    },
    [setSearchParams]
  );

  const limparFiltros = useCallback(() => {
    setSearchParams(new URLSearchParams());
  }, [setSearchParams]);

  return { filtros, setFiltro, limparFiltros };
}
