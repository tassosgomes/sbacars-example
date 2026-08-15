import type { HTMLAttributes, ReactNode } from 'react';

export interface CardProps extends HTMLAttributes<HTMLDivElement> {
  title?: string;
  description?: string;
  children?: ReactNode;
}

export function Card({ title, description, children, className = '', ...props }: CardProps) {
  return (
    <div
      className={[
        'rounded-lg border border-border bg-background p-6 shadow-sm',
        className,
      ].join(' ')}
      {...props}
    >
      {title ? <h3 className="text-lg font-semibold text-neutral-foreground">{title}</h3> : null}
      {description ? (
        <p className="mt-1 text-sm text-muted">{description}</p>
      ) : null}
      {children ? <div className={title || description ? 'mt-4' : ''}>{children}</div> : null}
    </div>
  );
}
