import { EmptyState } from '@shared/components/EmptyState';

export function InventoryPage() {
  return (
    <section>
      <h1 className="text-3xl font-bold text-neutral-foreground">Inventory</h1>
      <p className="mt-2 text-muted">Curated stock management (D02).</p>
      <div className="mt-8">
        <EmptyState
          title="No inventory items yet"
          description="Vehicle stock will be managed from this area."
        />
      </div>
    </section>
  );
}
