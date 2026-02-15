import { forwardRef } from 'react';
import { clsx } from 'clsx';

const Input = forwardRef(
  ({ label, error, helperText, icon: Icon, className = '', ...props }, ref) => {
    return (
      <div className="space-y-1.5">
        {label && (
          <label className="block text-sm font-medium text-surface-700">
            {label}
          </label>
        )}
        <div className="relative">
          {Icon && (
            <div className="pointer-events-none absolute inset-y-0 left-0 flex items-center pl-3">
              <Icon className="h-4 w-4 text-surface-400" />
            </div>
          )}
          <input
            ref={ref}
            className={clsx(
              'input-field',
              Icon && 'pl-10',
              error && 'input-error',
              className
            )}
            {...props}
          />
        </div>
        {error && <p className="text-sm text-red-500">{error}</p>}
        {helperText && !error && (
          <p className="text-sm text-surface-500">{helperText}</p>
        )}
      </div>
    );
  }
);

Input.displayName = 'Input';
export default Input;
