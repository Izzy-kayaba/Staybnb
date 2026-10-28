# Staybnb - Programming 742 Assignment

Staybnb is a student-level ASP.NET Core MVC property rental system. It demonstrates the complete lifecycle required by the assignment: a new user starts as a Guest, applies with a property and image, becomes a Host after Admin approval, and uses a role-specific dashboard. A SuperAdmin can promote existing Guests to Admin.

## Technology

- ASP.NET Core MVC on .NET 8
- ASP.NET Core Identity for login, passwords and roles
- Entity Framework Core 8
- SQL Server LocalDB
- Bootstrap and ordinary CSS
- xUnit for seven focused tests

The design intentionally stays shallow: Razor page -> controller action -> `ApplicationDbContext` -> SQL Server. Small services are used only for price calculation, audit logging, notifications and safe file storage.

## Run the project

Requirements: .NET 8 SDK and SQL Server LocalDB (installed with Visual Studio's ASP.NET workload).

```powershell
dotnet tool restore
dotnet restore Staybnb.sln
dotnet run --project Staybnb.Web
```

Open the HTTP or HTTPS address printed in the terminal. On first start, the program applies the included migration, creates the database and inserts demonstration data.

To run the focused tests:

```powershell
dotnet test Staybnb.sln
```

## Demonstration accounts

| Role | Email | Password | Demonstrates |
| --- | --- | --- | --- |
| SuperAdmin | `superadmin@staybnb.local` | `Admin123!` | Audit log, applications, Guest-to-Admin promotion |
| Admin | `admin@staybnb.local` | `Admin123!` | Application approval and rejection |
| Host | `host@staybnb.local` | `Host123!` | Listings, booking decisions, check-in verification |
| Guest | `guest@staybnb.local` | `Guest123!` | Discovery, booking, check-in, messages and notifications |
| Applicant | `applicant@staybnb.local` | `Guest123!` | Pending Host application ready for Admin approval |

The passwords are local demonstration credentials, not production secrets.

## Simple marking walkthrough

1. Browse the home page and filter active properties by city or property type.
2. Log in as Guest, request a property and note the stored total: `(price per night x nights) + cleaning fee + service fee`.
3. Log in as Host, approve the pending booking and edit or deactivate a property.
4. Log back in as Guest, open an approved booking and upload an ID or passport.
5. Log in as Host, download the private document, verify it and see the booking become `CheckedIn`.
6. Log in as Admin and approve the pending application for `applicant@staybnb.local`.
7. Log in as that applicant and confirm the Host dashboard is available while the Guest dashboard is denied.
8. Log in as SuperAdmin to view activity logs and the Guest-to-Admin promotion control.
9. Open Messages and Notifications; opening or marking an item updates its `IsRead` value in SQL Server.

## Main folders

- `Staybnb.Web/Controllers` - page actions and role checks
- `Staybnb.Web/Models` - database entities and status values
- `Staybnb.Web/ViewModels` - form inputs and dashboard data
- `Staybnb.Web/Data` - database context and startup seeding
- `Staybnb.Web/Migrations` - complete initial SQL Server migration
- `Staybnb.Web/Views` - Razor pages
- `Staybnb.Web/Services` - four small shared operations
- `Staybnb.Tests` - important calculation and validation tests
- `docs` - report and requirements evidence

## Important implementation notes

- Every new registration is assigned only the `Guest` role.
- Approving a first Host application adds `Host`, removes `Guest`, and activates the property.
- A submitted property cannot be activated until its application is approved.
- Role swaps and application approval use a database transaction so partial changes are rolled back.
- Only the `SuperAdmin` action can assign `Admin`.
- Guest, Host and Admin controllers use explicit role authorization attributes.
- Uploaded check-in documents are stored under `App_Data`, outside the public web folder. Only the owning Host can download them through an authorised controller action.
- The server calculates booking totals from database prices and never trusts a total sent by the browser.
- Login, role changes, booking status changes and important workflow actions create `ActivityLog` rows.

See [docs/REQUIREMENTS-TRACEABILITY.md](docs/REQUIREMENTS-TRACEABILITY.md) for the direct requirement-to-code-to-evidence map.
