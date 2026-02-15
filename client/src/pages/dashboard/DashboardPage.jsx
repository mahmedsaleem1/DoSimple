import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import {
  HiOutlineClipboardList,
  HiOutlineClock,
  HiOutlineCheckCircle,
  HiOutlineExclamationCircle,
  HiOutlineTrendingUp,
  HiOutlinePlus,
  HiOutlineArrowRight,
  HiOutlineCalendar,
} from 'react-icons/hi';
import { taskService } from '../../services/taskService';
import { useAuth } from '../../context/AuthContext';
import { Card, Badge, PageLoader } from '../../components/ui';
import { format } from 'date-fns';

export default function DashboardPage() {
  const { user } = useAuth();
  const [stats, setStats] = useState(null);
  const [recentTasks, setRecentTasks] = useState([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchDashboard = async () => {
      try {
        const [statsRes, tasksRes] = await Promise.all([
          taskService.getTaskStats(),
          taskService.getTasks({ pageSize: 5 }),
        ]);
        setStats(statsRes.data);
        setRecentTasks(tasksRes.data.tasks || []);
      } catch (err) {
        console.error('Failed to load dashboard', err);
      } finally {
        setLoading(false);
      }
    };
    fetchDashboard();
  }, []);

  if (loading) return <PageLoader />;

  const statCards = [
    {
      title: 'Total Tasks',
      value: stats?.totalTasks || 0,
      icon: HiOutlineClipboardList,
      color: 'bg-blue-50 text-blue-600',
      iconBg: 'bg-blue-100',
    },
    {
      title: 'In Progress',
      value: stats?.inProgressTasks || 0,
      icon: HiOutlineTrendingUp,
      color: 'bg-amber-50 text-amber-600',
      iconBg: 'bg-amber-100',
    },
    {
      title: 'Completed',
      value: stats?.completedTasks || 0,
      icon: HiOutlineCheckCircle,
      color: 'bg-emerald-50 text-emerald-600',
      iconBg: 'bg-emerald-100',
    },
    {
      title: 'Overdue',
      value: stats?.overdueTasks || 0,
      icon: HiOutlineExclamationCircle,
      color: 'bg-red-50 text-red-600',
      iconBg: 'bg-red-100',
    },
  ];

  return (
    <div className="space-y-8 animate-fade-in">
      {/* Welcome section */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h2 className="text-2xl font-bold text-surface-900">
            Good {getGreeting()}, {user?.name?.split(' ')[0]} 👋
          </h2>
          <p className="mt-1 text-surface-500">
            Here&apos;s what&apos;s happening with your tasks today.
          </p>
        </div>
        <Link
          to="/tasks?create=true"
          className="btn-primary inline-flex items-center gap-2"
        >
          <HiOutlinePlus className="h-4 w-4" />
          New Task
        </Link>
      </div>

      {/* Stats grid */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        {statCards.map((stat) => (
          <Card key={stat.title} className="hover:shadow-soft transition-shadow">
            <div className="flex items-center gap-4">
              <div className={`flex h-12 w-12 items-center justify-center rounded-xl ${stat.iconBg}`}>
                <stat.icon className={`h-6 w-6 ${stat.color.split(' ')[1]}`} />
              </div>
              <div>
                <p className="text-sm font-medium text-surface-500">{stat.title}</p>
                <p className="text-2xl font-bold text-surface-900">{stat.value}</p>
              </div>
            </div>
          </Card>
        ))}
      </div>

      {/* Bottom grid */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Recent tasks */}
        <Card className="lg:col-span-2" padding={false}>
          <div className="flex items-center justify-between px-6 py-4 border-b border-surface-100">
            <h3 className="text-base font-semibold text-surface-900">Recent Tasks</h3>
            <Link
              to="/tasks"
              className="text-sm font-medium text-primary-600 hover:text-primary-700 flex items-center gap-1"
            >
              View all
              <HiOutlineArrowRight className="h-3.5 w-3.5" />
            </Link>
          </div>
          <div className="divide-y divide-surface-100">
            {recentTasks.length === 0 ? (
              <div className="px-6 py-10 text-center text-surface-500 text-sm">
                No tasks yet. Create your first task to get started!
              </div>
            ) : (
              recentTasks.map((task) => (
                <Link
                  key={task.id}
                  to={`/tasks?view=${task.id}`}
                  className="flex items-center gap-4 px-6 py-3.5 hover:bg-surface-50 transition-colors"
                >
                  <div className="flex-1 min-w-0">
                    <p className="text-sm font-medium text-surface-900 truncate">
                      {task.title}
                    </p>
                    <p className="text-xs text-surface-500 mt-0.5">{task.category}</p>
                  </div>
                  <Badge color={task.priority}>{task.priority}</Badge>
                  <Badge color={task.status}>{formatStatus(task.status)}</Badge>
                </Link>
              ))
            )}
          </div>
        </Card>

        {/* Quick stats sidebar */}
        <Card>
          <h3 className="text-base font-semibold text-surface-900 mb-4">Overview</h3>
          <div className="space-y-4">
            <StatBar
              label="Pending"
              value={stats?.pendingTasks || 0}
              total={stats?.totalTasks || 1}
              color="bg-amber-500"
            />
            <StatBar
              label="In Progress"
              value={stats?.inProgressTasks || 0}
              total={stats?.totalTasks || 1}
              color="bg-blue-500"
            />
            <StatBar
              label="Completed"
              value={stats?.completedTasks || 0}
              total={stats?.totalTasks || 1}
              color="bg-emerald-500"
            />
            <StatBar
              label="Cancelled"
              value={stats?.cancelledTasks || 0}
              total={stats?.totalTasks || 1}
              color="bg-surface-400"
            />
          </div>

          <div className="mt-6 pt-4 border-t border-surface-100">
            <div className="flex items-center gap-2 text-sm">
              <HiOutlineCalendar className="h-4 w-4 text-surface-400" />
              <span className="text-surface-600">
                <strong className="text-surface-900">{stats?.dueThisWeek || 0}</strong> tasks due this week
              </span>
            </div>
          </div>
        </Card>
      </div>
    </div>
  );
}

function StatBar({ label, value, total, color }) {
  const pct = total > 0 ? Math.round((value / total) * 100) : 0;
  return (
    <div>
      <div className="flex items-center justify-between text-sm mb-1.5">
        <span className="text-surface-600">{label}</span>
        <span className="font-medium text-surface-900">{value}</span>
      </div>
      <div className="h-2 rounded-full bg-surface-100 overflow-hidden">
        <div
          className={`h-full rounded-full transition-all duration-500 ${color}`}
          style={{ width: `${pct}%` }}
        />
      </div>
    </div>
  );
}

function getGreeting() {
  const hour = new Date().getHours();
  if (hour < 12) return 'morning';
  if (hour < 18) return 'afternoon';
  return 'evening';
}

function formatStatus(status) {
  if (status === 'InProgress') return 'In Progress';
  return status;
}
