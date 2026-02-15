import { useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { HiOutlineLockClosed, HiOutlineArrowLeft, HiOutlineCheck } from 'react-icons/hi';
import { authService } from '../../services/authService';
import { Button, Input } from '../../components/ui';
import toast from 'react-hot-toast';

export default function ResetPasswordPage() {
  const [searchParams] = useSearchParams();
  const token = searchParams.get('token') || '';
  const email = searchParams.get('email') || '';

  const [form, setForm] = useState({ email, token, newPassword: '', confirmPassword: '' });
  const [errors, setErrors] = useState({});
  const [loading, setLoading] = useState(false);
  const [success, setSuccess] = useState(false);

  const validate = () => {
    const errs = {};
    if (!form.email) errs.email = 'Email is required';
    if (!form.newPassword) errs.newPassword = 'New password is required';
    else if (form.newPassword.length < 6) errs.newPassword = 'Minimum 6 characters';
    if (form.newPassword !== form.confirmPassword) errs.confirmPassword = 'Passwords do not match';
    setErrors(errs);
    return Object.keys(errs).length === 0;
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!validate()) return;

    setLoading(true);
    try {
      await authService.resetPassword({
        email: form.email,
        token: form.token,
        newPassword: form.newPassword,
      });
      setSuccess(true);
      toast.success('Password reset successfully!');
    } catch (err) {
      toast.error(err.response?.data?.message || 'Password reset failed');
    } finally {
      setLoading(false);
    }
  };

  if (success) {
    return (
      <div className="animate-fade-in text-center">
        <div className="mx-auto flex h-14 w-14 items-center justify-center rounded-full bg-emerald-100 mb-4">
          <HiOutlineCheck className="h-7 w-7 text-emerald-600" />
        </div>
        <h2 className="text-2xl font-bold text-surface-900">Password reset!</h2>
        <p className="mt-2 text-sm text-surface-500">
          Your password has been successfully reset. You can now sign in with your new password.
        </p>
        <Link to="/auth/login">
          <Button className="mt-6">Go to login</Button>
        </Link>
      </div>
    );
  }

  return (
    <div className="animate-fade-in">
      <h2 className="text-2xl font-bold text-surface-900">Reset password</h2>
      <p className="mt-2 text-sm text-surface-500">
        Enter your new password below
      </p>

      <form onSubmit={handleSubmit} className="mt-8 space-y-5">
        <Input
          label="New password"
          type="password"
          icon={HiOutlineLockClosed}
          placeholder="••••••••"
          value={form.newPassword}
          onChange={(e) => setForm({ ...form, newPassword: e.target.value })}
          error={errors.newPassword}
        />

        <Input
          label="Confirm new password"
          type="password"
          icon={HiOutlineLockClosed}
          placeholder="••••••••"
          value={form.confirmPassword}
          onChange={(e) => setForm({ ...form, confirmPassword: e.target.value })}
          error={errors.confirmPassword}
        />

        <Button type="submit" loading={loading} className="w-full">
          Reset password
        </Button>
      </form>

      <Link
        to="/auth/login"
        className="inline-flex items-center gap-1 mt-6 text-sm font-medium text-surface-600 hover:text-surface-900"
      >
        <HiOutlineArrowLeft className="h-4 w-4" />
        Back to login
      </Link>
    </div>
  );
}
