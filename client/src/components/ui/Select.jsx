import { forwardRef } from 'react';
import { clsx } from 'clsx';

const Select = forwardRef(
  ({ label, error, options = [], placeholder, className = '', ...props }, ref) => {
    return (
      <div className="space-y-1.5">
        {label && (
          <label className="block text-sm font-medium text-surface-700">
            {label}
          </label>
        )}
        <select
          ref={ref}
          className={clsx('input-field', error && 'input-error', className)}
          {...props}
        >
          {placeholder && (
            <option value="">{placeholder}</option>
          )}
          {options.map((opt) => (
            <option key={opt.value} value={opt.value}>
              {opt.label}
            </option>
          ))}
        </select>
        {error && <p className="text-sm text-red-500">{error}</p>}
      </div>
    );
  }
);

Select.displayName = 'Select';
export default Select;
