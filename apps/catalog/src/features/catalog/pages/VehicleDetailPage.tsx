import { useParams } from 'react-router-dom';
import { EmptyState } from '@shared/components/EmptyState';

export function VehicleDetailPage() {
  const { id } = useParams<{ id: string }>();

  return (
    <section>
      <h1 className="text-3xl font-bold text-neutral-foreground">Vehicle Detail</h1>
      <p className="mt-2 text-muted">Vehicle ID: {id ?? 'unknown'}</p>
      <div className="mt-8">
        <EmptyState
          title="Vehicle details are not available yet"
          description="Full vehicle information will be shown here."
        />
      </div>
    </section>
  );
}
