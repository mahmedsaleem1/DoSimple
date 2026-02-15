import { useState, useEffect } from 'react';
import { userService } from '../../services/userService';
import { Modal, Button, Input } from '../../components/ui';
import toast from 'react-hot-toast';

export default function UserEditModal({ isOpen, onClose, user, onSuccess }) {
  const [form, setForm] = useState({ name: '', email: '' });
  const [errors, setErrors] = useState({});
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (user) {
      setForm({ name: user.name || '', email: user.email || '' });
    }
    setErrors({});
  }, [user, isOpen]);

  const validate = () => {
    const errs = {};
    if (!form.name.trim()) errs.name = 'Name is required';
    if (!form.email.trim()) errs.email = 'Email is required';
    else if (!/\S+@\S+\.\S+/.test(form.email)) errs.email = 'Invalid email';
    setErrors(errs);
    return Object.keys(errs).length === 0;
  };

  const handleSubmit = async () => {
    if (!validate()) return;

    setLoading(true);
    try {
      await userService.updateUser(user.id, form);
      toast.success('User updated!');
      onSuccess();
    } catch (err) {
      toast.error(err.response?.data?.message || 'Failed to update user');
    } finally {
      setLoading(false);
    }
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title="Edit User"
      size="md"
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button loading={loading} onClick={handleSubmit}>
            Save Changes
          </Button>
        </>
      }
    >
      <div className="space-y-4">
        <Input
          label="Name"
          placeholder="Full name"
          value={form.name}
          onChange={(e) => {
            setForm({ ...form, name: e.target.value });
            if (errors.name) setErrors({ ...errors, name: undefined });
          }}
          error={errors.name}
        />

        <Input
          label="Email"
          type="email"
          placeholder="Email address"
          value={form.email}
          onChange={(e) => {
            setForm({ ...form, email: e.target.value });
            if (errors.email) setErrors({ ...errors, email: undefined });
          }}
          error={errors.email}
        />
      </div>
    </Modal>
  );
}
