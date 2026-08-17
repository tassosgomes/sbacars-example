import type { UseFormRegister, FieldErrors, UseFormSetValue, UseFormWatch } from 'react-hook-form';
import { UploadEvidencia } from './UploadEvidencia';
import type { FatosFormData } from '../schemas/fatos';
import type { BlocoFato, BlocoFatoTipo } from '@/shared/api/types';

export interface BlocoFatoFormProps {
  ofertaId: string;
  tipo: BlocoFatoTipo;
  titulo: string;
  descricaoAjuda: string;
  blocoAtual?: BlocoFato;
  register: UseFormRegister<FatosFormData>;
  errors: FieldErrors<FatosFormData>;
  setValue: UseFormSetValue<FatosFormData>;
  watch: UseFormWatch<FatosFormData>;
}

export function BlocoFatoForm({
  ofertaId,
  tipo,
  titulo,
  descricaoAjuda,
  blocoAtual,
  register,
  errors,
  setValue,
  watch,
}: BlocoFatoFormProps) {
  const indisponivel = watch(`${tipo}.indisponivel`);
  const blocoErrors = errors[tipo];

  return (
    <div className="rounded-xl border border-border bg-surface p-6 shadow-xs space-y-4">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between border-b border-border pb-3 gap-2">
        <div>
          <h2 className="text-base font-bold text-neutral-900">{titulo}</h2>
          <p className="text-xs text-neutral-600 mt-0.5">{descricaoAjuda}</p>
        </div>

        <label className="flex items-center gap-2 cursor-pointer text-xs font-semibold text-neutral-800 bg-neutral-100 hover:bg-neutral-200 px-3 py-1.5 rounded-lg transition-colors shrink-0">
          <input
            type="checkbox"
            {...register(`${tipo}.indisponivel`)}
            className="rounded border-border text-primary focus:ring-primary h-4 w-4"
          />
          <span>Informação indisponível</span>
        </label>
      </div>

      {indisponivel ? (
        /* Estado: Indisponível -> Exige Limitação Declarada */
        <div className="space-y-3 bg-amber-50/60 border border-amber-200 rounded-lg p-4">
          <div>
            <label
              htmlFor={`${tipo}-limitacao`}
              className="block text-xs font-bold uppercase tracking-wider text-amber-900 mb-1"
            >
              Declaração de Limitação *
            </label>
            <p className="text-xs text-amber-800 mb-2">
              Explique a limitação com transparência para o comprador (ex: &quot;Não foi possível consultar
              o histórico de sinistros devido a indisponibilidade na base de dados estadual&quot;).
            </p>
            <textarea
              id={`${tipo}-limitacao`}
              rows={3}
              maxLength={500}
              placeholder="Descreva a limitação com clareza para o comprador…"
              {...register(`${tipo}.limitacaoDeclarada`)}
              className="w-full rounded-lg border border-amber-300 bg-surface px-3.5 py-2 text-sm text-neutral-900 focus:border-amber-600 focus:outline-none focus:ring-1 focus:ring-amber-600"
            />
            {blocoErrors?.limitacaoDeclarada && (
              <p className="text-xs text-danger mt-1">
                {blocoErrors.limitacaoDeclarada.message}
              </p>
            )}
          </div>
        </div>
      ) : (
        /* Estado: Disponível -> Descrição, Fonte e Evidência */
        <div className="space-y-4">
          <div>
            <label
              htmlFor={`${tipo}-descricao`}
              className="block text-xs font-bold uppercase tracking-wider text-neutral-700 mb-1"
            >
              Descrição dos Fatos
            </label>
            <textarea
              id={`${tipo}-descricao`}
              rows={3}
              maxLength={1000}
              placeholder="Descreva os fatos apurados sobre este aspecto do veículo…"
              {...register(`${tipo}.descricao`)}
              className="w-full rounded-lg border border-border bg-background px-3.5 py-2 text-sm text-neutral-900 focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
            />
            {blocoErrors?.descricao && (
              <p className="text-xs text-danger mt-1">{blocoErrors.descricao.message}</p>
            )}
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <label
                htmlFor={`${tipo}-fonte`}
                className="block text-xs font-bold uppercase tracking-wider text-neutral-700 mb-1"
              >
                Fonte da Informação
              </label>
              <input
                id={`${tipo}-fonte`}
                type="text"
                maxLength={200}
                placeholder="Ex: Manual carimbado, Laudo Dekra 04/2026"
                {...register(`${tipo}.fonte`)}
                className="w-full rounded-lg border border-border bg-background px-3.5 py-2 text-sm text-neutral-900 focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
              />
              {blocoErrors?.fonte && (
                <p className="text-xs text-danger mt-1">{blocoErrors.fonte.message}</p>
              )}
            </div>

            <div>
              <label className="block text-xs font-bold uppercase tracking-wider text-neutral-700 mb-1">
                Evidência Comprobatória (Opcional)
              </label>
              <UploadEvidencia
                ofertaId={ofertaId}
                evidenciaAtual={blocoAtual?.evidencia}
                evidenciaIdValor={watch(`${tipo}.evidenciaId`)}
                onEvidenciaAlterada={(id) => setValue(`${tipo}.evidenciaId`, id)}
              />
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
