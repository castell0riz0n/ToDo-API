# ToDo-API
A common back-end API for the future ToDo Application
# TeamA ToDo App

A comprehensive task and expense management application with powerful features like recurring tasks, expense tracking, budgeting, and email notifications.

## Features

### Task Management
- Create, update, and delete tasks
- Organize tasks with categories and tags
- Set task priorities and due dates
- Create recurring tasks (daily, weekly, monthly, yearly)
- Add notes and reminders to tasks
- Mark tasks as completed
- Task statistics and reporting

### Expense Tracking
- Track expenses with categories and payment methods
- Recurring expenses
- Budget management with alerts
- Expense statistics and reporting
- Export reports to Excel and CSV
- Year-over-year expense comparison

### User Management
- User registration and authentication
- Role-based authorization
- Two-factor authentication
- Email notifications
- User profile management
- Activity logging
- Permission management

## Tech Stack

- **Backend**: .NET 9.0, ASP.NET Core
- **Database**: SQL Server with Entity Framework Core
- **Authentication**: JWT-based authentication
- **Email**: SendGrid and SMTP support
- **Background Processing**: Hangfire
- **Documentation**: Scalar API Reference
- **Reporting**: EPPlus for Excel generation

## Architecture

The application follows a clean architecture pattern with the following layers:

- **Core**: Domain entities and business logic
- **Application**: Application services, DTOs, and interfaces
- **EntityFramework**: Database context and configurations
- **Host**: API controllers and application configuration

## Getting Started

### Prerequisites

- .NET 9.0 SDK
- SQL Server
- SMTP server or SendGrid account (for email notifications)

### Installation

1. Clone the repository:
```bash
git clone https://github.com/your-organization/TeamA.ToDo.git
```

2. Navigate to the project directory:
```bash
cd TeamA.ToDo
```

3. Update the connection string in `appsettings.json` to point to your SQL Server instance.

4. Configure email settings in `appsettings.json`.

5. Run the database migrations:
```bash
dotnet ef database update
```

6. Build and run the application:
```bash
dotnet build
dotnet run --project TeamA.ToDo
```

7. Access the API at `https://localhost:7260` or `http://localhost:5258`.

## Project Structure

```
TeamA.ToDo.sln
├── TeamA.ToDo.Core/                 # Domain entities
├── TeamA.ToDo.Core.Shared/          # Shared domain models (enums, etc.)
├── TeamA.ToDo.Application/          # Application services and DTOs
├── TeamA.ToDo.EntityFramework/      # Database context and configurations
└── TeamA.ToDo/                      # Host project with API controllers
```

## API Endpoints

The application exposes RESTful API endpoints for task and expense management:

### Authentication
- `POST /api/auth/register`: Register a new user
- `POST /api/auth/login`: Log in a user
- `POST /api/auth/refresh-token`: Refresh JWT token

### Tasks
- `GET /api/tasks`: Get tasks for the current user
- `GET /api/tasks/{id}`: Get a specific task
- `POST /api/tasks`: Create a new task
- `PUT /api/tasks/{id}`: Update a task
- `DELETE /api/tasks/{id}`: Delete a task

### Expenses
- `GET /api/expenses`: Get expenses for the current user
- `POST /api/expenses`: Create a new expense
- `GET /api/expenses/statistics`: Get expense statistics

### Budgets
- `GET /api/budgets`: Get budgets for the current user
- `POST /api/budgets`: Create a new budget
- `GET /api/budgets/summary`: Get budget summary

## Security

- JWT-based authentication
- Password validation with custom rules
- Two-factor authentication
- CSRF protection
- XSS protection
- SQL injection protection
- Role-based authorization

## Email Notifications

The application sends email notifications for:
- Account verification
- Password reset
- Task reminders
- Task completion
- Budget alerts
- Monthly budget summaries

## License

[License details here]

## Contributing

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/my-feature`
3. Commit your changes: `git commit -am 'Add my feature'`
4. Push to the branch: `git push origin feature/my-feature`
5. Submit a pull request
