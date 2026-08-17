import { useMutation, useQueryClient } from '@tanstack/react-query';
import { chaves } from './chaves';
import {
  cadastrarVeiculo,
  atualizarVeiculo,
  excluirOferta,
  definirPrecoInicial,
  substituirFatos,
  alterarDisponibilidade,
  abrirSolicitacao,
} from './ofertas';
import type {
  VeiculoInput,
  VeiculoPatchInput,
  DefinirPrecoInicialInput,
  FatosInput,
  AlterarDisponibilidadeInput,
  AbrirSolicitacaoInput,
} from '@/shared/api/types';

export function useCadastrarVeiculo() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: VeiculoInput) => cadastrarVeiculo(input),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: chaves.ofertas });
    },
  });
}

export function useAtualizarVeiculo(ofertaId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: VeiculoPatchInput) => atualizarVeiculo(ofertaId, input),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: chaves.oferta(ofertaId) });
      qc.invalidateQueries({ queryKey: chaves.ofertas });
    },
  });
}

export function useExcluirOferta() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (ofertaId: string) => excluirOferta(ofertaId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: chaves.ofertas });
    },
  });
}

export function useDefinirPrecoInicial(ofertaId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: DefinirPrecoInicialInput) => definirPrecoInicial(ofertaId, input),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: chaves.oferta(ofertaId) });
      qc.invalidateQueries({ queryKey: chaves.ofertas });
    },
  });
}

export function useSubstituirFatos(ofertaId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: FatosInput) => substituirFatos(ofertaId, input),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: chaves.oferta(ofertaId) });
      qc.invalidateQueries({ queryKey: chaves.ofertas });
    },
  });
}

export function useAlterarDisponibilidade(ofertaId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: AlterarDisponibilidadeInput) => alterarDisponibilidade(ofertaId, input),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: chaves.oferta(ofertaId) });
      qc.invalidateQueries({ queryKey: chaves.ofertas });
    },
  });
}

export function useAbrirSolicitacao(ofertaId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: AbrirSolicitacaoInput) => abrirSolicitacao(ofertaId, input),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: chaves.oferta(ofertaId) });
      qc.invalidateQueries({ queryKey: chaves.ofertas });
      qc.invalidateQueries({ queryKey: chaves.solicitacoes });
    },
  });
}
