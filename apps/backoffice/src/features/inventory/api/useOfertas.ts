import { useQuery } from '@tanstack/react-query';
import { chaves } from './chaves';
import { listarOfertas, obterOferta } from './ofertas';
import type { ListarOfertasParams } from '@/shared/api/types';

export function useListarOfertas(params?: ListarOfertasParams) {
  return useQuery({
    queryKey: chaves.listaOfertas(params),
    queryFn: () => listarOfertas(params),
    staleTime: 30_000,
  });
}

export function useObterOferta(id?: string) {
  return useQuery({
    queryKey: chaves.oferta(id ?? ''),
    queryFn: () => obterOferta(id!),
    enabled: !!id,
    staleTime: 30_000,
  });
}
