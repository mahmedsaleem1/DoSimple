import { clsx } from 'clsx';

export default function Avatar({ name = '', size = 'md', className = '' }) {
  const initials = name
    .split(' ')
    .map((n) => n[0])
    .join('')
    .toUpperCase()
    .slice(0, 2);

  const sizeClasses = {
    sm: 'h-8 w-8 text-xs',
    md: 'h-10 w-10 text-sm',
    lg: 'h-12 w-12 text-base',
    xl: 'h-16 w-16 text-lg',
  };

  // Generate consistent color from name
  const colors = [
    'bg-blue-500',
    'bg-emerald-500',
    'bg-purple-500',
    'bg-orange-500',
    'bg-pink-500',
    'bg-cyan-500',
    'bg-indigo-500',
    'bg-teal-500',
  ];
  const colorIndex = name
    .split('')
    .reduce((acc, char) => acc + char.charCodeAt(0), 0) % colors.length;

  return (
    <div
      className={clsx(
        'inline-flex items-center justify-center rounded-full text-white font-semibold flex-shrink-0',
        sizeClasses[size],
        colors[colorIndex],
        className
      )}
    >
      {initials || '?'}
    </div>
  );
}
