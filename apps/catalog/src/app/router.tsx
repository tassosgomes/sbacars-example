import { BrowserRouter, Route, Routes } from 'react-router-dom';
import { PublicLayout } from '@app/layouts/PublicLayout';
import { HomePage, VehicleDetailPage, VehicleListPage } from '@features/catalog';
import { InterestPage } from '@features/interest';

export function AppRouter() {
  return (
    <BrowserRouter>
      <Routes>
        <Route element={<PublicLayout />}>
          <Route index element={<HomePage />} />
          <Route path="vehicles" element={<VehicleListPage />} />
          <Route path="vehicles/:id" element={<VehicleDetailPage />} />
          <Route path="vehicles/:id/interest" element={<InterestPage />} />
        </Route>
      </Routes>
    </BrowserRouter>
  );
}
