import { screen, waitFor, fireEvent } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { Routes, Route } from 'react-router-dom';
import { DetalheOfertaPage } from './DetalheOfertaPage';
import { renderWithProviders } from '@/test/test-utils';

describe('T03 - DetalheOfertaPage', () => {
  it('renderiza os dados completos da oferta, preço, disponibilidade e checklist', async () => {
    renderWithProviders(
      <Routes>
        <Route path="/estoque/:ofertaId" element={<DetalheOfertaPage />} />
      </Routes>,
      { initialEntries: ['/estoque/3f2a8c14-9d31-4c7e-b8a1-5e6f0d2b9c47'] }
    );

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: 'Honda Civic EXL 2.0' })).toBeInTheDocument();
    });

    expect(screen.getByText('Elegível')).toBeInTheDocument();
    expect(screen.getAllByText('Disponível').length).toBeGreaterThan(0);
    expect(screen.getByText('6 de 6 atendidos')).toBeInTheDocument();
    expect(screen.getByText('R$ 87.900,00')).toBeInTheDocument();
  });

  it('abre o modal de alteração de preço ao clicar no botão correspondente', async () => {
    renderWithProviders(
      <Routes>
        <Route path="/estoque/:ofertaId" element={<DetalheOfertaPage />} />
      </Routes>,
      { initialEntries: ['/estoque/3f2a8c14-9d31-4c7e-b8a1-5e6f0d2b9c47'] }
    );

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: 'Honda Civic EXL 2.0' })).toBeInTheDocument();
    });

    const btnPreco = screen.getByRole('button', { name: /solicitar alteração/i });
    fireEvent.click(btnPreco);

    expect(screen.getByRole('heading', { name: /solicitar alteração de preço oficial/i })).toBeInTheDocument();
  });
});
