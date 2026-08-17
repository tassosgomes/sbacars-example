import { z } from 'zod';
import type { VeiculoInput } from '@/shared/api/types';
import { LIMITES } from './limites';

export const veiculoSchema = z.object({
  tipoVeiculo: z.enum(['carro-seminovo', 'carro-usado']),
  placa: z
    .string()
    .trim()
    .toUpperCase()
    .optional()
    .nullable()
    .refine(
      (val) => !val || /^[A-Z]{3}-?\d[A-Z0-9]\d{2}$/.test(val),
      'Placa inválida. Utilize o padrão tradicional (ABC-1234) ou Mercosul (ABC1D23).'
    ),
  chassi: z
    .string()
    .trim()
    .toUpperCase()
    .optional()
    .nullable()
    .refine(
      (val) => !val || /^[A-HJ-NPR-Z0-9]{17}$/i.test(val),
      'Chassi (VIN) deve conter exatamente 17 caracteres alfanuméricos válidos.'
    ),
  marca: z.string().trim().max(LIMITES.marca.max).optional().nullable(),
  modelo: z.string().trim().max(LIMITES.modelo.max).optional().nullable(),
  versao: z.string().trim().max(LIMITES.versao.max).optional().nullable(),
  anoFabricacao: z.preprocess(
    (v) => (v === '' || v === null || v === undefined ? null : Number(v)),
    z
      .number()
      .int()
      .min(LIMITES.anoMin, `Ano de fabricação deve ser no mínimo ${LIMITES.anoMin}.`)
      .max(LIMITES.anoMax, `Ano de fabricação deve ser no máximo ${LIMITES.anoMax}.`)
      .optional()
      .nullable()
  ),
  anoModelo: z.preprocess(
    (v) => (v === '' || v === null || v === undefined ? null : Number(v)),
    z
      .number()
      .int()
      .min(LIMITES.anoMin, `Ano do modelo deve ser no mínimo ${LIMITES.anoMin}.`)
      .max(LIMITES.anoMax, `Ano do modelo deve ser no máximo ${LIMITES.anoMax}.`)
      .optional()
      .nullable()
  ),
  quilometragem: z.preprocess(
    (v) => (v === '' || v === null || v === undefined ? null : Number(v)),
    z
      .number()
      .int()
      .min(0, 'Quilometragem não pode ser negativa.')
      .optional()
      .nullable()
  ),
  cor: z.string().trim().max(LIMITES.cor.max).optional().nullable(),
  combustivel: z.string().trim().max(LIMITES.combustivel.max).optional().nullable(),
  cambio: z.string().trim().max(LIMITES.cambio.max).optional().nullable(),
  localizacao: z
    .object({
      cep: z
        .string()
        .trim()
        .optional()
        .nullable()
        .refine(
          (val) => !val || /^\d{5}-?\d{3}$/.test(val),
          'CEP inválido (ex: 13010-111).'
        ),
      cidade: z.string().trim().max(LIMITES.cidade.max).optional().nullable(),
      uf: z
        .string()
        .trim()
        .toUpperCase()
        .optional()
        .nullable()
        .refine((val) => !val || /^[A-Z]{2}$/.test(val), 'UF deve conter 2 letras (ex: SP).'),
    })
    .optional(),
});

export type VeiculoFormData = z.infer<typeof veiculoSchema>;

// Checagem de tipagem estática contra o OpenAPI
type _CheckVeiculo = VeiculoFormData extends VeiculoInput ? true : never;
const _check: _CheckVeiculo = true;
void _check;
