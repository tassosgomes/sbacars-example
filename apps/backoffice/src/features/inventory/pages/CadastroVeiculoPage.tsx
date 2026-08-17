import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Button } from '@sbacars/ui';
import { veiculoSchema, type VeiculoFormData } from '../schemas/veiculo';
import { useObterOferta } from '../api/useOfertas';
import { useCadastrarVeiculo, useAtualizarVeiculo, useExcluirOferta } from '../api/useMutacoesOferta';
import { useMutacaoComSuspensao } from '../api/useMutacaoComSuspensao';
import { DialogoSuspensao } from '../components/DialogoSuspensao';
import { ApiError } from '@/shared/api/problemDetails';
import { ErrorState } from '@/shared/components/ErrorState';

export function CadastroVeiculoPage() {
  const { ofertaId } = useParams<{ ofertaId: string }>();
  const isEdicao = !!ofertaId;
  const navigate = useNavigate();

  const { data: ofertaExistente, isLoading: isLoadingOferta, isError, error } = useObterOferta(ofertaId);

  const cadastrarMutation = useCadastrarVeiculo();
  const atualizarMutationBase = useAtualizarVeiculo(ofertaId ?? '');
  const {
    mutate: atualizarVeiculo,
    isPending: isPendingAtualizar,
    suspensaoPendente,
    confirmarSuspensao,
    cancelarSuspensao,
  } = useMutacaoComSuspensao(atualizarMutationBase);

  const excluirMutation = useExcluirOferta();
  const [erroGeral, setErroGeral] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    reset,
    watch,
    formState: { errors },
  } = useForm<VeiculoFormData>({
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    resolver: zodResolver(veiculoSchema) as any,
    defaultValues: {
      tipoVeiculo: 'carro-seminovo',
      placa: '',
      chassi: '',
      marca: '',
      modelo: '',
      versao: '',
      anoFabricacao: null,
      anoModelo: null,
      quilometragem: null,
      cor: '',
      combustivel: '',
      cambio: '',
      localizacao: {
        cep: '',
        cidade: '',
        uf: '',
      },
    },
  });

  // Preenche formulário em modo edição
  useEffect(() => {
    if (ofertaExistente?.veiculo) {
      const v = ofertaExistente.veiculo;
      reset({
        tipoVeiculo: v.tipoVeiculo,
        placa: v.placa ?? '',
        chassi: v.chassi ?? '',
        marca: v.marca ?? '',
        modelo: v.modelo ?? '',
        versao: v.versao ?? '',
        anoFabricacao: v.anoFabricacao ?? null,
        anoModelo: v.anoModelo ?? null,
        quilometragem: v.quilometragem ?? null,
        cor: v.cor ?? '',
        combustivel: v.combustivel ?? '',
        cambio: v.cambio ?? '',
        localizacao: {
          cep: v.localizacao?.cep ?? '',
          cidade: v.localizacao?.cidade ?? '',
          uf: v.localizacao?.uf ?? '',
        },
      });
    }
  }, [ofertaExistente, reset]);

  const watchedFields = watch();

  // Contagem dinâmica de critérios preenchidos no form para o aviso informativo (RF-01)
  const criteriosPreenchidos = [
    !!watchedFields.placa?.trim(),
    !!(
      watchedFields.marca?.trim() &&
      watchedFields.modelo?.trim() &&
      watchedFields.anoFabricacao &&
      watchedFields.quilometragem !== null &&
      watchedFields.quilometragem !== undefined &&
      watchedFields.cambio?.trim()
    ),
    !!(watchedFields.localizacao?.cidade?.trim() && watchedFields.localizacao?.uf?.trim()),
  ].filter(Boolean).length;

  const onSubmit = (formData: VeiculoFormData) => {
    setErroGeral(null);
    if (isEdicao) {
      atualizarVeiculo(formData, {
        onSuccess: (res) => {
          navigate(`/estoque/${res.ofertaId}`);
        },
        onError: (err) => {
          if (err instanceof ApiError) {
            setErroGeral(err.problem.detail ?? err.problem.title);
          } else {
            setErroGeral('Ocorreu um erro ao atualizar os dados do veículo.');
          }
        },
      });
    } else {
      cadastrarMutation.mutate(formData, {
        onSuccess: (res) => {
          navigate(`/estoque/${res.ofertaId}`);
        },
        onError: (err) => {
          if (err instanceof ApiError) {
            setErroGeral(err.problem.detail ?? err.problem.title);
          } else {
            setErroGeral('Ocorreu um erro ao cadastrar o veículo.');
          }
        },
      });
    }
  };

  const handleExcluir = () => {
    if (!ofertaId) return;
    if (window.confirm('Tem certeza de que deseja excluir este cadastro em preparação?')) {
      excluirMutation.mutate(ofertaId, {
        onSuccess: () => {
          navigate('/estoque');
        },
      });
    }
  };

  const isSalvando = cadastrarMutation.isPending || isPendingAtualizar;

  if (isEdicao && isLoadingOferta) {
    return (
      <div className="flex min-h-[300px] items-center justify-center p-8">
        <p className="text-sm text-neutral-600">Carregando dados da oferta…</p>
      </div>
    );
  }

  if (isEdicao && isError) {
    return (
      <ErrorState
        mensagem={error instanceof Error ? error.message : undefined}
        onRetry={() => navigate('/estoque')}
      />
    );
  }

  return (
    <div className="mx-auto max-w-4xl space-y-6">
      {/* Diálogo de confirmação de suspensão (ADR-003) */}
      <DialogoSuspensao
        isOpen={!!suspensaoPendente}
        criteriosAfetados={suspensaoPendente?.criteriosAfetados}
        onConfirmar={() =>
          confirmarSuspensao({
            onSuccess: (res) => navigate(`/estoque/${res.ofertaId}`),
          })
        }
        onCancelar={cancelarSuspensao}
        isLoading={isSalvando}
      />

      {/* Cabeçalho */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-neutral-900 tracking-tight">
            {isEdicao ? 'Editar dados do veículo' : 'Cadastrar novo veículo'}
          </h1>
          <p className="text-sm text-neutral-600 mt-1">
            Preencha as informações do veículo. Campos não preenchidos manterão a oferta em preparação.
          </p>
        </div>
        {isEdicao && ofertaExistente?.situacao === 'em-preparacao' && (
          <Button
            type="button"
            variant="danger"
            size="sm"
            onClick={handleExcluir}
            disabled={excluirMutation.isPending}
          >
            {excluirMutation.isPending ? 'Excluindo…' : 'Excluir oferta'}
          </Button>
        )}
      </div>

      {/* Aviso informativo de salvamento parcial (RF-01) */}
      <div className="rounded-xl border border-blue-200 bg-blue-50 p-4 text-sm text-blue-900 flex items-start gap-3">
        <span className="text-lg">ℹ️</span>
        <div>
          <p className="font-semibold">Cadastro flexível (RF-01)</p>
          <p className="text-xs text-blue-800 mt-0.5">
            Você pode salvar o veículo agora mesmo com dados parciais. Ele ficará{' '}
            <strong>Em preparação</strong> até que todos os 6 critérios mínimos sejam satisfeitos e validados.
            ({criteriosPreenchidos} de 3 blocos de veículo pré-atendidos).
          </p>
        </div>
      </div>

      {erroGeral && (
        <div className="rounded-xl border border-danger/30 bg-danger/10 p-4 text-sm text-danger font-medium">
          {erroGeral}
        </div>
      )}

      <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
        {/* Seção 1: Identificação */}
        <section className="rounded-xl border border-border bg-surface p-6 shadow-xs space-y-4">
          <h2 className="text-base font-bold text-neutral-900 border-b border-border pb-3">
            1. Identificação do Veículo
          </h2>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <label htmlFor="placa" className="block text-xs font-bold uppercase tracking-wider text-neutral-700 mb-1">
                Placa (Mercosul ou antiga)
              </label>
              <input
                id="placa"
                type="text"
                maxLength={8}
                placeholder="ABC1D23 ou ABC-1234"
                {...register('placa')}
                className="w-full rounded-lg border border-border bg-background px-3.5 py-2 text-sm text-neutral-900 uppercase font-mono focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
              />
              {errors.placa && <p className="text-xs text-danger mt-1">{errors.placa.message}</p>}
            </div>

            <div>
              <label htmlFor="chassi" className="block text-xs font-bold uppercase tracking-wider text-neutral-700 mb-1">
                Chassi (VIN)
              </label>
              <input
                id="chassi"
                type="text"
                maxLength={17}
                placeholder="17 caracteres alfanuméricos"
                {...register('chassi')}
                className="w-full rounded-lg border border-border bg-background px-3.5 py-2 text-sm text-neutral-900 uppercase font-mono focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
              />
              {errors.chassi && <p className="text-xs text-danger mt-1">{errors.chassi.message}</p>}
            </div>
          </div>
        </section>

        {/* Seção 2: Categoria (RN-01) */}
        <section className="rounded-xl border border-border bg-surface p-6 shadow-xs space-y-4">
          <h2 className="text-base font-bold text-neutral-900 border-b border-border pb-3">
            2. Categoria do Estoque
          </h2>
          <div>
            <label htmlFor="tipoVeiculo" className="block text-xs font-bold uppercase tracking-wider text-neutral-700 mb-1">
              Tipo de Veículo *
            </label>
            <select
              id="tipoVeiculo"
              {...register('tipoVeiculo')}
              className="w-full sm:w-1/2 rounded-lg border border-border bg-background px-3.5 py-2 text-sm text-neutral-900 focus:border-primary focus:outline-none"
            >
              <option value="carro-seminovo">Carro Seminovo</option>
              <option value="carro-usado">Carro Usado</option>
            </select>
            {errors.tipoVeiculo && <p className="text-xs text-danger mt-1">{errors.tipoVeiculo.message}</p>}
          </div>
        </section>

        {/* Seção 3: Dados Básicos */}
        <section className="rounded-xl border border-border bg-surface p-6 shadow-xs space-y-4">
          <h2 className="text-base font-bold text-neutral-900 border-b border-border pb-3">
            3. Dados Básicos do Carro
          </h2>
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
            <div>
              <label htmlFor="marca" className="block text-xs font-bold uppercase tracking-wider text-neutral-700 mb-1">
                Marca
              </label>
              <input
                id="marca"
                type="text"
                placeholder="Ex: Honda, Toyota"
                {...register('marca')}
                className="w-full rounded-lg border border-border bg-background px-3.5 py-2 text-sm text-neutral-900 focus:border-primary focus:outline-none"
              />
            </div>

            <div>
              <label htmlFor="modelo" className="block text-xs font-bold uppercase tracking-wider text-neutral-700 mb-1">
                Modelo
              </label>
              <input
                id="modelo"
                type="text"
                placeholder="Ex: Civic, Corolla"
                {...register('modelo')}
                className="w-full rounded-lg border border-border bg-background px-3.5 py-2 text-sm text-neutral-900 focus:border-primary focus:outline-none"
              />
            </div>

            <div>
              <label htmlFor="versao" className="block text-xs font-bold uppercase tracking-wider text-neutral-700 mb-1">
                Versão
              </label>
              <input
                id="versao"
                type="text"
                placeholder="Ex: EXL 2.0 Flex"
                {...register('versao')}
                className="w-full rounded-lg border border-border bg-background px-3.5 py-2 text-sm text-neutral-900 focus:border-primary focus:outline-none"
              />
            </div>

            <div>
              <label htmlFor="anoFabricacao" className="block text-xs font-bold uppercase tracking-wider text-neutral-700 mb-1">
                Ano de Fabricação
              </label>
              <input
                id="anoFabricacao"
                type="number"
                placeholder="Ex: 2021"
                {...register('anoFabricacao')}
                className="w-full rounded-lg border border-border bg-background px-3.5 py-2 text-sm text-neutral-900 font-mono focus:border-primary focus:outline-none"
              />
              {errors.anoFabricacao && (
                <p className="text-xs text-danger mt-1">{errors.anoFabricacao.message}</p>
              )}
            </div>

            <div>
              <label htmlFor="anoModelo" className="block text-xs font-bold uppercase tracking-wider text-neutral-700 mb-1">
                Ano Modelo
              </label>
              <input
                id="anoModelo"
                type="number"
                placeholder="Ex: 2022"
                {...register('anoModelo')}
                className="w-full rounded-lg border border-border bg-background px-3.5 py-2 text-sm text-neutral-900 font-mono focus:border-primary focus:outline-none"
              />
              {errors.anoModelo && (
                <p className="text-xs text-danger mt-1">{errors.anoModelo.message}</p>
              )}
            </div>

            <div>
              <label htmlFor="quilometragem" className="block text-xs font-bold uppercase tracking-wider text-neutral-700 mb-1">
                Quilometragem (KM)
              </label>
              <input
                id="quilometragem"
                type="number"
                placeholder="Ex: 48300"
                {...register('quilometragem')}
                className="w-full rounded-lg border border-border bg-background px-3.5 py-2 text-sm text-neutral-900 font-mono focus:border-primary focus:outline-none"
              />
              {errors.quilometragem && (
                <p className="text-xs text-danger mt-1">{errors.quilometragem.message}</p>
              )}
            </div>

            <div>
              <label htmlFor="cor" className="block text-xs font-bold uppercase tracking-wider text-neutral-700 mb-1">
                Cor
              </label>
              <input
                id="cor"
                type="text"
                placeholder="Ex: Prata"
                {...register('cor')}
                className="w-full rounded-lg border border-border bg-background px-3.5 py-2 text-sm text-neutral-900 focus:border-primary focus:outline-none"
              />
            </div>

            <div>
              <label htmlFor="combustivel" className="block text-xs font-bold uppercase tracking-wider text-neutral-700 mb-1">
                Combustível
              </label>
              <input
                id="combustivel"
                type="text"
                placeholder="Ex: Flex, Gasolina"
                {...register('combustivel')}
                className="w-full rounded-lg border border-border bg-background px-3.5 py-2 text-sm text-neutral-900 focus:border-primary focus:outline-none"
              />
            </div>

            <div>
              <label htmlFor="cambio" className="block text-xs font-bold uppercase tracking-wider text-neutral-700 mb-1">
                Câmbio
              </label>
              <input
                id="cambio"
                type="text"
                placeholder="Ex: Automático, Manual"
                {...register('cambio')}
                className="w-full rounded-lg border border-border bg-background px-3.5 py-2 text-sm text-neutral-900 focus:border-primary focus:outline-none"
              />
            </div>
          </div>
        </section>

        {/* Seção 4: Localização */}
        <section className="rounded-xl border border-border bg-surface p-6 shadow-xs space-y-4">
          <h2 className="text-base font-bold text-neutral-900 border-b border-border pb-3">
            4. Localização
          </h2>
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
            <div>
              <label htmlFor="cep" className="block text-xs font-bold uppercase tracking-wider text-neutral-700 mb-1">
                CEP
              </label>
              <input
                id="cep"
                type="text"
                placeholder="13010-111"
                {...register('localizacao.cep')}
                className="w-full rounded-lg border border-border bg-background px-3.5 py-2 text-sm text-neutral-900 font-mono focus:border-primary focus:outline-none"
              />
              {errors.localizacao?.cep && (
                <p className="text-xs text-danger mt-1">{errors.localizacao.cep.message}</p>
              )}
            </div>

            <div>
              <label htmlFor="cidade" className="block text-xs font-bold uppercase tracking-wider text-neutral-700 mb-1">
                Cidade
              </label>
              <input
                id="cidade"
                type="text"
                placeholder="Ex: Campinas"
                {...register('localizacao.cidade')}
                className="w-full rounded-lg border border-border bg-background px-3.5 py-2 text-sm text-neutral-900 focus:border-primary focus:outline-none"
              />
            </div>

            <div>
              <label htmlFor="uf" className="block text-xs font-bold uppercase tracking-wider text-neutral-700 mb-1">
                UF
              </label>
              <input
                id="uf"
                type="text"
                maxLength={2}
                placeholder="SP"
                {...register('localizacao.uf')}
                className="w-full rounded-lg border border-border bg-background px-3.5 py-2 text-sm text-neutral-900 uppercase font-mono focus:border-primary focus:outline-none"
              />
              {errors.localizacao?.uf && (
                <p className="text-xs text-danger mt-1">{errors.localizacao.uf.message}</p>
              )}
            </div>
          </div>
        </section>

        {/* Barra de Ações */}
        <div className="flex items-center justify-end gap-3 pt-4 border-t border-border">
          <Button
            type="button"
            variant="secondary"
            onClick={() => navigate(isEdicao ? `/estoque/${ofertaId}` : '/estoque')}
            disabled={isSalvando}
          >
            Cancelar
          </Button>
          <Button type="submit" variant="primary" disabled={isSalvando}>
            {isSalvando ? 'Salvando…' : isEdicao ? 'Salvar alterações' : 'Salvar e continuar'}
          </Button>
        </div>
      </form>
    </div>
  );
}
