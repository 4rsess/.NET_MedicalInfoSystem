using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedicalInfoSystem.Migrations
{
    /// <inheritdoc />
    public partial class Save : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "passwordHash",
                table: "Doctors",
                newName: "password");

            migrationBuilder.AlterColumn<Guid>(
                name: "speciality",
                table: "Doctors",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<int>(
                name: "gender",
                table: "Doctors",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<Guid>(
                name: "ID",
                table: "Doctors",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<DateTime>(
                name: "createTime",
                table: "Doctors",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "Icd10",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    createTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    fullName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Icd10", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Inspections",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    patientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    doctorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    anamnesis = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    complaints = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    treatment = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    conclusion = table.Column<int>(type: "int", nullable: false),
                    nextVisitDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deathDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    previousInspectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inspections", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Patients",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    createTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    fullName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    birthDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    gender = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Patients", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Speciality",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    createTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    fullName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Speciality", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Consultations",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    inspectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    specialityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Consultations", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Consultations_Inspections_inspectionId",
                        column: x => x.inspectionId,
                        principalTable: "Inspections",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Diagnoses",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    icdDiagnosisId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    type = table.Column<int>(type: "int", nullable: false),
                    InspectionID = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Diagnoses", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Diagnoses_Inspections_InspectionID",
                        column: x => x.InspectionID,
                        principalTable: "Inspections",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "InspectionComments",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    consultationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    content = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InspectionComments", x => x.ID);
                    table.ForeignKey(
                        name: "FK_InspectionComments_Consultations_consultationId",
                        column: x => x.consultationId,
                        principalTable: "Consultations",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Consultations_inspectionId",
                table: "Consultations",
                column: "inspectionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Diagnoses_InspectionID",
                table: "Diagnoses",
                column: "InspectionID");

            migrationBuilder.CreateIndex(
                name: "IX_InspectionComments_consultationId",
                table: "InspectionComments",
                column: "consultationId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Diagnoses");

            migrationBuilder.DropTable(
                name: "Icd10");

            migrationBuilder.DropTable(
                name: "InspectionComments");

            migrationBuilder.DropTable(
                name: "Patients");

            migrationBuilder.DropTable(
                name: "Speciality");

            migrationBuilder.DropTable(
                name: "Consultations");

            migrationBuilder.DropTable(
                name: "Inspections");

            migrationBuilder.DropColumn(
                name: "createTime",
                table: "Doctors");

            migrationBuilder.RenameColumn(
                name: "password",
                table: "Doctors",
                newName: "passwordHash");

            migrationBuilder.AlterColumn<string>(
                name: "speciality",
                table: "Doctors",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<string>(
                name: "gender",
                table: "Doctors",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "ID",
                table: "Doctors",
                type: "int",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier")
                .Annotation("SqlServer:Identity", "1, 1");
        }
    }
}
