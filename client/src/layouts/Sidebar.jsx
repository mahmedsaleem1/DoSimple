import { NavLink, useLocation } from 'react-router-dom';
import { clsx } from 'clsx';
import {
  HiOutlineHome,
  HiOutlineClipboardList,
  HiOutlineUsers,
  HiOutlineCog,
  HiX,
  HiOutlineLogout,
  HiOutlineLightningBolt,
} from 'react-icons/hi';
import { useAuth } from '../context/AuthContext';

const navigation = [
  { name: 'Dashboard', href: '/dashboard', icon: HiOutlineHome },
  { name: 'Tasks', href: '/tasks', icon: HiOutlineClipboardList },
];

const adminNavigation = [
  { name: 'Users', href: '/users', icon: HiOutlineUsers },
];

export default function Sidebar({ open, onClose }) {
  const { user, isAdmin, logout } = useAuth();
  const location = useLocation();

  const allNav = [...navigation, ...(isAdmin ? adminNavigation : [])];

  return (
    <>
      {/* Mobile sidebar */}
      <div
        className={clsx(
          'fixed inset-y-0 left-0 z-50 w-72 bg-white border-r border-surface-200 transform transition-transform duration-300 ease-in-out lg:hidden',
          open ? 'translate-x-0' : '-translate-x-full'
        )}
      >
        <SidebarContent nav={allNav} user={user} onClose={onClose} onLogout={logout} />
      </div>

      {/* Desktop sidebar */}
      <div className="hidden lg:fixed lg:inset-y-0 lg:left-0 lg:z-30 lg:block lg:w-72 lg:bg-white lg:border-r lg:border-surface-200">
        <SidebarContent nav={allNav} user={user} onLogout={logout} />
      </div>
    </>
  );
}

function SidebarContent({ nav, user, onClose, onLogout }) {
  return (
    <div className="flex h-full flex-col">
      {/* Logo */}
      <div className="flex h-16 items-center justify-between px-6 border-b border-surface-100">
        <div className="flex items-center gap-2.5">
          <div className="flex h-9 w-9 items-center justify-center rounded-xl bg-primary-600">
            <HiOutlineLightningBolt className="h-5 w-5 text-white" />
          </div>
          <span className="text-xl font-bold text-surface-900">
            Do<span className="text-primary-600">Simple</span>
          </span>
        </div>
        {onClose && (
          <button
            onClick={onClose}
            className="p-1.5 rounded-lg text-surface-400 hover:bg-surface-100 lg:hidden"
          >
            <HiX className="h-5 w-5" />
          </button>
        )}
      </div>

      {/* Navigation */}
      <nav className="flex-1 px-3 py-4 space-y-1 overflow-y-auto">
        {nav.map((item) => (
          <NavLink
            key={item.name}
            to={item.href}
            onClick={onClose}
            className={({ isActive }) =>
              clsx(
                'group flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium transition-all duration-200',
                isActive
                  ? 'bg-primary-50 text-primary-700'
                  : 'text-surface-600 hover:bg-surface-50 hover:text-surface-900'
              )
            }
          >
            {({ isActive }) => (
              <>
                <item.icon
                  className={clsx(
                    'h-5 w-5 flex-shrink-0 transition-colors',
                    isActive ? 'text-primary-600' : 'text-surface-400 group-hover:text-surface-600'
                  )}
                />
                {item.name}
              </>
            )}
          </NavLink>
        ))}
      </nav>

      {/* User info + Logout */}
      <div className="border-t border-surface-100 p-4">
        <div className="flex items-center gap-3 mb-3 px-2">
          <div className="flex h-9 w-9 items-center justify-center rounded-full bg-primary-100 text-primary-700 font-semibold text-sm">
            {user?.name?.charAt(0)?.toUpperCase() || '?'}
          </div>
          <div className="flex-1 min-w-0">
            <p className="text-sm font-medium text-surface-900 truncate">{user?.name}</p>
            <p className="text-xs text-surface-500 truncate">{user?.role}</p>
          </div>
        </div>
        <button
          onClick={onLogout}
          className="flex items-center gap-2 w-full rounded-xl px-3 py-2.5 text-sm font-medium text-surface-600 hover:bg-red-50 hover:text-red-600 transition-colors"
        >
          <HiOutlineLogout className="h-5 w-5" />
          Sign out
        </button>
      </div>
    </div>
  );
}
