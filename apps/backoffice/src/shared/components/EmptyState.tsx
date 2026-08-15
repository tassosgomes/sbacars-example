interface EmptyStateProps {
  title: string;
  description?: string;
}

export function EmptyState({ title, description }: EmptyStateProps) {
  return (
    <div className="rounded-lg border border-dashed border-border bg-surface p-8 text-center">
      <p className="text-sm font-medium text-muted">{title}</p>
      {description ? <p className="mt-2 text-sm text-neutral-600">{description}</p> : null}
    </div>
  );
}
