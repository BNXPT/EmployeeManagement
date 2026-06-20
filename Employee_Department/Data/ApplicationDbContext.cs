using Employee_Department.Models;
using Microsoft.EntityFrameworkCore;

namespace Employee_Department.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Department> Departments { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Manager> Managers { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<LeaveRequest> LeaveRequests { get; set; }
        public DbSet<Position> Positions { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Employee>()
                .HasIndex(e => e.NationalId).IsUnique();

            modelBuilder.Entity<Manager>()
                .HasIndex(m => m.NationalId).IsUnique();

            modelBuilder.Entity<Manager>()
                .HasIndex(m => m.DepartmentId).IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username).IsUnique();

            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Department)
                .WithMany(d => d.Employees)
                .HasForeignKey(e => e.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Manager>()
                .HasOne(m => m.Department)
                .WithOne(d => d.Manager!)
                .HasForeignKey<Manager>(m => m.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Position>()
                .HasIndex(p => p.PositionName)
                .IsUnique();

            // LeaveRequest computed column ignore
            modelBuilder.Entity<LeaveRequest>()
                .Ignore(l => l.DaysCount);

            modelBuilder.Entity<LeaveRequest>()
                .HasIndex(l => l.ApprovalToken).IsUnique();

            modelBuilder.Entity<LeaveRequest>()
                .HasOne(l => l.Employee)
                .WithMany()
                .HasForeignKey(l => l.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LeaveRequest>()
                .HasOne(l => l.RespondedByManager)
                .WithMany()
                .HasForeignKey(l => l.RespondedByManagerId)
                .OnDelete(DeleteBehavior.SetNull);

            //Soft delete filters
            modelBuilder.Entity<Department>().HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<Employee>().HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<Manager>().HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<User>().HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<Position>().HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<LeaveRequest>().HasQueryFilter(x => !x.IsDeleted);

        }
    }
}
