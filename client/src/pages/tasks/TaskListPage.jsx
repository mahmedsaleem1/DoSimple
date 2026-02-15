import { useState, useEffect, useCallback } from 'react';
import { useSearchParams } from 'react-router-dom';
import {
  HiOutlinePlus,
  HiOutlineSearch,
  HiOutlineFilter,
  HiOutlineTrash,
  HiOutlineClipboardList,
} from 'react-icons/hi';
import { taskService } from '../../services/taskService';
import { usePagination } from '../../hooks/usePagination';
import {
  Button,
  Card,
  Input,
  Select,
  Badge,
  Pagination,
  EmptyState,
  PageLoader,
  Modal,
} from '../../components/ui';
import TaskFormModal from './TaskFormModal';
import TaskDetailModal from './TaskDetailModal';
import { format } from 'date-fns';
import toast from 'react-hot-toast';

const statusOptions = [
  { value: '', label: 'All Statuses' },
  { value: '0', label: 'Pending' },
  { value: '1', label: 'In Progress' },
  { value: '2', label: 'Completed' },
  { value: '3', label: 'Cancelled' },
];

const priorityOptions = [
  { value: '', label: 'All Priorities' },
  { value: '0', label: 'Low' },
  { value: '1', label: 'Medium' },
  { value: '2', label: 'High' },
  { value: '3', label: 'Critical' },
];

export default function TaskListPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const [showFilters, setShowFilters] = useState(false);
  const [search, setSearch] = useState('');
  const [selectedTasks, setSelectedTasks] = useState([]);
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [editTask, setEditTask] = useState(null);
  const [viewTask, setViewTask] = useState(null);
  const [deleteConfirm, setDeleteConfirm] = useState(null);

  const {
    data,
    loading,
    page,
    filters,
    setPage,
    updateFilters,
    refresh,
  } = usePagination(taskService.getTasks, { pageSize: 10 });

  // Handle URL params for opening modals
  useEffect(() => {
    if (searchParams.get('create') === 'true') {
      setShowCreateModal(true);
      searchParams.delete('create');
      setSearchParams(searchParams, { replace: true });
    }
  }, [searchParams, setSearchParams]);

  const handleSearch = useCallback(
    (e) => {
      e.preventDefault();
      updateFilters({ searchTerm: search });
    },
    [search, updateFilters]
  );

  const handleDelete = async (id) => {
    try {
      await taskService.deleteTask(id);
      toast.success('Task deleted');
      setDeleteConfirm(null);
      refresh();
    } catch {
      toast.error('Failed to delete task');
    }
  };

  const handleBulkDelete = async () => {
    if (selectedTasks.length === 0) return;
    try {
      await taskService.bulkDelete(selectedTasks);
      toast.success(`${selectedTasks.length} tasks deleted`);
      setSelectedTasks([]);
      refresh();
    } catch {
      toast.error('Failed to delete tasks');
    }
  };

  const handleStatusChange = async (id, status) => {
    try {
      await taskService.updateTaskStatus(id, status);
      toast.success('Status updated');
      refresh();
    } catch {
      toast.error('Failed to update status');
    }
  };

  const toggleSelect = (id) => {
    setSelectedTasks((prev) =>
      prev.includes(id) ? prev.filter((t) => t !== id) : [...prev, id]
    );
  };

  const toggleSelectAll = () => {
    if (!data?.tasks) return;
    if (selectedTasks.length === data.tasks.length) {
      setSelectedTasks([]);
    } else {
      setSelectedTasks(data.tasks.map((t) => t.id));
    }
  };

  const tasks = data?.tasks || [];
  const totalPages = data?.totalPages || 0;
  const totalCount = data?.totalCount || 0;

  return (
    <div className="space-y-6 animate-fade-in">
      {/* Top bar */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h2 className="text-lg font-semibold text-surface-900">
            All Tasks
            {totalCount > 0 && (
              <span className="ml-2 text-sm font-normal text-surface-500">
                ({totalCount})
              </span>
            )}
          </h2>
        </div>
        <div className="flex items-center gap-3">
          {selectedTasks.length > 0 && (
            <Button variant="danger" size="sm" onClick={handleBulkDelete}>
              <HiOutlineTrash className="h-4 w-4" />
              Delete ({selectedTasks.length})
            </Button>
          )}
          <Button onClick={() => setShowCreateModal(true)}>
            <HiOutlinePlus className="h-4 w-4" />
            New Task
          </Button>
        </div>
      </div>

      {/* Search & Filters */}
      <Card>
        <div className="flex flex-col sm:flex-row gap-3">
          <form onSubmit={handleSearch} className="flex-1 flex gap-2">
            <Input
              icon={HiOutlineSearch}
              placeholder="Search tasks..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="flex-1"
            />
            <Button type="submit" variant="secondary">
              Search
            </Button>
          </form>
          <Button
            variant="ghost"
            onClick={() => setShowFilters(!showFilters)}
          >
            <HiOutlineFilter className="h-4 w-4" />
            Filters
          </Button>
        </div>

        {showFilters && (
          <div className="mt-4 pt-4 border-t border-surface-100 grid grid-cols-1 sm:grid-cols-3 gap-3">
            <Select
              label="Status"
              options={statusOptions}
              value={filters.status ?? ''}
              onChange={(e) => updateFilters({ status: e.target.value || undefined })}
            />
            <Select
              label="Priority"
              options={priorityOptions}
              value={filters.priority ?? ''}
              onChange={(e) => updateFilters({ priority: e.target.value || undefined })}
            />
            <div className="flex items-end">
              <Button
                variant="ghost"
                size="sm"
                onClick={() => {
                  updateFilters({ status: undefined, priority: undefined, searchTerm: undefined });
                  setSearch('');
                }}
              >
                Clear filters
              </Button>
            </div>
          </div>
        )}
      </Card>

      {/* Tasks table */}
      {loading ? (
        <PageLoader />
      ) : tasks.length === 0 ? (
        <EmptyState
          icon={HiOutlineClipboardList}
          title="No tasks found"
          description="Get started by creating your first task"
          action={
            <Button onClick={() => setShowCreateModal(true)}>
              <HiOutlinePlus className="h-4 w-4" />
              Create Task
            </Button>
          }
        />
      ) : (
        <Card padding={false}>
          {/* Desktop table */}
          <div className="hidden md:block overflow-x-auto">
            <table className="w-full">
              <thead>
                <tr className="border-b border-surface-100 text-left">
                  <th className="px-4 py-3">
                    <input
                      type="checkbox"
                      className="h-4 w-4 rounded border-surface-300 text-primary-600 focus:ring-primary-500"
                      checked={selectedTasks.length === tasks.length && tasks.length > 0}
                      onChange={toggleSelectAll}
                    />
                  </th>
                  <th className="px-4 py-3 text-xs font-medium text-surface-500 uppercase tracking-wider">
                    Task
                  </th>
                  <th className="px-4 py-3 text-xs font-medium text-surface-500 uppercase tracking-wider">
                    Status
                  </th>
                  <th className="px-4 py-3 text-xs font-medium text-surface-500 uppercase tracking-wider">
                    Priority
                  </th>
                  <th className="px-4 py-3 text-xs font-medium text-surface-500 uppercase tracking-wider">
                    Due Date
                  </th>
                  <th className="px-4 py-3 text-xs font-medium text-surface-500 uppercase tracking-wider">
                    Assigned To
                  </th>
                  <th className="px-4 py-3 text-xs font-medium text-surface-500 uppercase tracking-wider">
                    Actions
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-surface-100">
                {tasks.map((task) => (
                  <tr
                    key={task.id}
                    className="hover:bg-surface-50 transition-colors group"
                  >
                    <td className="px-4 py-3">
                      <input
                        type="checkbox"
                        className="h-4 w-4 rounded border-surface-300 text-primary-600 focus:ring-primary-500"
                        checked={selectedTasks.includes(task.id)}
                        onChange={() => toggleSelect(task.id)}
                      />
                    </td>
                    <td className="px-4 py-3">
                      <button
                        onClick={() => setViewTask(task)}
                        className="text-left"
                      >
                        <p className="text-sm font-medium text-surface-900 hover:text-primary-600 transition-colors">
                          {task.title}
                        </p>
                        <p className="text-xs text-surface-500 mt-0.5">
                          {task.category}
                        </p>
                      </button>
                    </td>
                    <td className="px-4 py-3">
                      <StatusDropdown
                        status={task.status}
                        onChange={(s) => handleStatusChange(task.id, s)}
                      />
                    </td>
                    <td className="px-4 py-3">
                      <Badge color={task.priority}>{task.priority}</Badge>
                    </td>
                    <td className="px-4 py-3 text-sm text-surface-500">
                      {task.dueDate
                        ? format(new Date(task.dueDate), 'MMM d, yyyy')
                        : '—'}
                    </td>
                    <td className="px-4 py-3 text-sm text-surface-500">
                      {task.assignedToUserName || '—'}
                    </td>
                    <td className="px-4 py-3">
                      <div className="flex items-center gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
                        <button
                          onClick={() => setEditTask(task)}
                          className="p-1.5 rounded-lg text-surface-400 hover:text-primary-600 hover:bg-primary-50"
                        >
                          <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                            <path strokeLinecap="round" strokeLinejoin="round" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                          </svg>
                        </button>
                        <button
                          onClick={() => setDeleteConfirm(task)}
                          className="p-1.5 rounded-lg text-surface-400 hover:text-red-600 hover:bg-red-50"
                        >
                          <HiOutlineTrash className="h-4 w-4" />
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {/* Mobile cards */}
          <div className="md:hidden divide-y divide-surface-100">
            {tasks.map((task) => (
              <div
                key={task.id}
                className="p-4 space-y-3"
                onClick={() => setViewTask(task)}
              >
                <div className="flex items-start justify-between">
                  <div className="flex-1 min-w-0">
                    <p className="text-sm font-medium text-surface-900 truncate">
                      {task.title}
                    </p>
                    <p className="text-xs text-surface-500 mt-0.5">{task.category}</p>
                  </div>
                  <Badge color={task.priority} className="ml-2">{task.priority}</Badge>
                </div>
                <div className="flex items-center gap-2">
                  <Badge color={task.status}>{formatStatus(task.status)}</Badge>
                  {task.dueDate && (
                    <span className="text-xs text-surface-500">
                      Due {format(new Date(task.dueDate), 'MMM d')}
                    </span>
                  )}
                </div>
              </div>
            ))}
          </div>

          {/* Pagination */}
          <div className="px-4 border-t border-surface-100">
            <Pagination page={page} totalPages={totalPages} onPageChange={setPage} />
          </div>
        </Card>
      )}

      {/* Create/Edit modal */}
      <TaskFormModal
        isOpen={showCreateModal || !!editTask}
        onClose={() => {
          setShowCreateModal(false);
          setEditTask(null);
        }}
        task={editTask}
        onSuccess={() => {
          setShowCreateModal(false);
          setEditTask(null);
          refresh();
        }}
      />

      {/* View detail modal */}
      <TaskDetailModal
        isOpen={!!viewTask}
        onClose={() => setViewTask(null)}
        task={viewTask}
        onEdit={(task) => {
          setViewTask(null);
          setEditTask(task);
        }}
        onStatusChange={(id, status) => {
          handleStatusChange(id, status);
          setViewTask(null);
        }}
      />

      {/* Delete confirmation */}
      <Modal
        isOpen={!!deleteConfirm}
        onClose={() => setDeleteConfirm(null)}
        title="Delete Task"
        size="sm"
        footer={
          <>
            <Button variant="secondary" onClick={() => setDeleteConfirm(null)}>
              Cancel
            </Button>
            <Button variant="danger" onClick={() => handleDelete(deleteConfirm?.id)}>
              Delete
            </Button>
          </>
        }
      >
        <p className="text-sm text-surface-600">
          Are you sure you want to delete{' '}
          <strong>&quot;{deleteConfirm?.title}&quot;</strong>? This action cannot be undone.
        </p>
      </Modal>
    </div>
  );
}

function StatusDropdown({ status, onChange }) {
  const statusMap = {
    Pending: 0,
    InProgress: 1,
    Completed: 2,
    Cancelled: 3,
  };

  return (
    <select
      value={statusMap[status] ?? 0}
      onChange={(e) => onChange(Number(e.target.value))}
      className="text-xs rounded-lg border-0 bg-transparent py-1 pl-1 pr-6 font-medium focus:ring-2 focus:ring-primary-500 cursor-pointer"
    >
      <option value={0}>Pending</option>
      <option value={1}>In Progress</option>
      <option value={2}>Completed</option>
      <option value={3}>Cancelled</option>
    </select>
  );
}

function formatStatus(status) {
  if (status === 'InProgress') return 'In Progress';
  return status;
}
