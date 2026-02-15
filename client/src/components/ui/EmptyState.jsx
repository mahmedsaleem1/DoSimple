import { clsx } from 'clsx';

export default function EmptyState({
  icon: Icon,
  title,
  description,
  action,
  className = '',
}) {
  return (
    <div className={clsx('flex flex-col items-center justify-center py-12 px-4 text-center', className)}>
      {Icon && (
        <div className="mx-auto flex h-14 w-14 items-center justify-center rounded-full bg-surface-100 mb-4">
          <Icon className="h-7 w-7 text-surface-400" />
        </div>
      )}
      <h3 className="text-base font-semibold text-surface-900">{title}</h3>
      {description && (
        <p className="mt-1 text-sm text-surface-500 max-w-sm">{description}</p>
      )}
      {action && <div className="mt-4">{action}</div>}
    </div>
  );
}
