# Task Management Application

A full-stack task management application built with .NET Core Web API and React, similar to Jira, Trello, or Basecamp.

## Features Implemented

### ✅ Core Task Management
- **Add new tasks** with name, description, and deadline
- **Edit tasks** and all details including marking as favorite
- **Delete tasks** from the board
- **View task details** with full information display

### ✅ Board Management
- **Default columns**: To Do, In Progress, Done
- **Add custom columns** to represent different work states
- **Move tasks between columns** with proper ordering
- **Delete empty columns** (prevents deletion if tasks exist)

### ✅ Smart Sorting
- **Alphabetical sorting** within each column
- **Favorites prioritized** - favorite tasks always appear at the top
- **Automatic ordering** when moving tasks between columns

### ✅ File Attachments
- **Upload images** and files to tasks
- **Download attachments** with original filenames
- **Delete attachments** as needed

### ✅ Comprehensive Testing
- **Unit tests** with NUnit for all services
- **Integration tests** for API endpoints
- **Test coverage** for core business logic

## Technical Stack

- **Backend**: .NET 9 Web API
- **Frontend**: React 19
- **Database**: Entity Framework with In-Memory Database
- **Testing**: NUnit framework
- **Documentation**: Swagger/OpenAPI

## Getting Started

### Prerequisites
- .NET 9 SDK
- Node.js 18+

### Backend Setup
1. Navigate to the backend directory:
   ```bash
   cd backend
   ```

2. Restore dependencies:
   ```bash
   dotnet restore
   ```

3. Run the API:
   ```bash
   dotnet run
   ```

The API will be available at `https://localhost:7042` with Swagger documentation at the root URL.

### Frontend Setup
1. Navigate to the frontend directory:
   ```bash
   cd frontend
   ```

2. Install dependencies:
   ```bash
   npm install
   ```

3. Start the development server:
   ```bash
   npm start
   ```

The frontend will be available at `http://localhost:3000`.

### Running Tests
```bash
cd backend/Tests
dotnet test
```

## API Endpoints

### Tasks
- `GET /api/tasks` - Get all tasks
- `GET /api/tasks/{id}` - Get specific task
- `POST /api/tasks` - Create new task
- `PUT /api/tasks/{id}` - Update task
- `DELETE /api/tasks/{id}` - Delete task
- `PATCH /api/tasks/{id}/move` - Move task between columns

### Columns
- `GET /api/columns` - Get all columns
- `POST /api/columns` - Create new column
- `DELETE /api/columns/{id}` - Delete column

### Board
- `GET /api/board` - Get complete board with all columns and tasks

### Attachments
- `POST /api/attachments/tasks/{taskId}` - Upload attachment
- `GET /api/attachments/{id}/download` - Download attachment
- `DELETE /api/attachments/{id}` - Delete attachment

## Key Design Decisions

### Architecture
- **Clean Architecture**: Separated concerns with Models, DTOs, Services, and Controllers
- **Dependency Injection**: Proper service registration and lifetime management
- **In-Memory Database**: For quick setup and testing (easily switchable to SQL Server)

### Business Logic
- **Favorite Tasks**: Always sorted to the top within each column
- **Smart Ordering**: Automatic sort order management when moving tasks
- **File Upload**: Secure file handling with unique naming to prevent conflicts

### Testing Strategy
- **Unit Tests**: Focused on business logic in services
- **Integration Tests**: End-to-end API testing
- **Test Data**: Isolated test databases for each test run

## Demo Data
The application starts with three default columns:
- To Do
- In Progress  
- Done

You can immediately start adding tasks and testing the functionality.

## Future Enhancements
- User authentication and authorization
- Real-time updates with SignalR
- Task assignment and collaboration features
- Advanced filtering and search
- Persistent database with migrations
- Azure deployment configuration
