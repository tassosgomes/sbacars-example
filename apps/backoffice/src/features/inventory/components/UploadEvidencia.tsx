import { useState, useRef } from 'react';
import { Button } from '@sbacars/ui';
import { gerarUrlUploadEvidencia, uploadParaS3 } from '../api/evidencias';
import type { Evidencia } from '@/shared/api/types';

export interface UploadEvidenciaProps {
  ofertaId: string;
  evidenciaAtual?: Evidencia | null;
  evidenciaIdValor?: string | null;
  onEvidenciaAlterada: (evidenciaId: string | null, nomeArquivo?: string) => void;
  disabled?: boolean;
}

export function UploadEvidencia({
  ofertaId,
  evidenciaAtual,
  evidenciaIdValor,
  onEvidenciaAlterada,
  disabled = false,
}: UploadEvidenciaProps) {
  const [isUploading, setIsUploading] = useState(false);
  const [erroUpload, setErroUpload] = useState<string | null>(null);
  const [nomeArquivoCarregado, setNomeArquivoCarregado] = useState<string | null>(
    evidenciaAtual?.nomeArquivo ?? null
  );

  const fileInputRef = useRef<HTMLInputElement>(null);

  const handleFileSelect = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    setErroUpload(null);

    // Validação de tipo: PDF, JPEG, PNG
    const tiposValidos = ['application/pdf', 'image/jpeg', 'image/png'] as const;
    if (!tiposValidos.includes(file.type as typeof tiposValidos[number])) {
      setErroUpload('Tipo inválido. Apenas PDF, JPEG ou PNG são aceitos.');
      return;
    }

    // Validação de tamanho: máximo 10 MiB (10485760 bytes)
    if (file.size > 10 * 1024 * 1024) {
      setErroUpload('Tamanho máximo permitido é de 10 MB.');
      return;
    }

    try {
      setIsUploading(true);
      const resUrl = await gerarUrlUploadEvidencia(ofertaId, {
        nomeArquivo: file.name,
        tipoConteudo: file.type as typeof tiposValidos[number],
        tamanhoBytes: file.size,
      });

      await uploadParaS3(resUrl.uploadUrl, resUrl.headersObrigatorios, file);

      setNomeArquivoCarregado(file.name);
      onEvidenciaAlterada(resUrl.evidenciaId, file.name);
    } catch (err) {
      setErroUpload(err instanceof Error ? err.message : 'Falha ao enviar arquivo de evidência.');
    } finally {
      setIsUploading(false);
    }
  };

  const handleRemover = () => {
    setNomeArquivoCarregado(null);
    onEvidenciaAlterada(null);
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  };

  const temEvidencia = !!evidenciaIdValor || !!nomeArquivoCarregado;

  return (
    <div className="space-y-2">
      <input
        ref={fileInputRef}
        type="file"
        accept=".pdf,image/jpeg,image/png"
        onChange={handleFileSelect}
        disabled={disabled || isUploading}
        className="hidden"
        id={`upload-evidencia-${ofertaId}`}
      />

      {temEvidencia ? (
        <div className="flex items-center justify-between rounded-lg border border-emerald-200 bg-emerald-50 p-3 text-xs">
          <div className="flex items-center gap-2 text-emerald-900 font-medium truncate">
            <span>📎</span>
            <span className="truncate">{nomeArquivoCarregado || 'Evidência anexada'}</span>
          </div>
          {!disabled && (
            <button
              type="button"
              onClick={handleRemover}
              className="ml-3 font-semibold text-danger hover:underline shrink-0"
            >
              Remover
            </button>
          )}
        </div>
      ) : (
        <div>
          <Button
            type="button"
            variant="secondary"
            size="sm"
            disabled={disabled || isUploading}
            onClick={() => fileInputRef.current?.click()}
          >
            {isUploading ? 'Enviando arquivo…' : 'Anexar evidência (PDF, JPG, PNG)'}
          </Button>
          <span className="text-[11px] text-muted ml-2">Máximo 10 MB</span>
        </div>
      )}

      {erroUpload && <p className="text-xs text-danger font-medium">{erroUpload}</p>}
    </div>
  );
}
