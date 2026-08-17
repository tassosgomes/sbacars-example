export interface TableSkeletonProps {
  rows?: number;
  cols?: number;
}

export function TableSkeleton({ rows = 6, cols = 7 }: TableSkeletonProps) {
  return (
    <div className="w-full animate-pulse divide-y divide-border border-b border-border">
      {Array.from({ length: rows }).map((_, rIdx) => (
        <div key={rIdx} className="flex items-center gap-4 px-6 py-4">
          {Array.from({ length: cols }).map((_, cIdx) => (
            <div
              key={cIdx}
              className={[
                'h-4 rounded bg-neutral-200',
                cIdx === 0 ? 'w-1/3' : 'w-1/6',
              ].join(' ')}
            />
          ))}
        </div>
      ))}
    </div>
  );
}
