import { Link, Outlet } from 'react-router-dom';

export function PublicLayout() {
  return (
    <div className="flex min-h-screen flex-col">
      <header className="border-b border-border bg-background">
        <div className="mx-auto flex h-16 max-w-6xl items-center justify-between px-4">
          <Link to="/" className="text-lg font-semibold text-primary">
            AutoTransparência
          </Link>
          <nav className="flex gap-6 text-sm font-medium text-neutral-600">
            <Link to="/" className="hover:text-primary">
              Home
            </Link>
            <Link to="/vehicles" className="hover:text-primary">
              Vehicles
            </Link>
          </nav>
        </div>
      </header>

      <main className="mx-auto w-full max-w-6xl flex-1 px-4 py-8">
        <Outlet />
      </main>

      <footer className="border-t border-border bg-surface py-6">
        <div className="mx-auto max-w-6xl px-4 text-center text-sm text-muted">
          Plataforma AutoTransparência — curated vehicle catalog
        </div>
      </footer>
    </div>
  );
}
