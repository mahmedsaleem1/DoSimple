import { useState, useEffect } from 'react';
import { HiOutlinePhotograph } from 'react-icons/hi';
import { taskService } from '../../services/taskService';
import { Modal, Button, Input, Select } from '../../components/ui';
import toast from 'react-hot-toast';

const priorityOptions = [
  { value: '0', label: 'Low' },
  { value: '1', label: 'Medium' },
  { value: '2', label: 'High' },
  { value: '3', label: 'Critical' },
];

const statusOptions = [
  { value: '0', label: 'Pending' },
  { value: '1', label: 'In Progress' },
  { value: '2', label: 'Completed' },
  { value: '3', label: 'Cancelled' },
];

const defaultForm = {
  title: '',
  description: '',
  priority: '1',
  category: '',
  dueDate: '',
  assignedToUserId: '',
  status: '0',
};

const priorityEnumToValue = {
  Low: '0',
  Medium: '1',
  High: '2',
  Critical: '3',
};

const statusEnumToValue = {
  Pending: '0',
  InProgress: '1',
  Completed: '2',
  Cancelled: '3',
};

export default function TaskFormModal({ isOpen, onClose, task, onSuccess }) {
  const isEdit = !!task;
  const [form, setForm] = useState(defaultForm);
  const [image, setImage] = useState(null);
  const [imagePreview, setImagePreview] = useState(null);
  const [errors, setErrors] = useState({});
  const [loading, setLoading] = useState(false);
  const [categories, setCategories] = useState([]);

  useEffect(() => {
    if (task) {
      setForm({
        title: task.title || '',
        description: task.description || '',
        priority: priorityEnumToValue[task.priority] ?? '1',
        category: task.category || '',
        dueDate: task.dueDate ? task.dueDate.split('T')[0] : '',
        assignedToUserId: task.assignedToUserId?.toString() || '',
        status: statusEnumToValue[task.status] ?? '0',
      });
      setImagePreview(task.imageUrl || null);
    } else {
      setForm(defaultForm);
      setImagePreview(null);
    }
    setImage(null);
    setErrors({});
  }, [task, isOpen]);

  useEffect(() => {
    if (isOpen) {
      taskService.getCategories().then((res) => {
        setCategories(res.data || []);
      }).catch(() => {});
    }
  }, [isOpen]);

  const validate = () => {
    const errs = {};
    if (!form.title.trim()) errs.title = 'Title is required';
    if (!form.description.trim()) errs.description = 'Description is required';
    if (!form.category.trim()) errs.category = 'Category is required';
    setErrors(errs);
    return Object.keys(errs).length === 0;
  };

  const handleImageChange = (e) => {
    const file = e.target.files?.[0];
    if (file) {
      setImage(file);
      setImagePreview(URL.createObjectURL(file));
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!validate()) return;

    setLoading(true);
    try {
      const payload = {
        title: form.title,
        description: form.description,
        priority: Number(form.priority),
        category: form.category,
        dueDate: form.dueDate || null,
        assignedToUserId: form.assignedToUserId ? Number(form.assignedToUserId) : null,
      };

      if (isEdit) {
        payload.status = Number(form.status);
        await taskService.updateTask(task.id, payload, image);
        toast.success('Task updated!');
      } else {
        await taskService.createTask(payload, image);
        toast.success('Task created!');
      }
      onSuccess();
    } catch (err) {
      const msg = err.response?.data?.message || `Failed to ${isEdit ? 'update' : 'create'} task`;
      toast.error(msg);
    } finally {
      setLoading(false);
    }
  };

  const updateField = (field) => (e) => {
    setForm((prev) => ({ ...prev, [field]: e.target.value }));
    if (errors[field]) setErrors((prev) => ({ ...prev, [field]: undefined }));
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title={isEdit ? 'Edit Task' : 'Create New Task'}
      size="lg"
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button loading={loading} onClick={handleSubmit}>
            {isEdit ? 'Save Changes' : 'Create Task'}
          </Button>
        </>
      }
    >
      <form onSubmit={handleSubmit} className="space-y-5">
        <Input
          label="Title"
          placeholder="Enter task title"
          value={form.title}
          onChange={updateField('title')}
          error={errors.title}
        />

        <div className="space-y-1.5">
          <label className="block text-sm font-medium text-surface-700">
            Description
          </label>
          <textarea
            rows={3}
            className={`input-field resize-none ${errors.description ? 'input-error' : ''}`}
            placeholder="Describe the task..."
            value={form.description}
            onChange={updateField('description')}
          />
          {errors.description && (
            <p className="text-sm text-red-500">{errors.description}</p>
          )}
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <Select
            label="Priority"
            options={priorityOptions}
            value={form.priority}
            onChange={updateField('priority')}
          />

          {isEdit && (
            <Select
              label="Status"
              options={statusOptions}
              value={form.status}
              onChange={updateField('status')}
            />
          )}

          <div className="space-y-1.5">
            <label className="block text-sm font-medium text-surface-700">
              Category
            </label>
            <input
              list="categories-list"
              className={`input-field ${errors.category ? 'input-error' : ''}`}
              placeholder="e.g. Development, Design"
              value={form.category}
              onChange={updateField('category')}
            />
            <datalist id="categories-list">
              {categories.map((c) => (
                <option key={c} value={c} />
              ))}
            </datalist>
            {errors.category && (
              <p className="text-sm text-red-500">{errors.category}</p>
            )}
          </div>

          <Input
            label="Due Date"
            type="date"
            value={form.dueDate}
            onChange={updateField('dueDate')}
          />

          <Input
            label="Assign To (User ID)"
            type="number"
            placeholder="Optional user ID"
            value={form.assignedToUserId}
            onChange={updateField('assignedToUserId')}
          />
        </div>

        {/* Image upload */}
        <div className="space-y-1.5">
          <label className="block text-sm font-medium text-surface-700">
            Attachment
          </label>
          <div className="flex items-center gap-4">
            <label className="flex items-center gap-2 px-4 py-2.5 rounded-lg border border-dashed border-surface-300 text-sm text-surface-600 hover:border-primary-400 hover:text-primary-600 cursor-pointer transition-colors">
              <HiOutlinePhotograph className="h-5 w-5" />
              {image ? image.name : 'Upload image'}
              <input
                type="file"
                accept="image/*"
                onChange={handleImageChange}
                className="hidden"
              />
            </label>
            {imagePreview && (
              <img
                src={imagePreview}
                alt="Preview"
                className="h-12 w-12 rounded-lg object-cover border border-surface-200"
              />
            )}
          </div>
        </div>
      </form>
    </Modal>
  );
}
