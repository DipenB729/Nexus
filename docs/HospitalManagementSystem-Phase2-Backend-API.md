# Phase 2: Backend API Implementation (ASP.NET Core Web API + EF Core + SQL Server)

This is the **next practical part** after DB design. It gives a beginner-friendly but scalable backend setup you can implement directly.

---

## 1) Create Solution Structure

```bash
dotnet new sln -n HospitalManagementSystem
mkdir -p src tests

cd src
dotnet new webapi -n HospitalManagementSystem.Api
dotnet new classlib -n HospitalManagementSystem.Application
dotnet new classlib -n HospitalManagementSystem.Domain
dotnet new classlib -n HospitalManagementSystem.Infrastructure

cd ../tests
dotnet new xunit -n HospitalManagementSystem.UnitTests
```

Add references:

```bash
cd ../
dotnet sln add src/HospitalManagementSystem.Api/HospitalManagementSystem.Api.csproj
dotnet sln add src/HospitalManagementSystem.Application/HospitalManagementSystem.Application.csproj
dotnet sln add src/HospitalManagementSystem.Domain/HospitalManagementSystem.Domain.csproj
dotnet sln add src/HospitalManagementSystem.Infrastructure/HospitalManagementSystem.Infrastructure.csproj
dotnet sln add tests/HospitalManagementSystem.UnitTests/HospitalManagementSystem.UnitTests.csproj

dotnet add src/HospitalManagementSystem.Api/HospitalManagementSystem.Api.csproj reference src/HospitalManagementSystem.Application/HospitalManagementSystem.Application.csproj
dotnet add src/HospitalManagementSystem.Api/HospitalManagementSystem.Api.csproj reference src/HospitalManagementSystem.Infrastructure/HospitalManagementSystem.Infrastructure.csproj
dotnet add src/HospitalManagementSystem.Application/HospitalManagementSystem.Application.csproj reference src/HospitalManagementSystem.Domain/HospitalManagementSystem.Domain.csproj
dotnet add src/HospitalManagementSystem.Infrastructure/HospitalManagementSystem.Infrastructure.csproj reference src/HospitalManagementSystem.Application/HospitalManagementSystem.Application.csproj
dotnet add src/HospitalManagementSystem.Infrastructure/HospitalManagementSystem.Infrastructure.csproj reference src/HospitalManagementSystem.Domain/HospitalManagementSystem.Domain.csproj
```

---

## 2) Install Required NuGet Packages

In `HospitalManagementSystem.Api`:
- `Microsoft.AspNetCore.Authentication.JwtBearer`
- `Swashbuckle.AspNetCore`

In `HospitalManagementSystem.Infrastructure`:
- `Microsoft.EntityFrameworkCore`
- `Microsoft.EntityFrameworkCore.SqlServer`
- `Microsoft.EntityFrameworkCore.Design`

In `HospitalManagementSystem.Application`:
- `FluentValidation`

---

## 3) Domain Layer (Entities + Enums)

Create entities:
- `Role`
- `User`
- `Patient`
- `Doctor`
- `DoctorSchedule`
- `Staff`
- `HospitalService`
- `Appointment`

Create enums:
- `UserRoleType` (`Admin`, `User`)
- `AppointmentStatus` (`Pending`, `Approved`, `Rescheduled`, `Cancelled`, `Completed`)

Example (`Appointment.cs`):

```csharp
public class Appointment
{
    public long AppointmentId { get; set; }
    public long PatientId { get; set; }
    public long DoctorId { get; set; }
    public long? ScheduleId { get; set; }
    public long? ServiceId { get; set; }

    public DateOnly AppointmentDate { get; set; }
    public TimeOnly SlotStartTime { get; set; }
    public TimeOnly SlotEndTime { get; set; }

    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;
    public string? Reason { get; set; }
    public string? AdminRemarks { get; set; }

    public long CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
```

---

## 4) Application Layer (DTOs + Interfaces + Validation)

## Recommended DTO folders

```text
Application/
  Common/
    ApiResponse.cs
    PagedRequestDto.cs
    PagedResponseDto.cs
  Auth/Dtos/
    RegisterRequestDto.cs
    LoginRequestDto.cs
    AuthResponseDto.cs
  Doctors/Dtos/
    DoctorCreateDto.cs
    DoctorUpdateDto.cs
    DoctorReadDto.cs
  Appointments/Dtos/
    AppointmentCreateDto.cs
    AppointmentStatusUpdateDto.cs
    AppointmentReadDto.cs
```

## Common API response wrapper

```csharp
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public IEnumerable<string>? Errors { get; set; }

    public static ApiResponse<T> Ok(T data, string message = "Success") =>
        new() { Success = true, Message = message, Data = data };

    public static ApiResponse<T> Fail(string message, IEnumerable<string>? errors = null) =>
        new() { Success = false, Message = message, Errors = errors };
}
```

## Service interfaces
- `IAuthService`
- `IDoctorService`
- `IDoctorScheduleService`
- `IStaffService`
- `IHospitalServiceService`
- `IAppointmentService`
- `IReportService`

## Validation examples (FluentValidation)
- Validate `Email`, `Phone`, required `FullName`.
- Validate appointment date is not in past.
- Validate slot start < end.

---

## 5) Infrastructure Layer (EF Core + Auth + Services)

## DbContext
Create `HospitalDbContext` with `DbSet<>` for all entities and configure:
- Unique indexes (`Users.Email`, `Doctors.Email`, `Staff.Email`, `HospitalServices.ServiceName`)
- Relationships and cascade behaviors
- Enum-to-string conversion for `AppointmentStatus`

Example registration in DI:

```csharp
services.AddDbContext<HospitalDbContext>(options =>
    options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
```

## JWT token service
Create `IJwtTokenService` and `JwtTokenService` to generate token with claims:
- `sub` = `UserId`
- `email`
- `role` = `Admin`/`User`

Use config section:

```json
"Jwt": {
  "Issuer": "HospitalManagementSystem",
  "Audience": "HospitalManagementSystem.Client",
  "Key": "CHANGE_THIS_TO_A_LONG_SECURE_KEY_32+_CHARS",
  "ExpiresInMinutes": 120
}
```

## Repository/Service pattern
Use service + EF Core directly for simple modules, and repository only where complexity is high (appointments/reporting queries).

Business rules to enforce:
- Prevent overlapping doctor schedules.
- Prevent double booking same doctor/date/time.
- Users can only access their own appointments.
- Only Admin can approve/reschedule/cancel globally.

---

## 6) API Layer (Controllers + Middleware + Security)

## Program.cs checklist
1. Add Swagger.
2. Add DB context.
3. Add application/infrastructure DI.
4. Configure JWT Bearer authentication.
5. Add authorization policies:
   - `AdminOnly`
   - `UserOnly`
6. Add global exception middleware.
7. Add CORS policy for Angular client URL.

Example authorization policies:

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
    options.AddPolicy("UserOnly", p => p.RequireRole("User"));
});
```

## Controllers to build first
1. `AuthController`
2. `DoctorsController`
3. `DoctorSchedulesController`
4. `AppointmentsController`
5. `HospitalServicesController`
6. `StaffController`
7. `ReportsController`

## Example endpoint contracts

### Auth
- `POST /api/auth/register`
- `POST /api/auth/login`

### Doctors
- `GET /api/doctors?page=1&pageSize=10&search=`
- `POST /api/doctors` (Admin)
- `PUT /api/doctors/{doctorId}` (Admin)

### Appointments
- `POST /api/appointments` (User)
- `GET /api/appointments/my` (User)
- `GET /api/appointments` (Admin)
- `PUT /api/appointments/{id}/status` (Admin)
- `PUT /api/appointments/{id}/reschedule` (Admin)

---

## 7) Minimal Folder Structure (Backend)

```text
src/
  HospitalManagementSystem.Api/
    Controllers/
    Middleware/
    Extensions/
    Program.cs
    appsettings.json

  HospitalManagementSystem.Application/
    Common/
    Auth/
      Dtos/
      Interfaces/
    Doctors/
      Dtos/
      Interfaces/
    Appointments/
      Dtos/
      Interfaces/
    Validation/

  HospitalManagementSystem.Domain/
    Entities/
    Enums/

  HospitalManagementSystem.Infrastructure/
    Persistence/
      HospitalDbContext.cs
      Configurations/
    Services/
      Auth/
      Doctors/
      Appointments/
    Security/
      JwtTokenService.cs
```

---

## 8) Migrations and Database Update

```bash
dotnet ef migrations add InitialCreate -p src/HospitalManagementSystem.Infrastructure -s src/HospitalManagementSystem.Api
dotnet ef database update -p src/HospitalManagementSystem.Infrastructure -s src/HospitalManagementSystem.Api
```

---

## 9) Phase 2 Done Criteria

You can mark backend phase complete when:
- DB migrations run successfully.
- Register/Login return valid JWT.
- Admin CRUD works for doctors, schedules, staff, services.
- User can book appointment and view own appointments.
- Admin can approve/reschedule/cancel appointments.
- Pagination/search works on list APIs.
- Swagger documents all endpoints.
- Global error format is consistent.

---

## 10) What to build immediately after this (Phase 3)

- Start Angular app with role-based layouts.
- Implement auth screens first.
- Connect doctors/services listing APIs.
- Build appointment booking UI and history.
- Add admin dashboard and management pages.

