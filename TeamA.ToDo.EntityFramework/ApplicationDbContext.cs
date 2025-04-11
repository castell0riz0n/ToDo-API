using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TeamA.ToDo.Core.Models;
using TeamA.ToDo.Core.Models.Expenses;
using TeamA.ToDo.Core.Models.Todo;
using TeamA.ToDo.EntityFramework.EntityConfigurations;
using TeamA.ToDo.EntityFramework.EntityConfigurations.Expenses;
using TeamA.ToDo.EntityFramework.EntityConfigurations.Todo;

namespace TeamA.ToDo.EntityFramework;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<UserActivity> UserActivities { get; set; }

    // Todo related
    public DbSet<TodoTask> TodoTasks { get; set; }
    public DbSet<RecurrenceInfo> RecurrenceInfos { get; set; }
    public DbSet<TaskCategory> TaskCategories { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<TaskTag> TaskTags { get; set; }
    public DbSet<TaskReminder> TaskReminders { get; set; }
    public DbSet<TodoNote> TodoNotes { get; set; }

    // Expense related
    public DbSet<Expense> Expenses { get; set; }
    public DbSet<ExpenseCategory> ExpenseCategories { get; set; }
    public DbSet<PaymentMethod> PaymentMethods { get; set; }
    public DbSet<ExpenseTag> ExpenseTags { get; set; }
    public DbSet<ExpenseRecurrence> ExpenseRecurrences { get; set; }
    public DbSet<Budget> Budgets { get; set; }
    public DbSet<BudgetAlertSetting> BudgetAlertSettings { get; set; }


    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure the RefreshToken table
        builder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure the Permission table
        builder.Entity<Permission>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(200);
            entity.Property(e => e.Category).HasMaxLength(50);
        });

        // Configure the RolePermission table (many-to-many relationship)
        builder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(e => e.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.RoleId, e.PermissionId }).IsUnique();
        });

        // Configure the UserActivity table
        builder.Entity<UserActivity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(255);
            entity.Property(e => e.IpAddress).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Timestamp).IsRequired();
        });

        // Apply Todo entity configurations
        builder.ApplyConfiguration(new TodoTaskConfiguration());
        builder.ApplyConfiguration(new RecurrenceInfoConfiguration());
        builder.ApplyConfiguration(new TaskCategoryConfiguration());
        builder.ApplyConfiguration(new TagConfiguration());
        builder.ApplyConfiguration(new TaskTagConfiguration());
        builder.ApplyConfiguration(new TaskReminderConfiguration());
        builder.ApplyConfiguration(new TodoNoteConfiguration());

        // Apply Expense entity configurations
        builder.ApplyConfiguration(new ExpenseConfiguration());
        builder.ApplyConfiguration(new ExpenseCategoryConfiguration());
        builder.ApplyConfiguration(new PaymentMethodConfiguration());
        builder.ApplyConfiguration(new ExpenseTagConfiguration());
        builder.ApplyConfiguration(new ExpenseRecurrenceConfiguration());
        builder.ApplyConfiguration(new BudgetConfiguration());
        
        // Configure the many-to-many relationship with proper delete behavior
        builder.Entity<Expense>()
            .HasMany(e => e.Tags)
            .WithMany(t => t.Expenses)
            .UsingEntity(join => join.ToTable("ExpenseExpenseTag"));
        
        // Configure the join table to avoid multiple cascade paths
        builder.Entity("ExpenseExpenseTag", b =>
        {
            b.HasKey("ExpensesId", "TagsId");
        
            b.HasOne("TeamA.ToDo.Core.Models.Expenses.Expense", null)
                .WithMany()
                .HasForeignKey("ExpensesId")
                .OnDelete(DeleteBehavior.Cascade);
            
            b.HasOne("TeamA.ToDo.Core.Models.Expenses.ExpenseTag", null)
                .WithMany()
                .HasForeignKey("TagsId")
                .OnDelete(DeleteBehavior.NoAction);
        });
    }
}