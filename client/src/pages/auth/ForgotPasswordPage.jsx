import { useState } from 'react';
import { Link } from 'react-router-dom';
import { HiOutlineMail, HiOutlineArrowLeft } from 'react-icons/hi';
import { authService } from '../../services/authService';
import { Button, Input } from '../../components/ui';
import toast from 'react-hot-toast';

export default function ForgotPasswordPage() {
  const [email, setEmail] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const [sent, setSent] = useState(false);

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!email) {
      setError('Email is required');
      return;
    }

    setLoading(true);
    try {
      await authService.forgotPassword({ email });
      setSent(true);
      toast.success('Reset link sent! Check your email.');
    } catch (err) {
      toast.error(err.response?.data?.message || 'Failed to send reset link');
    } finally {
      setLoading(false);
    }
  };

  if (sent) {
    return (
      <div className="animate-fade-in text-center">
        <div className="mx-auto flex h-14 w-14 items-center justify-center rounded-full bg-emerald-100 mb-4">
          <HiOutlineMail className="h-7 w-7 text-emerald-600" />
        </div>
        <h2 className="text-2xl font-bold text-surface-900">Check your email</h2>
        <p className="mt-2 text-sm text-surface-500 max-w-sm mx-auto">
          We&apos;ve sent a password reset link to <strong>{email}</strong>. 
          It may take a few minutes to arrive.
        </p>
        <Link
          to="/auth/login"
          className="inline-flex items-center gap-1 mt-6 text-sm font-medium text-primary-600 hover:text-primary-700"
        >
          <HiOutlineArrowLeft className="h-4 w-4" />
          Back to login
        </Link>
      </div>
    );
  }

  return (
    <div className="animate-fade-in">
      <h2 className="text-2xl font-bold text-surface-900">Forgot password?</h2>
      <p className="mt-2 text-sm text-surface-500">
        No worries, we&apos;ll send you reset instructions.
      </p>

      <form onSubmit={handleSubmit} className="mt-8 space-y-5">
        <Input
          label="Email address"
          type="email"
          icon={HiOutlineMail}
          placeholder="you@example.com"
          value={email}
          onChange={(e) => {
            setEmail(e.target.value);
            setError('');
          }}
          error={error}
        />

        <Button type="submit" loading={loading} className="w-full">
          Send reset link
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
