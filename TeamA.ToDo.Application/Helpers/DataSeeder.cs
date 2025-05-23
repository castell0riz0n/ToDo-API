using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TeamA.ToDo.Application.Configuration;
using TeamA.ToDo.Core.Models;
using TeamA.ToDo.Core.Models.FeatureManagement;
using TeamA.ToDo.Core.Models.General;
using TeamA.ToDo.EntityFramework;

public class DataSeeder
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DataSeeder> _logger;
    private readonly IOptions<AppConfig> _appConfig;
    private readonly IConfiguration _configuration;

    public DataSeeder(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ApplicationDbContext context,
        ILogger<DataSeeder> logger,
        IOptions<AppConfig> appConfig,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
        _logger = logger;
        _appConfig = appConfig;
        _configuration = configuration;
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
                // Seed feature definitions
                await SeedFeatureDefinitionsAsync();

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

                await SeedFeatureRolesAsync();

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
                new Permission { Name = "ViewStatistics", Description = "Can view system statistics", Category = "SystemManagement" },
                new Permission { Name = "ViewAllExpenses", Description = "Can view all users' expenses", Category = "SystemManagement" }
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

        // Get admin settings from environment variables or generate secure password
        var adminSettings = AdminConfiguration.GetAdminSettings(_configuration);
        var adminEmail = adminSettings.DefaultAdminEmail;
        var adminPassword = adminSettings.DefaultAdminPassword;

        // Check if admin already exists
        var existingAdmin = await _userManager.FindByEmailAsync(adminEmail);
        if (existingAdmin != null)
        {
            _logger.LogInformation("Admin user already exists. Skipping admin creation.");
            return;
        }

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

        // Log the admin credentials if they were generated
        if (adminSettings.IsGeneratedPassword)
        {
            _logger.LogWarning("================== IMPORTANT ==================");
            _logger.LogWarning($"Admin user created with email: {adminEmail}");
            _logger.LogWarning($"Generated admin password: {adminPassword}");
            _logger.LogWarning("Please change this password immediately after first login!");
            _logger.LogWarning("To set a custom admin password, use the ADMIN_PASSWORD environment variable.");
            _logger.LogWarning("==============================================");
        }
        else
        {
            _logger.LogInformation($"Admin user created successfully with email: {adminEmail}");
        }
    }

    private async Task SeedSampleUsersAsync()
    {
        _logger.LogInformation("Seeding sample users...");

        // Generate secure passwords for sample users
        var userPassword = AdminConfiguration.GenerateSecurePassword();
        var managerPassword = AdminConfiguration.GenerateSecurePassword();
        var readOnlyPassword = AdminConfiguration.GenerateSecurePassword();

        // Sample users with different roles
        var sampleUsers = new List<(string Email, string FirstName, string LastName, DateTime DOB, string Role, string Password)>
        {
            ("user@todo.com", "Regular", "User", new DateTime(1985, 5, 15), "User", userPassword),
            ("manager@todo.com", "Team", "Manager", new DateTime(1979, 8, 22), "Manager", managerPassword),
            ("readonly@todo.com", "View", "Only", new DateTime(1990, 3, 10), "ReadOnly", readOnlyPassword)
        };

        _logger.LogWarning("================== SAMPLE USERS ==================");
        
        foreach (var (email, firstName, lastName, dob, role, password) in sampleUsers)
        {
            // Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                _logger.LogInformation($"User {email} already exists. Skipping.");
                continue;
            }

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
            
            _logger.LogWarning($"Created sample user - Email: {email}, Password: {password}");
        }

        _logger.LogWarning("Please change these passwords immediately!");
        _logger.LogWarning("=================================================");
        _logger.LogInformation("Sample users created successfully.");
    }

    private async Task SeedFeatureDefinitionsAsync()
    {
        // Check if we already have feature definitions
        if (await _context.FeatureDefinitions.AnyAsync())
            return;

        // Create our standard feature definitions
        var features = new List<FeatureDefinition>
        {
            new FeatureDefinition
            {
                Id = Guid.NewGuid(),
                Name = "TodoApp",
                Description = "Todo task management features",
                EnabledByDefault = true,
                CreatedAt = DateTime.UtcNow
            },
            new FeatureDefinition
            {
                Id = Guid.NewGuid(),
                Name = "ExpenseApp",
                Description = "Expense tracking and budgeting features",
                EnabledByDefault = true,
                CreatedAt = DateTime.UtcNow
            }
        };

        await _context.FeatureDefinitions.AddRangeAsync(features);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Feature definitions seeded successfully");
    }

    private async Task SeedFeatureRolesAsync()
    {
        // Get TodoApp feature
        var todoFeature = await _context.FeatureDefinitions
            .FirstOrDefaultAsync(f => f.Name == "TodoApp");

        // Get ExpenseApp feature
        var expenseFeature = await _context.FeatureDefinitions
            .FirstOrDefaultAsync(f => f.Name == "ExpenseApp");

        if (todoFeature == null || expenseFeature == null)
            return;

        // Get standard roles
        var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
        var userRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "User");

        if (adminRole == null || userRole == null)
            return;

        // Set up default role permissions

        // Admin has access to all features
        if (!await _context.RoleFeatureAccess.AnyAsync(rf =>
            rf.FeatureDefinitionId == todoFeature.Id && rf.RoleId == adminRole.Id))
        {
            await _context.RoleFeatureAccess.AddAsync(new RoleFeatureAccess
            {
                Id = Guid.NewGuid(),
                FeatureDefinitionId = todoFeature.Id,
                RoleId = adminRole.Id,
                IsEnabled = true,
                CreatedAt = DateTime.UtcNow
            });
        }

        if (!await _context.RoleFeatureAccess.AnyAsync(rf =>
            rf.FeatureDefinitionId == expenseFeature.Id && rf.RoleId == adminRole.Id))
        {
            await _context.RoleFeatureAccess.AddAsync(new RoleFeatureAccess
            {
                Id = Guid.NewGuid(),
                FeatureDefinitionId = expenseFeature.Id,
                RoleId = adminRole.Id,
                IsEnabled = true,
                CreatedAt = DateTime.UtcNow
            });
        }

        // Regular users also have access to all features by default
        if (!await _context.RoleFeatureAccess.AnyAsync(rf =>
            rf.FeatureDefinitionId == todoFeature.Id && rf.RoleId == userRole.Id))
        {
            await _context.RoleFeatureAccess.AddAsync(new RoleFeatureAccess
            {
                Id = Guid.NewGuid(),
                FeatureDefinitionId = todoFeature.Id,
                RoleId = userRole.Id,
                IsEnabled = true,
                CreatedAt = DateTime.UtcNow
            });
        }

        if (!await _context.RoleFeatureAccess.AnyAsync(rf =>
            rf.FeatureDefinitionId == expenseFeature.Id && rf.RoleId == userRole.Id))
        {
            await _context.RoleFeatureAccess.AddAsync(new RoleFeatureAccess
            {
                Id = Guid.NewGuid(),
                FeatureDefinitionId = expenseFeature.Id,
                RoleId = userRole.Id,
                IsEnabled = true,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Feature roles seeded successfully");
    }
}