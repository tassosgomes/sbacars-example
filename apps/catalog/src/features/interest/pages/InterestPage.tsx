import { useParams } from 'react-router-dom';
import { EmptyState } from '@shared/components/EmptyState';

export function InterestPage() {
  const { id } = useParams<{ id: string }>();

  return (
    <section>
      <h1 className="text-3xl font-bold text-neutral-foreground">Express Interest</h1>
      <p className="mt-2 text-muted">
        Interest capture for vehicle {id ?? 'unknown'} (D03).
      </p>
      <div className="mt-8">
        <EmptyState
          title="Interest form is not implemented yet"
          description="Buyers will submit interest requests from this page."
        />
      </div>
    </section>
  );
}
