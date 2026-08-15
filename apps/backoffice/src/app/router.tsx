import { BrowserRouter, Route, Routes } from 'react-router-dom';
import { BackofficeLayout } from '@app/layouts/BackofficeLayout';
import { CallbackPage, LoginPage, ProtectedRoute } from '@features/auth';
import { DashboardPage } from '@features/dashboard';
import { InventoryPage } from '@features/inventory';
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
            <Route path="inventory" element={<InventoryPage />} />
            <Route path="leads" element={<LeadsPage />} />
            <Route path="purchases" element={<PurchasesPage />} />
          </Route>
        </Route>
      </Routes>
    </BrowserRouter>
  );
}
