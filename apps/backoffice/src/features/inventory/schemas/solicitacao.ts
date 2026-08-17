import { z } from 'zod';
import { LIMITES } from './limites';

export const precoInicialSchema = z.object({
  valorCentavos: z
    .number({ message: 'Informe o preço oficial inicial' })
    .min(100, 'Preço deve ser maior que zero (mínimo R$ 1,00)'),
});

export const solicitarPrecoSchema = z.object({
  novoPrecoCentavos: z
    .number({ message: 'Informe o novo preço oficial' })
    .min(100, 'Preço deve ser maior que zero (mínimo R$ 1,00)'),
  justificativa: z
    .string({ message: 'A justificativa é obrigatória' })
    .trim()
    .min(5, 'A justificativa deve ter no mínimo 5 caracteres')
    .max(LIMITES.justificativaSolicitacao, `Máximo de ${LIMITES.justificativaSolicitacao} caracteres`),
});

export const alterarDisponibilidadeSchema = z.object({
  novoEstado: z.enum(['disponivel', 'reservado', 'vendido']),
  observacao: z.string().trim().max(LIMITES.observacaoDisponibilidade).optional().nullable(),
  justificativa: z.string().trim().max(LIMITES.justificativaSolicitacao).optional().nullable(),
});

export const rejeitarSolicitacaoSchema = z.object({
  justificativa: z
    .string({ message: 'O motivo da rejeição é obrigatório' })
    .trim()
    .min(5, 'O motivo deve conter no mínimo 5 caracteres')
    .max(LIMITES.motivoRejeicao, `Máximo de ${LIMITES.motivoRejeicao} caracteres`),
});

export type PrecoInicialFormData = z.infer<typeof precoInicialSchema>;
export type SolicitarPrecoFormData = z.infer<typeof solicitarPrecoSchema>;
export type AlterarDisponibilidadeFormData = z.infer<typeof alterarDisponibilidadeSchema>;
export type RejeitarSolicitacaoFormData = z.infer<typeof rejeitarSolicitacaoSchema>;
