using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MediVerify.Models;

namespace MediVerify.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Patient> Patients { get; set; }
        public DbSet<Prescription> Prescriptions { get; set; }
        public DbSet<PrescriptionItem> PrescriptionItems { get; set; }
        public DbSet<Medicine> Medicines { get; set; }
        public DbSet<Inventory> Inventories { get; set; }
        public DbSet<SafetyAlert> SafetyAlerts { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<DispensingRecord> DispensingRecords { get; set; }
        public DbSet<DispensedItem> DispensedItems { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Patient>()
                .HasOne(p => p.ApplicationUser)
                .WithMany()
                .HasForeignKey(p => p.ApplicationUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Prescription>()
                .HasOne(p => p.Patient)
                .WithMany(p => p.Prescriptions)
                .HasForeignKey(p => p.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            // PrescriptionItem relationships
            builder.Entity<PrescriptionItem>()
                .HasOne(pi => pi.Prescription)
                .WithMany(p => p.Items)
                .HasForeignKey(pi => pi.PrescriptionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<PrescriptionItem>()
                .HasOne(pi => pi.Medicine)
                .WithMany()
                .HasForeignKey(pi => pi.MedicineId)
                .OnDelete(DeleteBehavior.Restrict);

            // DispensingRecord relationships
            builder.Entity<DispensingRecord>()
                .HasOne(dr => dr.Prescription)
                .WithMany()
                .HasForeignKey(dr => dr.PrescriptionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<DispensingRecord>()
                .HasOne(dr => dr.Pharmacist)
                .WithMany()
                .HasForeignKey(dr => dr.PharmacistId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<DispensedItem>()
                .HasOne(di => di.DispensingRecord)
                .WithMany(dr => dr.Items)
                .HasForeignKey(di => di.DispensingRecordId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<DispensedItem>()
                .HasOne(di => di.Medicine)
                .WithMany()
                .HasForeignKey(di => di.MedicineId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
