import { Button } from '@sbacars/ui';
import { useAuth } from 'react-oidc-context';
import { NavLink, Outlet } from 'react-router-dom';

const navItems = [
  { to: '/', label: 'Dashboard', end: true },
  { to: '/inventory', label: 'Inventory' },
  { to: '/leads', label: 'Leads' },
  { to: '/purchases', label: 'Purchases' },
];

export function BackofficeLayout() {
  const auth = useAuth();
  const displayName =
    (auth.user?.profile.name as string | undefined) ??
    (auth.user?.profile.preferred_username as string | undefined) ??
    'User';

  return (
    <div className="flex min-h-screen">
      <aside className="flex w-64 flex-col border-r border-border bg-surface">
        <div className="border-b border-border px-6 py-5">
          <p className="text-lg font-semibold text-primary">AutoTransparência</p>
          <p className="text-xs text-muted">Backoffice</p>
        </div>
        <nav className="flex flex-1 flex-col gap-1 p-4">
          {navItems.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.end}
              className={({ isActive }) =>
                [
                  'rounded-md px-3 py-2 text-sm font-medium transition-colors',
                  isActive
                    ? 'bg-primary text-primary-foreground'
                    : 'text-neutral-600 hover:bg-neutral-100',
                ].join(' ')
              }
            >
              {item.label}
            </NavLink>
          ))}
        </nav>
      </aside>

      <div className="flex flex-1 flex-col">
        <header className="flex h-16 items-center justify-between border-b border-border bg-background px-8">
          <h1 className="text-sm font-medium text-muted">Operations workspace</h1>
          <div className="flex items-center gap-4">
            <span className="text-sm text-muted">{displayName}</span>
            <Button type="button" variant="ghost" size="sm" onClick={() => auth.signoutRedirect()}>
              Sign out
            </Button>
          </div>
        </header>
        <main className="flex-1 p-8">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
