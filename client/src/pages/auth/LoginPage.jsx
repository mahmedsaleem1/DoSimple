import { useState } from 'react';
import { Link, useNavigate, useLocation } from 'react-router-dom';
import { HiOutlineMail, HiOutlineLockClosed } from 'react-icons/hi';
import { useAuth } from '../../context/AuthContext';
import { Button, Input } from '../../components/ui';
import toast from 'react-hot-toast';

export default function LoginPage() {
  const [form, setForm] = useState({ email: '', password: '' });
  const [errors, setErrors] = useState({});
  const [loading, setLoading] = useState(false);
  const { login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const from = location.state?.from?.pathname || '/dashboard';

  const validate = () => {
    const errs = {};
    if (!form.email) errs.email = 'Email is required';
    else if (!/\S+@\S+\.\S+/.test(form.email)) errs.email = 'Invalid email address';
    if (!form.password) errs.password = 'Password is required';
    setErrors(errs);
    return Object.keys(errs).length === 0;
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!validate()) return;

    setLoading(true);
    try {
      await login(form);
      navigate(from, { replace: true });
    } catch (err) {
      const msg = err.response?.data?.message || 'Login failed';
      toast.error(msg);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="animate-fade-in">
      <h2 className="text-2xl font-bold text-surface-900">Welcome back</h2>
      <p className="mt-2 text-sm text-surface-500">
        Sign in to your account to continue
      </p>

      <form onSubmit={handleSubmit} className="mt-8 space-y-5">
        <Input
          label="Email address"
          type="email"
          icon={HiOutlineMail}
          placeholder="you@example.com"
          value={form.email}
          onChange={(e) => setForm({ ...form, email: e.target.value })}
          error={errors.email}
        />

        <Input
          label="Password"
          type="password"
          icon={HiOutlineLockClosed}
          placeholder="••••••••"
          value={form.password}
          onChange={(e) => setForm({ ...form, password: e.target.value })}
          error={errors.password}
        />

        <div className="flex items-center justify-between">
          <label className="flex items-center gap-2">
            <input
              type="checkbox"
              className="h-4 w-4 rounded border-surface-300 text-primary-600 focus:ring-primary-500"
            />
            <span className="text-sm text-surface-600">Remember me</span>
          </label>
          <Link
            to="/auth/forgot-password"
            className="text-sm font-medium text-primary-600 hover:text-primary-700"
          >
            Forgot password?
          </Link>
        </div>

        <Button type="submit" loading={loading} className="w-full">
          Sign in
        </Button>
      </form>

      <p className="mt-6 text-center text-sm text-surface-500">
        Don&apos;t have an account?{' '}
        <Link
          to="/auth/register"
          className="font-semibold text-primary-600 hover:text-primary-700"
        >
          Create account
        </Link>
      </p>
    </div>
  );
}
