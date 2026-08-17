import { screen, waitFor, fireEvent } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { Routes, Route } from 'react-router-dom';
import { FatosConhecidosPage } from './FatosConhecidosPage';
import { renderWithProviders } from '@/test/test-utils';

describe('T04 - FatosConhecidosPage', () => {
  it('renderiza os 3 blocos de fatos e preenche com os dados da oferta', async () => {
    renderWithProviders(
      <Routes>
        <Route path="/estoque/:ofertaId/fatos" element={<FatosConhecidosPage />} />
      </Routes>,
      { initialEntries: ['/estoque/3f2a8c14-9d31-4c7e-b8a1-5e6f0d2b9c47/fatos'] }
    );

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: /curadoria de fatos conhecidos/i })).toBeInTheDocument();
    });

    expect(screen.getByRole('heading', { name: /1\. origem do veículo/i })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: /2\. condição do veículo/i })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: /3\. histórico de sinistros e leilão/i })).toBeInTheDocument();
  });

  it('permite alternar entre informação disponível e limitação declarada', async () => {
    renderWithProviders(
      <Routes>
        <Route path="/estoque/:ofertaId/fatos" element={<FatosConhecidosPage />} />
      </Routes>,
      { initialEntries: ['/estoque/3f2a8c14-9d31-4c7e-b8a1-5e6f0d2b9c47/fatos'] }
    );

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: /1\. origem do veículo/i })).toBeInTheDocument();
    });

    const checkboxes = screen.getAllByRole('checkbox', { name: /informação indisponível/i });
    // Marca origem como indisponível
    fireEvent.click(checkboxes[0]);

    expect(screen.getAllByLabelText(/declaração de limitação/i).length).toBeGreaterThan(0);
  });
});
