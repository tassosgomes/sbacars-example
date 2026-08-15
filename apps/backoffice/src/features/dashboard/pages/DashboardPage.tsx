import { EmptyState } from '@shared/components/EmptyState';

export function DashboardPage() {
  return (
    <section>
      <h1 className="text-3xl font-bold text-neutral-foreground">Dashboard</h1>
      <p className="mt-2 text-muted">Operations overview placeholder.</p>
      <div className="mt-8">
        <EmptyState
          title="Dashboard metrics are not available yet"
          description="Summary widgets will appear here."
        />
      </div>
    </section>
  );
}
