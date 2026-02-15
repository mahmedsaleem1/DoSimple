import { clsx } from 'clsx';

export default function Card({ children, className = '', padding = true, ...props }) {
  return (
    <div className={clsx('card', padding && 'p-6', className)} {...props}>
      {children}
    </div>
  );
}

Card.Header = function CardHeader({ children, className = '' }) {
  return (
    <div className={clsx('flex items-center justify-between mb-4', className)}>
      {children}
    </div>
  );
};

Card.Title = function CardTitle({ children, className = '' }) {
  return (
    <h3 className={clsx('text-lg font-semibold text-surface-900', className)}>
      {children}
    </h3>
  );
};

Card.Description = function CardDescription({ children, className = '' }) {
  return (
    <p className={clsx('text-sm text-surface-500', className)}>
      {children}
    </p>
  );
};
