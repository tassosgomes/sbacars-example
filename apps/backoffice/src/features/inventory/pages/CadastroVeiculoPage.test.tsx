import { screen, waitFor, fireEvent } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { Routes, Route } from 'react-router-dom';
import { CadastroVeiculoPage } from './CadastroVeiculoPage';
import { renderWithProviders } from '@/test/test-utils';

describe('T02 - CadastroVeiculoPage', () => {
  it('renderiza o formulário de cadastro com valores default e aviso de salvamento parcial', () => {
    renderWithProviders(
      <Routes>
        <Route path="/estoque/novo" element={<CadastroVeiculoPage />} />
      </Routes>,
      { initialEntries: ['/estoque/novo'] }
    );

    expect(screen.getByRole('heading', { name: /cadastrar novo veículo/i })).toBeInTheDocument();
    expect(screen.getByText(/cadastro flexível \(rf-01\)/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/tipo de veículo/i)).toBeInTheDocument();
  });

  it('permite submeter dados e chama API de cadastro', async () => {
    renderWithProviders(
      <Routes>
        <Route path="/estoque/novo" element={<CadastroVeiculoPage />} />
        <Route path="/estoque/:ofertaId" element={<div>Detalhe da Oferta Criada</div>} />
      </Routes>,
      { initialEntries: ['/estoque/novo'] }
    );

    fireEvent.change(screen.getByLabelText(/placa/i), { target: { value: 'ABC1D23' } });
    fireEvent.change(screen.getByLabelText(/^marca$/i), { target: { value: 'Honda' } });
    fireEvent.change(screen.getByLabelText(/^modelo$/i), { target: { value: 'Civic' } });

    const btnSalvar = screen.getByRole('button', { name: /salvar e continuar/i });
    fireEvent.click(btnSalvar);

    await waitFor(() => {
      expect(screen.getByText('Detalhe da Oferta Criada')).toBeInTheDocument();
    });
  });
});
