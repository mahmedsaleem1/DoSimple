import { useLocation } from 'react-router-dom';
import { HiOutlineMenuAlt2, HiOutlineBell } from 'react-icons/hi';
import { useAuth } from '../context/AuthContext';
import Avatar from '../components/ui/Avatar';

const pageTitles = {
  '/dashboard': 'Dashboard',
  '/tasks': 'Tasks',
  '/users': 'Users',
};

export default function Navbar({ onMenuClick }) {
  const location = useLocation();
  const { user } = useAuth();

  const title =
    pageTitles[location.pathname] ||
    location.pathname
      .split('/')
      .filter(Boolean)
      .pop()
      ?.replace(/-/g, ' ')
      ?.replace(/\b\w/g, (c) => c.toUpperCase()) ||
    'Dashboard';

  return (
    <header className="sticky top-0 z-20 bg-white/80 backdrop-blur-md border-b border-surface-200">
      <div className="flex h-16 items-center justify-between px-4 sm:px-6 lg:px-8">
        <div className="flex items-center gap-4">
          <button
            onClick={onMenuClick}
            className="p-2 rounded-lg text-surface-500 hover:bg-surface-100 lg:hidden"
          >
            <HiOutlineMenuAlt2 className="h-5 w-5" />
          </button>
          <h1 className="text-xl font-bold text-surface-900">{title}</h1>
        </div>

        <div className="flex items-center gap-3">
          <button className="relative p-2 rounded-lg text-surface-500 hover:bg-surface-100 transition-colors">
            <HiOutlineBell className="h-5 w-5" />
          </button>
          <div className="hidden sm:flex items-center gap-2.5 pl-3 border-l border-surface-200">
            <Avatar name={user?.name || ''} size="sm" />
            <div className="text-right">
              <p className="text-sm font-medium text-surface-900">{user?.name}</p>
              <p className="text-xs text-surface-500">{user?.email}</p>
            </div>
          </div>
        </div>
      </div>
    </header>
  );
}
