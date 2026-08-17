import { screen, waitFor, fireEvent } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { FilaValidacaoPage } from './FilaValidacaoPage';
import { renderWithProviders } from '@/test/test-utils';

describe('T07 - FilaValidacaoPage', () => {
  it('renderiza a fila com solicitações pendentes e alerta de SLA', async () => {
    renderWithProviders(<FilaValidacaoPage />, { initialEntries: ['/validacao'] });

    expect(screen.getByRole('heading', { name: /fila de validação/i })).toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getByText('Toyota Corolla XEI 2021/2022')).toBeInTheDocument();
    });

    expect(screen.getByText('Jeep Compass Longitude 2023/2023')).toBeInTheDocument();
    expect(screen.getByText(/atenção ao sla operacional/i)).toBeInTheDocument();
  });

  it('permite alternar entre abas de status', async () => {
    renderWithProviders(<FilaValidacaoPage />, { initialEntries: ['/validacao'] });

    await waitFor(() => {
      expect(screen.getByText('Toyota Corolla XEI 2021/2022')).toBeInTheDocument();
    });

    const abaAprovada = screen.getByRole('button', { name: /aprovada/i });
    fireEvent.click(abaAprovada);

    expect(abaAprovada).toHaveClass('bg-surface');
  });
});
