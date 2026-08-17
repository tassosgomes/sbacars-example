import { requestJson } from '@/shared/api/client';
import type {
  UploadEvidenciaInput,
  UploadEvidenciaResponse,
  DownloadEvidenciaResponse,
} from '@/shared/api/types';

export async function gerarUrlUploadEvidencia(
  ofertaId: string,
  input: UploadEvidenciaInput
): Promise<UploadEvidenciaResponse> {
  return requestJson<UploadEvidenciaResponse>(`/api/ofertas/${ofertaId}/evidencias/upload-url`, {
    method: 'POST',
    body: JSON.stringify(input),
  });
}

export async function uploadParaS3(
  uploadUrl: string,
  headersObrigatorios: Record<string, string>,
  arquivo: File
): Promise<void> {
  const headers = new Headers();
  Object.entries(headersObrigatorios).forEach(([key, val]) => {
    headers.set(key, val);
  });

  const res = await fetch(uploadUrl, {
    method: 'PUT',
    headers,
    body: arquivo,
  });

  if (!res.ok) {
    throw new Error(`Falha no upload do arquivo para o S3 (HTTP ${res.status}).`);
  }
}

export async function gerarUrlDownloadEvidencia(evidenciaId: string): Promise<DownloadEvidenciaResponse> {
  return requestJson<DownloadEvidenciaResponse>(`/api/evidencias/${evidenciaId}/download-url`);
}
