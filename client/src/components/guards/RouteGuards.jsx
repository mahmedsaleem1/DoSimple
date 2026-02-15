import { Navigate, useLocation } from 'react-router-dom';
import { useAuth } from '../../context/AuthContext';
import { FullPageLoader } from '../ui/Spinner';

export function ProtectedRoute({ children }) {
  const { isAuthenticated, loading } = useAuth();
  const location = useLocation();

  if (loading) return <FullPageLoader />;

  if (!isAuthenticated) {
    return <Navigate to="/auth/login" state={{ from: location }} replace />;
  }

  return children;
}

export function AdminRoute({ children }) {
  const { isAuthenticated, isAdmin, loading } = useAuth();
  const location = useLocation();

  if (loading) return <FullPageLoader />;

  if (!isAuthenticated) {
    return <Navigate to="/auth/login" state={{ from: location }} replace />;
  }

  if (!isAdmin) {
    return <Navigate to="/dashboard" replace />;
  }

  return children;
}

export function PublicRoute({ children }) {
  const { isAuthenticated, loading } = useAuth();

  if (loading) return <FullPageLoader />;

  if (isAuthenticated) {
    return <Navigate to="/dashboard" replace />;
  }

  return children;
}
