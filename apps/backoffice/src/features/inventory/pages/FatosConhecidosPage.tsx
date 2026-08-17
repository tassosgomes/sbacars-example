import { useEffect, useState } from 'react';
import { useNavigate, useParams, Link } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Button } from '@sbacars/ui';
import { fatosSchema, type FatosFormData } from '../schemas/fatos';
import { useObterOferta } from '../api/useOfertas';
import { useSubstituirFatos } from '../api/useMutacoesOferta';
import { useMutacaoComSuspensao } from '../api/useMutacaoComSuspensao';
import { BlocoFatoForm } from '../components/BlocoFatoForm';
import { DialogoSuspensao } from '../components/DialogoSuspensao';
import { ErrorState } from '@/shared/components/ErrorState';
import { ApiError } from '@/shared/api/problemDetails';
import { formatarPlaca } from '@/shared/formatters/placa';

export function FatosConhecidosPage() {
  const { ofertaId } = useParams<{ ofertaId: string }>();
  const navigate = useNavigate();

  const { data: oferta, isLoading, isError, error } = useObterOferta(ofertaId);

  const substituirMutationBase = useSubstituirFatos(ofertaId ?? '');
  const {
    mutate: substituirFatos,
    isPending: isSalvando,
    suspensaoPendente,
    confirmarSuspensao,
    cancelarSuspensao,
  } = useMutacaoComSuspensao(substituirMutationBase);

  const [erroGeral, setErroGeral] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    reset,
    setValue,
    watch,
    formState: { errors },
  } = useForm<FatosFormData>({
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    resolver: zodResolver(fatosSchema) as any,
    defaultValues: {
      origem: {
        indisponivel: false,
        descricao: '',
        fonte: '',
        evidenciaId: null,
        limitacaoDeclarada: '',
      },
      condicao: {
        indisponivel: false,
        descricao: '',
        fonte: '',
        evidenciaId: null,
        limitacaoDeclarada: '',
      },
      historico: {
        indisponivel: false,
        descricao: '',
        fonte: '',
        evidenciaId: null,
        limitacaoDeclarada: '',
      },
      confirmaSuspensao: false,
    },
  });

  useEffect(() => {
    if (oferta?.fatos) {
      const f = oferta.fatos;
      reset({
        origem: {
          indisponivel: f.origem.indisponivel,
          descricao: f.origem.descricao ?? '',
          fonte: f.origem.fonte ?? '',
          evidenciaId: f.origem.evidencia?.evidenciaId ?? null,
          limitacaoDeclarada: f.origem.limitacaoDeclarada ?? '',
        },
        condicao: {
          indisponivel: f.condicao.indisponivel,
          descricao: f.condicao.descricao ?? '',
          fonte: f.condicao.fonte ?? '',
          evidenciaId: f.condicao.evidencia?.evidenciaId ?? null,
          limitacaoDeclarada: f.condicao.limitacaoDeclarada ?? '',
        },
        historico: {
          indisponivel: f.historico.indisponivel,
          descricao: f.historico.descricao ?? '',
          fonte: f.historico.fonte ?? '',
          evidenciaId: f.historico.evidencia?.evidenciaId ?? null,
          limitacaoDeclarada: f.historico.limitacaoDeclarada ?? '',
        },
        confirmaSuspensao: false,
      });
    }
  }, [oferta, reset]);

  if (isLoading) {
    return (
      <div className="flex min-h-[400px] items-center justify-center">
        <p className="text-sm text-neutral-600">Carregando fatos da oferta…</p>
      </div>
    );
  }

  if (isError || !oferta || !ofertaId) {
    return (
      <ErrorState
        mensagem={error instanceof Error ? error.message : 'Oferta não encontrada.'}
        onRetry={() => navigate('/estoque')}
      />
    );
  }

  const onSubmit = (formData: FatosFormData) => {
    setErroGeral(null);
    substituirFatos(formData, {
      onSuccess: () => {
        navigate(`/estoque/${ofertaId}`);
      },
      onError: (err) => {
        if (err instanceof ApiError) {
          setErroGeral(err.problem.detail ?? err.problem.title);
        } else {
          setErroGeral('Erro ao salvar fatos conhecidos.');
        }
      },
    });
  };

  const veiculo = oferta.veiculo;

  return (
    <div className="mx-auto max-w-4xl space-y-6">
      {/* Diálogo de suspensão 409 (ADR-003) */}
      <DialogoSuspensao
        isOpen={!!suspensaoPendente}
        criteriosAfetados={suspensaoPendente?.criteriosAfetados}
        onConfirmar={() =>
          confirmarSuspensao({
            onSuccess: () => navigate(`/estoque/${ofertaId}`),
          })
        }
        onCancelar={cancelarSuspensao}
        isLoading={isSalvando}
      />

      {/* Breadcrumb */}
      <nav className="flex items-center gap-2 text-xs text-neutral-500 font-medium">
        <Link to="/estoque" className="hover:text-neutral-900 transition-colors">
          Estoque
        </Link>
        <span>/</span>
        <Link to={`/estoque/${ofertaId}`} className="hover:text-neutral-900 transition-colors">
          {formatarPlaca(veiculo.placa)}
        </Link>
        <span>/</span>
        <span className="text-neutral-900 font-semibold">Fatos conhecidos</span>
      </nav>

      {/* Cabeçalho */}
      <div>
        <h1 className="text-2xl font-bold text-neutral-900 tracking-tight">
          Curadoria de Fatos Conhecidos
        </h1>
        <p className="text-sm text-neutral-600 mt-1">
          A transparência exige que cada aspecto tenha informações verificadas <strong>ou</strong> uma
          declaração clara de limitação para o comprador (Critério CM-6).
        </p>
      </div>

      {erroGeral && (
        <div className="rounded-xl border border-danger/30 bg-danger/10 p-4 text-sm text-danger font-medium">
          {erroGeral}
        </div>
      )}

      <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
        {/* Bloco 1: Origem */}
        <BlocoFatoForm
          ofertaId={ofertaId}
          tipo="origem"
          titulo="1. Origem do Veículo"
          descricaoAjuda="Proprietários anteriores, procedência corporativa/locadora, estado de registro."
          blocoAtual={oferta.fatos.origem}
          register={register}
          errors={errors}
          setValue={setValue}
          watch={watch}
        />

        {/* Bloco 2: Condição */}
        <BlocoFatoForm
          ofertaId={ofertaId}
          tipo="condicao"
          titulo="2. Condição do Veículo"
          descricaoAjuda="Manutenções realizadas, estado dos pneus, revisões carimbadas, laudo cautelar."
          blocoAtual={oferta.fatos.condicao}
          register={register}
          errors={errors}
          setValue={setValue}
          watch={watch}
        />

        {/* Bloco 3: Histórico */}
        <BlocoFatoForm
          ofertaId={ofertaId}
          tipo="historico"
          titulo="3. Histórico de Sinistros e Leilão"
          descricaoAjuda="Consultas a bases de sinistros, restrições e passagens por leilão."
          blocoAtual={oferta.fatos.historico}
          register={register}
          errors={errors}
          setValue={setValue}
          watch={watch}
        />

        {/* Barra de Ações */}
        <div className="flex items-center justify-end gap-3 pt-4 border-t border-border">
          <Button
            type="button"
            variant="secondary"
            onClick={() => navigate(`/estoque/${ofertaId}`)}
            disabled={isSalvando}
          >
            Cancelar
          </Button>
          <Button type="submit" variant="primary" disabled={isSalvando}>
            {isSalvando ? 'Salvando fatos…' : 'Salvar fatos conhecidos'}
          </Button>
        </div>
      </form>
    </div>
  );
}
