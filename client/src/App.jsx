import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { Toaster } from 'react-hot-toast';
import { AuthProvider } from './context/AuthContext';
import { ProtectedRoute, AdminRoute, PublicRoute } from './components/guards/RouteGuards';

// Layouts
import DashboardLayout from './layouts/DashboardLayout';
import AuthLayout from './layouts/AuthLayout';

// Auth pages
import LoginPage from './pages/auth/LoginPage';
import RegisterPage from './pages/auth/RegisterPage';
import ForgotPasswordPage from './pages/auth/ForgotPasswordPage';
import ResetPasswordPage from './pages/auth/ResetPasswordPage';

// App pages
import DashboardPage from './pages/dashboard/DashboardPage';
import TaskListPage from './pages/tasks/TaskListPage';
import UserListPage from './pages/users/UserListPage';

export default function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <Routes>
          {/* Public auth routes */}
          <Route
            element={
              <PublicRoute>
                <AuthLayout />
              </PublicRoute>
            }
          >
            <Route path="/auth/login" element={<LoginPage />} />
            <Route path="/auth/register" element={<RegisterPage />} />
            <Route path="/auth/forgot-password" element={<ForgotPasswordPage />} />
            <Route path="/auth/reset-password" element={<ResetPasswordPage />} />
          </Route>

          {/* Protected app routes */}
          <Route
            element={
              <ProtectedRoute>
                <DashboardLayout />
              </ProtectedRoute>
            }
          >
            <Route path="/dashboard" element={<DashboardPage />} />
            <Route path="/tasks" element={<TaskListPage />} />

            {/* Admin only */}
            <Route
              path="/users"
              element={
                <AdminRoute>
                  <UserListPage />
                </AdminRoute>
              }
            />
          </Route>

          {/* Default redirect */}
          <Route path="/" element={<Navigate to="/dashboard" replace />} />
          <Route path="*" element={<Navigate to="/dashboard" replace />} />
        </Routes>

        {/* Global toast notifications */}
        <Toaster
          position="top-right"
          toastOptions={{
            duration: 4000,
            style: {
              background: '#fff',
              color: '#0f172a',
              boxShadow: '0 10px 40px -10px rgba(0, 0, 0, 0.1)',
              borderRadius: '12px',
              padding: '14px 18px',
              fontSize: '14px',
              border: '1px solid #e2e8f0',
            },
            success: {
              iconTheme: { primary: '#10b981', secondary: '#fff' },
            },
            error: {
              iconTheme: { primary: '#ef4444', secondary: '#fff' },
            },
          }}
        />
      </AuthProvider>
    </BrowserRouter>
  );
}
