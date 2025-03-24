﻿// <auto-generated />
using System;
using MedicalInfoSystem.DB;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace MedicalInfoSystem.Migrations
{
    [DbContext(typeof(DbConnect))]
    [Migration("20241102142117_SecondSave")]
    partial class SecondSave
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.10")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("MedicalInfoSystem.Models.Consultation", b =>
                {
                    b.Property<Guid>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("inspectionId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("specialityId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("ID");

                    b.HasIndex("inspectionId")
                        .IsUnique();

                    b.ToTable("Consultations");
                });

            modelBuilder.Entity("MedicalInfoSystem.Models.Diagnosis", b =>
                {
                    b.Property<Guid>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid?>("InspectionID")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("description")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<Guid>("icdDiagnosisId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("type")
                        .HasColumnType("int");

                    b.HasKey("ID");

                    b.HasIndex("InspectionID");

                    b.ToTable("Diagnoses");
                });

            modelBuilder.Entity("MedicalInfoSystem.Models.Doctor", b =>
                {
                    b.Property<Guid>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("birthDate")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("createTime")
                        .HasColumnType("datetime2");

                    b.Property<string>("email")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("fullName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("gender")
                        .HasColumnType("int");

                    b.Property<string>("password")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("phoneNumber")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<Guid>("speciality")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("ID");

                    b.ToTable("Doctors");
                });

            modelBuilder.Entity("MedicalInfoSystem.Models.Icd10RecordModel", b =>
                {
                    b.Property<Guid>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("code")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("createTime")
                        .HasColumnType("datetime2");

                    b.Property<string>("fullName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("ID");

                    b.ToTable("Icd10");
                });

            modelBuilder.Entity("MedicalInfoSystem.Models.Inspection", b =>
                {
                    b.Property<Guid>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("anamnesis")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("complaints")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("conclusion")
                        .HasColumnType("int");

                    b.Property<DateTime>("date")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("deathDate")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("doctorId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime?>("nextVisitDate")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("patientId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid?>("previousInspectionId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("treatment")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("ID");

                    b.ToTable("Inspections");
                });

            modelBuilder.Entity("MedicalInfoSystem.Models.InspectionComment", b =>
                {
                    b.Property<Guid>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("consultationId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("content")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("ID");

                    b.HasIndex("consultationId")
                        .IsUnique();

                    b.ToTable("InspectionComments");
                });

            modelBuilder.Entity("MedicalInfoSystem.Models.Patient", b =>
                {
                    b.Property<Guid>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("birthDate")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("createTime")
                        .HasColumnType("datetime2");

                    b.Property<string>("fullName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("gender")
                        .HasColumnType("int");

                    b.HasKey("ID");

                    b.ToTable("Patients");
                });

            modelBuilder.Entity("MedicalInfoSystem.Models.SpecialityModel", b =>
                {
                    b.Property<Guid>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("createTime")
                        .HasColumnType("datetime2");

                    b.Property<string>("fullName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("ID");

                    b.ToTable("Speciality");
                });

            modelBuilder.Entity("MedicalInfoSystem.Models.Consultation", b =>
                {
                    b.HasOne("MedicalInfoSystem.Models.Inspection", null)
                        .WithOne("consultation")
                        .HasForeignKey("MedicalInfoSystem.Models.Consultation", "inspectionId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("MedicalInfoSystem.Models.Diagnosis", b =>
                {
                    b.HasOne("MedicalInfoSystem.Models.Inspection", null)
                        .WithMany("diagnoses")
                        .HasForeignKey("InspectionID");
                });

            modelBuilder.Entity("MedicalInfoSystem.Models.InspectionComment", b =>
                {
                    b.HasOne("MedicalInfoSystem.Models.Consultation", null)
                        .WithOne("comment")
                        .HasForeignKey("MedicalInfoSystem.Models.InspectionComment", "consultationId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("MedicalInfoSystem.Models.Consultation", b =>
                {
                    b.Navigation("comment")
                        .IsRequired();
                });

            modelBuilder.Entity("MedicalInfoSystem.Models.Inspection", b =>
                {
                    b.Navigation("consultation")
                        .IsRequired();

                    b.Navigation("diagnoses");
                });
#pragma warning restore 612, 618
        }
    }
}
