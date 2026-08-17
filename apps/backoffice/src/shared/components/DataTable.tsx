import type { ReactNode } from 'react';
import { TableSkeleton } from './TableSkeleton';

export interface Column<T> {
  key: string;
  header: ReactNode;
  render: (item: T) => ReactNode;
  align?: 'left' | 'center' | 'right';
  className?: string;
}

export interface DataTableProps<T> {
  columns: Column<T>[];
  data?: T[];
  keyExtractor: (item: T) => string;
  onRowClick?: (item: T) => void;
  isLoading?: boolean;
  emptyMessage?: ReactNode;
  caption?: string;
}

export function DataTable<T>({
  columns,
  data = [],
  keyExtractor,
  onRowClick,
  isLoading = false,
  emptyMessage = 'Nenhum registro encontrado.',
  caption,
}: DataTableProps<T>) {
  if (isLoading) {
    return <TableSkeleton cols={columns.length} />;
  }

  if (data.length === 0) {
    return (
      <div className="flex min-h-[200px] items-center justify-center p-8 text-center text-sm text-muted">
        {emptyMessage}
      </div>
    );
  }

  return (
    <div className="w-full overflow-x-auto">
      <table className="w-full text-left border-collapse text-sm">
        {caption && <caption className="sr-only">{caption}</caption>}
        <thead>
          <tr className="border-b border-border bg-neutral-50 text-xs font-semibold uppercase tracking-wider text-neutral-600">
            {columns.map((col) => (
              <th
                key={col.key}
                scope="col"
                className={[
                  'px-5 py-3.5',
                  col.align === 'right' ? 'text-right' : col.align === 'center' ? 'text-center' : 'text-left',
                  col.className ?? '',
                ].join(' ')}
              >
                {col.header}
              </th>
            ))}
          </tr>
        </thead>
        <tbody className="divide-y divide-border bg-surface">
          {data.map((item) => {
            const key = keyExtractor(item);
            const isClickable = !!onRowClick;
            return (
              <tr
                key={key}
                onClick={() => onRowClick?.(item)}
                className={[
                  'transition-colors',
                  isClickable ? 'cursor-pointer hover:bg-neutral-50' : '',
                ].join(' ')}
              >
                {columns.map((col) => (
                  <td
                    key={col.key}
                    className={[
                      'px-5 py-4 align-middle text-neutral-800',
                      col.align === 'right' ? 'text-right' : col.align === 'center' ? 'text-center' : 'text-left',
                      col.className ?? '',
                    ].join(' ')}
                  >
                    {col.render(item)}
                  </td>
                ))}
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}
