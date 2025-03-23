﻿// Data/DataSeeder.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using TeamA.ToDo.Core.Models;
using TeamA.ToDo.Core.Models.General;
using TeamA.ToDo.EntityFramework;

public class DataSeeder
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DataSeeder> _logger;
    private readonly IOptions<AppConfig> _appConfig;

    public DataSeeder(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ApplicationDbContext context,
        ILogger<DataSeeder> logger,
        IOptions<AppConfig> appConfig)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
        _logger = logger;
        _appConfig = appConfig;
    }

    public async Task SeedDataAsync()
    {
        try
        {
            // Only seed data if the database is empty
            if (await _context.Users.AnyAsync())
            {
                _logger.LogInformation("Database already contains data. Seeding skipped.");
                return;
            }

            _logger.LogInformation("Starting to seed database...");

            // Create transaction to ensure all-or-nothing seeding
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Seed roles
                await SeedRolesAsync();

                // Seed permissions
                await SeedPermissionsAsync();

                // Seed admin user
                await SeedAdminUserAsync();

                // Seed sample users (optional)
                if (_appConfig.Value.SeedSampleData)
                {
                    await SeedSampleUsersAsync();
                }

                // Commit transaction if everything succeeded
                await transaction.CommitAsync();
                _logger.LogInformation("Database seeding completed successfully.");
            }
            catch (Exception ex)
            {
                // Rollback transaction if anything failed
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error occurred during database seeding.");
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error occurred during database seeding.");
            throw;
        }
    }

    private async Task SeedRolesAsync()
    {
        _logger.LogInformation("Seeding roles...");

        // Define default roles
        var roles = new List<ApplicationRole>
        {
            new ApplicationRole
            {
                Name = "Admin",
                NormalizedName = "ADMIN",
                Description = "Administrator role with full access to all features",
                CreatedAt = DateTime.UtcNow
            },
            new ApplicationRole
            {
                Name = "User",
                NormalizedName = "USER",
                Description = "Standard user role with limited access",
                CreatedAt = DateTime.UtcNow
            },
            new ApplicationRole
            {
                Name = "Manager",
                NormalizedName = "MANAGER",
                Description = "Manager role with access to manage team members",
                CreatedAt = DateTime.UtcNow
            },
            new ApplicationRole
            {
                Name = "ReadOnly",
                NormalizedName = "READONLY",
                Description = "Read-only role with view-only access",
                CreatedAt = DateTime.UtcNow
            }
        };

        // Create roles if they don't exist
        foreach (var role in roles)
        {
            if (!await _roleManager.RoleExistsAsync(role.Name))
            {
                var result = await _roleManager.CreateAsync(role);
                if (!result.Succeeded)
                {
                    throw new Exception($"Failed to create role {role.Name}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
        }

        _logger.LogInformation("Roles seeded successfully.");
    }

    private async Task SeedPermissionsAsync()
    {
        _logger.LogInformation("Seeding permissions...");

        // Define permissions by category
        var permissionsByCategory = new Dictionary<string, List<Permission>>
        {
            ["UserManagement"] = new List<Permission>
            {
                new Permission { Name = "ViewUsers", Description = "Can view user list", Category = "UserManagement" },
                new Permission { Name = "CreateUsers", Description = "Can create users", Category = "UserManagement" },
                new Permission { Name = "UpdateUsers", Description = "Can update users", Category = "UserManagement" },
                new Permission { Name = "DeleteUsers", Description = "Can delete users", Category = "UserManagement" },
                new Permission { Name = "ManageUserRoles", Description = "Can manage user roles", Category = "UserManagement" }
            },
            ["RoleManagement"] = new List<Permission>
            {
                new Permission { Name = "ViewRoles", Description = "Can view role list", Category = "RoleManagement" },
                new Permission { Name = "ManageRoles", Description = "Can manage roles", Category = "RoleManagement" },
                new Permission { Name = "ManagePermissions", Description = "Can manage permissions", Category = "RoleManagement" }
            },
            ["ToDoManagement"] = new List<Permission>
            {
                new Permission { Name = "ViewAllTodos", Description = "Can view all users' todos", Category = "ToDoManagement" },
                new Permission { Name = "ManageAllTodos", Description = "Can manage all users' todos", Category = "ToDoManagement" },
                new Permission { Name = "ExportTodos", Description = "Can export todos to file", Category = "ToDoManagement" },
                new Permission { Name = "ImportTodos", Description = "Can import todos from file", Category = "ToDoManagement" }
            },
            ["SystemManagement"] = new List<Permission>
            {
                new Permission { Name = "ViewSystemLogs", Description = "Can view system logs", Category = "SystemManagement" },
                new Permission { Name = "ManageSettings", Description = "Can manage application settings", Category = "SystemManagement" },
                new Permission { Name = "ViewStatistics", Description = "Can view system statistics", Category = "SystemManagement" }
            }
        };

        // Create all permissions
        var allPermissions = permissionsByCategory.SelectMany(kv => kv.Value).ToList();
        foreach (var permission in allPermissions)
        {
            var existingPermission = await _context.Permissions
                .FirstOrDefaultAsync(p => p.Name == permission.Name);

            if (existingPermission == null)
            {
                await _context.Permissions.AddAsync(permission);
            }
        }

        await _context.SaveChangesAsync();

        // Assign permissions to roles
        var adminRole = await _roleManager.FindByNameAsync("Admin");
        var userRole = await _roleManager.FindByNameAsync("User");
        var managerRole = await _roleManager.FindByNameAsync("Manager");
        var readOnlyRole = await _roleManager.FindByNameAsync("ReadOnly");

        // Admin gets all permissions
        foreach (var permission in allPermissions)
        {
            await AssignPermissionToRole(adminRole, permission);
        }

        // User gets basic permissions
        var userPermissions = new List<string>
        {
            "ViewUsers" // Can see other users but not modify them
        };

        foreach (var permissionName in userPermissions)
        {
            var permission = allPermissions.FirstOrDefault(p => p.Name == permissionName);
            if (permission != null)
            {
                await AssignPermissionToRole(userRole, permission);
            }
        }

        // Manager gets management permissions
        var managerPermissions = new List<string>
        {
            "ViewUsers", "ViewRoles", "ViewAllTodos", "ManageAllTodos",
            "ExportTodos", "ViewStatistics"
        };

        foreach (var permissionName in managerPermissions)
        {
            var permission = allPermissions.FirstOrDefault(p => p.Name == permissionName);
            if (permission != null)
            {
                await AssignPermissionToRole(managerRole, permission);
            }
        }

        // ReadOnly gets view-only permissions
        var readOnlyPermissions = new List<string>
        {
            "ViewUsers", "ViewRoles", "ViewAllTodos", "ViewStatistics"
        };

        foreach (var permissionName in readOnlyPermissions)
        {
            var permission = allPermissions.FirstOrDefault(p => p.Name == permissionName);
            if (permission != null)
            {
                await AssignPermissionToRole(readOnlyRole, permission);
            }
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Permissions seeded and assigned successfully.");
    }

    private async Task AssignPermissionToRole(ApplicationRole role, Permission permission)
    {
        // Check if permission is already assigned to role
        var existingAssignment = await _context.RolePermissions
            .FirstOrDefaultAsync(rp => rp.RoleId == role.Id && rp.PermissionId == permission.Id);

        if (existingAssignment == null)
        {
            await _context.RolePermissions.AddAsync(new RolePermission
            {
                RoleId = role.Id,
                PermissionId = permission.Id
            });

            // Also add as claim for easier access control
            await _roleManager.AddClaimAsync(role, new Claim("Permission", permission.Name));
        }
    }

    private async Task SeedAdminUserAsync()
    {
        _logger.LogInformation("Seeding admin user...");

        var adminEmail = _appConfig.Value.DefaultAdminEmail ?? "admin@todo.com";
        var adminPassword = _appConfig.Value.DefaultAdminPassword ?? "@likh0rs4nD"; // Should be changed immediately in production

        var adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            NormalizedEmail = adminEmail.ToUpper(),
            NormalizedUserName = adminEmail.ToUpper(),
            FirstName = "System",
            LastName = "Administrator",
            EmailConfirmed = true,
            PhoneNumberConfirmed = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            DateOfBirth = new DateTime(1980, 1, 1)
        };

        var result = await _userManager.CreateAsync(adminUser, adminPassword);
        if (!result.Succeeded)
        {
            throw new Exception($"Failed to create admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        // Assign admin user to Admin role
        await _userManager.AddToRoleAsync(adminUser, "Admin");

        _logger.LogInformation("Admin user created successfully.");
    }

    private async Task SeedSampleUsersAsync()
    {
        _logger.LogInformation("Seeding sample users...");

        // Sample users with different roles
        var sampleUsers = new List<(string Email, string FirstName, string LastName, DateTime DOB, string Role, string Password)>
        {
            ("user@todo.com", "Regular", "User", new DateTime(1985, 5, 15), "User", "@likh0rs4nD"),
            ("manager@todo.com", "Team", "Manager", new DateTime(1979, 8, 22), "Manager", "@likh0rs4nD"),
            ("readonly@todo.com", "View", "Only", new DateTime(1990, 3, 10), "ReadOnly", "@likh0rs4nD")
        };

        foreach (var (email, firstName, lastName, dob, role, password) in sampleUsers)
        {
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                NormalizedEmail = email.ToUpper(),
                NormalizedUserName = email.ToUpper(),
                FirstName = firstName,
                LastName = lastName,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                DateOfBirth = dob
            };

            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                _logger.LogWarning($"Failed to create sample user {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                continue;
            }

            // Assign user to role
            await _userManager.AddToRoleAsync(user, role);
        }

        _logger.LogInformation("Sample users created successfully.");
    }
}