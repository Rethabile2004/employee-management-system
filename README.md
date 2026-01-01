# Employee Management System

This Employee Management System is built with ASP.NET MVC and integrates Firebase for authentication and secure data storage. It features a modern, responsive interface and role‑based access control with three distinct user roles: **Admin**, **HR**, and **Employee**.

## Features

### Admin
- Approve or reject new user accounts  
- Oversee system performance  
- Access and review all audit logs  

### HR
- Approve or reject employee qualifications, trainings, or skills  
- Send notifications to employees  
- Manage HR workflows related to employee development  

### Employee
- Full CRUD functionality over qualifications, trainings, and skills  
- Manage personal account details  
- Access a personalized dashboard  

## Tech Stack
- **ASP.NET MVC** for application structure  
- **Firebase Authentication** for secure login and role management  
- **Firebase Firestore** for real‑time data storage  
- **Entity Framework Core** for data access patterns  
- **Bootstrap / Modern UI libraries** for responsive design  

## Setup Instructions

### Prerequisites
- Visual Studio 2022 or later  
- .NET 6.0 SDK or later  
- Firebase project with service account credentials  
- Node.js (optional, if using Firebase CLI or frontend tooling)  

### Steps
1. **Clone the repository**  
   ```bash
   git clone https://github.com/rethabile2004/employee-management-system.git
   cd employee-management-system
   ```

2. **Open the solution**  
   - Launch Visual Studio  
   - Open `auth.sln` from the project root  

3. **Configure Firebase**  
   - Place your `serviceAccountKey.json` file in the `auth/` project folder  
   - Update `appsettings.json` and `appsettings.Development.json` with your Firebase project details  

4. **Restore dependencies**  
   ```bash
   dotnet restore
   ```

5. **Run database migrations (if applicable)**  
   ```bash
   dotnet ef database update
   ```

6. **Start the application**  
   ```bash
   dotnet run
   ```

7. **Access the app**  
   - Navigate to `https://localhost:5001` in your browser  

## Project Structure
- `auth/Controllers` – MVC controllers for handling requests  
- `auth/Models` – Data models for users, roles, and records  
- `auth/Views` – Razor views for UI rendering  
- `auth/Services` – Firebase and business logic services  
- `wwwroot/` – Static files and frontend assets  

## Future Improvements
- Enhanced reporting and analytics for Admin  
- Role‑based dashboards with richer UI components  
- Integration with external APIs for HR workflows  
