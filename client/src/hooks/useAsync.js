import { useState, useCallback } from 'react';

export function useAsync(asyncFn) {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [data, setData] = useState(null);

  const execute = useCallback(
    async (...args) => {
      setLoading(true);
      setError(null);
      try {
        const result = await asyncFn(...args);
        setData(result.data ?? result);
        return result;
      } catch (err) {
        const message =
          err.response?.data?.message || err.message || 'An error occurred';
        setError(message);
        throw err;
      } finally {
        setLoading(false);
      }
    },
    [asyncFn]
  );

  return { loading, error, data, execute };
}
