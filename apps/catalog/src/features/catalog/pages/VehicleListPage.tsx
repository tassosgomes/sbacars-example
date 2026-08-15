import { EmptyState } from '@shared/components/EmptyState';

export function VehicleListPage() {
  return (
    <section>
      <h1 className="text-3xl font-bold text-neutral-foreground">Vehicles</h1>
      <p className="mt-2 text-muted">Browse the curated catalog (D01).</p>
      <div className="mt-8">
        <EmptyState
          title="No vehicles to display"
          description="Vehicle listings will appear here once inventory is synced."
        />
      </div>
    </section>
  );
}
