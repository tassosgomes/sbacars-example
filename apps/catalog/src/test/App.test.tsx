import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { AppRouter } from '@app/router';

describe('Catalog app', () => {
  it('renders the public layout with home page shell', () => {
    render(<AppRouter />);

    expect(screen.getByText('AutoTransparência')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: /vehicle catalog/i })).toBeInTheDocument();
    expect(screen.getByText(/catalog home is not implemented yet/i)).toBeInTheDocument();
  });
});
