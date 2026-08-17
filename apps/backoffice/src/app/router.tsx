import { BrowserRouter, Route, Routes, Navigate } from 'react-router-dom';
import { BackofficeLayout } from '@app/layouts/BackofficeLayout';
import { CallbackPage, LoginPage, ProtectedRoute } from '@features/auth';
import { DashboardPage } from '@features/dashboard';
import {
  ListaEstoquePage,
  CadastroVeiculoPage,
  DetalheOfertaPage,
  FatosConhecidosPage,
  FilaValidacaoPage,
  DetalheSolicitacaoPage,
} from '@features/inventory';
import { LeadsPage } from '@features/leads';
import { PurchasesPage } from '@features/purchase';

export function AppRouter() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/auth/callback" element={<CallbackPage />} />

        <Route element={<ProtectedRoute />}>
          <Route element={<BackofficeLayout />}>
            <Route index element={<DashboardPage />} />

            {/* Rotas de Estoque Curado (D02) */}
            <Route path="estoque" element={<ListaEstoquePage />} />
            <Route path="estoque/novo" element={<CadastroVeiculoPage />} />
            <Route path="estoque/:ofertaId" element={<DetalheOfertaPage />} />
            <Route path="estoque/:ofertaId/editar" element={<CadastroVeiculoPage />} />
            <Route path="estoque/:ofertaId/fatos" element={<FatosConhecidosPage />} />

            {/* Alias de compatibilidade */}
            <Route path="inventory" element={<Navigate to="/estoque" replace />} />

            {/* Rotas de Fila de Validação (Requer estoque:validar) */}
            <Route element={<ProtectedRoute permissao="estoque:validar" />}>
              <Route path="validacao" element={<FilaValidacaoPage />} />
              <Route path="validacao/:solicitacaoId" element={<DetalheSolicitacaoPage />} />
            </Route>

            <Route path="leads" element={<LeadsPage />} />
            <Route path="purchases" element={<PurchasesPage />} />
          </Route>
        </Route>

        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  );
}
