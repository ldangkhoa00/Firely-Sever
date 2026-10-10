using Microsoft.EntityFrameworkCore;
using HospitalUsers.Models;

namespace HospitalUsers.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        
        // Map các Class ở trên thành các Bảng (Tables) trong SQL Server
        public DbSet<AppUser> Users { get; set; }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<CsiRecord> CsiRecords { get; set; }
    }
}