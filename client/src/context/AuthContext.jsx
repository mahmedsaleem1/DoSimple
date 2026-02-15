import { createContext, useContext, useState, useEffect, useCallback } from 'react';
import { authService } from '../services/authService';
import toast from 'react-hot-toast';

const AuthContext = createContext(null);

export function AuthProvider({ children }) {
  const [user, setUser] = useState(null);
  const [loading, setLoading] = useState(true);
  const [isAuthenticated, setIsAuthenticated] = useState(false);

  // Initialize auth state from localStorage
  useEffect(() => {
    const initAuth = async () => {
      const token = localStorage.getItem('token');
      const savedUser = localStorage.getItem('user');

      if (token && savedUser) {
        try {
          // Verify token is still valid
          await authService.verifyToken();
          const parsed = JSON.parse(savedUser);
          setUser(parsed);
          setIsAuthenticated(true);
        } catch {
          // Token expired or invalid
          localStorage.removeItem('token');
          localStorage.removeItem('user');
        }
      }
      setLoading(false);
    };

    initAuth();
  }, []);

  const login = useCallback(async (credentials) => {
    const response = await authService.login(credentials);
    const { token, email, name, role, expiresAt } = response.data;

    const userData = { email, name, role, expiresAt };
    localStorage.setItem('token', token);
    localStorage.setItem('user', JSON.stringify(userData));

    setUser(userData);
    setIsAuthenticated(true);
    toast.success(`Welcome back, ${name}!`);
    return response.data;
  }, []);

  const register = useCallback(async (data) => {
    const response = await authService.register(data);
    if (response.data.emailSent === false) {
      toast.error('Account created, but verification email could not be sent. Please try again later.');
    } else {
      toast.success('Account created! Please check your email to verify.');
    }
    return response.data;
  }, []);

  const logout = useCallback(() => {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    setUser(null);
    setIsAuthenticated(false);
    toast.success('Logged out successfully');
  }, []);

  const isAdmin = user?.role === 'Admin' || user?.role === 'SuperAdmin';

  const value = {
    user,
    loading,
    isAuthenticated,
    isAdmin,
    login,
    register,
    logout,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}
