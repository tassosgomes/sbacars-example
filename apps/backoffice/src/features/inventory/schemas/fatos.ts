import { z } from 'zod';
import type { FatosInput } from '@/shared/api/types';
import { LIMITES } from './limites';

export const blocoFatoSchema = z
  .object({
    indisponivel: z.boolean(),
    descricao: z.string().trim().max(LIMITES.descricaoFato).optional().nullable(),
    fonte: z.string().trim().max(LIMITES.fonteFato).optional().nullable(),
    evidenciaId: z.string().uuid().optional().nullable(),
    limitacaoDeclarada: z.string().trim().max(LIMITES.limitacaoDeclarada).optional().nullable(),
  })
  .refine(
    (b) => !b.indisponivel || (typeof b.limitacaoDeclarada === 'string' && b.limitacaoDeclarada.trim().length > 0),
    {
      message: 'Declare a limitação quando a informação estiver indisponível.',
      path: ['limitacaoDeclarada'],
    }
  );

export const fatosSchema = z.object({
  origem: blocoFatoSchema,
  condicao: blocoFatoSchema,
  historico: blocoFatoSchema,
  confirmaSuspensao: z.boolean(),
});

export type FatosFormData = z.infer<typeof fatosSchema>;

// Checagem de tipagem estática contra o OpenAPI
type _CheckFatos = FatosFormData extends FatosInput ? true : never;
const _check: _CheckFatos = true;
void _check;
