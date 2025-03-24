using Microsoft.EntityFrameworkCore;
using MedicalInfoSystem.Models;
using System;

namespace MedicalInfoSystem.DB
{
    public class DbConnect : DbContext
    {
        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<SpecialityModel> Speciality { get; set; }
        public DbSet<Icd10RecordModel> Icd10 { get; set; }
        public DbSet<Inspection> Inspections { get; set; }
        public DbSet<Diagnosis> Diagnoses { get; set; }
        public DbSet<Consultation> Consultations { get; set; }
        public DbSet<InspectionComment> InspectionComments { get; set; }
        public DbConnect(DbContextOptions<DbConnect> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Doctor>()
                .Property(d => d.gender)
                .HasConversion(
                    v => v.ToString(),
                    v => (Gender)Enum.Parse(typeof(Gender), v)
                );

            modelBuilder.Entity<Patient>()
                .Property(d => d.gender)
                .HasConversion(
                    v => v.ToString(),
                    v => (Gender)Enum.Parse(typeof(Gender), v)
                );

            modelBuilder.Entity<Inspection>()
                .Property(i => i.conclusion)
                .HasConversion(
                    v => v.ToString(),
                    v => (Conclusion)Enum.Parse(typeof(Conclusion), v)
                );

            modelBuilder.Entity<Diagnosis>()
                .Property(d => d.type)
                .HasConversion(
                    v => v.ToString(),
                    v => (DiagnosisType)Enum.Parse(typeof(DiagnosisType), v)
                );

            base.OnModelCreating(modelBuilder);
        }
    }
}
