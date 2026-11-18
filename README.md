# 💊 MediVerify

### Digital Prescription Verification and Medication Safety Management System

![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)
![ASP.NET Core MVC](https://img.shields.io/badge/ASP.NET%20Core-MVC-5C2D91)
![EF Core](https://img.shields.io/badge/EF%20Core-8.0-0d9488)
![SQL Server](https://img.shields.io/badge/SQL%20Server-Express-CC2927?logo=microsoftsqlserver&logoColor=white)
![Bootstrap 5](https://img.shields.io/badge/Bootstrap-5-7952B3?logo=bootstrap&logoColor=white)

MediVerify is an ASP.NET Core 8 MVC web application that digitizes the complete prescription lifecycle: a **doctor** writes a prescription, a **pharmacist** verifies it against automated safety checks and dispenses it, an **administrator** oversees medicines, inventory, users and audit trails, and the **patient** tracks their own prescriptions, all with role-based access, notifications and exportable reports.

---

## 📑 Table of Contents

1. [Key Features](#-key-features)
2. [Prescription Workflow](#-prescription-workflow)
3. [Technology Stack](#-technology-stack)
4. [Architecture](#-architecture)
5. [Database Design](#-database-design)
6. [Getting Started](#-getting-started)
7. [Demo Accounts](#-demo-accounts)
8. [Project Structure](#-project-structure)
9. [Security](#-security)
10. [Screenshots](#-screenshots)
11. [Known Limitations & Future Work](#-known-limitations--future-work)
12. [Disclaimer](#-disclaimer)
13. [Author](#-author)

---

## ✨ Key Features

### 👨‍⚕️ Doctor
- Create prescriptions with multiple medicine items (quantity, dosage instructions, duration, notes)
- Save as Draft, then Submit to the pharmacy
- View own patients and own prescription history
- Printable digital prescription with status timeline

### 💊 Pharmacist
- Verification queue of submitted prescriptions
- Automated **medication safety checks** (drug interactions, duplicate therapy, stock and expiry, prescription validity, excessive quantity)
- Approve or reject prescriptions
- Dispense against inventory with stock decrement
- Dispensing history

### 🛠️ Administrator
- Medicine management (add, edit, view, activate/deactivate)
- Inventory tracking with low-stock and expiry monitoring
- User and role management
- Audit log review
- Reports in **PDF, Excel and CSV**

### 🧑‍🤝‍🧑 Patient
- Self-registration (Patient role only; privileged roles are enforced server-side)
- Personal dashboard with prescription status and history

### 🔔 Common to All Roles
- Role-based dashboards and navigation
- In-app notifications with unread count
- Audit logging of key actions (user, action, entity, timestamp, IP address)
- Cookie-based authentication with ASP.NET Core Identity

> **Note:** Feature availability depends on the project version you are running. See [Known Limitations](#-known-limitations--future-work).

---

## 🔄 Prescription Workflow

```
Doctor creates       Doctor submits       Pharmacist reviews      Pharmacist dispenses     Patient views
 prescription   ──►   prescription   ──►  safety alerts and  ──►  and inventory is   ──►   status and
   (Draft)            (Submitted)         verifies (Verified)     updated (Dispensed)       history
```

Every step writes an **audit log** entry and sends a **notification** to the next person in the chain.

| Status | Meaning |
|---|---|
| Draft | Created by the doctor, not yet sent to the pharmacy |
| Submitted | Awaiting pharmacist verification |
| Verified | Approved by the pharmacist, ready to dispense |
| Dispensed | Medicines handed over, inventory updated |

---

## 🧰 Technology Stack

| Category | Technology |
|---|---|
| Language | C# 12 |
| Framework | ASP.NET Core 8 (MVC + Razor Pages) |
| ORM | Entity Framework Core 8 |
| Database | Microsoft SQL Server (Express) |
| Authentication | ASP.NET Core Identity (cookie-based) |
| Authorization | Role-based policies (Admin / Doctor / Pharmacist / Patient) |
| Object Mapping | AutoMapper 12 |
| Excel Export | ClosedXML |
| PDF Export | iTextSharp (LGPLv2.Core) |
| Frontend | Razor views, HTML, CSS, JavaScript, Bootstrap 5 |
| Tools | Visual Studio / VS Code, SSMS, Git |

---

## 🏗️ Architecture

MediVerify follows a layered MVC structure with dependency injection throughout.

| Layer | Responsibility |
|---|---|
| **Controllers** | Handle requests, enforce authorization policies, return views |
| **Services** | Business logic behind interfaces (`IAuditService`, `INotificationService`, `IReportService`) |
| **ViewModels** | Input and output models, validated with data annotations |
| **Models** | EF Core entities |
| **Data** | `ApplicationDbContext`, migrations, `DbInitializer` (migrate + seed on startup) |
| **Mapping** | AutoMapper profile for entity ↔ ViewModel conversion |

**Startup (`Program.cs`)** registers the DbContext, Identity, authorization policies, services and AutoMapper, then runs `DbInitializer.InitializeAsync()` to apply migrations and seed roles and demo users.

---

## 🗄️ Database Design

| Entity | Purpose |
|---|---|
| `ApplicationUser` | Identity user with full name, active flag, created date |
| `Patient` | Patient profile linked to a user (DOB, gender, address, emergency contact) |
| `Medicine` | Medicine catalogue (name, generic name, manufacturer, category) |
| `Inventory` | Stock per medicine (quantity, reorder level, batch, expiry) |
| `Prescription` | Header: patient, doctor, number, dates, status |
| `PrescriptionItem` | Medicine lines: quantity, instructions, duration |
| `DispensingRecord` / `DispensedItem` | What the pharmacist dispensed, when and by whom |
| `SafetyAlert` | Medication safety warnings with severity and review status |
| `AuditLog` | Who did what, to which entity, when, from which IP |
| `Notification` | In-app messages per user |

**Key relationships:** Patient → User, Prescription → Patient and Doctor, PrescriptionItem → Prescription (cascade) and Medicine, DispensingRecord → Prescription and Pharmacist, DispensedItem → DispensingRecord (cascade) and Medicine.

---

## 🚀 Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Microsoft SQL Server Express (instance `localhost\SQLEXPRESS`) and the service running
- SQL Server Management Studio (optional, for inspecting data)
- Visual Studio 2022 or VS Code
- Git

### 1. Clone the repository

```bash
git clone https://github.com/sachin-gautam-12/MediVerify-.git
cd MediVerify-
```

### 2. Configure the connection string

Edit `MediVerify/appsettings.json` if your SQL Server instance differs:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=MediVerifyDB;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

### 3. Restore, build and run

```bash
dotnet restore
dotnet build MediVerify/MediVerify.csproj
dotnet run --project MediVerify/MediVerify.csproj --urls "http://localhost:5050"
```

Then open **http://localhost:5050**.

On first run the app **applies migrations automatically and seeds** roles and demo users, so no manual database setup is needed.

### Fallback: apply migrations manually

If automatic migration fails:

```bash
dotnet tool install --global dotnet-ef --version 8.*
cd MediVerify
dotnet ef database update
```

### Troubleshooting

| Problem | Fix |
|---|---|
| Cannot connect to SQL Server | Start the **SQL Server (SQLEXPRESS)** service in Windows Services and check the connection string |
| Port already in use | Change the port in `--urls` |
| HTTPS certificate warning | Run `dotnet dev-certs https --trust` or use the HTTP URL above |
| Stale schema errors | Drop `MediVerifyDB` in SSMS and run the app again |

---

## 🔑 Demo Accounts

These accounts are created by the seeder for **demo purposes only**.

| Role | Email | Password |
|---|---|---|
| Administrator | admin@mediverify.com | `Admin#123` |
| Doctor | doctor@mediverify.com | `Doctor#123` |
| Pharmacist | pharma@mediverify.com | `Pharma#123` |
| Patient | patient@mediverify.com | `Patient#123` |

> ⚠️ Change or remove these credentials before any real deployment.

---

## 📁 Project Structure

```text
MediVerify/
├── MediVerify.sln
├── README.md
└── MediVerify/
    ├── Controllers/     # Home, Account, Dashboard, Medicine, Prescription, ...
    ├── Models/          # EF Core entities
    ├── Data/            # ApplicationDbContext, DbInitializer
    ├── Services/        # Audit, Notification, Report (+ interfaces)
    ├── ViewModels/      # Input and output models
    ├── Mapping/         # AutoMapper profile
    ├── Migrations/      # EF Core migrations
    ├── Views/           # Razor views
    ├── wwwroot/         # CSS, JS, static assets
    ├── Program.cs       # Composition root
    ├── appsettings.json
    └── MediVerify.csproj
```

---

## 🔒 Security

- Passwords are hashed by ASP.NET Core Identity
- Role-based authorization policies on controllers and actions
- Anti-forgery tokens on all POST actions
- Public registration always creates a **Patient**; privileged roles are assigned by an administrator
- Audit trail of important actions
- Do **not** commit real connection strings, secrets or real patient data

---

## 📸 Screenshots

| Login | Admin Dashboard |
|---|---|
| _add screenshot_ | _add screenshot_ |

| Create Prescription | Pharmacist Verification |
|---|---|
| _add screenshot_ | _add screenshot_ |

| Dispensing | Reports |
|---|---|
| _add screenshot_ | _add screenshot_ |

*(Place images in `docs/screenshots/` and link them here.)*

---

## 🔭 Known Limitations & Future Work

- Safety checks are rule-based on seeded sample data, not a certified drug database
- Notifications are in-app only (no email or SMS)
- Planned: cloud deployment, secure hosted database, e-signature, advanced analytics, mobile-friendly PWA, email/SMS alerts, accessibility improvements

---

## ⚠️ Disclaimer

MediVerify is an **academic project** for digital prescription record management and medication safety workflows. It is not a substitute for professional medical advice, diagnosis or treatment. All medication decisions must be reviewed by qualified healthcare professionals. All demo data is fictional.

---

## 👤 Author

**Sachin Kumar Singh**
B.Tech, Computer Science and Engineering, Lovely Professional University

- GitHub: [sachin-gautam-12](https://github.com/sachin-gautam-12)
- LinkedIn: [Sachin Kumar Singh](https://www.linkedin.com/in/sachin-kumar-singh-5a193522a/)

---

⭐ If you find this project interesting, feel free to star the repository and share feedback.
