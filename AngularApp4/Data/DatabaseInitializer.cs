using System.Security.Cryptography;
using System.Text;
using AngularApp4.Model.Hms;
using Microsoft.EntityFrameworkCore;
using ServiceModel = AngularApp4.Model.Service;

namespace AngularApp4.Data;

public sealed class DatabaseInitializer
{
    private static readonly (string Name, string Description)[] SystemRoles =
    {
        ("Admin", "Full hospital administration access"),
        ("Receptionist", "Front desk scheduling and patient intake"),
        ("Doctor", "Clinical workflow and appointment access"),
        ("Pharmacist", "Medicine dispensing and pharmacy control"),
        ("Lab", "Laboratory workflow and results processing"),
        ("Storekeeper", "Inventory and store operations"),
        ("Accountant", "Billing, invoices, and payment oversight"),
        ("User", "Self-service patient portal access")
    };

    private static readonly (string Key, string Name)[] Modules =
    {
        ("dashboard", "Dashboard"),
        ("masters", "Master Setup"),
        ("patients", "Patients"),
        ("appointments", "Appointments"),
        ("pharmacy", "Pharmacy"),
        ("laboratory", "Laboratory"),
        ("inventory", "Inventory"),
        ("billing", "Billing"),
        ("branches", "Branches"),
        ("settings", "Settings")
    };

    private readonly AppDbContext _db;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(AppDbContext db, ILogger<DatabaseInitializer> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _db.Database.MigrateAsync(cancellationToken);
        await EnsureRolesAsync(cancellationToken);
        await EnsureRolePermissionsAsync(cancellationToken);
        await EnsureDemoAccountsAsync(cancellationToken);
        await EnsureHospitalProfileAsync(cancellationToken);
        await EnsureBranchesAsync(cancellationToken);
        await EnsureDepartmentsAsync(cancellationToken);
        await EnsurePatientCategoriesAsync(cancellationToken);
        await EnsurePatientCategoryAssignmentsAsync(cancellationToken);
        await EnsurePatientProfilesAsync(cancellationToken);
        await EnsureSeedServicesAsync(cancellationToken);
        await EnsureDoctorsAsync(cancellationToken);
        await EnsureDoctorSchedulesAsync(cancellationToken);
        await EnsureStaffAsync(cancellationToken);
        await EnsureWardsAndBedsAsync(cancellationToken);
        await EnsureTokenSettingsAsync(cancellationToken);
        await EnsureAppointmentsAsync(cancellationToken);
        await EnsureAdmissionsAsync(cancellationToken);
        await EnsureInventoryAsync(cancellationToken);
        await EnsurePhase5InventoryInfrastructureAsync(cancellationToken);
        await EnsurePhase5InventorySeedAsync(cancellationToken);
        await EnsureLabTestsAsync(cancellationToken);
        await EnsureServicePackagesAsync(cancellationToken);
        await EnsureBillingChargeDefinitionsAsync(cancellationToken);
        await EnsureBillingPaymentMethodsAsync(cancellationToken);
        await EnsureBillingPartnersAsync(cancellationToken);
        await EnsureBillingRulesAsync(cancellationToken);
        await EnsureBillingAsync(cancellationToken);

        _logger.LogInformation("Database schema verified and initial seed data applied.");
    }

    private async Task EnsureRolesAsync(CancellationToken cancellationToken)
    {
        var hasChanges = false;

        foreach (var (name, description) in SystemRoles)
        {
            var role = await _db.Roles.FirstOrDefaultAsync(x => x.Name == name, cancellationToken);
            if (role is null)
            {
                _db.Roles.Add(new Role
                {
                    Name = name,
                    Description = description,
                    IsSystemRole = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
                hasChanges = true;
                continue;
            }

            if (string.IsNullOrWhiteSpace(role.Description))
            {
                role.Description = description;
                hasChanges = true;
            }

            if (!role.IsSystemRole)
            {
                role.IsSystemRole = true;
                hasChanges = true;
            }

            if (!role.IsActive)
            {
                role.IsActive = true;
                hasChanges = true;
            }
        }

        if (hasChanges)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task EnsureRolePermissionsAsync(CancellationToken cancellationToken)
    {
        var roles = await _db.Roles.AsNoTracking().ToListAsync(cancellationToken);
        var existingPermissions = await _db.RolePermissions.ToListAsync(cancellationToken);
        var hasChanges = false;

        foreach (var role in roles)
        {
            var defaults = GetDefaultPermissions(role.RoleId, role.Name).ToDictionary(x => x.ModuleKey, StringComparer.OrdinalIgnoreCase);
            foreach (var module in Modules)
            {
                if (existingPermissions.Any(x => x.RoleId == role.RoleId && x.ModuleKey == module.Key))
                {
                    continue;
                }

                if (!defaults.TryGetValue(module.Key, out var permission))
                {
                    permission = new RolePermission
                    {
                        RoleId = role.RoleId,
                        ModuleKey = module.Key,
                        ModuleName = module.Name,
                        UpdatedAt = DateTime.UtcNow
                    };
                }

                _db.RolePermissions.Add(permission);
                hasChanges = true;
            }
        }

        if (hasChanges)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task EnsureDemoAccountsAsync(CancellationToken cancellationToken)
    {
        await EnsureUserAsync("Nexus Admin", "admin@nexus.local", "Admin@123", "Admin", false, cancellationToken);
        await EnsureUserAsync("Nexus User", "user@nexus.local", "User@123", "User", true, cancellationToken);
        await EnsureUserAsync("Mira Adhikari", "mira.patient@nexus.local", "User@123", "User", true, cancellationToken);
        await EnsureUserAsync("Mira Adhikari", "mira.duplicate@nexus.local", "User@123", "User", true, cancellationToken);
        await EnsureUserAsync("Sudeep Khadka", "sudeep.patient@nexus.local", "User@123", "User", true, cancellationToken);
        await EnsureUserAsync("Anisha Shrestha", "anisha.patient@nexus.local", "User@123", "User", true, cancellationToken);
        await EnsureUserAsync("Rohan Gautam", "rohan.patient@nexus.local", "User@123", "User", true, cancellationToken);
    }

    private async Task EnsureHospitalProfileAsync(CancellationToken cancellationToken)
    {
        var profile = await _db.HospitalProfiles.OrderBy(x => x.HospitalProfileId).FirstOrDefaultAsync(cancellationToken);
        if (profile is not null)
        {
            return;
        }

        _db.HospitalProfiles.Add(new HospitalProfile
        {
            HospitalName = "Nexus Multi-Speciality Hospital",
            LogoUrl = "https://placehold.co/160x160?text=Nexus",
            AddressLine1 = "Putalisadak, Ward 28",
            City = "Kathmandu",
            StateOrProvince = "Bagmati",
            PostalCode = "44600",
            Country = "Nepal",
            ContactEmail = "info@nexushospital.local",
            ContactPhone = "+977-01-5550000",
            TaxLabel = "VAT",
            TaxRegistrationNumber = "VAT-490231",
            TaxPercentage = 13m,
            CurrencyCode = "NPR",
            InvoicePrefix = "NEX",
            InvoiceStartingNumber = 5001,
            InvoiceFooterNote = "Thank you for trusting Nexus Hospital.",
            MultiBranchEnabled = true,
            UpdatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureBranchesAsync(CancellationToken cancellationToken)
    {
        if (await _db.Branches.AnyAsync(cancellationToken))
        {
            return;
        }

        _db.Branches.AddRange(
            new Branch
            {
                Name = "Kathmandu Central",
                Code = "KTM",
                Address = "Putalisadak, Kathmandu",
                ContactPhone = "+977-01-5550000",
                ContactEmail = "ktm@nexushospital.local",
                IsPrimary = true,
                IsActive = true,
                TotalBeds = 120,
                OccupiedBeds = 94,
                CreatedAt = DateTime.UtcNow
            },
            new Branch
            {
                Name = "Lalitpur Care Center",
                Code = "LTP",
                Address = "Jawalakhel, Lalitpur",
                ContactPhone = "+977-01-5551010",
                ContactEmail = "lalitpur@nexushospital.local",
                IsPrimary = false,
                IsActive = true,
                TotalBeds = 60,
                OccupiedBeds = 41,
                CreatedAt = DateTime.UtcNow
            },
            new Branch
            {
                Name = "Bhaktapur Diagnostic Hub",
                Code = "BKT",
                Address = "Suryabinayak, Bhaktapur",
                ContactPhone = "+977-01-5552020",
                ContactEmail = "bhaktapur@nexushospital.local",
                IsPrimary = false,
                IsActive = true,
                TotalBeds = 40,
                OccupiedBeds = 19,
                CreatedAt = DateTime.UtcNow
            });

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureDepartmentsAsync(CancellationToken cancellationToken)
    {
        var branches = await _db.Branches.OrderBy(x => x.BranchId).ToListAsync(cancellationToken);
        if (branches.Count == 0)
        {
            return;
        }

        foreach (var branch in branches)
        {
            await EnsureDepartmentAsync(branch.BranchId, "Outpatient Department", "OPD", "General outpatient consultation unit", cancellationToken);
            await EnsureDepartmentAsync(branch.BranchId, "Inpatient Department", "IPD", "Inpatient admissions and care", cancellationToken);
            await EnsureDepartmentAsync(branch.BranchId, "Pharmacy", "PHARM", "Medicine dispensing and pharmacy operations", cancellationToken);
            await EnsureDepartmentAsync(branch.BranchId, "Laboratory", "LAB", "Diagnostic laboratory and sample handling", cancellationToken);
            await EnsureDepartmentAsync(branch.BranchId, "Operation Theatre", "OT", "Surgical theatre services", cancellationToken);
            await EnsureDepartmentAsync(branch.BranchId, "Intensive Care Unit", "ICU", "Critical care and monitoring", cancellationToken);
        }
    }

    private async Task EnsurePatientCategoriesAsync(CancellationToken cancellationToken)
    {
        await EnsurePatientCategoryAsync("General", "Standard outpatient and inpatient patients", 1, cancellationToken);
        await EnsurePatientCategoryAsync("Emergency", "Emergency and urgent-care patients", 2, cancellationToken);
        await EnsurePatientCategoryAsync("Corporate", "Corporate or employer-sponsored patients", 3, cancellationToken);
        await EnsurePatientCategoryAsync("Insurance", "Patients billed via insurance partners", 4, cancellationToken);
        await EnsurePatientCategoryAsync("VIP", "VIP and priority-service patients", 5, cancellationToken);
    }

    private async Task EnsurePatientCategoryAssignmentsAsync(CancellationToken cancellationToken)
    {
        var categoryMap = await _db.PatientCategories
            .AsNoTracking()
            .ToDictionaryAsync(x => x.Name, x => x.PatientCategoryId, cancellationToken);

        var assignments = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["user@nexus.local"] = "General",
            ["mira.patient@nexus.local"] = "Emergency",
            ["sudeep.patient@nexus.local"] = "Corporate",
            ["anisha.patient@nexus.local"] = "Insurance",
            ["rohan.patient@nexus.local"] = "VIP"
        };

        var users = await _db.Users
            .Where(x => assignments.Keys.Contains(x.Email))
            .ToDictionaryAsync(x => x.Email, cancellationToken);

        var patients = await _db.Patients.ToListAsync(cancellationToken);
        var hasChanges = false;

        foreach (var (email, categoryName) in assignments)
        {
            if (!users.TryGetValue(email, out var user) || !categoryMap.TryGetValue(categoryName, out var categoryId))
            {
                continue;
            }

            var patient = patients.FirstOrDefault(x => x.UserId == user.UserId);
            if (patient is null || patient.PatientCategoryId == categoryId)
            {
                continue;
            }

            patient.PatientCategoryId = categoryId;
            patient.UpdatedAt = DateTime.UtcNow;
            hasChanges = true;
        }

        if (hasChanges)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task EnsurePatientProfilesAsync(CancellationToken cancellationToken)
    {
        await EnsurePatientProfileAsync(
            "user@nexus.local",
            "+977-9801000001",
            "Female",
            new DateTime(1992, 8, 14),
            "Baneshwor, Kathmandu",
            "A+",
            "+977-9812000001",
            "Portal patient with recurring wellness visits.",
            cancellationToken);

        await EnsurePatientProfileAsync(
            "mira.patient@nexus.local",
            "+977-9801000002",
            "Female",
            new DateTime(1988, 5, 9),
            "Lazimpat, Kathmandu",
            "B+",
            "+977-9812000002",
            "Emergency intake case with cardiac follow-up.",
            cancellationToken);

        await EnsurePatientProfileAsync(
            "mira.duplicate@nexus.local",
            "+977-9801000099",
            "Female",
            new DateTime(1988, 5, 9),
            "Lazimpat, Kathmandu",
            "B+",
            "+977-9812000002",
            "Intentional duplicate registration seeded for merge testing.",
            cancellationToken);

        await EnsurePatientProfileAsync(
            "sudeep.patient@nexus.local",
            "+977-9801000003",
            "Male",
            new DateTime(1985, 11, 2),
            "Bhaisepati, Lalitpur",
            "O+",
            "+977-9812000003",
            "Corporate patient with neurology consult and inpatient monitoring.",
            cancellationToken);

        await EnsurePatientProfileAsync(
            "anisha.patient@nexus.local",
            "+977-9801000004",
            "Female",
            new DateTime(1996, 2, 28),
            "Sallaghari, Bhaktapur",
            "AB-",
            "+977-9812000004",
            "Insurance-backed therapy follow-up and billing review.",
            cancellationToken);

        await EnsurePatientProfileAsync(
            "rohan.patient@nexus.local",
            "+977-9801000005",
            "Male",
            new DateTime(1990, 7, 19),
            "Tokha, Kathmandu",
            "O-",
            "+977-9812000005",
            "VIP patient with scheduled admission oversight.",
            cancellationToken);
    }

    private async Task EnsurePatientProfileAsync(
        string email,
        string phone,
        string gender,
        DateTime dateOfBirth,
        string address,
        string bloodGroup,
        string emergencyContact,
        string notes,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);
        if (user is null)
        {
            return;
        }

        var patient = await _db.Patients.FirstOrDefaultAsync(x => x.UserId == user.UserId, cancellationToken);
        if (patient is null)
        {
            return;
        }

        user.Phone = phone;
        user.IsActive = true;
        user.UpdatedAt = DateTime.UtcNow;

        patient.MedicalRecordNumber ??= $"MRN-{patient.PatientId:D5}";
        patient.Gender = gender;
        patient.DateOfBirth = dateOfBirth.Date;
        patient.Address = address;
        patient.BloodGroup = bloodGroup;
        patient.EmergencyContact = emergencyContact;
        patient.Notes = notes;
        patient.IsActive = true;
        patient.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureDoctorSchedulesAsync(CancellationToken cancellationToken)
    {
        var doctors = await _db.Doctors.Where(x => x.IsActive).ToListAsync(cancellationToken);
        foreach (var doctor in doctors)
        {
            if (string.IsNullOrWhiteSpace(doctor.OpdDays) || !doctor.OpdStartTime.HasValue || !doctor.OpdEndTime.HasValue)
            {
                continue;
            }

            var tokens = doctor.OpdDays
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(x => x.ToLowerInvariant())
                .Distinct()
                .ToList();

            foreach (var token in tokens)
            {
                var dayOfWeek = token switch
                {
                    "sun" => (byte)1,
                    "mon" => (byte)2,
                    "tue" => (byte)3,
                    "wed" => (byte)4,
                    "thu" => (byte)5,
                    "fri" => (byte)6,
                    _ => (byte)7
                };

                var schedule = await _db.DoctorSchedules.FirstOrDefaultAsync(
                    x => x.DoctorId == doctor.DoctorId && x.DayOfWeek == dayOfWeek,
                    cancellationToken);

                if (schedule is null)
                {
                    _db.DoctorSchedules.Add(new DoctorSchedule
                    {
                        DoctorId = doctor.DoctorId,
                        DayOfWeek = dayOfWeek,
                        StartTime = doctor.OpdStartTime.Value,
                        EndTime = doctor.OpdEndTime.Value,
                        SlotDurationMinutes = 30,
                        MaxPatientsPerSlot = 1,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    });
                }
                else
                {
                    schedule.StartTime = doctor.OpdStartTime.Value;
                    schedule.EndTime = doctor.OpdEndTime.Value;
                    schedule.SlotDurationMinutes = 30;
                    schedule.MaxPatientsPerSlot = 1;
                    schedule.IsActive = true;
                    schedule.UpdatedAt = DateTime.UtcNow;
                }
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureTokenSettingsAsync(CancellationToken cancellationToken)
    {
        var settings = await _db.AppointmentTokenSettings
            .OrderBy(x => x.AppointmentTokenSettingId)
            .FirstOrDefaultAsync(cancellationToken);

        if (settings is null)
        {
            _db.AppointmentTokenSettings.Add(new AppointmentTokenSetting
            {
                Prefix = "OPD",
                StartingNumber = 1,
                NumberPadding = 3,
                ResetDaily = true,
                UpdatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync(cancellationToken);
            return;
        }

        if (string.IsNullOrWhiteSpace(settings.Prefix))
        {
            settings.Prefix = "OPD";
        }

        settings.StartingNumber = Math.Max(settings.StartingNumber, 1);
        settings.NumberPadding = Math.Clamp(settings.NumberPadding, 3, 6);
        settings.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureDoctorsAsync(CancellationToken cancellationToken)
    {
        var branches = await _db.Branches.OrderBy(x => x.BranchId).ToListAsync(cancellationToken);
        if (branches.Count == 0)
        {
            return;
        }

        var branchMap = branches.ToDictionary(x => x.Code, x => x.BranchId, StringComparer.OrdinalIgnoreCase);
        var departments = await _db.Departments.ToListAsync(cancellationToken);
        var departmentMap = departments.ToDictionary(
            x => $"{x.BranchId}:{x.Code}",
            x => x.DepartmentId,
            StringComparer.OrdinalIgnoreCase);

        await EnsureDoctorAsync(
            "Dr. Aryan Shah",
            "Cardiology",
            "aryan.shah@nexushospital.local",
            "+977-9800000001",
            12,
            "MD, DM Cardiology",
            2500m,
            branchMap["KTM"],
            departmentMap[$"{branchMap["KTM"]}:OPD"],
            "sun,mon,tue,wed,thu",
            new TimeSpan(9, 0, 0),
            new TimeSpan(13, 0, 0),
            cancellationToken);

        await EnsureDoctorAsync(
            "Dr. Nisha Gurung",
            "Neurology",
            "nisha.gurung@nexushospital.local",
            "+977-9800000002",
            9,
            "MD Neurology",
            2800m,
            branchMap["LTP"],
            departmentMap[$"{branchMap["LTP"]}:OPD"],
            "sun,mon,tue,wed,fri",
            new TimeSpan(10, 0, 0),
            new TimeSpan(15, 0, 0),
            cancellationToken);

        await EnsureDoctorAsync(
            "Dr. Samir Rana",
            "General Medicine",
            "samir.rana@nexushospital.local",
            "+977-9800000003",
            7,
            "MD Internal Medicine",
            1800m,
            branchMap["BKT"],
            departmentMap[$"{branchMap["BKT"]}:OPD"],
            "sun,mon,wed,thu,fri",
            new TimeSpan(8, 30, 0),
            new TimeSpan(12, 30, 0),
            cancellationToken);
    }

    private async Task EnsureStaffAsync(CancellationToken cancellationToken)
    {
        var branches = await _db.Branches.OrderBy(x => x.BranchId).ToListAsync(cancellationToken);
        var departments = await _db.Departments.ToListAsync(cancellationToken);
        if (branches.Count == 0 || departments.Count == 0)
        {
            return;
        }

        var branchMap = branches.ToDictionary(x => x.Code, x => x.BranchId, StringComparer.OrdinalIgnoreCase);
        var departmentMap = departments.ToDictionary(
            x => $"{x.BranchId}:{x.Code}",
            x => x.DepartmentId,
            StringComparer.OrdinalIgnoreCase);

        await EnsureStaffMemberAsync(
            "Sanjana Karki",
            "STF-1001",
            "Nurse Supervisor",
            "Day",
            "sanjana.karki@nexushospital.local",
            "+977-9811111101",
            branchMap["KTM"],
            departmentMap[$"{branchMap["KTM"]}:ICU"],
            new DateTime(2024, 4, 1),
            cancellationToken);

        await EnsureStaffMemberAsync(
            "Prabesh KC",
            "STF-1002",
            "Lab Technician",
            "Morning",
            "prabesh.kc@nexushospital.local",
            "+977-9811111102",
            branchMap["LTP"],
            departmentMap[$"{branchMap["LTP"]}:LAB"],
            new DateTime(2024, 8, 15),
            cancellationToken);

        await EnsureStaffMemberAsync(
            "Nirmala Tamang",
            "STF-1003",
            "Pharmacy Officer",
            "Evening",
            "nirmala.tamang@nexushospital.local",
            "+977-9811111103",
            branchMap["BKT"],
            departmentMap[$"{branchMap["BKT"]}:PHARM"],
            new DateTime(2025, 1, 5),
            cancellationToken);
    }

    private async Task EnsureWardsAndBedsAsync(CancellationToken cancellationToken)
    {
        var branches = await _db.Branches.OrderBy(x => x.BranchId).ToListAsync(cancellationToken);
        var departments = await _db.Departments.ToListAsync(cancellationToken);
        if (branches.Count == 0 || departments.Count == 0)
        {
            return;
        }

        var branchMap = branches.ToDictionary(x => x.Code, x => x.BranchId, StringComparer.OrdinalIgnoreCase);
        var departmentMap = departments.ToDictionary(
            x => $"{x.BranchId}:{x.Code}",
            x => x.DepartmentId,
            StringComparer.OrdinalIgnoreCase);

        var ktmIcuWard = await EnsureWardAsync(
            branchMap["KTM"],
            departmentMap[$"{branchMap["KTM"]}:ICU"],
            "Critical Care Ward",
            "ICU",
            "Private",
            8500m,
            cancellationToken);

        var ltpOtWard = await EnsureWardAsync(
            branchMap["LTP"],
            departmentMap[$"{branchMap["LTP"]}:OT"],
            "Surgical Recovery Ward",
            "OT",
            "Semi-Private",
            6200m,
            cancellationToken);

        var bktIpdWard = await EnsureWardAsync(
            branchMap["BKT"],
            departmentMap[$"{branchMap["BKT"]}:IPD"],
            "General Admission Ward",
            "IPD",
            "General",
            3200m,
            cancellationToken);

        await EnsureBedAsync(ktmIcuWard.WardId, branchMap["KTM"], departmentMap[$"{branchMap["KTM"]}:ICU"], "ICU-01", 9000m, true, cancellationToken);
        await EnsureBedAsync(ktmIcuWard.WardId, branchMap["KTM"], departmentMap[$"{branchMap["KTM"]}:ICU"], "ICU-02", 9000m, false, cancellationToken);
        await EnsureBedAsync(ltpOtWard.WardId, branchMap["LTP"], departmentMap[$"{branchMap["LTP"]}:OT"], "REC-01", 6500m, false, cancellationToken);
        await EnsureBedAsync(bktIpdWard.WardId, branchMap["BKT"], departmentMap[$"{branchMap["BKT"]}:IPD"], "GEN-12", 3200m, true, cancellationToken);
        await RefreshBranchOccupancyAsync(cancellationToken);
    }

    private async Task EnsureAppointmentsAsync(CancellationToken cancellationToken)
    {
        var settings = await _db.AppointmentTokenSettings
            .OrderBy(x => x.AppointmentTokenSettingId)
            .FirstAsync(cancellationToken);

        if (await _db.Appointments.AnyAsync(cancellationToken))
        {
            await EnsureAppointmentTokensAsync(settings, cancellationToken);
            return;
        }

        var patients = await _db.Patients
            .Where(x => x.IsActive && !x.MergedIntoPatientId.HasValue)
            .OrderBy(x => x.PatientId)
            .ToListAsync(cancellationToken);
        var patientUsers = await _db.Users
            .Where(x => patients.Select(p => p.UserId).Contains(x.UserId))
            .ToDictionaryAsync(x => x.UserId, cancellationToken);
        var doctors = await _db.Doctors.OrderBy(x => x.DoctorId).ToListAsync(cancellationToken);
        var services = await _db.Services.OrderBy(x => x.Id).ToListAsync(cancellationToken);
        var schedules = await _db.DoctorSchedules.Where(x => x.IsActive).ToListAsync(cancellationToken);

        if (patients.Count < 5 || doctors.Count < 3 || services.Count < 3)
        {
            return;
        }

        var today = DateTime.Today;
        var appointments = new[]
        {
            new Appointment
            {
                PatientId = patients[0].PatientId,
                DoctorId = doctors[0].DoctorId,
                ScheduleId = ResolveScheduleId(schedules, doctors[0].DoctorId, today),
                ServiceId = services[0].Id,
                AppointmentDate = today,
                SlotStartTime = new TimeSpan(9, 0, 0),
                SlotEndTime = new TimeSpan(9, 45, 0),
                Status = AppointmentStatus.Pending,
                Reason = "Routine cardiac follow-up",
                CreatedByUserId = patientUsers[patients[0].UserId].UserId,
                CreatedAt = DateTime.UtcNow
            },
            new Appointment
            {
                PatientId = patients[1].PatientId,
                DoctorId = doctors[1].DoctorId,
                ScheduleId = ResolveScheduleId(schedules, doctors[1].DoctorId, today),
                ServiceId = services[2].Id,
                AppointmentDate = today,
                SlotStartTime = new TimeSpan(11, 0, 0),
                SlotEndTime = new TimeSpan(12, 0, 0),
                Status = AppointmentStatus.Approved,
                TokenNumber = FormatToken(settings.Prefix, today, settings.StartingNumber, settings.NumberPadding),
                Reason = "Migraine review",
                CreatedByUserId = patientUsers[patients[1].UserId].UserId,
                CreatedAt = DateTime.UtcNow
            },
            new Appointment
            {
                PatientId = patients[2].PatientId,
                DoctorId = doctors[2].DoctorId,
                ScheduleId = ResolveScheduleId(schedules, doctors[2].DoctorId, today.AddDays(1)),
                ServiceId = services[1].Id,
                AppointmentDate = today.AddDays(1),
                SlotStartTime = new TimeSpan(14, 0, 0),
                SlotEndTime = new TimeSpan(15, 0, 0),
                Status = AppointmentStatus.Rescheduled,
                TokenNumber = FormatToken(settings.Prefix, today.AddDays(1), settings.StartingNumber, settings.NumberPadding),
                Reason = "Post therapy evaluation",
                AdminRemarks = "Rescheduled from morning slot due to doctor availability.",
                CreatedByUserId = patientUsers[patients[2].UserId].UserId,
                CreatedAt = DateTime.UtcNow
            },
            new Appointment
            {
                PatientId = patients[3].PatientId,
                DoctorId = doctors[0].DoctorId,
                ScheduleId = ResolveScheduleId(schedules, doctors[0].DoctorId, today.AddDays(-1)),
                ServiceId = services[3].Id,
                AppointmentDate = today.AddDays(-1),
                SlotStartTime = new TimeSpan(10, 0, 0),
                SlotEndTime = new TimeSpan(11, 0, 0),
                Status = AppointmentStatus.Completed,
                TokenNumber = FormatToken(settings.Prefix, today.AddDays(-1), settings.StartingNumber, settings.NumberPadding),
                Reason = "Follow-up consultation completed successfully",
                AdminRemarks = "Discharge planning completed after review.",
                CreatedByUserId = patientUsers[patients[3].UserId].UserId,
                CreatedAt = DateTime.UtcNow
            },
            new Appointment
            {
                PatientId = patients[4].PatientId,
                DoctorId = doctors[2].DoctorId,
                ScheduleId = ResolveScheduleId(schedules, doctors[2].DoctorId, today.AddDays(2)),
                ServiceId = services[3].Id,
                AppointmentDate = today.AddDays(2),
                SlotStartTime = new TimeSpan(16, 0, 0),
                SlotEndTime = new TimeSpan(17, 0, 0),
                Status = AppointmentStatus.Cancelled,
                Reason = "Cancelled by patient",
                AdminRemarks = "Cancelled after duplicate booking detected.",
                CreatedByUserId = patientUsers[patients[4].UserId].UserId,
                CreatedAt = DateTime.UtcNow
            }
        };

        _db.Appointments.AddRange(appointments);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureAdmissionsAsync(CancellationToken cancellationToken)
    {
        if (await _db.PatientAdmissions.AnyAsync(cancellationToken))
        {
            await RefreshBranchOccupancyAsync(cancellationToken);
            return;
        }

        var patients = await _db.Patients
            .Where(x => x.IsActive && !x.MergedIntoPatientId.HasValue)
            .OrderBy(x => x.PatientId)
            .ToListAsync(cancellationToken);
        var doctors = await _db.Doctors.OrderBy(x => x.DoctorId).ToListAsync(cancellationToken);
        var appointments = await _db.Appointments.OrderBy(x => x.AppointmentId).ToListAsync(cancellationToken);
        var beds = await _db.Beds
            .Include(x => x.Ward)
            .OrderBy(x => x.BedId)
            .ToListAsync(cancellationToken);

        var icuBed = beds.FirstOrDefault(x => x.BedNumber == "ICU-01");
        var generalBed = beds.FirstOrDefault(x => x.BedNumber == "GEN-12");
        var recoveryBed = beds.FirstOrDefault(x => x.BedNumber == "REC-01");

        if (patients.Count < 4 || doctors.Count < 3 || icuBed?.Ward is null || generalBed?.Ward is null || recoveryBed?.Ward is null)
        {
            return;
        }

        _db.PatientAdmissions.AddRange(
            new PatientAdmission
            {
                AdmissionNumber = $"ADM-{DateTime.Today:yyyyMMdd}-001",
                PatientId = patients[1].PatientId,
                AppointmentId = appointments.ElementAtOrDefault(1)?.AppointmentId,
                DoctorId = doctors[1].DoctorId,
                BranchId = icuBed.BranchId,
                WardId = icuBed.WardId,
                BedId = icuBed.BedId,
                Status = AdmissionStatus.Active,
                AdmissionDate = DateTime.Today.AddDays(-1).AddHours(8),
                ExpectedDischargeDate = DateTime.Today.AddDays(2),
                Reason = "Observation after acute neurology episode",
                Notes = "High priority observation and vitals watch.",
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            },
            new PatientAdmission
            {
                AdmissionNumber = $"ADM-{DateTime.Today:yyyyMMdd}-002",
                PatientId = patients[2].PatientId,
                AppointmentId = appointments.ElementAtOrDefault(2)?.AppointmentId,
                DoctorId = doctors[2].DoctorId,
                BranchId = generalBed.BranchId,
                WardId = generalBed.WardId,
                BedId = generalBed.BedId,
                Status = AdmissionStatus.Active,
                AdmissionDate = DateTime.Today.AddHours(7),
                ExpectedDischargeDate = DateTime.Today.AddDays(3),
                Reason = "Post-therapy inpatient monitoring",
                Notes = "Transferred from recovery bed after initial stabilization.",
                CreatedAt = DateTime.UtcNow
            },
            new PatientAdmission
            {
                AdmissionNumber = $"ADM-{DateTime.Today.AddDays(-3):yyyyMMdd}-001",
                PatientId = patients[3].PatientId,
                AppointmentId = appointments.ElementAtOrDefault(3)?.AppointmentId,
                DoctorId = doctors[0].DoctorId,
                BranchId = recoveryBed.BranchId,
                WardId = recoveryBed.WardId,
                BedId = recoveryBed.BedId,
                Status = AdmissionStatus.Discharged,
                AdmissionDate = DateTime.Today.AddDays(-3).AddHours(9),
                ExpectedDischargeDate = DateTime.Today.AddDays(-1),
                DischargeDate = DateTime.Today.AddDays(-1).AddHours(11),
                Reason = "Short stay surgical observation",
                Notes = "Recovered well and discharged home.",
                DischargeSummary = "Stable vitals, pain managed, continue medication for 5 days.",
                DischargeApprovedAt = DateTime.UtcNow.AddDays(-1),
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            });

        await _db.SaveChangesAsync(cancellationToken);

        var secondAdmission = await _db.PatientAdmissions
            .OrderBy(x => x.PatientAdmissionId)
            .Skip(1)
            .FirstOrDefaultAsync(cancellationToken);

        if (secondAdmission is not null)
        {
            _db.AdmissionTransfers.Add(new AdmissionTransfer
            {
                PatientAdmissionId = secondAdmission.PatientAdmissionId,
                FromWardId = recoveryBed.WardId,
                FromBedId = recoveryBed.BedId,
                ToWardId = generalBed.WardId,
                ToBedId = generalBed.BedId,
                TransferDate = DateTime.Today.AddHours(9),
                Notes = "Transferred after recovery observation window.",
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(cancellationToken);
        }

        recoveryBed.IsOccupied = false;
        generalBed.IsOccupied = true;
        icuBed.IsOccupied = true;
        await _db.SaveChangesAsync(cancellationToken);
        await RefreshBranchOccupancyAsync(cancellationToken);
    }

    private async Task EnsureInventoryAsync(CancellationToken cancellationToken)
    {
        if (await _db.MedicineInventoryItems.AnyAsync(cancellationToken))
        {
            return;
        }

        var branches = await _db.Branches.OrderBy(x => x.BranchId).ToListAsync(cancellationToken);
        if (branches.Count == 0)
        {
            return;
        }

        var primaryBranchId = branches[0].BranchId;
        var secondaryBranchId = branches.ElementAtOrDefault(1)?.BranchId ?? primaryBranchId;
        var tertiaryBranchId = branches.ElementAtOrDefault(2)?.BranchId ?? secondaryBranchId;

        var today = DateTime.Today;
        _db.MedicineInventoryItems.AddRange(
            new MedicineInventoryItem
            {
                BranchId = primaryBranchId,
                Name = "Amoxicillin 500mg",
                Category = "Antibiotic",
                BatchNumber = "AMX-2401",
                QuantityInStock = 14,
                ReorderLevel = 25,
                ExpiryDate = today.AddMonths(5),
                UnitPrice = 24m,
                CreatedAt = DateTime.UtcNow
            },
            new MedicineInventoryItem
            {
                BranchId = primaryBranchId,
                Name = "Insulin Pen",
                Category = "Diabetes Care",
                BatchNumber = "INS-2402",
                QuantityInStock = 8,
                ReorderLevel = 15,
                ExpiryDate = today.AddMonths(2),
                UnitPrice = 650m,
                CreatedAt = DateTime.UtcNow
            },
            new MedicineInventoryItem
            {
                BranchId = secondaryBranchId,
                Name = "Vitamin D Syrup",
                Category = "Supplements",
                BatchNumber = "VDS-2311",
                QuantityInStock = 22,
                ReorderLevel = 20,
                ExpiryDate = today.AddDays(-12),
                UnitPrice = 180m,
                CreatedAt = DateTime.UtcNow
            },
            new MedicineInventoryItem
            {
                BranchId = tertiaryBranchId,
                Name = "Cefixime 200mg",
                Category = "Antibiotic",
                BatchNumber = "CFX-2308",
                QuantityInStock = 6,
                ReorderLevel = 18,
                ExpiryDate = today.AddDays(-2),
                UnitPrice = 32m,
                CreatedAt = DateTime.UtcNow
            },
            new MedicineInventoryItem
            {
                BranchId = secondaryBranchId,
                Name = "Paracetamol 500mg",
                Category = "Analgesic",
                BatchNumber = "PCM-2405",
                QuantityInStock = 56,
                ReorderLevel = 30,
                ExpiryDate = today.AddMonths(10),
                UnitPrice = 6m,
                CreatedAt = DateTime.UtcNow
            });

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsurePhase5InventoryInfrastructureAsync(CancellationToken cancellationToken)
    {
        var scriptPath = Path.Combine(AppContext.BaseDirectory, "Data", "Sql", "Phase5InventoryInfrastructure.sql");
        if (!File.Exists(scriptPath))
        {
            _logger.LogWarning("Phase 5 inventory infrastructure script not found at {ScriptPath}", scriptPath);
            return;
        }

        var script = await File.ReadAllTextAsync(scriptPath, cancellationToken);
        foreach (var batch in SplitSqlBatches(script))
        {
            if (string.IsNullOrWhiteSpace(batch))
            {
                continue;
            }

            await _db.Database.ExecuteSqlRawAsync(batch, cancellationToken);
        }
    }

    private async Task EnsurePhase5InventorySeedAsync(CancellationToken cancellationToken)
    {
        var branches = await _db.Branches
            .AsNoTracking()
            .OrderBy(x => x.BranchId)
            .ToListAsync(cancellationToken);

        if (branches.Count == 0)
        {
            return;
        }

        var primaryBranchId = branches[0].BranchId;
        var secondaryBranchId = branches.ElementAtOrDefault(1)?.BranchId ?? primaryBranchId;
        var tertiaryBranchId = branches.ElementAtOrDefault(2)?.BranchId ?? secondaryBranchId;

        if (!await _db.InventoryUnits.AnyAsync(cancellationToken))
        {
            _db.InventoryUnits.AddRange(
                new InventoryUnit { Name = "Box", ShortName = "box", Description = "Box packaging unit", IsActive = true, CreatedAt = DateTime.UtcNow },
                new InventoryUnit { Name = "Piece", ShortName = "pc", Description = "Individual countable piece", IsActive = true, CreatedAt = DateTime.UtcNow },
                new InventoryUnit { Name = "Vial", ShortName = "vial", Description = "Small medicine vial", IsActive = true, CreatedAt = DateTime.UtcNow },
                new InventoryUnit { Name = "Strip", ShortName = "strip", Description = "Tablet strip unit", IsActive = true, CreatedAt = DateTime.UtcNow },
                new InventoryUnit { Name = "Bottle", ShortName = "bottle", Description = "Bottle unit", IsActive = true, CreatedAt = DateTime.UtcNow });
            await _db.SaveChangesAsync(cancellationToken);
        }

        if (!await _db.InventoryCategories.AnyAsync(cancellationToken))
        {
            _db.InventoryCategories.AddRange(
                new InventoryCategory { CategoryType = InventoryCategoryType.Medicine, Name = "Antibiotic", Description = "Antibiotic medicines", IsActive = true, CreatedAt = DateTime.UtcNow },
                new InventoryCategory { CategoryType = InventoryCategoryType.Medicine, Name = "Analgesic", Description = "Pain relief medicines", IsActive = true, CreatedAt = DateTime.UtcNow },
                new InventoryCategory { CategoryType = InventoryCategoryType.Medicine, Name = "Diabetes Care", Description = "Diabetes management medicines", IsActive = true, CreatedAt = DateTime.UtcNow },
                new InventoryCategory { CategoryType = InventoryCategoryType.Item, Name = "Consumables", Description = "Routine consumable store items", IsActive = true, CreatedAt = DateTime.UtcNow },
                new InventoryCategory { CategoryType = InventoryCategoryType.Item, Name = "Surgical Supplies", Description = "Surgery support items", IsActive = true, CreatedAt = DateTime.UtcNow },
                new InventoryCategory { CategoryType = InventoryCategoryType.Item, Name = "Lab Reagents", Description = "Laboratory reagent items", IsActive = true, CreatedAt = DateTime.UtcNow },
                new InventoryCategory { CategoryType = InventoryCategoryType.Item, Name = "Non-medical Supplies", Description = "General support items", IsActive = true, CreatedAt = DateTime.UtcNow },
                new InventoryCategory { CategoryType = InventoryCategoryType.Item, Name = "Spare Parts", Description = "Equipment spare inventory", IsActive = true, CreatedAt = DateTime.UtcNow });
            await _db.SaveChangesAsync(cancellationToken);
        }

        var unitMap = await _db.InventoryUnits
            .AsNoTracking()
            .ToDictionaryAsync(x => x.Name, x => x.InventoryUnitId, cancellationToken);

        var categoryMap = await _db.InventoryCategories
            .AsNoTracking()
            .ToDictionaryAsync(x => $"{x.CategoryType}:{x.Name}", x => x.InventoryCategoryId, cancellationToken);

        if (!await _db.MedicineMasters.AnyAsync(cancellationToken))
        {
            _db.MedicineMasters.AddRange(
                new MedicineMaster
                {
                    MedicineName = "Amoxicillin 500mg",
                    GenericName = "Amoxicillin",
                    Brand = "Moxilin",
                    InventoryUnitId = unitMap["Strip"],
                    InventoryCategoryId = categoryMap[$"{InventoryCategoryType.Medicine}:Antibiotic"],
                    Strength = "500mg",
                    BatchRequired = true,
                    MinimumStock = 30,
                    MaximumStock = 200,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new MedicineMaster
                {
                    MedicineName = "Paracetamol 500mg",
                    GenericName = "Paracetamol",
                    Brand = "Pacimol",
                    InventoryUnitId = unitMap["Strip"],
                    InventoryCategoryId = categoryMap[$"{InventoryCategoryType.Medicine}:Analgesic"],
                    Strength = "500mg",
                    BatchRequired = true,
                    MinimumStock = 50,
                    MaximumStock = 500,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new MedicineMaster
                {
                    MedicineName = "Insulin Pen",
                    GenericName = "Insulin",
                    Brand = "Humapen",
                    InventoryUnitId = unitMap["Piece"],
                    InventoryCategoryId = categoryMap[$"{InventoryCategoryType.Medicine}:Diabetes Care"],
                    Strength = "100 IU",
                    BatchRequired = true,
                    MinimumStock = 10,
                    MaximumStock = 80,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            await _db.SaveChangesAsync(cancellationToken);
        }

        if (!await _db.StockItemMasters.AnyAsync(cancellationToken))
        {
            _db.StockItemMasters.AddRange(
                new StockItemMaster
                {
                    ItemType = InventoryItemType.Consumable,
                    ItemName = "Surgical Gloves",
                    Specification = "Latex powder-free",
                    InventoryUnitId = unitMap["Box"],
                    InventoryCategoryId = categoryMap[$"{InventoryCategoryType.Item}:Consumables"],
                    MinimumStock = 20,
                    MaximumStock = 150,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new StockItemMaster
                {
                    ItemType = InventoryItemType.LabReagent,
                    ItemName = "CBC Reagent Kit",
                    Specification = "Automated hematology analyzer reagent",
                    InventoryUnitId = unitMap["Bottle"],
                    InventoryCategoryId = categoryMap[$"{InventoryCategoryType.Item}:Lab Reagents"],
                    MinimumStock = 8,
                    MaximumStock = 40,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new StockItemMaster
                {
                    ItemType = InventoryItemType.EquipmentSpare,
                    ItemName = "ECG Thermal Paper Roll",
                    Specification = "Standard ECG machine compatible",
                    InventoryUnitId = unitMap["Piece"],
                    InventoryCategoryId = categoryMap[$"{InventoryCategoryType.Item}:Spare Parts"],
                    MinimumStock = 6,
                    MaximumStock = 50,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            await _db.SaveChangesAsync(cancellationToken);
        }

        if (!await _db.Suppliers.AnyAsync(cancellationToken))
        {
            _db.Suppliers.AddRange(
                new Supplier
                {
                    SupplierName = "Himalayan Meditech",
                    SupplierCode = "SUP-HIM-01",
                    ContactPerson = "Sanjay Karki",
                    ContactPhone = "+977-9801001001",
                    ContactEmail = "orders@himalayanmeditech.local",
                    Address = "Teku, Kathmandu",
                    PaymentTermsDays = 30,
                    Notes = "Primary medicine supplier for central branch.",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Supplier
                {
                    SupplierName = "BioLab Supply Nepal",
                    SupplierCode = "SUP-BIO-02",
                    ContactPerson = "Richa Tiwari",
                    ContactPhone = "+977-9801002002",
                    ContactEmail = "procurement@biolabsupply.local",
                    Address = "Kupondole, Lalitpur",
                    PaymentTermsDays = 21,
                    Notes = "Lab reagent and analyzer consumables partner.",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            await _db.SaveChangesAsync(cancellationToken);
        }

        if (!await _db.StockLocations.AnyAsync(cancellationToken))
        {
            _db.StockLocations.AddRange(
                new StockLocation { BranchId = primaryBranchId, Name = "Main Store", Code = "MAIN-STORE", LocationType = StockLocationType.MainStore, Description = "Primary central stock room", IsActive = true, CreatedAt = DateTime.UtcNow },
                new StockLocation { BranchId = primaryBranchId, Name = "Pharmacy Store", Code = "PHARM-STORE", LocationType = StockLocationType.PharmacyStore, Description = "Outpatient pharmacy dispensing store", IsActive = true, CreatedAt = DateTime.UtcNow },
                new StockLocation { BranchId = secondaryBranchId, Name = "Lab Store", Code = "LAB-STORE", LocationType = StockLocationType.LabStore, Description = "Laboratory reagent store", IsActive = true, CreatedAt = DateTime.UtcNow },
                new StockLocation { BranchId = primaryBranchId, Name = "OT Store", Code = "OT-STORE", LocationType = StockLocationType.OtStore, Description = "Operation theatre item store", IsActive = true, CreatedAt = DateTime.UtcNow },
                new StockLocation { BranchId = tertiaryBranchId, Name = "Ward Stock", Code = "WARD-STOCK", LocationType = StockLocationType.WardStock, Description = "Ward floor backup stock", IsActive = true, CreatedAt = DateTime.UtcNow });
            await _db.SaveChangesAsync(cancellationToken);
        }

        var medicineMap = await _db.MedicineMasters
            .AsNoTracking()
            .ToDictionaryAsync(x => x.MedicineName, x => x.MedicineMasterId, cancellationToken);
        var itemMap = await _db.StockItemMasters
            .AsNoTracking()
            .ToDictionaryAsync(x => x.ItemName, x => x.StockItemMasterId, cancellationToken);
        var locationMap = await _db.StockLocations
            .AsNoTracking()
            .ToDictionaryAsync(x => x.Code, x => x.StockLocationId, cancellationToken);
        var supplierMap = await _db.Suppliers
            .AsNoTracking()
            .ToDictionaryAsync(x => x.SupplierCode, x => x.SupplierId, cancellationToken);

        if (!await _db.StockBatches.AnyAsync(cancellationToken))
        {
            var today = DateTime.Today;
            _db.StockBatches.AddRange(
                new StockBatch { StockLocationId = locationMap["MAIN-STORE"], MedicineMasterId = medicineMap["Amoxicillin 500mg"], BatchNumber = "AMX-2601", ExpiryDate = today.AddMonths(5), QuantityOnHand = 18, UnitCost = 24, LastMovementAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow },
                new StockBatch { StockLocationId = locationMap["PHARM-STORE"], MedicineMasterId = medicineMap["Paracetamol 500mg"], BatchNumber = "PCM-2603", ExpiryDate = today.AddMonths(11), QuantityOnHand = 120, UnitCost = 6, LastMovementAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow },
                new StockBatch { StockLocationId = locationMap["PHARM-STORE"], MedicineMasterId = medicineMap["Insulin Pen"], BatchNumber = "INS-2512", ExpiryDate = today.AddDays(20), QuantityOnHand = 7, UnitCost = 650, LastMovementAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow },
                new StockBatch { StockLocationId = locationMap["LAB-STORE"], StockItemMasterId = itemMap["CBC Reagent Kit"], BatchNumber = "CBC-2510", ExpiryDate = today.AddDays(12), QuantityOnHand = 4, UnitCost = 1450, LastMovementAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow },
                new StockBatch { StockLocationId = locationMap["OT-STORE"], StockItemMasterId = itemMap["Surgical Gloves"], BatchNumber = "GLV-2501", ExpiryDate = today.AddMonths(8), QuantityOnHand = 16, UnitCost = 420, LastMovementAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow },
                new StockBatch { StockLocationId = locationMap["WARD-STOCK"], MedicineMasterId = medicineMap["Amoxicillin 500mg"], BatchNumber = "AMX-2410", ExpiryDate = today.AddDays(-3), QuantityOnHand = 6, UnitCost = 22, LastMovementAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow });
            await _db.SaveChangesAsync(cancellationToken);
        }

        if (!await _db.PurchaseOrders.AnyAsync(cancellationToken))
        {
            var order = new PurchaseOrder
            {
                OrderNumber = "PO-000001",
                SupplierId = supplierMap["SUP-HIM-01"],
                StockLocationId = locationMap["MAIN-STORE"],
                OrderDate = DateTime.Today.AddDays(-6),
                ExpectedDeliveryDate = DateTime.Today.AddDays(-2),
                Status = PurchaseOrderStatus.PartiallyReceived,
                Notes = "Phase 5 seed purchase order",
                CreatedAt = DateTime.UtcNow
            };
            _db.PurchaseOrders.Add(order);
            await _db.SaveChangesAsync(cancellationToken);

            _db.PurchaseOrderLines.AddRange(
                new PurchaseOrderLine { PurchaseOrderId = order.PurchaseOrderId, MedicineMasterId = medicineMap["Amoxicillin 500mg"], ItemName = "Amoxicillin 500mg", UnitName = "Strip", OrderedQuantity = 80, ReceivedQuantity = 50, UnitCost = 24, CreatedAt = DateTime.UtcNow },
                new PurchaseOrderLine { PurchaseOrderId = order.PurchaseOrderId, StockItemMasterId = itemMap["Surgical Gloves"], ItemName = "Surgical Gloves", UnitName = "Box", OrderedQuantity = 40, ReceivedQuantity = 20, UnitCost = 420, CreatedAt = DateTime.UtcNow });
            await _db.SaveChangesAsync(cancellationToken);
        }

        if (!await _db.PurchaseInvoices.AnyAsync(cancellationToken))
        {
            var order = await _db.PurchaseOrders.AsNoTracking().OrderBy(x => x.PurchaseOrderId).FirstAsync(cancellationToken);
            var invoice = new PurchaseInvoice
            {
                PurchaseOrderId = order.PurchaseOrderId,
                SupplierId = order.SupplierId,
                StockLocationId = order.StockLocationId,
                InvoiceNumber = "INV-PH5-0001",
                InvoiceDate = DateTime.Today.AddDays(-4),
                DueDate = DateTime.Today.AddDays(26),
                TotalAmount = 9600,
                PaidAmount = 4000,
                Status = PurchaseInvoiceStatus.Partial,
                Notes = "Seed supplier invoice with due amount",
                CreatedAt = DateTime.UtcNow
            };
            _db.PurchaseInvoices.Add(invoice);
            await _db.SaveChangesAsync(cancellationToken);

            var orderLines = await _db.PurchaseOrderLines.Where(x => x.PurchaseOrderId == order.PurchaseOrderId).OrderBy(x => x.PurchaseOrderLineId).ToListAsync(cancellationToken);
            _db.PurchaseInvoiceLines.AddRange(
                new PurchaseInvoiceLine { PurchaseInvoiceId = invoice.PurchaseInvoiceId, PurchaseOrderLineId = orderLines[0].PurchaseOrderLineId, MedicineMasterId = orderLines[0].MedicineMasterId, ItemName = orderLines[0].ItemName, UnitName = orderLines[0].UnitName, BatchNumber = "AMX-2602", ExpiryDate = DateTime.Today.AddMonths(9), Quantity = 50, UnitCost = 24, LineTotal = 1200, CreatedAt = DateTime.UtcNow },
                new PurchaseInvoiceLine { PurchaseInvoiceId = invoice.PurchaseInvoiceId, PurchaseOrderLineId = orderLines[1].PurchaseOrderLineId, StockItemMasterId = orderLines[1].StockItemMasterId, ItemName = orderLines[1].ItemName, UnitName = orderLines[1].UnitName, BatchNumber = "GLV-2602", ExpiryDate = DateTime.Today.AddMonths(18), Quantity = 20, UnitCost = 420, LineTotal = 8400, CreatedAt = DateTime.UtcNow });
            await _db.SaveChangesAsync(cancellationToken);
        }

        if (!await _db.StockTransfers.AnyAsync(cancellationToken))
        {
            var sourceBatch = await _db.StockBatches.OrderByDescending(x => x.QuantityOnHand).FirstAsync(cancellationToken);
            var transfer = new StockTransfer
            {
                TransferNumber = "TR-000001",
                FromStockLocationId = sourceBatch.StockLocationId,
                ToStockLocationId = locationMap["WARD-STOCK"],
                TransferDate = DateTime.Today,
                Status = TransferStatus.Pending,
                Notes = "Pending ward replenishment transfer",
                CreatedAt = DateTime.UtcNow
            };
            _db.StockTransfers.Add(transfer);
            await _db.SaveChangesAsync(cancellationToken);

            _db.StockTransferLines.Add(new StockTransferLine
            {
                StockTransferId = transfer.StockTransferId,
                StockBatchId = sourceBatch.StockBatchId,
                ItemName = sourceBatch.MedicineMasterId.HasValue ? "Paracetamol 500mg" : "Seed batch",
                BatchNumber = sourceBatch.BatchNumber,
                ExpiryDate = sourceBatch.ExpiryDate,
                Quantity = 10,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(cancellationToken);
        }

        if (!await _db.StockAdjustments.AnyAsync(cancellationToken))
        {
            var batch = await _db.StockBatches.OrderBy(x => x.StockBatchId).FirstAsync(cancellationToken);
            var adjustment = new StockAdjustment
            {
                AdjustmentNumber = "ADJ-000001",
                StockLocationId = batch.StockLocationId,
                Reason = StockAdjustmentReason.ManualCorrection,
                Status = ApprovalStatus.Pending,
                AdjustmentDate = DateTime.Today,
                Notes = "Seed stock correction awaiting approval",
                CreatedAt = DateTime.UtcNow
            };
            _db.StockAdjustments.Add(adjustment);
            await _db.SaveChangesAsync(cancellationToken);

            _db.StockAdjustmentLines.Add(new StockAdjustmentLine
            {
                StockAdjustmentId = adjustment.StockAdjustmentId,
                StockBatchId = batch.StockBatchId,
                ItemName = batch.MedicineMasterId.HasValue ? "Amoxicillin 500mg" : "Seed item",
                BatchNumber = batch.BatchNumber,
                ExpiryDate = batch.ExpiryDate,
                QuantityDelta = -2,
                Notes = "Physical count variance",
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    private static IEnumerable<string> SplitSqlBatches(string script)
    {
        using var reader = new StringReader(script);
        var current = new List<string>();
        string? line;

        while ((line = reader.ReadLine()) is not null)
        {
            if (string.Equals(line.Trim(), "GO", StringComparison.OrdinalIgnoreCase))
            {
                if (current.Count != 0)
                {
                    yield return string.Join(Environment.NewLine, current);
                    current.Clear();
                }

                continue;
            }

            current.Add(line);
        }

        if (current.Count != 0)
        {
            yield return string.Join(Environment.NewLine, current);
        }
    }

    private async Task EnsureLabTestsAsync(CancellationToken cancellationToken)
    {
        if (await _db.LabTestMasters.AnyAsync(cancellationToken))
        {
            return;
        }

        _db.LabTestMasters.AddRange(
            new LabTestMaster
            {
                TestName = "Complete Blood Count",
                DepartmentName = "Laboratory",
                Price = 900m,
                SampleType = "Whole Blood",
                ReportFormat = "Numeric panel with reference range",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new LabTestMaster
            {
                TestName = "Liver Function Test",
                DepartmentName = "Laboratory",
                Price = 1450m,
                SampleType = "Serum",
                ReportFormat = "Biochemistry report with interpretation",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new LabTestMaster
            {
                TestName = "Urine Routine Examination",
                DepartmentName = "Outpatient Department",
                Price = 550m,
                SampleType = "Urine",
                ReportFormat = "Microscopy and chemistry summary",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new LabTestMaster
            {
                TestName = "ABG Analysis",
                DepartmentName = "Intensive Care Unit",
                Price = 1800m,
                SampleType = "Arterial Blood",
                ReportFormat = "Critical care gas analysis sheet",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureServicePackagesAsync(CancellationToken cancellationToken)
    {
        if (await _db.ServicePackages.AnyAsync(cancellationToken))
        {
            return;
        }

        _db.ServicePackages.AddRange(
            new ServicePackage
            {
                Kind = ServicePackageKind.HealthPackage,
                PackageName = "Executive Wellness Package",
                DepartmentName = "Outpatient Department",
                Price = 12000m,
                DiscountAmount = 1500m,
                Description = "Annual executive screening bundle with consultation, CBC, sugar profile, ECG, and ultrasound.",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new ServicePackage
            {
                Kind = ServicePackageKind.SurgeryPackage,
                PackageName = "Laparoscopic Cholecystectomy Pack",
                DepartmentName = "Operation Theatre",
                Price = 85000m,
                DiscountAmount = 5000m,
                Description = "Procedure, OT support, bed charge, anesthesia, and routine post-op consumables.",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new ServicePackage
            {
                Kind = ServicePackageKind.CorporatePackage,
                PackageName = "Factory Workforce Checkup",
                DepartmentName = "Outpatient Department",
                Price = 9500m,
                DiscountAmount = 1000m,
                Description = "Group employee screening bundle with vitals, consultation, CBC, urine exam, and summary reporting.",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new ServicePackage
            {
                Kind = ServicePackageKind.DiscountedBundle,
                PackageName = "Mother & Child Diagnostic Bundle",
                DepartmentName = "Laboratory",
                Price = 6000m,
                DiscountAmount = 1200m,
                Description = "Discounted bundle for antenatal checkup diagnostics, CBC, urine routine, and blood sugar.",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureBillingChargeDefinitionsAsync(CancellationToken cancellationToken)
    {
        if (await _db.BillingChargeDefinitions.AnyAsync(cancellationToken))
        {
            return;
        }

        _db.BillingChargeDefinitions.AddRange(
            new BillingChargeDefinition
            {
                ChargeType = BillingChargeType.Consultation,
                Name = "Specialist Consultation",
                Code = "CONS-STD",
                Description = "Standard doctor consultation charge.",
                UnitLabel = "visit",
                DefaultAmount = 2500m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new BillingChargeDefinition
            {
                ChargeType = BillingChargeType.Lab,
                Name = "Diagnostic Lab Panel",
                Code = "LAB-BASIC",
                Description = "Routine laboratory diagnostics package.",
                UnitLabel = "panel",
                DefaultAmount = 1700m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new BillingChargeDefinition
            {
                ChargeType = BillingChargeType.Procedure,
                Name = "Minor Procedure Pack",
                Code = "PROC-MINOR",
                Description = "Standard minor procedure and theatre support charge.",
                UnitLabel = "procedure",
                DefaultAmount = 4800m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new BillingChargeDefinition
            {
                ChargeType = BillingChargeType.Bed,
                Name = "Bed Day Charge",
                Code = "BED-DAY",
                Description = "Per-day inpatient bed charge.",
                UnitLabel = "day",
                DefaultAmount = 1800m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new BillingChargeDefinition
            {
                ChargeType = BillingChargeType.NursingService,
                Name = "Nursing & Service Round",
                Code = "NURS-ROUND",
                Description = "Per-round nursing and bedside service charge.",
                UnitLabel = "round",
                DefaultAmount = 950m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureBillingPaymentMethodsAsync(CancellationToken cancellationToken)
    {
        if (await _db.BillingPaymentMethods.AnyAsync(cancellationToken))
        {
            return;
        }

        _db.BillingPaymentMethods.AddRange(
            new BillingPaymentMethod { Name = "Cash Counter", MethodType = PaymentMethodType.Cash, RequiresReference = false, SortOrder = 1, IsActive = true, CreatedAt = DateTime.UtcNow },
            new BillingPaymentMethod { Name = "POS Card", MethodType = PaymentMethodType.Card, ProviderName = "Nabil Bank POS", RequiresReference = true, SortOrder = 2, IsActive = true, CreatedAt = DateTime.UtcNow },
            new BillingPaymentMethod { Name = "Bank Transfer", MethodType = PaymentMethodType.Bank, ProviderName = "Global IME Bank", RequiresReference = true, SortOrder = 3, IsActive = true, CreatedAt = DateTime.UtcNow },
            new BillingPaymentMethod { Name = "eSewa Wallet", MethodType = PaymentMethodType.MobileWallet, ProviderName = "eSewa", RequiresReference = true, SortOrder = 4, IsActive = true, CreatedAt = DateTime.UtcNow },
            new BillingPaymentMethod { Name = "Insurance Claim Settlement", MethodType = PaymentMethodType.InsuranceClaim, ProviderName = "Insurance Desk", RequiresReference = true, SortOrder = 5, IsActive = true, CreatedAt = DateTime.UtcNow });

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureBillingPartnersAsync(CancellationToken cancellationToken)
    {
        if (await _db.BillingPartners.AnyAsync(cancellationToken))
        {
            return;
        }

        _db.BillingPartners.AddRange(
            new BillingPartner
            {
                Kind = BillingPartnerKind.InsuranceCompany,
                Name = "NLG Insurance",
                Code = "NLGI",
                ContactPerson = "Claims Desk",
                ContactEmail = "claims@nlgi.local",
                ContactPhone = "+977-9803000001",
                CreditLimit = 150000m,
                ClaimSubmissionMode = "Portal Upload",
                Notes = "General outpatient and inpatient claims partner.",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new BillingPartner
            {
                Kind = BillingPartnerKind.InsuranceCompany,
                Name = "Himalayan Health Cover",
                Code = "HHC",
                ContactPerson = "Partner Support",
                ContactEmail = "partners@hhc.local",
                ContactPhone = "+977-9803000002",
                CreditLimit = 100000m,
                ClaimSubmissionMode = "Email Submission",
                Notes = "Therapy and chronic care insurance panel.",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new BillingPartner
            {
                Kind = BillingPartnerKind.PanelOrganization,
                Name = "Sunrise Manufacturing Ltd",
                Code = "SUN-CORP",
                ContactPerson = "HR Benefits Team",
                ContactEmail = "benefits@sunrise.local",
                ContactPhone = "+977-9803000101",
                CreditLimit = 250000m,
                ClaimSubmissionMode = "Monthly Statement",
                Notes = "Corporate panel for staff and dependants.",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new BillingPartner
            {
                Kind = BillingPartnerKind.PanelOrganization,
                Name = "Orbit Tech Services",
                Code = "ORB-TECH",
                ContactPerson = "Finance Controller",
                ContactEmail = "finance@orbit.local",
                ContactPhone = "+977-9803000102",
                CreditLimit = 180000m,
                ClaimSubmissionMode = "API Export",
                Notes = "Corporate wellness and emergency panel.",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureBillingRulesAsync(CancellationToken cancellationToken)
    {
        if (await _db.BillingRules.AnyAsync(cancellationToken))
        {
            return;
        }

        var partners = await _db.BillingPartners.OrderBy(x => x.BillingPartnerId).ToListAsync(cancellationToken);
        if (partners.Count == 0)
        {
            return;
        }

        var insurancePartner = partners.FirstOrDefault(x => x.Kind == BillingPartnerKind.InsuranceCompany);
        var corporatePartner = partners.FirstOrDefault(x => x.Kind == BillingPartnerKind.PanelOrganization);

        if (insurancePartner is not null)
        {
            _db.BillingRules.Add(new BillingRule
            {
                BillingPartnerId = insurancePartner.BillingPartnerId,
                RuleName = "Standard Insurance Coverage",
                PolicyName = "80/20 Plan",
                DiscountPercentage = 12m,
                CoPayPercentage = 20m,
                CreditLimit = insurancePartner.CreditLimit,
                ClaimSubmissionWindowDays = 7,
                RequiresPreApproval = true,
                Notes = "Standard inpatient and specialist claim rule.",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }

        if (corporatePartner is not null)
        {
            _db.BillingRules.Add(new BillingRule
            {
                BillingPartnerId = corporatePartner.BillingPartnerId,
                RuleName = "Corporate Employee Panel",
                PolicyName = "Quarterly Settlement",
                DiscountPercentage = 10m,
                CoPayPercentage = 0m,
                CreditLimit = corporatePartner.CreditLimit,
                ClaimSubmissionWindowDays = 30,
                RequiresPreApproval = false,
                Notes = "Corporate billing rule for employer-sponsored care.",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureBillingAsync(CancellationToken cancellationToken)
    {
        if (!await _db.BillingInvoices.AnyAsync(cancellationToken))
        {
            var appointments = await _db.Appointments.OrderBy(x => x.AppointmentId).ToListAsync(cancellationToken);
            var patients = await _db.Patients.OrderBy(x => x.PatientId).ToListAsync(cancellationToken);
            var branches = await _db.Branches.OrderBy(x => x.BranchId).ToListAsync(cancellationToken);
            var partners = await _db.BillingPartners.OrderBy(x => x.BillingPartnerId).ToListAsync(cancellationToken);
            var rules = await _db.BillingRules.OrderBy(x => x.BillingRuleId).ToListAsync(cancellationToken);
            var today = DateTime.Today;

            if (patients.Count == 0 || branches.Count == 0)
            {
                return;
            }

            var insurancePartner = partners.FirstOrDefault(x => x.Kind == BillingPartnerKind.InsuranceCompany);
            var corporatePartner = partners.FirstOrDefault(x => x.Kind == BillingPartnerKind.PanelOrganization);
            var insuranceRule = rules.FirstOrDefault(x => x.BillingPartnerId == insurancePartner?.BillingPartnerId);
            var corporateRule = rules.FirstOrDefault(x => x.BillingPartnerId == corporatePartner?.BillingPartnerId);

            _db.BillingInvoices.AddRange(
                new BillingInvoice
                {
                    InvoiceNumber = "NEX-5001",
                    PatientId = patients[0].PatientId,
                    AppointmentId = appointments.ElementAtOrDefault(0)?.AppointmentId,
                    BranchId = branches[0].BranchId,
                    PayerType = InvoicePayerType.SelfPay,
                    TotalAmount = 7500m,
                    AmountPaid = 7500m,
                    Status = InvoiceStatus.Paid,
                    InvoiceDate = today,
                    LastPaymentDate = today,
                    DueDate = today,
                    Notes = "Paid at reception",
                    CreatedAt = DateTime.UtcNow
                },
                new BillingInvoice
                {
                    InvoiceNumber = "NEX-5002",
                    PatientId = patients[1].PatientId,
                    AppointmentId = appointments.ElementAtOrDefault(1)?.AppointmentId,
                    BranchId = branches[Math.Min(1, branches.Count - 1)].BranchId,
                    PayerType = InvoicePayerType.Corporate,
                    BillingPartnerId = corporatePartner?.BillingPartnerId,
                    BillingRuleId = corporateRule?.BillingRuleId,
                    RequestedDiscountAmount = 300m,
                    ApprovedDiscountAmount = 200m,
                    DiscountNotes = "Corporate courtesy adjustment approved.",
                    TotalAmount = 4200m,
                    AmountPaid = 2000m,
                    Status = InvoiceStatus.Partial,
                    InvoiceDate = today,
                    LastPaymentDate = today,
                    DueDate = today.AddDays(5),
                    Notes = "Balance due after lab test",
                    CreatedAt = DateTime.UtcNow
                },
                new BillingInvoice
                {
                    InvoiceNumber = "NEX-5003",
                    PatientId = patients[2].PatientId,
                    AppointmentId = appointments.ElementAtOrDefault(2)?.AppointmentId,
                    BranchId = branches[0].BranchId,
                    PayerType = InvoicePayerType.Insurance,
                    BillingPartnerId = insurancePartner?.BillingPartnerId,
                    BillingRuleId = insuranceRule?.BillingRuleId,
                    RequestedDiscountAmount = 1500m,
                    ApprovedDiscountAmount = 1200m,
                    ClaimStatus = BillingClaimStatus.Submitted,
                    ClaimReferenceNumber = "CLM-2026-0043",
                    ClaimSubmittedAt = DateTime.UtcNow.AddDays(-1),
                    TotalAmount = 9800m,
                    AmountPaid = 0m,
                    Status = InvoiceStatus.Pending,
                    InvoiceDate = today.AddDays(-1),
                    DueDate = today.AddDays(3),
                    Notes = "Pending insurance confirmation",
                    CreatedAt = DateTime.UtcNow
                },
                new BillingInvoice
                {
                    InvoiceNumber = "NEX-5004",
                    PatientId = patients[3].PatientId,
                    AppointmentId = appointments.ElementAtOrDefault(3)?.AppointmentId,
                    BranchId = branches[Math.Min(2, branches.Count - 1)].BranchId,
                    PayerType = InvoicePayerType.SelfPay,
                    TotalAmount = 5600m,
                    AmountPaid = 5600m,
                    RefundedAmount = 400m,
                    Status = InvoiceStatus.Paid,
                    InvoiceDate = today.AddDays(-2),
                    LastPaymentDate = today.AddDays(-2),
                    DueDate = today.AddDays(-2),
                    Notes = "Paid online and partially refunded after item correction.",
                    CreatedAt = DateTime.UtcNow
                });

            await _db.SaveChangesAsync(cancellationToken);
        }

        await EnsureBillingInvoiceDetailsAsync(cancellationToken);
    }

    private async Task EnsureBillingInvoiceDetailsAsync(CancellationToken cancellationToken)
    {
        if (await _db.BillingInvoiceItems.AnyAsync(cancellationToken))
        {
            return;
        }

        var invoices = await _db.BillingInvoices.OrderBy(x => x.InvoiceNumber).ToListAsync(cancellationToken);
        if (invoices.Count == 0)
        {
            return;
        }

        var chargeMap = await _db.BillingChargeDefinitions.ToDictionaryAsync(x => x.Code, cancellationToken);
        var paymentMethodMap = await _db.BillingPaymentMethods.ToDictionaryAsync(x => x.MethodType, cancellationToken);

        if (!chargeMap.ContainsKey("CONS-STD") || !chargeMap.ContainsKey("LAB-BASIC") || !chargeMap.ContainsKey("PROC-MINOR") ||
            !chargeMap.ContainsKey("BED-DAY") || !chargeMap.ContainsKey("NURS-ROUND"))
        {
            return;
        }

        var invoice1 = invoices.ElementAtOrDefault(0);
        var invoice2 = invoices.ElementAtOrDefault(1);
        var invoice3 = invoices.ElementAtOrDefault(2);
        var invoice4 = invoices.ElementAtOrDefault(3);

        if (invoice1 is not null)
        {
            _db.BillingInvoiceItems.AddRange(
                BuildInvoiceItem(invoice1.BillingInvoiceId, chargeMap["CONS-STD"], "Neurology consultation", 1m, 2500m, 0m),
                BuildInvoiceItem(invoice1.BillingInvoiceId, chargeMap["LAB-BASIC"], "Advanced lab diagnostics", 2m, 2500m, 0m));

            if (paymentMethodMap.TryGetValue(PaymentMethodType.Cash, out var cashMethod))
            {
                _db.BillingInvoicePayments.Add(new BillingInvoicePayment
                {
                    BillingInvoiceId = invoice1.BillingInvoiceId,
                    BillingPaymentMethodId = cashMethod.BillingPaymentMethodId,
                    Amount = 7500m,
                    PaymentDate = invoice1.InvoiceDate,
                    ReferenceNumber = null,
                    Notes = "Settled at billing counter.",
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        if (invoice2 is not null)
        {
            _db.BillingInvoiceItems.AddRange(
                BuildInvoiceItem(invoice2.BillingInvoiceId, chargeMap["CONS-STD"], "Therapy review consultation", 1m, 1800m, 0m),
                BuildInvoiceItem(invoice2.BillingInvoiceId, chargeMap["LAB-BASIC"], "Follow-up lab package", 1m, 2400m, 0m));

            if (paymentMethodMap.TryGetValue(PaymentMethodType.Card, out var cardMethod))
            {
                _db.BillingInvoicePayments.Add(new BillingInvoicePayment
                {
                    BillingInvoiceId = invoice2.BillingInvoiceId,
                    BillingPaymentMethodId = cardMethod.BillingPaymentMethodId,
                    Amount = 2000m,
                    PaymentDate = invoice2.InvoiceDate,
                    ReferenceNumber = "POS-220391",
                    Notes = "Initial card payment collected.",
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        if (invoice3 is not null)
        {
            _db.BillingInvoiceItems.AddRange(
                BuildInvoiceItem(invoice3.BillingInvoiceId, chargeMap["PROC-MINOR"], "Day procedure support pack", 1m, 4800m, 0m),
                BuildInvoiceItem(invoice3.BillingInvoiceId, chargeMap["LAB-BASIC"], "Insurance-covered diagnostics", 2m, 1700m, 0m),
                BuildInvoiceItem(invoice3.BillingInvoiceId, chargeMap["NURS-ROUND"], "Special nursing rounds", 2m, 800m, 0m));
        }

        if (invoice4 is not null)
        {
            _db.BillingInvoiceItems.AddRange(
                BuildInvoiceItem(invoice4.BillingInvoiceId, chargeMap["BED-DAY"], "Observation bed stay", 2m, 1800m, 0m),
                BuildInvoiceItem(invoice4.BillingInvoiceId, chargeMap["NURS-ROUND"], "Bedside nursing service", 2m, 1000m, 0m));

            if (paymentMethodMap.TryGetValue(PaymentMethodType.MobileWallet, out var walletMethod))
            {
                _db.BillingInvoicePayments.Add(new BillingInvoicePayment
                {
                    BillingInvoiceId = invoice4.BillingInvoiceId,
                    BillingPaymentMethodId = walletMethod.BillingPaymentMethodId,
                    Amount = 6000m,
                    PaymentDate = invoice4.InvoiceDate,
                    ReferenceNumber = "ESEWA-884391",
                    Notes = "Paid through mobile wallet.",
                    CreatedAt = DateTime.UtcNow
                });

                _db.BillingRefunds.Add(new BillingRefund
                {
                    BillingInvoiceId = invoice4.BillingInvoiceId,
                    BillingPaymentMethodId = walletMethod.BillingPaymentMethodId,
                    Amount = 400m,
                    Status = RefundStatus.Processed,
                    Reason = "Duplicate nursing round reversed",
                    Notes = "Refund issued after final bill audit.",
                    RequestedAt = invoice4.InvoiceDate.AddHours(2),
                    ProcessedAt = invoice4.InvoiceDate.AddHours(3),
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        foreach (var invoice in invoices)
        {
            await SyncInvoiceTotalsAsync(invoice, cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task SyncInvoiceTotalsAsync(BillingInvoice invoice, CancellationToken cancellationToken)
    {
        var itemTotal = await _db.BillingInvoiceItems
            .Where(x => x.BillingInvoiceId == invoice.BillingInvoiceId)
            .SumAsync(x => (decimal?)x.TotalAmount, cancellationToken) ?? 0m;

        var paymentTotal = await _db.BillingInvoicePayments
            .Where(x => x.BillingInvoiceId == invoice.BillingInvoiceId)
            .SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;

        var refundTotal = await _db.BillingRefunds
            .Where(x => x.BillingInvoiceId == invoice.BillingInvoiceId && x.Status == RefundStatus.Processed)
            .SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;

        var lastPayment = await _db.BillingInvoicePayments
            .Where(x => x.BillingInvoiceId == invoice.BillingInvoiceId)
            .OrderByDescending(x => x.PaymentDate)
            .Select(x => (DateTime?)x.PaymentDate)
            .FirstOrDefaultAsync(cancellationToken);

        invoice.TotalAmount = itemTotal;
        invoice.AmountPaid = Math.Max(paymentTotal - refundTotal, 0m);
        invoice.RefundedAmount = refundTotal;
        invoice.LastPaymentDate = lastPayment;

        if (invoice.Status != InvoiceStatus.Cancelled)
        {
            var dueAmount = Math.Max(invoice.TotalAmount - invoice.ApprovedDiscountAmount - invoice.AmountPaid, 0m);
            invoice.Status = dueAmount <= 0m
                ? InvoiceStatus.Paid
                : invoice.AmountPaid > 0m
                    ? InvoiceStatus.Partial
                    : InvoiceStatus.Pending;
        }
    }

    private static BillingInvoiceItem BuildInvoiceItem(long billingInvoiceId, BillingChargeDefinition definition, string description, decimal quantity, decimal unitPrice, decimal discountAmount)
    {
        return new BillingInvoiceItem
        {
            BillingInvoiceId = billingInvoiceId,
            BillingChargeDefinitionId = definition.BillingChargeDefinitionId,
            ChargeType = definition.ChargeType,
            Description = description,
            Quantity = quantity,
            UnitPrice = unitPrice,
            DiscountAmount = discountAmount,
            TotalAmount = Math.Max(quantity * unitPrice - discountAmount, 0m),
            CreatedAt = DateTime.UtcNow
        };
    }

    private async Task EnsureDepartmentAsync(long branchId, string name, string code, string description, CancellationToken cancellationToken)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();
        var department = await _db.Departments.FirstOrDefaultAsync(
            x => x.BranchId == branchId && x.Code == normalizedCode,
            cancellationToken);

        if (department is null)
        {
            _db.Departments.Add(new Department
            {
                BranchId = branchId,
                Name = name,
                Code = normalizedCode,
                Description = description,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(cancellationToken);
            return;
        }

        var hasChanges = false;
        if (department.Name != name)
        {
            department.Name = name;
            hasChanges = true;
        }

        if (department.Description != description)
        {
            department.Description = description;
            hasChanges = true;
        }

        if (!department.IsActive)
        {
            department.IsActive = true;
            hasChanges = true;
        }

        if (hasChanges)
        {
            department.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task EnsurePatientCategoryAsync(string name, string description, int priorityOrder, CancellationToken cancellationToken)
    {
        var item = await _db.PatientCategories.FirstOrDefaultAsync(x => x.Name == name, cancellationToken);
        if (item is null)
        {
            _db.PatientCategories.Add(new PatientCategory
            {
                Name = name,
                Description = description,
                PriorityOrder = priorityOrder,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(cancellationToken);
            return;
        }

        var hasChanges = false;
        if (item.Description != description)
        {
            item.Description = description;
            hasChanges = true;
        }

        if (item.PriorityOrder != priorityOrder)
        {
            item.PriorityOrder = priorityOrder;
            hasChanges = true;
        }

        if (!item.IsActive)
        {
            item.IsActive = true;
            hasChanges = true;
        }

        if (hasChanges)
        {
            item.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task EnsureDoctorAsync(
        string fullName,
        string specialization,
        string email,
        string phone,
        int experienceYears,
        string qualification,
        decimal consultationFee,
        long branchId,
        long departmentId,
        string opdDays,
        TimeSpan opdStartTime,
        TimeSpan opdEndTime,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var doctor = await _db.Doctors.FirstOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);

        if (doctor is null)
        {
            _db.Doctors.Add(new Doctor
            {
                BranchId = branchId,
                DepartmentId = departmentId,
                FullName = fullName,
                Specialization = specialization,
                Email = normalizedEmail,
                Phone = phone,
                ExperienceYears = experienceYears,
                Qualification = qualification,
                OpdDays = opdDays,
                OpdStartTime = opdStartTime,
                OpdEndTime = opdEndTime,
                ConsultationFee = consultationFee,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync(cancellationToken);
            return;
        }

        doctor.BranchId = branchId;
        doctor.DepartmentId = departmentId;
        doctor.FullName = fullName;
        doctor.Specialization = specialization;
        doctor.Phone = phone;
        doctor.ExperienceYears = experienceYears;
        doctor.Qualification = qualification;
        doctor.OpdDays = opdDays;
        doctor.OpdStartTime = opdStartTime;
        doctor.OpdEndTime = opdEndTime;
        doctor.ConsultationFee = consultationFee;
        doctor.IsActive = true;
        doctor.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureStaffMemberAsync(
        string fullName,
        string employeeCode,
        string designation,
        string shift,
        string email,
        string phone,
        long branchId,
        long departmentId,
        DateTime joinDate,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var departmentName = await _db.Departments
            .Where(x => x.DepartmentId == departmentId)
            .Select(x => x.Name)
            .FirstAsync(cancellationToken);

        var item = await _db.Staff.FirstOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);
        if (item is null)
        {
            _db.Staff.Add(new Staff
            {
                BranchId = branchId,
                DepartmentId = departmentId,
                Department = departmentName,
                FullName = fullName,
                EmployeeCode = employeeCode,
                Designation = designation,
                Shift = shift,
                Email = normalizedEmail,
                Phone = phone,
                JoinDate = joinDate.Date,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(cancellationToken);
            return;
        }

        item.BranchId = branchId;
        item.DepartmentId = departmentId;
        item.Department = departmentName;
        item.FullName = fullName;
        item.EmployeeCode = employeeCode;
        item.Designation = designation;
        item.Shift = shift;
        item.Phone = phone;
        item.JoinDate = joinDate.Date;
        item.IsActive = true;
        item.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Ward> EnsureWardAsync(
        long branchId,
        long? departmentId,
        string name,
        string wardType,
        string roomType,
        decimal chargePerDay,
        CancellationToken cancellationToken)
    {
        var item = await _db.Wards.FirstOrDefaultAsync(x => x.BranchId == branchId && x.Name == name, cancellationToken);
        if (item is null)
        {
            item = new Ward
            {
                BranchId = branchId,
                DepartmentId = departmentId,
                Name = name,
                WardType = wardType,
                RoomType = roomType,
                ChargePerDay = chargePerDay,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _db.Wards.Add(item);
            await _db.SaveChangesAsync(cancellationToken);
            return item;
        }

        item.DepartmentId = departmentId;
        item.WardType = wardType;
        item.RoomType = roomType;
        item.ChargePerDay = chargePerDay;
        item.IsActive = true;
        item.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return item;
    }

    private async Task EnsureBedAsync(
        long wardId,
        long branchId,
        long? departmentId,
        string bedNumber,
        decimal chargePerDay,
        bool isOccupied,
        CancellationToken cancellationToken)
    {
        var normalizedBedNumber = bedNumber.Trim().ToUpperInvariant();
        var item = await _db.Beds.FirstOrDefaultAsync(
            x => x.WardId == wardId && x.BedNumber == normalizedBedNumber,
            cancellationToken);

        if (item is null)
        {
            _db.Beds.Add(new Bed
            {
                WardId = wardId,
                BranchId = branchId,
                DepartmentId = departmentId,
                BedNumber = normalizedBedNumber,
                ChargePerDay = chargePerDay,
                IsOccupied = isOccupied,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(cancellationToken);
            return;
        }

        item.BranchId = branchId;
        item.DepartmentId = departmentId;
        item.ChargePerDay = chargePerDay;
        item.IsOccupied = isOccupied;
        item.IsActive = true;
        item.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureUserAsync(
        string fullName,
        string email,
        string password,
        string roleName,
        bool ensurePatientProfile,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var role = await _db.Roles.FirstAsync(x => x.Name == roleName, cancellationToken);
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);

        if (user is null)
        {
            CreatePasswordHash(password, out var hash, out var salt);

            user = new User
            {
                FullName = fullName,
                Email = normalizedEmail,
                RoleId = role.RoleId,
                PasswordHash = hash,
                PasswordSalt = salt,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync(cancellationToken);
        }
        else
        {
            var hasChanges = false;

            if (user.RoleId != role.RoleId)
            {
                user.RoleId = role.RoleId;
                hasChanges = true;
            }

            if (!user.IsActive)
            {
                user.IsActive = true;
                hasChanges = true;
            }

            if (string.IsNullOrWhiteSpace(user.FullName))
            {
                user.FullName = fullName;
                hasChanges = true;
            }

            if (hasChanges)
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
        }

        if (ensurePatientProfile && !await _db.Patients.AnyAsync(x => x.UserId == user.UserId, cancellationToken))
        {
            _db.Patients.Add(new Patient
            {
                UserId = user.UserId,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task EnsureSeedServicesAsync(CancellationToken cancellationToken)
    {
        if (await _db.Services.AnyAsync(cancellationToken))
        {
            return;
        }

        _db.Services.AddRange(GetSeedServices());
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureAppointmentTokensAsync(AppointmentTokenSetting settings, CancellationToken cancellationToken)
    {
        var appointments = await _db.Appointments
            .Where(x =>
                x.Status == AppointmentStatus.Approved ||
                x.Status == AppointmentStatus.Rescheduled ||
                x.Status == AppointmentStatus.Completed)
            .OrderBy(x => x.DoctorId)
            .ThenBy(x => x.AppointmentDate)
            .ThenBy(x => x.SlotStartTime)
            .ToListAsync(cancellationToken);

        if (appointments.Count == 0)
        {
            return;
        }

        var hasChanges = false;
        var groups = settings.ResetDaily
            ? appointments.GroupBy(x => $"{x.DoctorId}:{x.AppointmentDate:yyyyMMdd}")
            : appointments.GroupBy(x => x.DoctorId.ToString());

        foreach (var group in groups)
        {
            var sequence = settings.StartingNumber;
            foreach (var appointment in group)
            {
                var tokenNumber = FormatToken(settings.Prefix, appointment.AppointmentDate, sequence, settings.NumberPadding);
                if (!string.Equals(appointment.TokenNumber, tokenNumber, StringComparison.Ordinal))
                {
                    appointment.TokenNumber = tokenNumber;
                    appointment.UpdatedAt = DateTime.UtcNow;
                    hasChanges = true;
                }

                sequence++;
            }
        }

        if (hasChanges)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task RefreshBranchOccupancyAsync(CancellationToken cancellationToken)
    {
        var occupancyMap = await _db.Beds
            .AsNoTracking()
            .Where(x => x.IsActive)
            .GroupBy(x => x.BranchId)
            .Select(group => new { BranchId = group.Key, OccupiedBeds = group.Count(x => x.IsOccupied) })
            .ToDictionaryAsync(x => x.BranchId, x => x.OccupiedBeds, cancellationToken);

        var branches = await _db.Branches.ToListAsync(cancellationToken);
        foreach (var branch in branches)
        {
            branch.OccupiedBeds = occupancyMap.GetValueOrDefault(branch.BranchId);
            branch.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private static long? ResolveScheduleId(IEnumerable<DoctorSchedule> schedules, long doctorId, DateTime date)
    {
        var dayOfWeek = date.DayOfWeek switch
        {
            DayOfWeek.Sunday => (byte)1,
            DayOfWeek.Monday => (byte)2,
            DayOfWeek.Tuesday => (byte)3,
            DayOfWeek.Wednesday => (byte)4,
            DayOfWeek.Thursday => (byte)5,
            DayOfWeek.Friday => (byte)6,
            _ => (byte)7
        };

        return schedules.FirstOrDefault(x => x.DoctorId == doctorId && x.DayOfWeek == dayOfWeek)?.ScheduleId;
    }

    private static string FormatToken(string prefix, DateTime appointmentDate, int sequence, int numberPadding)
    {
        return $"{prefix}-{appointmentDate:yyyyMMdd}-{sequence.ToString($"D{numberPadding}")}";
    }

    private static IEnumerable<RolePermission> GetDefaultPermissions(long roleId, string roleName)
    {
        return roleName switch
        {
            "Admin" => FullAccess(roleId),
            "Receptionist" => ModuleSet(roleId, new Dictionary<string, (bool View, bool Add, bool Edit, bool Delete)>
            {
                ["dashboard"] = (true, false, false, false),
                ["masters"] = (true, false, false, false),
                ["patients"] = (true, true, true, false),
                ["appointments"] = (true, true, true, true),
                ["billing"] = (true, true, true, false),
                ["branches"] = (true, false, false, false)
            }),
            "Doctor" => ModuleSet(roleId, new Dictionary<string, (bool View, bool Add, bool Edit, bool Delete)>
            {
                ["dashboard"] = (true, false, false, false),
                ["masters"] = (true, false, false, false),
                ["patients"] = (true, false, true, false),
                ["appointments"] = (true, false, true, false),
                ["laboratory"] = (true, true, true, false)
            }),
            "Pharmacist" => ModuleSet(roleId, new Dictionary<string, (bool View, bool Add, bool Edit, bool Delete)>
            {
                ["dashboard"] = (true, false, false, false),
                ["masters"] = (true, false, false, false),
                ["pharmacy"] = (true, true, true, false),
                ["inventory"] = (true, true, true, false),
                ["billing"] = (true, false, false, false)
            }),
            "Lab" => ModuleSet(roleId, new Dictionary<string, (bool View, bool Add, bool Edit, bool Delete)>
            {
                ["dashboard"] = (true, false, false, false),
                ["masters"] = (true, false, false, false),
                ["laboratory"] = (true, true, true, false),
                ["patients"] = (true, false, false, false)
            }),
            "Storekeeper" => ModuleSet(roleId, new Dictionary<string, (bool View, bool Add, bool Edit, bool Delete)>
            {
                ["dashboard"] = (true, false, false, false),
                ["masters"] = (true, false, false, false),
                ["inventory"] = (true, true, true, true),
                ["branches"] = (true, false, false, false)
            }),
            "Accountant" => ModuleSet(roleId, new Dictionary<string, (bool View, bool Add, bool Edit, bool Delete)>
            {
                ["dashboard"] = (true, false, false, false),
                ["masters"] = (true, false, false, false),
                ["billing"] = (true, true, true, false),
                ["patients"] = (true, false, false, false),
                ["branches"] = (true, false, false, false)
            }),
            "User" => ModuleSet(roleId, new Dictionary<string, (bool View, bool Add, bool Edit, bool Delete)>
            {
                ["appointments"] = (true, true, false, false)
            }),
            _ => ModuleSet(roleId, new Dictionary<string, (bool View, bool Add, bool Edit, bool Delete)>())
        };
    }

    private static IEnumerable<RolePermission> FullAccess(long roleId) =>
        Modules.Select(module => new RolePermission
        {
            RoleId = roleId,
            ModuleKey = module.Key,
            ModuleName = module.Name,
            CanView = true,
            CanAdd = true,
            CanEdit = true,
            CanDelete = true,
            UpdatedAt = DateTime.UtcNow
        });

    private static IEnumerable<RolePermission> ModuleSet(
        long roleId,
        IReadOnlyDictionary<string, (bool View, bool Add, bool Edit, bool Delete)> allowed)
    {
        return Modules.Select(module =>
        {
            allowed.TryGetValue(module.Key, out var permission);
            return new RolePermission
            {
                RoleId = roleId,
                ModuleKey = module.Key,
                ModuleName = module.Name,
                CanView = permission.View,
                CanAdd = permission.Add,
                CanEdit = permission.Edit,
                CanDelete = permission.Delete,
                UpdatedAt = DateTime.UtcNow
            };
        });
    }

    private static IEnumerable<ServiceModel> GetSeedServices()
    {
        return new[]
        {
            new ServiceModel
            {
                Name = "Initial Consultation",
                Category = "Health",
                DurationMinutes = 45,
                Price = 75.00m,
                Icon = "medical_services",
                Color = "#3b82f6",
                Description = "Comprehensive assessment",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new ServiceModel
            {
                Name = "Deep Tissue Massage",
                Category = "Therapy",
                DurationMinutes = 60,
                Price = 120.00m,
                Icon = "spa",
                Color = "#8b5cf6",
                Description = "Muscle recovery",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new ServiceModel
            {
                Name = "Wellness Coaching",
                Category = "Consulting",
                DurationMinutes = 60,
                Price = 90.00m,
                Icon = "psychology",
                Color = "#f59e0b",
                Description = "Lifestyle coaching",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new ServiceModel
            {
                Name = "Physical Coaching",
                Category = "Checkup",
                DurationMinutes = 60,
                Price = 90.00m,
                Icon = "psychology",
                Color = "#10b981",
                Description = "Personalized physical assessment and coaching",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        };
    }

    private static void CreatePasswordHash(string password, out byte[] hash, out byte[] salt)
    {
        using var hmac = new HMACSHA512();
        salt = hmac.Key;
        hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
    }
}
