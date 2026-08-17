import { useState } from 'react';
import { useNavigate, useParams, Link } from 'react-router-dom';
import { Button } from '@sbacars/ui';
import { useObterOferta } from '../api/useOfertas';
import { BadgeSituacao } from '../components/BadgeSituacao';
import { BadgeDisponibilidade } from '../components/BadgeDisponibilidade';
import { BadgeTipoSolicitacao } from '../components/BadgeTipoSolicitacao';
import { ValorComProcedencia } from '../components/ValorComProcedencia';
import { ChecklistElegibilidade } from '../components/ChecklistElegibilidade';
import { SeloLimitacao } from '../components/SeloLimitacao';
import { ModalPrecoInicial } from '../components/ModalPrecoInicial';
import { ModalSolicitarPreco } from '../components/ModalSolicitarPreco';
import { ModalDisponibilidade } from '../components/ModalDisponibilidade';
import { ModalSolicitarElegibilidade } from '../components/ModalSolicitarElegibilidade';
import { ModalSolicitarRetirada } from '../components/ModalSolicitarRetirada';
import { ErrorState } from '@/shared/components/ErrorState';
import { centavosParaBrl } from '@/shared/formatters/moeda';
import { formatarDataHora } from '@/shared/formatters/data';
import { formatarPlaca } from '@/shared/formatters/placa';

export function DetalheOfertaPage() {
  const { ofertaId } = useParams<{ ofertaId: string }>();
  const navigate = useNavigate();

  const { data: oferta, isLoading, isError, error, refetch } = useObterOferta(ofertaId);

  // Estados dos modais
  const [modalPrecoInicialAberto, setModalPrecoInicialAberto] = useState(false);
  const [modalSolicitarPrecoAberto, setModalSolicitarPrecoAberto] = useState(false);
  const [modalDisponibilidadeAberto, setModalDisponibilidadeAberto] = useState(false);
  const [modalElegibilidadeAberto, setModalElegibilidadeAberto] = useState(false);
  const [modalRetiradaAberto, setModalRetiradaAberto] = useState(false);

  if (isLoading) {
    return (
      <div className="flex min-h-[400px] items-center justify-center">
        <p className="text-sm text-neutral-600">Carregando detalhes da oferta…</p>
      </div>
    );
  }

  if (isError || !oferta) {
    return (
      <ErrorState
        mensagem={error instanceof Error ? error.message : 'Oferta não encontrada.'}
        onRetry={() => refetch()}
      />
    );
  }

  const { veiculo, fatos, precoOficial, disponibilidade, elegibilidade, pendencias, situacao } = oferta;
  const tituloVeiculo = [veiculo.marca, veiculo.modelo, veiculo.versao].filter(Boolean).join(' ') || 'Veículo em cadastro';

  return (
    <div className="mx-auto max-w-6xl space-y-6">
      {/* Modais */}
      {ofertaId && (
        <>
          <ModalPrecoInicial
            isOpen={modalPrecoInicialAberto}
            ofertaId={ofertaId}
            onClose={() => setModalPrecoInicialAberto(false)}
          />
          <ModalSolicitarPreco
            isOpen={modalSolicitarPrecoAberto}
            ofertaId={ofertaId}
            precoAtualCentavos={precoOficial?.valorCentavos}
            onClose={() => setModalSolicitarPrecoAberto(false)}
          />
          <ModalDisponibilidade
            isOpen={modalDisponibilidadeAberto}
            ofertaId={ofertaId}
            estadoAtual={disponibilidade.estado}
            transicoesPermitidas={disponibilidade.transicoesPermitidas}
            onClose={() => setModalDisponibilidadeAberto(false)}
          />
          <ModalSolicitarElegibilidade
            isOpen={modalElegibilidadeAberto}
            ofertaId={ofertaId}
            onClose={() => setModalElegibilidadeAberto(false)}
          />
          <ModalSolicitarRetirada
            isOpen={modalRetiradaAberto}
            ofertaId={ofertaId}
            onClose={() => setModalRetiradaAberto(false)}
          />
        </>
      )}

      {/* Breadcrumb */}
      <nav className="flex items-center gap-2 text-xs text-neutral-500 font-medium">
        <Link to="/estoque" className="hover:text-neutral-900 transition-colors">
          Estoque
        </Link>
        <span>/</span>
        <span className="text-neutral-900 font-semibold">{formatarPlaca(veiculo.placa)}</span>
      </nav>

      {/* Banner de Suspensão (T03-b) */}
      {situacao === 'suspensa' && (
        <div className="rounded-xl border border-red-300 bg-red-50 p-4 text-red-900 flex items-start gap-3 shadow-xs">
          <span className="text-xl">⚠️</span>
          <div>
            <h3 className="text-sm font-bold">Oferta com Elegibilidade Suspensa</h3>
            <p className="text-xs text-red-800 mt-0.5">
              {oferta.motivoSuspensao ||
                'Esta oferta deixou de cumprir os critérios mínimos de elegibilidade e foi suspensa do catálogo público.'}
              {oferta.suspensaEm && ` (Suspensa em ${formatarDataHora(oferta.suspensaEm)}).`}
            </p>
            <p className="text-xs text-red-800 mt-1 font-medium">
              Corrija as pendências apontadas no checklist ao lado e solicite uma nova validação para republicá-la.
            </p>
          </div>
        </div>
      )}

      {/* Cabeçalho Principal da Oferta */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 rounded-xl border border-border bg-surface p-6 shadow-xs">
        <div className="space-y-1.5">
          <div className="flex flex-wrap items-center gap-2.5">
            <h1 className="text-2xl font-bold text-neutral-900 tracking-tight">{tituloVeiculo}</h1>
            <BadgeSituacao situacao={situacao} />
            <BadgeDisponibilidade disponibilidade={disponibilidade.estado} />
          </div>
          <div className="flex items-center gap-3 text-xs text-neutral-600 font-mono">
            <span>Placa: <strong>{formatarPlaca(veiculo.placa)}</strong></span>
            <span>•</span>
            <span>Chassi: <strong>{veiculo.chassi || '—'}</strong></span>
          </div>
        </div>

        {/* Ações Primárias da Oferta */}
        <div className="flex flex-wrap items-center gap-3 self-start md:self-auto">
          {situacao !== 'retirada' && (
            <Button
              type="button"
              variant="ghost"
              size="sm"
              onClick={() => setModalRetiradaAberto(true)}
            >
              Retirar oferta
            </Button>
          )}

          {situacao !== 'elegivel' && situacao !== 'retirada' && (
            <Button
              type="button"
              variant="primary"
              disabled={!elegibilidade.podeSolicitarElegibilidade}
              onClick={() => setModalElegibilidadeAberto(true)}
              title={
                !elegibilidade.podeSolicitarElegibilidade
                  ? 'Atenda todos os 6 critérios mínimos para solicitar elegibilidade.'
                  : 'Enviar oferta para validação'
              }
            >
              Solicitar elegibilidade
            </Button>
          )}
        </div>
      </div>

      {/* Grid Principal: 2 Colunas */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Coluna Esquerda: Dados e Curadoria (2 cols) */}
        <div className="lg:col-span-2 space-y-6">
          {/* Card: Dados do Veículo */}
          <section className="rounded-xl border border-border bg-surface p-6 shadow-xs space-y-4">
            <div className="flex items-center justify-between border-b border-border pb-3">
              <h2 className="text-base font-bold text-neutral-900">Dados do Veículo</h2>
              <Button
                type="button"
                variant="secondary"
                size="sm"
                onClick={() => navigate(`/estoque/${ofertaId}/editar`)}
              >
                Editar dados
              </Button>
            </div>

            <div className="grid grid-cols-2 sm:grid-cols-3 gap-y-4 gap-x-6 text-sm">
              <div>
                <span className="text-xs font-bold uppercase tracking-wider text-muted">Categoria</span>
                <p className="font-semibold text-neutral-900 capitalize">
                  {veiculo.tipoVeiculo === 'carro-seminovo' ? 'Seminovo' : 'Usado'}
                </p>
              </div>

              <div>
                <span className="text-xs font-bold uppercase tracking-wider text-muted">Ano Fab./Mod.</span>
                <p className="font-semibold text-neutral-900 font-mono">
                  {veiculo.anoFabricacao ? `${veiculo.anoFabricacao}${veiculo.anoModelo ? `/${veiculo.anoModelo}` : ''}` : '—'}
                </p>
              </div>

              <div>
                <span className="text-xs font-bold uppercase tracking-wider text-muted">Quilometragem</span>
                <p className="font-semibold text-neutral-900 font-mono">
                  {veiculo.quilometragem !== null && veiculo.quilometragem !== undefined
                    ? `${veiculo.quilometragem.toLocaleString('pt-BR')} km`
                    : '—'}
                </p>
              </div>

              <div>
                <span className="text-xs font-bold uppercase tracking-wider text-muted">Cor</span>
                <p className="font-semibold text-neutral-900">{veiculo.cor || '—'}</p>
              </div>

              <div>
                <span className="text-xs font-bold uppercase tracking-wider text-muted">Combustível</span>
                <p className="font-semibold text-neutral-900">{veiculo.combustivel || '—'}</p>
              </div>

              <div>
                <span className="text-xs font-bold uppercase tracking-wider text-muted">Câmbio</span>
                <p className="font-semibold text-neutral-900">{veiculo.cambio || '—'}</p>
              </div>

              <div className="sm:col-span-3 border-t border-border/60 pt-3 flex flex-col sm:flex-row sm:items-center justify-between text-xs text-neutral-600">
                <span>
                  Localização: <strong>{veiculo.localizacao?.cidade || '—'}/{veiculo.localizacao?.uf || '—'}</strong> (CEP: {veiculo.localizacao?.cep || '—'})
                </span>
              </div>
            </div>
          </section>

          {/* Card: Preço Oficial Vigente (QF-01) */}
          <section className="rounded-xl border border-border bg-surface p-6 shadow-xs space-y-4">
            <div className="flex items-center justify-between border-b border-border pb-3">
              <h2 className="text-base font-bold text-neutral-900">Preço Oficial</h2>
              {precoOficial ? (
                <Button
                  type="button"
                  variant="secondary"
                  size="sm"
                  onClick={() => setModalSolicitarPrecoAberto(true)}
                >
                  Solicitar alteração
                </Button>
              ) : (
                <Button
                  type="button"
                  variant="primary"
                  size="sm"
                  onClick={() => setModalPrecoInicialAberto(true)}
                >
                  Definir preço inicial
                </Button>
              )}
            </div>

            {precoOficial ? (
              <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
                <ValorComProcedencia
                  label="Valor oficial vigente"
                  valor={<span className="text-2xl font-bold font-mono text-neutral-900">{centavosParaBrl(precoOficial.valorCentavos)}</span>}
                  autoria={precoOficial.definidoPor}
                />
              </div>
            ) : (
              <div className="rounded-lg bg-neutral-50 border border-dashed border-border p-4 text-center">
                <p className="text-sm text-neutral-600 font-medium">Nenhum preço oficial definido.</p>
                <p className="text-xs text-muted mt-1">
                  Defina o primeiro preço oficial para satisfazer o critério CM-4.
                </p>
              </div>
            )}
          </section>

          {/* Card: Disponibilidade Operacional */}
          <section className="rounded-xl border border-border bg-surface p-6 shadow-xs space-y-4">
            <div className="flex items-center justify-between border-b border-border pb-3">
              <h2 className="text-base font-bold text-neutral-900">Disponibilidade Operacional</h2>
              <Button
                type="button"
                variant="secondary"
                size="sm"
                onClick={() => setModalDisponibilidadeAberto(true)}
              >
                Alterar estado
              </Button>
            </div>

            <div className="flex items-center justify-between">
              <ValorComProcedencia
                label="Estado atual"
                valor={<BadgeDisponibilidade disponibilidade={disponibilidade.estado} className="text-sm px-3 py-1" />}
                autoria={disponibilidade.alteradaPor}
              />
            </div>
          </section>

          {/* Card: Fatos Conhecidos */}
          <section className="rounded-xl border border-border bg-surface p-6 shadow-xs space-y-4">
            <div className="flex items-center justify-between border-b border-border pb-3">
              <div>
                <h2 className="text-base font-bold text-neutral-900">Fatos Conhecidos</h2>
                <p className="text-xs text-neutral-600 mt-0.5">
                  Origem, Condição e Histórico com transparência declarada (CM-6).
                </p>
              </div>
              <Button
                type="button"
                variant="secondary"
                size="sm"
                onClick={() => navigate(`/estoque/${ofertaId}/fatos`)}
              >
                Editar fatos
              </Button>
            </div>

            <div className="space-y-4">
              {/* Origem */}
              <div className="rounded-lg border border-border/80 p-4 space-y-2">
                <div className="flex items-center justify-between">
                  <span className="text-xs font-bold uppercase tracking-wider text-neutral-700">Origem</span>
                  <span className="text-[11px] text-neutral-500">
                    {fatos.origem.atendeTransparencia ? '✓ Transparência atendida' : '✗ Pendente'}
                  </span>
                </div>
                {fatos.origem.indisponivel ? (
                  <SeloLimitacao texto={fatos.origem.limitacaoDeclarada} />
                ) : (
                  <div className="text-xs text-neutral-800 space-y-1">
                    <p>{fatos.origem.descricao || 'Sem descrição.'}</p>
                    {fatos.origem.fonte && (
                      <p className="text-muted font-medium">Fonte: {fatos.origem.fonte}</p>
                    )}
                    {fatos.origem.evidencia && (
                      <p className="text-primary font-medium">
                        📎 Evidência anexada: {fatos.origem.evidencia.nomeArquivo}
                      </p>
                    )}
                  </div>
                )}
              </div>

              {/* Condição */}
              <div className="rounded-lg border border-border/80 p-4 space-y-2">
                <div className="flex items-center justify-between">
                  <span className="text-xs font-bold uppercase tracking-wider text-neutral-700">Condição</span>
                  <span className="text-[11px] text-neutral-500">
                    {fatos.condicao.atendeTransparencia ? '✓ Transparência atendida' : '✗ Pendente'}
                  </span>
                </div>
                {fatos.condicao.indisponivel ? (
                  <SeloLimitacao texto={fatos.condicao.limitacaoDeclarada} />
                ) : (
                  <div className="text-xs text-neutral-800 space-y-1">
                    <p>{fatos.condicao.descricao || 'Sem descrição.'}</p>
                    {fatos.condicao.fonte && (
                      <p className="text-muted font-medium">Fonte: {fatos.condicao.fonte}</p>
                    )}
                    {fatos.condicao.evidencia && (
                      <p className="text-primary font-medium">
                        📎 Evidência anexada: {fatos.condicao.evidencia.nomeArquivo}
                      </p>
                    )}
                  </div>
                )}
              </div>

              {/* Histórico */}
              <div className="rounded-lg border border-border/80 p-4 space-y-2">
                <div className="flex items-center justify-between">
                  <span className="text-xs font-bold uppercase tracking-wider text-neutral-700">Histórico</span>
                  <span className="text-[11px] text-neutral-500">
                    {fatos.historico.atendeTransparencia ? '✓ Transparência atendida' : '✗ Pendente'}
                  </span>
                </div>
                {fatos.historico.indisponivel ? (
                  <SeloLimitacao texto={fatos.historico.limitacaoDeclarada} />
                ) : (
                  <div className="text-xs text-neutral-800 space-y-1">
                    <p>{fatos.historico.descricao || 'Sem descrição.'}</p>
                    {fatos.historico.fonte && (
                      <p className="text-muted font-medium">Fonte: {fatos.historico.fonte}</p>
                    )}
                    {fatos.historico.evidencia && (
                      <p className="text-primary font-medium">
                        📎 Evidência anexada: {fatos.historico.evidencia.nomeArquivo}
                      </p>
                    )}
                  </div>
                )}
              </div>
            </div>
          </section>
        </div>

        {/* Coluna Direita: Checklist & Validação (1 col) */}
        <div className="space-y-6">
          {/* Checklist de Elegibilidade CM-1..CM-6 */}
          <ChecklistElegibilidade checklist={elegibilidade} />

          {/* Card: Solicitações Pendentes */}
          <div className="rounded-xl border border-border bg-surface p-5 shadow-xs space-y-3">
            <h3 className="text-sm font-bold uppercase tracking-wider text-neutral-900">
              Solicitações em Aberto
            </h3>
            {pendencias && pendencias.length > 0 ? (
              <ul className="divide-y divide-border/60">
                {pendencias.map((p) => (
                  <li key={p.solicitacaoId} className="py-2.5 space-y-1">
                    <div className="flex items-center justify-between">
                      <BadgeTipoSolicitacao tipo={p.tipo} />
                      <span className="text-[11px] text-neutral-500 font-mono">
                        {formatarDataHora(p.abertaEm)}
                      </span>
                    </div>
                    {p.resumoAlteracao && (
                      <p className="text-xs font-semibold text-neutral-800">{p.resumoAlteracao}</p>
                    )}
                    <div className="flex items-center justify-between text-[11px] text-muted">
                      <span>Por: {p.abertaPor.nome}</span>
                      <Link
                        to={`/validacao/${p.solicitacaoId}`}
                        className="text-primary font-semibold hover:underline"
                      >
                        Ver solicitação →
                      </Link>
                    </div>
                  </li>
                ))}
              </ul>
            ) : (
              <p className="text-xs text-neutral-500 py-2">Nenhuma solicitação pendente nesta oferta.</p>
            )}
          </div>

          {/* Card: Metadados / Auditoria */}
          <div className="rounded-xl border border-border bg-surface p-5 shadow-xs text-xs text-neutral-600 space-y-2">
            <h3 className="text-xs font-bold uppercase tracking-wider text-muted">Auditoria</h3>
            <div className="flex justify-between">
              <span>Criada em:</span>
              <strong className="text-neutral-800">{formatarDataHora(oferta.criadaEm)}</strong>
            </div>
            <div className="flex justify-between">
              <span>Última atualização:</span>
              <strong className="text-neutral-800">{formatarDataHora(oferta.atualizadoEm)}</strong>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
