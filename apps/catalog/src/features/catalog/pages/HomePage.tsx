import { EmptyState } from '@shared/components/EmptyState';

export function HomePage() {
  return (
    <section>
      <h1 className="text-3xl font-bold text-neutral-foreground">Vehicle Catalog</h1>
      <p className="mt-2 text-muted">Discover curated vehicles with transparent information.</p>
      <div className="mt-8">
        <EmptyState
          title="Catalog home is not implemented yet"
          description="Browse vehicles from the list page when inventory is connected."
        />
      </div>
    </section>
  );
}
