import { Modal, Badge, Button } from '../../components/ui';
import { format } from 'date-fns';
import { HiOutlinePencil, HiOutlineCalendar, HiOutlineUser, HiOutlineTag } from 'react-icons/hi';

export default function TaskDetailModal({ isOpen, onClose, task, onEdit, onStatusChange }) {
  if (!task) return null;

  const statusActions = {
    Pending: { next: 1, label: 'Start Working', color: 'primary' },
    InProgress: { next: 2, label: 'Mark Complete', color: 'primary' },
    Completed: null,
    Cancelled: { next: 0, label: 'Reopen', color: 'secondary' },
  };

  const action = statusActions[task.status];

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title="Task Details"
      size="lg"
      footer={
        <>
          {action && (
            <Button
              variant={action.color}
              onClick={() => onStatusChange(task.id, action.next)}
            >
              {action.label}
            </Button>
          )}
          <Button variant="secondary" onClick={() => onEdit(task)}>
            <HiOutlinePencil className="h-4 w-4" />
            Edit
          </Button>
        </>
      }
    >
      <div className="space-y-6">
        {/* Title & badges */}
        <div>
          <h3 className="text-xl font-semibold text-surface-900">{task.title}</h3>
          <div className="flex items-center gap-2 mt-2">
            <Badge color={task.status}>
              {task.status === 'InProgress' ? 'In Progress' : task.status}
            </Badge>
            <Badge color={task.priority}>{task.priority}</Badge>
          </div>
        </div>

        {/* Description */}
        <div>
          <h4 className="text-sm font-medium text-surface-500 mb-1">Description</h4>
          <p className="text-sm text-surface-700 whitespace-pre-wrap leading-relaxed">
            {task.description}
          </p>
        </div>

        {/* Image */}
        {task.imageUrl && (
          <div>
            <h4 className="text-sm font-medium text-surface-500 mb-2">Attachment</h4>
            <img
              src={task.imageUrl}
              alt="Task attachment"
              className="rounded-xl border border-surface-200 max-h-64 object-cover"
            />
          </div>
        )}

        {/* Metadata grid */}
        <div className="grid grid-cols-2 gap-4 p-4 bg-surface-50 rounded-xl">
          <DetailItem
            icon={HiOutlineTag}
            label="Category"
            value={task.category}
          />
          <DetailItem
            icon={HiOutlineCalendar}
            label="Due Date"
            value={task.dueDate ? format(new Date(task.dueDate), 'MMM d, yyyy') : 'No due date'}
          />
          <DetailItem
            icon={HiOutlineUser}
            label="Created By"
            value={task.createdByUserName}
          />
          <DetailItem
            icon={HiOutlineUser}
            label="Assigned To"
            value={task.assignedToUserName || 'Unassigned'}
          />
        </div>

        {/* Timestamps */}
        <div className="text-xs text-surface-400 space-y-1">
          <p>Created: {format(new Date(task.createdAt), 'MMM d, yyyy h:mm a')}</p>
          {task.updatedAt && (
            <p>Updated: {format(new Date(task.updatedAt), 'MMM d, yyyy h:mm a')}</p>
          )}
        </div>
      </div>
    </Modal>
  );
}

function DetailItem({ icon: Icon, label, value }) {
  return (
    <div className="flex items-start gap-2">
      <Icon className="h-4 w-4 text-surface-400 mt-0.5" />
      <div>
        <p className="text-xs text-surface-500">{label}</p>
        <p className="text-sm font-medium text-surface-900">{value}</p>
      </div>
    </div>
  );
}
