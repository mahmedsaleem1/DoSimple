import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { HiOutlineMail, HiOutlineLockClosed, HiOutlineUser } from 'react-icons/hi';
import { useAuth } from '../../context/AuthContext';
import { Button, Input } from '../../components/ui';
import toast from 'react-hot-toast';

export default function RegisterPage() {
  const [form, setForm] = useState({ name: '', email: '', password: '', confirmPassword: '' });
  const [errors, setErrors] = useState({});
  const [loading, setLoading] = useState(false);
  const { register } = useAuth();
  const navigate = useNavigate();

  const validate = () => {
    const errs = {};
    if (!form.name.trim()) errs.name = 'Name is required';
    if (!form.email) errs.email = 'Email is required';
    else if (!/\S+@\S+\.\S+/.test(form.email)) errs.email = 'Invalid email address';
    if (!form.password) errs.password = 'Password is required';
    else if (form.password.length < 6) errs.password = 'Password must be at least 6 characters';
    if (form.password !== form.confirmPassword)
      errs.confirmPassword = 'Passwords do not match';
    setErrors(errs);
    return Object.keys(errs).length === 0;
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!validate()) return;

    setLoading(true);
    try {
      await register({ name: form.name, email: form.email, password: form.password });
      navigate('/auth/login');
    } catch (err) {
      const msg = err.response?.data?.message || 'Registration failed';
      toast.error(msg);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="animate-fade-in">
      <h2 className="text-2xl font-bold text-surface-900">Create an account</h2>
      <p className="mt-2 text-sm text-surface-500">
        Get started with DoSimple for free
      </p>

      <form onSubmit={handleSubmit} className="mt-8 space-y-5">
        <Input
          label="Full name"
          type="text"
          icon={HiOutlineUser}
          placeholder="John Doe"
          value={form.name}
          onChange={(e) => setForm({ ...form, name: e.target.value })}
          error={errors.name}
        />

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
          helperText="Must be at least 6 characters"
        />

        <Input
          label="Confirm password"
          type="password"
          icon={HiOutlineLockClosed}
          placeholder="••••••••"
          value={form.confirmPassword}
          onChange={(e) => setForm({ ...form, confirmPassword: e.target.value })}
          error={errors.confirmPassword}
        />

        <Button type="submit" loading={loading} className="w-full">
          Create account
        </Button>
      </form>

      <p className="mt-6 text-center text-sm text-surface-500">
        Already have an account?{' '}
        <Link
          to="/auth/login"
          className="font-semibold text-primary-600 hover:text-primary-700"
        >
          Sign in
        </Link>
      </p>
    </div>
  );
}
