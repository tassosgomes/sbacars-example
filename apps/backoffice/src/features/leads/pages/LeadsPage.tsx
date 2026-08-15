import { EmptyState } from '@shared/components/EmptyState';

export function LeadsPage() {
  return (
    <section>
      <h1 className="text-3xl font-bold text-neutral-foreground">Leads</h1>
      <p className="mt-2 text-muted">Interest follow-up and attendance (D03).</p>
      <div className="mt-8">
        <EmptyState
          title="No leads to display"
          description="Incoming interest requests will appear here."
        />
      </div>
    </section>
  );
}
