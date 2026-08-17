import { screen, waitFor, fireEvent } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { Routes, Route } from 'react-router-dom';
import { DetalheSolicitacaoPage } from './DetalheSolicitacaoPage';
import { renderWithProviders } from '@/test/test-utils';

describe('T08 - DetalheSolicitacaoPage', () => {
  it('renderiza os detalhes da solicitação de preço e botões de decisão', async () => {
    renderWithProviders(
      <Routes>
        <Route path="/validacao/:solicitacaoId" element={<DetalheSolicitacaoPage />} />
      </Routes>,
      { initialEntries: ['/validacao/9c1f7e28-4b6a-4d5f-8a09-2c3e5b7d1f40'] }
    );

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: 'Toyota Corolla XEI 2021/2022' })).toBeInTheDocument();
    });

    expect(screen.getByText('R$ 115.000')).toBeInTheDocument();
    expect(screen.getByText('R$ 112.500')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /aprovar solicitação/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /rejeitar solicitação/i })).toBeInTheDocument();
  });

  it('abre modal de rejeição com campo de justificativa obrigatório', async () => {
    renderWithProviders(
      <Routes>
        <Route path="/validacao/:solicitacaoId" element={<DetalheSolicitacaoPage />} />
      </Routes>,
      { initialEntries: ['/validacao/9c1f7e28-4b6a-4d5f-8a09-2c3e5b7d1f40'] }
    );

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: 'Toyota Corolla XEI 2021/2022' })).toBeInTheDocument();
    });

    const btnRejeitar = screen.getByRole('button', { name: /rejeitar solicitação/i });
    fireEvent.click(btnRejeitar);

    expect(screen.getByRole('heading', { name: /rejeitar solicitação de validação/i })).toBeInTheDocument();
    expect(screen.getByLabelText(/motivo da rejeição/i)).toBeInTheDocument();
  });
});
