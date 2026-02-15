import { useState, useCallback } from 'react';
import {
  HiOutlineSearch,
  HiOutlineUsers,
  HiOutlineMail,
  HiOutlineShieldCheck,
  HiOutlineTrash,
  HiOutlinePencil,
  HiOutlineCheckCircle,
} from 'react-icons/hi';
import { userService } from '../../services/userService';
import { usePagination } from '../../hooks/usePagination';
import {
  Button,
  Card,
  Input,
  Select,
  Badge,
  Avatar,
  Pagination,
  EmptyState,
  PageLoader,
  Modal,
} from '../../components/ui';
import UserEditModal from './UserEditModal';
import { format } from 'date-fns';
import toast from 'react-hot-toast';

const roleOptions = [
  { value: '', label: 'All Roles' },
  { value: 'User', label: 'User' },
  { value: 'Admin', label: 'Admin' },
  { value: 'SuperAdmin', label: 'Super Admin' },
];

export default function UserListPage() {
  const [search, setSearch] = useState('');
  const [editUser, setEditUser] = useState(null);
  const [deleteConfirm, setDeleteConfirm] = useState(null);

  const {
    data,
    loading,
    page,
    filters,
    setPage,
    updateFilters,
    refresh,
  } = usePagination(userService.getUsers, { pageSize: 10 });

  const handleSearch = useCallback(
    (e) => {
      e.preventDefault();
      updateFilters({ searchTerm: search });
    },
    [search, updateFilters]
  );

  const handleDelete = async (id) => {
    try {
      await userService.deleteUser(id);
      toast.success('User deleted');
      setDeleteConfirm(null);
      refresh();
    } catch (err) {
      toast.error(err.response?.data?.message || 'Failed to delete user');
    }
  };

  const handleRoleChange = async (id, role) => {
    try {
      await userService.updateUserRole(id, role);
      toast.success('Role updated');
      refresh();
    } catch (err) {
      toast.error(err.response?.data?.message || 'Failed to update role');
    }
  };

  const handleVerifyEmail = async (id) => {
    try {
      await userService.verifyUserEmail(id);
      toast.success('Email verified');
      refresh();
    } catch {
      toast.error('Failed to verify email');
    }
  };

  const users = data?.users || [];
  const totalPages = data?.totalPages || 0;
  const totalCount = data?.totalCount || 0;

  return (
    <div className="space-y-6 animate-fade-in">
      {/* Header */}
      <div>
        <h2 className="text-lg font-semibold text-surface-900">
          User Management
          {totalCount > 0 && (
            <span className="ml-2 text-sm font-normal text-surface-500">
              ({totalCount})
            </span>
          )}
        </h2>
        <p className="text-sm text-surface-500 mt-1">
          Manage users, roles, and email verification
        </p>
      </div>

      {/* Search & Filter */}
      <Card>
        <div className="flex flex-col sm:flex-row gap-3">
          <form onSubmit={handleSearch} className="flex-1 flex gap-2">
            <Input
              icon={HiOutlineSearch}
              placeholder="Search users..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="flex-1"
            />
            <Button type="submit" variant="secondary">
              Search
            </Button>
          </form>
          <Select
            options={roleOptions}
            value={filters.role ?? ''}
            onChange={(e) => updateFilters({ role: e.target.value || undefined })}
            className="sm:w-40"
          />
        </div>
      </Card>

      {/* Users Table */}
      {loading ? (
        <PageLoader />
      ) : users.length === 0 ? (
        <EmptyState
          icon={HiOutlineUsers}
          title="No users found"
          description="Try adjusting your search or filter"
        />
      ) : (
        <Card padding={false}>
          {/* Desktop table */}
          <div className="hidden md:block overflow-x-auto">
            <table className="w-full">
              <thead>
                <tr className="border-b border-surface-100 text-left">
                  <th className="px-4 py-3 text-xs font-medium text-surface-500 uppercase tracking-wider">
                    User
                  </th>
                  <th className="px-4 py-3 text-xs font-medium text-surface-500 uppercase tracking-wider">
                    Role
                  </th>
                  <th className="px-4 py-3 text-xs font-medium text-surface-500 uppercase tracking-wider">
                    Email Status
                  </th>
                  <th className="px-4 py-3 text-xs font-medium text-surface-500 uppercase tracking-wider">
                    Joined
                  </th>
                  <th className="px-4 py-3 text-xs font-medium text-surface-500 uppercase tracking-wider">
                    Actions
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-surface-100">
                {users.map((user) => (
                  <tr key={user.id} className="hover:bg-surface-50 transition-colors group">
                    <td className="px-4 py-3">
                      <div className="flex items-center gap-3">
                        <Avatar name={user.name} size="sm" />
                        <div>
                          <p className="text-sm font-medium text-surface-900">{user.name}</p>
                          <p className="text-xs text-surface-500">{user.email}</p>
                        </div>
                      </div>
                    </td>
                    <td className="px-4 py-3">
                      <select
                        value={user.role}
                        onChange={(e) => handleRoleChange(user.id, e.target.value)}
                        className="text-xs rounded-lg border-0 bg-transparent py-1 pl-1 pr-6 font-medium focus:ring-2 focus:ring-primary-500 cursor-pointer"
                      >
                        <option value="User">User</option>
                        <option value="Admin">Admin</option>
                        <option value="SuperAdmin">Super Admin</option>
                      </select>
                    </td>
                    <td className="px-4 py-3">
                      {user.isEmailVerified ? (
                        <Badge color="success" dot>Verified</Badge>
                      ) : (
                        <div className="flex items-center gap-2">
                          <Badge color="warning" dot>Unverified</Badge>
                          <button
                            onClick={() => handleVerifyEmail(user.id)}
                            className="text-xs text-primary-600 hover:text-primary-700 font-medium"
                            title="Manually verify"
                          >
                            Verify
                          </button>
                        </div>
                      )}
                    </td>
                    <td className="px-4 py-3 text-sm text-surface-500">
                      {format(new Date(user.createdAt), 'MMM d, yyyy')}
                    </td>
                    <td className="px-4 py-3">
                      <div className="flex items-center gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
                        <button
                          onClick={() => setEditUser(user)}
                          className="p-1.5 rounded-lg text-surface-400 hover:text-primary-600 hover:bg-primary-50"
                          title="Edit user"
                        >
                          <HiOutlinePencil className="h-4 w-4" />
                        </button>
                        <button
                          onClick={() => setDeleteConfirm(user)}
                          className="p-1.5 rounded-lg text-surface-400 hover:text-red-600 hover:bg-red-50"
                          title="Delete user"
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
            {users.map((user) => (
              <div key={user.id} className="p-4 space-y-3">
                <div className="flex items-center gap-3">
                  <Avatar name={user.name} size="sm" />
                  <div className="flex-1 min-w-0">
                    <p className="text-sm font-medium text-surface-900 truncate">{user.name}</p>
                    <p className="text-xs text-surface-500 truncate">{user.email}</p>
                  </div>
                  <Badge color={user.role}>{user.role}</Badge>
                </div>
                <div className="flex items-center justify-between">
                  {user.isEmailVerified ? (
                    <Badge color="success" dot>Verified</Badge>
                  ) : (
                    <Badge color="warning" dot>Unverified</Badge>
                  )}
                  <div className="flex gap-2">
                    <button
                      onClick={() => setEditUser(user)}
                      className="p-1.5 rounded-lg text-surface-400 hover:text-primary-600"
                    >
                      <HiOutlinePencil className="h-4 w-4" />
                    </button>
                    <button
                      onClick={() => setDeleteConfirm(user)}
                      className="p-1.5 rounded-lg text-surface-400 hover:text-red-600"
                    >
                      <HiOutlineTrash className="h-4 w-4" />
                    </button>
                  </div>
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

      {/* Edit user modal */}
      <UserEditModal
        isOpen={!!editUser}
        onClose={() => setEditUser(null)}
        user={editUser}
        onSuccess={() => {
          setEditUser(null);
          refresh();
        }}
      />

      {/* Delete confirmation */}
      <Modal
        isOpen={!!deleteConfirm}
        onClose={() => setDeleteConfirm(null)}
        title="Delete User"
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
          Are you sure you want to delete user{' '}
          <strong>&quot;{deleteConfirm?.name}&quot;</strong>? This action cannot be undone.
        </p>
      </Modal>
    </div>
  );
}
