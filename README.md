# MediVerify – Digital Prescription Verification and Medication Safety Management System

MediVerify is an ASP.NET Core MVC-based web application designed to digitize prescription management, organize patient and medicine records, and support medication safety workflows. It provides role-based access for administrators, doctors, pharmacists, and patients.

## Overview

The goal of MediVerify is to provide a centralized platform where prescription-related information can be managed digitally, reducing manual record-keeping and improving coordination between healthcare users.

The system uses ASP.NET Core MVC with Entity Framework Core and Microsoft SQL Server for data management.

## Key Features

- **Role-Based Access:** Separate access for Admin, Doctor, Pharmacist, and Patient roles.
- **User Management:** User registration, authentication, and account management using ASP.NET Core Identity.
- **Patient Management:** Store and manage patient information.
- **Prescription Management:** Create and manage digital prescription records.
- **Medicine Management:** Maintain medicine information.
- **Inventory Management:** Support medicine inventory tracking.
- **Safety Alerts:** Maintain medication-related safety alerts for review.
- **Dispensing Management:** Manage prescription dispensing records.
- **Notifications:** Support notifications for relevant system activities.
- **Audit Logs:** Maintain records of selected system activities.
- **Reports:** Support report generation and data management.

*Note: Availability of individual features depends on the implementation and current project version.*

## Technology Stack

| Category | Technology |
|---|---|
| Backend | C# |
| Framework | ASP.NET Core MVC (.NET 8) |
| Frontend | HTML, CSS, JavaScript |
| UI Framework | Bootstrap 5 |
| Database | Microsoft SQL Server |
| ORM | Entity Framework Core 8 |
| Authentication | ASP.NET Core Identity |
| Object Mapping | AutoMapper |
| Excel Reports | ClosedXML |
| PDF Reports | iTextSharp |
| Development Tools | Visual Studio / VS Code, SSMS |

## Project Structure

```text
MediVerify/
│
├── MediVerify.sln
├── README.md
│
└── MediVerify/
    ├── Controllers/
    ├── Models/
    ├── Data/
    ├── Services/
    ├── ViewModels/
    ├── Views/
    ├── wwwroot/
    ├── Program.cs
    ├── appsettings.json
    └── MediVerify.csproj
```

## Database Configuration

MediVerify uses Microsoft SQL Server with Entity Framework Core.

1. Install SQL Server and SQL Server Management Studio.
2. Create or configure the `MediVerifyDB` database.
3. Update the `DefaultConnection` connection string in your local configuration.
4. Apply Entity Framework migrations.

Example connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=MediVerifyDB;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

Replace `YOUR_SERVER` with your local or hosted SQL Server instance.

## Installation and Setup

### Prerequisites

- .NET 8 SDK
- Microsoft SQL Server
- SQL Server Management Studio (SSMS)
- Visual Studio or Visual Studio Code
- Git

### 1. Clone the Repository

```bash
git clone https://github.com/sachin-gautam-12/MediVerify-.git
```

### 2. Open the Project Folder

```bash
cd MediVerify-
```

Open the solution file `MediVerify.sln` in Visual Studio, or open the project folder in VS Code.

### 3. Restore Dependencies

```bash
dotnet restore
```

### 4. Apply Database Migrations

Run these commands from the folder containing `MediVerify.csproj`:

```bash
dotnet ef migrations list
dotnet ef database update
```

If Entity Framework CLI is not installed:

```bash
dotnet tool install --global dotnet-ef --version 8.*
```

### 5. Build the Project

```bash
dotnet build
```

### 6. Run the Application

```bash
dotnet run --urls http://localhost:5050
```

Open the application:

http://localhost:5050

## User Roles

| Role | Purpose |
|---|---|
| Admin | Manage system users and administrative activities |
| Doctor | Manage patient and prescription workflows |
| Pharmacist | Manage medicine and dispensing workflows |
| Patient | Access relevant patient information |

Role assignment should be controlled securely by the application administrator.

## Security and Privacy

- Use secure authentication and authorization.
- Never commit passwords, API keys, or private connection strings.
- Do not upload real patient information or confidential medical records to a public repository.
- Store passwords using ASP.NET Core Identity password hashing.
- Restrict privileged roles to authorized administrators.

## Important Disclaimer

MediVerify is an academic software project intended to support digital prescription record management and medication safety workflows. It is not a substitute for professional medical advice, diagnosis, or treatment. Medication decisions must be reviewed by qualified healthcare professionals.

## Future Enhancements

- Cloud-based deployment.
- Secure online database integration.
- Improved prescription verification workflows.
- Enhanced reporting and analytics.
- Additional notification and safety-review features.
- Improved user interface and accessibility.

## Author

**Sachin Kumar Singh**

B.Tech – Computer Science and Engineering  
Lovely Professional University

- **GitHub:** https://github.com/sachin-gautam-12
- **LinkedIn:** https://www.linkedin.com/in/sachin-kumar-singh-5a193522a/

---

⭐ If you find this project interesting, feel free to explore the repository and share your feedback.

**Project:** MediVerify  
**Category:** Healthcare Management System  
**Platform:** ASP.NET Core MVC  
**Database:** Microsoft SQL Server
