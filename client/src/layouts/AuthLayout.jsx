import { Outlet } from 'react-router-dom';
import { HiOutlineLightningBolt } from 'react-icons/hi';

export default function AuthLayout() {
  return (
    <div className="min-h-screen flex">
      {/* Left — Brand panel */}
      <div className="hidden lg:flex lg:w-1/2 bg-gradient-to-br from-primary-600 via-primary-700 to-primary-900 relative overflow-hidden">
        <div className="absolute inset-0 opacity-10">
          <svg className="absolute -top-40 -left-40 h-[800px] w-[800px]" viewBox="0 0 800 800">
            <circle cx="400" cy="400" r="300" fill="none" stroke="white" strokeWidth="1" />
            <circle cx="400" cy="400" r="200" fill="none" stroke="white" strokeWidth="1" />
            <circle cx="400" cy="400" r="100" fill="none" stroke="white" strokeWidth="1" />
          </svg>
        </div>
        <div className="relative flex flex-col justify-center px-16 text-white">
          <div className="flex items-center gap-3 mb-8">
            <div className="flex h-12 w-12 items-center justify-center rounded-2xl bg-white/20 backdrop-blur">
              <HiOutlineLightningBolt className="h-7 w-7" />
            </div>
            <span className="text-3xl font-bold">DoSimple</span>
          </div>
          <h2 className="text-4xl font-bold leading-tight mb-4">
            Manage tasks,<br />simplified.
          </h2>
          <p className="text-lg text-primary-100 max-w-md">
            Organize, track, and complete your tasks with a beautiful and intuitive interface. 
            Built for teams that value simplicity.
          </p>
          <div className="mt-12 grid grid-cols-3 gap-6 max-w-sm">
            <div>
              <p className="text-3xl font-bold">99%</p>
              <p className="text-sm text-primary-200">Uptime</p>
            </div>
            <div>
              <p className="text-3xl font-bold">10k+</p>
              <p className="text-sm text-primary-200">Tasks done</p>
            </div>
            <div>
              <p className="text-3xl font-bold">500+</p>
              <p className="text-sm text-primary-200">Users</p>
            </div>
          </div>
        </div>
      </div>

      {/* Right — Form panel */}
      <div className="flex flex-1 flex-col justify-center px-6 py-12 lg:px-16">
        <div className="mx-auto w-full max-w-md">
          {/* Mobile logo */}
          <div className="flex items-center gap-2.5 mb-8 lg:hidden">
            <div className="flex h-9 w-9 items-center justify-center rounded-xl bg-primary-600">
              <HiOutlineLightningBolt className="h-5 w-5 text-white" />
            </div>
            <span className="text-xl font-bold text-surface-900">
              Do<span className="text-primary-600">Simple</span>
            </span>
          </div>

          <Outlet />
        </div>
      </div>
    </div>
  );
}
