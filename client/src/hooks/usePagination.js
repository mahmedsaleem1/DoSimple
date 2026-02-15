import { useState, useEffect, useCallback, useRef } from 'react';

export function usePagination(fetchFn, initialParams = {}) {
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [page, setPage] = useState(1);
  const [pageSize] = useState(initialParams.pageSize || 10);
  const [filters, setFilters] = useState(initialParams);
  const abortRef = useRef(null);

  const fetchData = useCallback(
    async (currentPage, currentFilters) => {
      setLoading(true);
      setError(null);

      try {
        const params = {
          ...currentFilters,
          pageNumber: currentPage,
          pageSize,
        };
        // Remove empty values
        Object.keys(params).forEach((key) => {
          if (params[key] === '' || params[key] === null || params[key] === undefined) {
            delete params[key];
          }
        });
        const response = await fetchFn(params);
        setData(response.data);
      } catch (err) {
        if (err.name !== 'CanceledError') {
          setError(err.response?.data?.message || 'Failed to fetch data');
        }
      } finally {
        setLoading(false);
      }
    },
    [fetchFn, pageSize]
  );

  useEffect(() => {
    fetchData(page, filters);
  }, [page, filters, fetchData]);

  const refresh = useCallback(() => {
    fetchData(page, filters);
  }, [fetchData, page, filters]);

  const goToPage = useCallback((p) => setPage(p), []);
  const nextPage = useCallback(() => setPage((prev) => prev + 1), []);
  const prevPage = useCallback(() => setPage((prev) => Math.max(1, prev - 1)), []);

  const updateFilters = useCallback((newFilters) => {
    setPage(1);
    setFilters((prev) => ({ ...prev, ...newFilters }));
  }, []);

  return {
    data,
    loading,
    error,
    page,
    pageSize,
    filters,
    setPage: goToPage,
    nextPage,
    prevPage,
    updateFilters,
    refresh,
  };
}
