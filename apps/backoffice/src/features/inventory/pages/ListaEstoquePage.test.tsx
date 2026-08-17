import { screen, waitFor, fireEvent } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { ListaEstoquePage } from './ListaEstoquePage';
import { renderWithProviders } from '@/test/test-utils';

describe('T01 - ListaEstoquePage', () => {
  it('renderiza o cabeçalho e a lista de ofertas do mock MSW', async () => {
    renderWithProviders(<ListaEstoquePage />, { initialEntries: ['/estoque'] });

    expect(screen.getByRole('heading', { name: /estoque curado/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /cadastrar veículo/i })).toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getByText('Honda Civic EXL 2.0')).toBeInTheDocument();
    });

    expect(screen.getByText('Toyota Corolla XEi 2.0')).toBeInTheDocument();
    // 'Elegível' appears in badge and situation filter chip
    expect(screen.getAllByText('Elegível').length).toBeGreaterThan(0);
    expect(screen.getAllByText('Em preparação').length).toBeGreaterThan(0);
  });

  it('permite filtrar por situação clicando nos chips', async () => {
    renderWithProviders(<ListaEstoquePage />, { initialEntries: ['/estoque'] });

    await waitFor(() => {
      expect(screen.getByText('Honda Civic EXL 2.0')).toBeInTheDocument();
    });

    const chipEmPreparacao = screen.getByRole('button', { name: 'Em preparação' });
    fireEvent.click(chipEmPreparacao);

    expect(chipEmPreparacao).toHaveClass('bg-[#2E2E3A]');
  });
});
