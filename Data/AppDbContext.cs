using HospitalUsers.Models;
using Microsoft.EntityFrameworkCore;

namespace HospitalUsers.Data
{
    /// <summary>
    /// EF Core context cua HospitalUsers.
    /// Chu y: giu nguyen cau hinh schema hien tai de tranh phat sinh Migration ngoai y muon.
    /// </summary>
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // EF Core tu khoi tao cac DbSet. null! chi de lam ro rang buoc nullable
        // cho trinh bien dich, khong thay doi cau truc bang trong SQL Server.
        public DbSet<AppUser> Users { get; set; } = null!;
        public DbSet<Department> Departments { get; set; } = null!;
        public DbSet<Patient> Patients { get; set; } = null!;
        public DbSet<CsiRecord> CsiRecords { get; set; } = null!;
        public DbSet<SecurityAuditLog> SecurityAuditLogs { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Giu nguyen gia tri mac dinh va hai unique index da duoc su dung.
            // Neu muon doi do dai cot, CHECK constraint hoac foreign key,
            // can doi chieu model va migration truoc khi thuc hien.
            modelBuilder.Entity<AppUser>(entity =>
            {
                entity.Property(user => user.TokenVersion)
                    .HasDefaultValue(1);

                entity.HasIndex(user => user.StaffId)
                    .IsUnique();

                entity.HasIndex(user => user.HospitalEmail)
                    .IsUnique()
                    .HasFilter("[HospitalEmail] IS NOT NULL");
            });
        }
    }
}
