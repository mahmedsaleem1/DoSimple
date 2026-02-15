import { HiChevronLeft, HiChevronRight } from 'react-icons/hi';
import { clsx } from 'clsx';

export default function Pagination({ page, totalPages, onPageChange }) {
  if (totalPages <= 1) return null;

  const pages = [];
  const maxVisible = 5;
  let start = Math.max(1, page - Math.floor(maxVisible / 2));
  let end = Math.min(totalPages, start + maxVisible - 1);
  if (end - start + 1 < maxVisible) {
    start = Math.max(1, end - maxVisible + 1);
  }

  for (let i = start; i <= end; i++) {
    pages.push(i);
  }

  return (
    <nav className="flex items-center justify-between px-1 py-3">
      <p className="text-sm text-surface-500">
        Page <span className="font-medium text-surface-700">{page}</span> of{' '}
        <span className="font-medium text-surface-700">{totalPages}</span>
      </p>
      <div className="flex items-center gap-1">
        <button
          onClick={() => onPageChange(page - 1)}
          disabled={page <= 1}
          className="p-2 rounded-lg text-surface-500 hover:bg-surface-100 disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
        >
          <HiChevronLeft className="h-4 w-4" />
        </button>

        {start > 1 && (
          <>
            <PageButton page={1} current={page} onClick={onPageChange} />
            {start > 2 && <span className="px-1 text-surface-400">...</span>}
          </>
        )}

        {pages.map((p) => (
          <PageButton key={p} page={p} current={page} onClick={onPageChange} />
        ))}

        {end < totalPages && (
          <>
            {end < totalPages - 1 && <span className="px-1 text-surface-400">...</span>}
            <PageButton page={totalPages} current={page} onClick={onPageChange} />
          </>
        )}

        <button
          onClick={() => onPageChange(page + 1)}
          disabled={page >= totalPages}
          className="p-2 rounded-lg text-surface-500 hover:bg-surface-100 disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
        >
          <HiChevronRight className="h-4 w-4" />
        </button>
      </div>
    </nav>
  );
}

function PageButton({ page, current, onClick }) {
  return (
    <button
      onClick={() => onClick(page)}
      className={clsx(
        'min-w-[36px] h-9 rounded-lg text-sm font-medium transition-colors',
        page === current
          ? 'bg-primary-600 text-white'
          : 'text-surface-600 hover:bg-surface-100'
      )}
    >
      {page}
    </button>
  );
}
