import { clsx } from 'clsx';

const colorMap = {
  // Task statuses
  Pending: 'bg-amber-50 text-amber-700 ring-amber-600/20',
  InProgress: 'bg-blue-50 text-blue-700 ring-blue-600/20',
  Completed: 'bg-emerald-50 text-emerald-700 ring-emerald-600/20',
  Cancelled: 'bg-surface-100 text-surface-600 ring-surface-500/20',

  // Task priorities
  Low: 'bg-surface-100 text-surface-600 ring-surface-500/20',
  Medium: 'bg-blue-50 text-blue-700 ring-blue-600/20',
  High: 'bg-orange-50 text-orange-700 ring-orange-600/20',
  Critical: 'bg-red-50 text-red-700 ring-red-600/20',

  // Roles
  User: 'bg-surface-100 text-surface-700 ring-surface-500/20',
  Admin: 'bg-purple-50 text-purple-700 ring-purple-600/20',
  SuperAdmin: 'bg-rose-50 text-rose-700 ring-rose-600/20',

  // Generic
  success: 'bg-emerald-50 text-emerald-700 ring-emerald-600/20',
  warning: 'bg-amber-50 text-amber-700 ring-amber-600/20',
  danger: 'bg-red-50 text-red-700 ring-red-600/20',
  info: 'bg-blue-50 text-blue-700 ring-blue-600/20',
  default: 'bg-surface-100 text-surface-600 ring-surface-500/20',
};

export default function Badge({ children, color = 'default', dot = false, className = '' }) {
  return (
    <span
      className={clsx(
        'badge ring-1 ring-inset',
        colorMap[color] || colorMap.default,
        className
      )}
    >
      {dot && (
        <span className="mr-1.5 h-1.5 w-1.5 rounded-full bg-current" />
      )}
      {children}
    </span>
  );
}
