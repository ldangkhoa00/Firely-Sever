using Microsoft.EntityFrameworkCore;
using HospitalUsers.Models;

namespace HospitalUsers.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        
        // Map các Class ở trên thành các Bảng (Tables) trong SQL Server
        public DbSet<AppUser> Users { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<CsiRecord> CsiRecords { get; set; }

        
        public DbSet<SecurityAuditLog> SecurityAuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<AppUser>()
                .Property(u => u.TokenVersion)
                .HasDefaultValue(1);
            
            modelBuilder.Entity<AppUser>()
                .HasIndex(u => u.StaffId)
                .IsUnique();

            
            modelBuilder.Entity<AppUser>()
                .HasIndex(u => u.HospitalEmail)
                .IsUnique()
                .HasFilter("[HospitalEmail] IS NOT NULL");

        }

    }
}