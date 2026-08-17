import { Button } from '@sbacars/ui';
import { useAuth } from 'react-oidc-context';
import { NavLink, Outlet } from 'react-router-dom';
import { useContagemPendentes } from '@/features/inventory/api/useSolicitacoes';

export function BackofficeLayout() {
  const auth = useAuth();
  const rawScope = (auth.user?.profile?.scope as string | undefined) ?? '';
  const scopes = rawScope.split(' ');
  const canValidate = scopes.includes('estoque:validar');

  const { data: contagem } = useContagemPendentes(auth.isAuthenticated && canValidate);

  const displayName =
    (auth.user?.profile?.name as string | undefined) ??
    (auth.user?.profile?.preferred_username as string | undefined) ??
    'Operador';

  const navItems = [
    { to: '/', label: 'Painel', end: true },
    { to: '/estoque', label: 'Estoque' },
    ...(canValidate
      ? [
          {
            to: '/validacao',
            label: 'Validação',
            badge: contagem?.total ?? 0,
            hasAlert: (contagem?.foraDoSla ?? 0) > 0,
          },
        ]
      : []),
    { to: '/leads', label: 'Interesses' },
    { to: '/purchases', label: 'Compras' },
  ];

  return (
    <div className="flex min-h-screen bg-background text-neutral-900">
      <aside className="flex w-64 flex-col border-r border-border bg-[#2E2E3A] text-white">
        <div className="border-b border-[#3c3c4b] px-6 py-5">
          <p className="text-lg font-semibold text-white tracking-tight">AutoTransparência</p>
          <p className="text-xs uppercase tracking-wider text-neutral-400 font-bold">Backoffice</p>
        </div>
        <nav className="flex flex-1 flex-col gap-1.5 p-4">
          {navItems.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.end}
              className={({ isActive }) =>
                [
                  'flex items-center justify-between rounded-md px-3.5 py-2.5 text-sm font-medium transition-colors',
                  isActive
                    ? 'bg-[#191925] text-white shadow-sm'
                    : 'text-neutral-300 hover:bg-[#383847] hover:text-white',
                ].join(' ')
              }
            >
              <span>{item.label}</span>
              {item.badge !== undefined && item.badge > 0 && (
                <span
                  className={[
                    'inline-flex items-center justify-center rounded-full px-2 py-0.5 text-xs font-bold',
                    item.hasAlert ? 'bg-danger text-white' : 'bg-neutral-600 text-white',
                  ].join(' ')}
                >
                  {item.badge}
                </span>
              )}
            </NavLink>
          ))}
        </nav>
      </aside>

      <div className="flex flex-1 flex-col min-w-0">
        <header className="flex h-16 items-center justify-between border-b border-border bg-surface px-8">
          <h1 className="text-sm font-medium text-muted">Área de operação</h1>
          <div className="flex items-center gap-4">
            <div className="flex items-center gap-2">
              <div className="w-8 h-8 rounded-full bg-primary text-white flex items-center justify-center text-xs font-bold">
                {displayName.slice(0, 2).toUpperCase()}
              </div>
              <span className="text-sm font-medium text-neutral-800">{displayName}</span>
            </div>
            <Button
              type="button"
              variant="ghost"
              size="sm"
              onClick={() => auth.signoutRedirect()}
            >
              Sair
            </Button>
          </div>
        </header>
        <main className="flex-1 p-8 overflow-y-auto">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
