import { EmptyState } from '@shared/components/EmptyState';

export function PurchasesPage() {
  return (
    <section>
      <h1 className="text-3xl font-bold text-neutral-foreground">Purchases</h1>
      <p className="mt-2 text-muted">Assisted purchase workflow stub (D04).</p>
      <div className="mt-8">
        <EmptyState
          title="Purchase flow is not implemented yet"
          description="Qualified leads will progress to assisted purchase here."
        />
      </div>
    </section>
  );
}
