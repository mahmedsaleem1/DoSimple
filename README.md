# DoSimple

A full-stack task management application with role-based access control, built with ASP.NET Core and React.

## ✨ Features

### Authentication & Authorization
- 🔐 JWT-based authentication
- 👥 Role-based access control (Admin, User)
- ✉️ Email verification
- 🔑 Password reset functionality
- 🛡️ Secure password hashing with BCrypt

### Task Management
- ✅ Create, read, update, and delete tasks
- 📷 Image upload support via Cloudinary
- 🎯 Task priorities (Low, Medium, High)
- 📊 Task status tracking (Pending, In Progress, Completed, Cancelled)
- 🏷️ Category-based organization
- 📅 Due date management with overdue tracking
- 🔍 Advanced filtering and search
- 📄 Pagination support
- 🔀 Bulk operations (delete, status update)

### User Management (Admin Only)
- 👤 View and manage all users
- 🎭 Update user roles
- ✅ Manual email verification
- 📊 User statistics dashboard

### Logging & Monitoring
- 📝 Structured logging with Serilog
- 📂 File-based logs (daily rolling)
- 🖥️ Console output with colored formatting
- 🔍 Request/response logging with user context
- ⚠️ Separate error log files

## 🛠️ Tech Stack

### Backend
- **Framework:** ASP.NET Core 10.0
- **Database:** SQL Server with Entity Framework Core
- **Authentication:** JWT Bearer tokens
- **Email:** MailKit (SMTP)
- **Image Storage:** Cloudinary
- **Logging:** Serilog
- **Testing:** xUnit

### Frontend
- **Framework:** React 18
- **Build Tool:** Vite
- **Styling:** Tailwind CSS
- **Routing:** React Router v6
- **HTTP Client:** Axios
- **Notifications:** React Hot Toast
- **Icons:** React Icons
- **Date Handling:** date-fns

## 📋 Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js 18+](https://nodejs.org/)
- SQL Server (LocalDB, Express, or full version)
- [Git](https://git-scm.com/)

## 🚀 Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/your-username/DoSimple.git
cd DoSimple
```

### 2. Backend Setup

#### Install Dependencies

```bash
cd Server
dotnet restore
```

#### Configure Environment Variables

Create a `.env` file in the `Server` directory:

```env
# Database
ConnectionStrings__DefaultConnection=Server=localhost;Database=DoSimpleDB;Trusted_Connection=True;TrustServerCertificate=True;

# JWT Settings
Jwt__Key=your-super-secret-key-minimum-32-characters-long
Jwt__Issuer=DoSimpleServer
Jwt__Audience=DoSimpleClient
Jwt__ExpiryInMinutes=15

# Email Configuration (Gmail example)
Email__SmtpHost=smtp.gmail.com
Email__SmtpPort=587
Email__SmtpUsername=your-email@gmail.com
Email__SmtpPassword=your-app-password
Email__FromEmail=your-email@gmail.com
Email__FromName=DoSimple
Email__AppUrl=http://localhost:5248

# Admin Registration Secret
AdminSettings__SecretKey=your-admin-secret-key

# Cloudinary
Cloudinary__CloudName=your-cloud-name
Cloudinary__ApiKey=your-api-key
Cloudinary__ApiSecret=your-api-secret
```

#### Apply Database Migrations

```bash
dotnet ef database update
```

#### Run the Backend

```bash
dotnet run
```

The API will be available at `https://localhost:5248`

### 3. Frontend Setup

#### Install Dependencies

```bash
cd ../client
npm install
```

#### Configure Environment Variables

Create a `.env` file in the `client` directory:

```env
VITE_API_URL=https://localhost:5248/api
```

#### Run the Frontend

```bash
npm run dev
```

The client will be available at `http://localhost:5173`

## 🧪 Running Tests

```bash
cd Server.Tests
dotnet test
```

## 📁 Project Structure

```
DoSimple/
├── Server/                      # ASP.NET Core Backend
│   ├── Data/                    # Database context
│   ├── DTOs/                    # Data Transfer Objects
│   ├── Endpoints/               # API Controllers
│   ├── Migrations/              # EF Core migrations
│   ├── Models/                  # Domain models
│   ├── Services/                # Business logic services
│   ├── Utills/                  # Utilities (JWT, password hashing)
│   ├── Logs/                    # Serilog log files (not committed)
│   ├── Program.cs               # Application entry point
│   └── appsettings.json         # Configuration
├── Server.Tests/                # xUnit test project
│   ├── Controllers/             # Controller tests
│   ├── Services/                # Service tests
│   ├── Utilities/               # Utility tests
│   └── Helpers/                 # Test helpers
├── client/                      # React Frontend
│   ├── src/
│   │   ├── components/          # Reusable components
│   │   ├── context/             # React Context (Auth)
│   │   ├── hooks/               # Custom hooks
│   │   ├── layouts/             # Layout components
│   │   ├── pages/               # Page components
│   │   ├── services/            # API service layer
│   │   └── App.jsx              # Root component
│   ├── public/                  # Static assets
│   └── index.html               # HTML template
└── DoSimple.sln                 # Visual Studio solution
```

## 🔌 API Endpoints

### Authentication (`/api/auth`)
- `POST /register` - Register new user
- `POST /register-admin` - Register admin (requires secret key)
- `POST /login` - Login
- `POST /forgot-password` - Request password reset
- `POST /reset-password` - Reset password with token
- `GET /verify-email?token={token}` - Verify email address
- `GET /verify` - Verify JWT token validity

### Tasks (`/api/task`)
- `GET /` - Get all tasks (with filters & pagination)
- `GET /{id}` - Get task by ID
- `GET /stats` - Get task statistics
- `GET /categories` - Get all categories
- `GET /my-assigned` - Get tasks assigned to current user
- `GET /my-created` - Get tasks created by current user
- `GET /overdue` - Get overdue tasks
- `POST /` - Create task (with optional image)
- `PUT /{id}` - Update task
- `PATCH /{id}/status` - Update task status
- `PUT /{id}/assign` - Assign task to user
- `PUT /{id}/unassign` - Unassign task
- `DELETE /{id}` - Delete task
- `POST /bulk-delete` - Bulk delete tasks
- `POST /bulk-update-status` - Bulk update task status

### Users (`/api/user`) - Admin Only
- `GET /` - Get all users (with filters & pagination)
- `GET /{id}` - Get user by ID
- `GET /stats` - Get user statistics
- `PUT /{id}` - Update user information
- `PATCH /{id}/role` - Update user role
- `PATCH /{id}/verify-email` - Manually verify user email
- `DELETE /{id}` - Delete user

## 📊 Database Schema

### Users
- Id, Name, Email, Password (hashed)
- Role (User, Admin, SuperAdmin)
- Email verification fields
- Password reset tokens
- Timestamps

### Tasks
- Id, Title, Description
- Priority, Status, Category
- DueDate, ImageUrl
- CreatedByUserId, AssignedToUserId
- Timestamps

## 🔒 Environment Variables

See [Backend Setup](#configure-environment-variables) section for all required environment variables.

**Important:** Never commit `.env` files or sensitive credentials to version control.

## 🔐 Security Features

- Password hashing with BCrypt
- JWT token-based authentication with expiry
- Role-based authorization
- Email verification before login
- Secure password reset with time-limited tokens
- Request logging with user context
- CORS configuration

## 📝 Logging

Logs are stored in three locations:

1. **Console** - Real-time colored output
2. **General Logs** - `Server/Logs/log-YYYY-MM-DD.txt` (30-day retention)
3. **Error Logs** - `Server/Logs/error-YYYY-MM-DD.txt` (60-day retention)

All logs include:
- Timestamp
- Log level
- Source context
- Structured properties (UserId, Email, etc.)
- Exception details
- HTTP request information

## 🧩 Key Dependencies

### Backend
- Microsoft.EntityFrameworkCore.SqlServer
- Microsoft.AspNetCore.Authentication.JwtBearer
- BCrypt.Net-Next
- MailKit
- CloudinaryDotNet
- Serilog.AspNetCore
- xUnit (testing)

### Frontend
- react & react-dom
- react-router-dom
- axios
- tailwindcss
- react-hot-toast
- date-fns

## 🤝 Contributing

Contributions are welcome! Please follow these steps:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 👤 Author

**Muhammad Ahmed Saleem Khan**

## 🙏 Acknowledgments

- Built with ASP.NET Core and React
- Styled with Tailwind CSS
- Logged with Serilog
- Tested with xUnit

---

**Note:** This project is for educational and portfolio purposes. For production use, ensure proper security audits and configuration.
