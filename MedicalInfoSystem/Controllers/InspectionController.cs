using MedicalInfoSystem.DB;
using MedicalInfoSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Swashbuckle.AspNetCore.Annotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace MedicalInfoSystem.Controllers
{
    [ApiController]
    [Route("api/inspection")]
    public class InspectionController : ControllerBase
    {
        private readonly DbConnect dbData;
        private readonly IConfiguration dbconfiguration;

        public InspectionController(DbConnect context, IConfiguration configuration)
        {
            dbData = context;
            dbconfiguration = configuration;
        }

        [HttpGet("{id}")]
        [SwaggerResponse(200, "Inspection found and successfully extracted", typeof(InspectionModel))]
        [SwaggerResponse(401, "Unauthorized")]
        [SwaggerResponse(404, "Not Found")]
        [SwaggerResponse(500, "InternalServerError", typeof(ResponseModel))]
        [EndpointSummary("Get full information about specified inspection")]
        public IActionResult GetInspection(Guid id)
        {
            try
            {
                var doctorId = GetCurrentDoctorId();
                if (doctorId == null)
                {
                    return Unauthorized("Ошибка 401, вы не авторизированы");
                }

                var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                if (TokenBlackList.IsTokenDeactivated(token))
                {
                    return Unauthorized("Этот токен деактивирован");
                }

                var inspection = GetInspectionFromDatabase(id);

                if (inspection == null)
                {
                    return NotFound("Ошибка 404, осмотр не найден");
                }

                return Ok(inspection);
            }
            catch (Exception er)
            {
                return StatusCode(500, new ResponseModel("Ошибка 500", $"Ошибка: {er.Message}"));
            }
        }

        [HttpPut("{id}")]
        [SwaggerResponse(200, "Success")]
        [SwaggerResponse(400, "Invalid arguments")]
        [SwaggerResponse(401, "Unauthorized")]
        [SwaggerResponse(403, "User doesn't have editing rights (not the inspection author)")]
        [SwaggerResponse(404, "Patient not found")]
        [SwaggerResponse(500, "Internal server error", typeof(ResponseModel))]
        [EndpointSummary("Edit concrete inspection")]
        public IActionResult EditInspection(
            Guid id,
            [FromBody] InspectionEditModel editModel)
        {
            try
            {
                var doctorId = GetCurrentDoctorId();
                if (doctorId == null)
                {
                    return Unauthorized("Ошибка 401, вы не авторизированы");
                }

                var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                if (TokenBlackList.IsTokenDeactivated(token))
                {
                    return Unauthorized("Этот токен деактивирован");
                }

                if (editModel.nextVisitDate <= DateTime.Now)
                {
                    return BadRequest("Ошибка, поле nextVisitDate не должно быть в прошлом и настоящем");
                }

                if (editModel.deathDate.HasValue && editModel.deathDate > DateTime.Now)
                {
                    return BadRequest("Ошибка, поле deathDate не должно быть в будущем");
                }

                UpdateInspection(id, editModel);

                return Ok(editModel);
            }
            catch (Exception er)
            {
                return StatusCode(500, new ResponseModel("Ошибка 500", $"Ошибка: {er.Message}"));
            }
        }

        [HttpGet("{id}/chain")]
        [SwaggerResponse(200, "Success", typeof(InspectionPreviewModel))]
        [SwaggerResponse(400, "Bad Request")]
        [SwaggerResponse(401, "Unauthorized")]
        [SwaggerResponse(404, "Not Found")]
        [SwaggerResponse(500, "Internal server error", typeof(ResponseModel))]
        [EndpointSummary("Get medical inspection chain for root inspection")]
        public IActionResult GetChainInspection(Guid id)
        {
            try
            {
                var doctorId = GetCurrentDoctorId();
                if (doctorId == null)
                {
                    return Unauthorized("Ошибка 401, вы не авторизированы");
                }

                var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                if (TokenBlackList.IsTokenDeactivated(token))
                {
                    return Unauthorized("Этот токен деактивирован");
                }

                var initialInspection = GetInspectionChainFromDatabase(id);

                if (initialInspection == null)
                {
                    return NotFound("Ошибка 404");
                }

                return Ok(initialInspection);
            }
            catch (ArgumentException)
            {
                return BadRequest("Ошибка 400");
            }
            catch (Exception er)
            {
                return StatusCode(500, new ResponseModel("Ошибка 500", $"Ошибка: {er.Message}"));
            }
        }


        private InspectionModel GetInspectionFromDatabase(Guid inspectionId)
        {
            InspectionModel inspection = null;

            try
            {
                using (var connection = new SqlConnection("Server=(localdb)\\mssqllocaldb;Database=MedicalInfoSystem;Trusted_Connection=True;"))
                {
                    connection.Open();

                    string query = @"
                SELECT i.ID, i.createTime, i.date, i.anamnesis, i.complaints, i.treatment, i.conclusion, 
                       i.nextVisitDate, i.deathDate, i.previousInspectionId, 
                       p.ID AS PatientId, p.fullName AS PatientName, p.birthDate AS PatientBirthDate, p.gender AS PatientGender, p.createTime As PatientCreateTime,
                       d.phoneNumber AS DoctorPhone
                FROM Inspections i
                LEFT JOIN Patients p ON i.patientId = p.ID
                LEFT JOIN Doctors d ON i.doctorId = d.ID
                WHERE i.ID = @inspectionId";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@inspectionId", inspectionId);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var doctor = GetDoctorFromToken();
                                var genderString = reader.GetString(reader.GetOrdinal("PatientGender"));
                                if (!Enum.TryParse<Gender>(genderString, out var gender))
                                {
                                    throw new InvalidOperationException("Ошибка с полем gender");
                                }
                                var conclusionString = reader.GetString(reader.GetOrdinal("conclusion"));
                                if (!Enum.TryParse<Conclusion>(conclusionString, out var conclusion))
                                {
                                    throw new InvalidOperationException("Ошибка с полем conclusion");
                                }

                                inspection = new InspectionModel
                                {
                                    ID = reader.GetGuid(reader.GetOrdinal("ID")),
                                    CreateTime = reader.GetDateTime(reader.GetOrdinal("createTime")),
                                    Date = reader.GetDateTime(reader.GetOrdinal("date")),
                                    Anamnesis = reader.GetString(reader.GetOrdinal("anamnesis")),
                                    Complaints = reader.GetString(reader.GetOrdinal("complaints")),
                                    Treatment = reader.GetString(reader.GetOrdinal("treatment")),
                                    Conclusion = conclusion,
                                    NextVisitDate = reader.GetDateTime(reader.GetOrdinal("nextVisitDate")),
                                    DeathDate = reader.GetDateTime(reader.GetOrdinal("deathDate")),
                                    BaseInspectionId = new Guid("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
                                    PreviousInspectionId = reader.IsDBNull(reader.GetOrdinal("previousInspectionId")) ? (Guid?)null : reader.GetGuid(reader.GetOrdinal("previousInspectionId")),

                                    Patient = new PatientModel
                                    {
                                        ID = reader.GetGuid(reader.GetOrdinal("PatientId")),
                                        fullName = reader.GetString(reader.GetOrdinal("PatientName")),
                                        birthDate = reader.GetDateTime(reader.GetOrdinal("PatientBirthDate")),
                                        createTime = reader.GetDateTime(reader.GetOrdinal("PatientCreateTime")),
                                        gender = gender
                                    },

                                    Doctor = doctor,

                                    Diagnoses = GetDiagnosesForInspection(inspectionId),
                                    Consultations = GetConsultationsForInspection(inspectionId)
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при получении данных осмотра: {ex.Message}");
            }

            return inspection;
        }

        private List<InspectionConsultationModel> GetConsultationsForInspection(Guid inspectionId)
        {
            var consultations = new List<InspectionConsultationModel>();

            try
            {
                using (var connection = new SqlConnection("Server=(localdb)\\mssqllocaldb;Database=MedicalInfoSystem;Trusted_Connection=True;"))
                {
                    connection.Open();
                    string query = @"
                SELECT c.ID, c.createTime, c.inspectionId, c.specialityId,
                       com.ID AS CommentID, com.createTime AS CommentCreateTime, com.content AS CommentContent,
                       (SELECT COUNT(*) FROM InspectionComments WHERE ConsultationId = c.ID) AS CommentsCount
                FROM Consultations c
                LEFT JOIN InspectionComments com ON c.ID = com.ConsultationId
                WHERE c.inspectionId = @inspectionId";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@inspectionId", inspectionId);

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var doctor = GetDoctorFromToken();

                                Guid? specialityId = null;
                                if (!reader.IsDBNull(reader.GetOrdinal("specialityId")))
                                {
                                    specialityId = reader.GetGuid(reader.GetOrdinal("specialityId"));
                                }

                                string specialityFullName;
                                if (specialityId.HasValue)
                                {
                                    specialityFullName = GetSpecialityFullName(specialityId.Value);
                                }
                                else
                                {
                                    specialityFullName = "Unknown";
                                }

                                int commentsCount = reader.GetInt32(reader.GetOrdinal("CommentsCount"));

                                var consultation = new InspectionConsultationModel
                                {
                                    ID = reader.GetGuid(reader.GetOrdinal("ID")),
                                    CreateTime = reader.GetDateTime(reader.GetOrdinal("createTime")),
                                    InspectionId = reader.GetGuid(reader.GetOrdinal("inspectionId")),
                                    Speciality = new SpecialityModel
                                    {
                                        ID = specialityId ?? Guid.Empty,
                                        fullName = specialityFullName,
                                        createTime = reader.GetDateTime(reader.GetOrdinal("createTime"))
                                    },
                                    RootComment = new InspectionCommentModel
                                    {
                                        ID = reader.GetGuid(reader.GetOrdinal("CommentID")),
                                        CreateTime = reader.GetDateTime(reader.GetOrdinal("CommentCreateTime")),
                                        ParentId = new Guid("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
                                        Content = reader.GetString(reader.GetOrdinal("CommentContent")),
                                        Author = doctor
                                    },
                                    CommentsNumber = commentsCount
                                };

                                consultations.Add(consultation);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при получении консультаций: {ex.Message}");
            }

            return consultations;
        }

        private List<DiagnosisModel> GetDiagnosesForInspection(Guid inspectionId)
        {
            var diagnoses = new List<DiagnosisModel>();

            try
            {
                using (var connection = new SqlConnection("Server=(localdb)\\mssqllocaldb;Database=MedicalInfoSystem;Trusted_Connection=True;"))
                {
                    connection.Open();
                    string query = @"
                SELECT d.ID, d.createTime, d.icdDiagnosisId, d.description, d.type FROM Diagnoses d WHERE d.inspectionId = @inspectionId";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@inspectionId", inspectionId);

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {

                                var diagnosisId = reader.GetGuid(reader.GetOrdinal("ID"));
                                Guid? icdDiagnosisId = null;
                                if (!reader.IsDBNull(reader.GetOrdinal("icdDiagnosisId")))
                                {
                                    icdDiagnosisId = reader.GetGuid(reader.GetOrdinal("icdDiagnosisId"));
                                }


                                var (code, name) = icdDiagnosisId.HasValue ? GetIcd10Info(icdDiagnosisId.Value) : (null, null);
                                var typeString = reader.GetString(reader.GetOrdinal("type"));
                                if (!Enum.TryParse<DiagnosisType>(typeString, out var type))
                                {
                                    throw new InvalidOperationException("Ошибка с полем type");
                                }

                                var diagnosis = new DiagnosisModel
                                {
                                    ID = diagnosisId,
                                    createTime = reader.GetDateTime(reader.GetOrdinal("createTime")),
                                    code = code,
                                    name = name,
                                    description = reader.GetString(reader.GetOrdinal("description")),
                                    type = type
                                };

                                diagnoses.Add(diagnosis);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при получении диагнозов: {ex.Message}");
            }

            return diagnoses;
        }

        private DoctorModel GetDoctorFromToken()
        {
            var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var doctorIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "ID");

            Guid doctorId = Guid.Parse(doctorIdClaim.Value);

            try
            {
                using (var connection = new SqlConnection("Server=(localdb)\\mssqllocaldb;Database=MedicalInfoSystem;Trusted_Connection=True;"))
                {
                    connection.Open();

                    string query = @"
                SELECT ID, createTime, fullName, birthDate, gender, email, phoneNumber, speciality FROM Doctors WHERE ID = @doctorId";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@doctorId", doctorId);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var genderString = reader.GetString(reader.GetOrdinal("gender"));
                                if (!Enum.TryParse<Gender>(genderString, out var gender))
                                {
                                    throw new InvalidOperationException("Ошибка с полем gender");
                                }
                                return new DoctorModel
                                {
                                    Id = reader.GetGuid(reader.GetOrdinal("ID")),
                                    CreateTime = reader.GetDateTime(reader.GetOrdinal("createTime")),
                                    Name = reader.GetString(reader.GetOrdinal("fullName")),
                                    BirthDate = reader.GetDateTime(reader.GetOrdinal("birthDate")),
                                    Gender = gender,
                                    Email = reader.GetString(reader.GetOrdinal("email")),
                                    Phone = reader.GetString(reader.GetOrdinal("phoneNumber")),
                                    Speciality = reader.GetGuid(reader.GetOrdinal("speciality"))
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при получении данных доктора: {ex.Message}");
            }

            throw new Exception("Доктор не найден");
        }

        private Guid? GetCurrentDoctorId()
        {
            var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            if (string.IsNullOrEmpty(token))
            {
                return null;
            }

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var doctorId = jwtToken.Claims.FirstOrDefault(id => id.Type == "ID");

            if (doctorId != null)
            {
                return Guid.Parse(doctorId.Value);
            }
            else
            {
                return null;
            }

        }

        private (string Code, string Name) GetIcd10Info(Guid icdDiagnosisId)
        {
            try
            {
                using (var connection = new SqlConnection("Server=(localdb)\\mssqllocaldb;Database=MedicalInfoSystem;Trusted_Connection=True;"))
                {
                    connection.Open();

                    string query = @"SELECT code, fullName FROM Icd10 WHERE ID = @icdDiagnosisId";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@icdDiagnosisId", icdDiagnosisId);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var code = reader.GetString(reader.GetOrdinal("code"));
                                var name = reader.GetString(reader.GetOrdinal("fullName"));

                                return (code, name);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при получении данных из Icd10: {ex.Message}");
            }

            return (null, null);
        }

        private string GetSpecialityFullName(Guid specialityId)
        {
            try
            {
                using (var connection = new SqlConnection("Server=(localdb)\\mssqllocaldb;Database=MedicalInfoSystem;Trusted_Connection=True;"))
                {
                    connection.Open();

                    string query = @"SELECT fullName FROM Speciality WHERE ID = @specialityId";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@specialityId", specialityId);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return reader.GetString(reader.GetOrdinal("fullName"));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при получении данных специальности: {ex.Message}");
            }

            return "Unknown";
        }

        private void UpdateInspection(Guid inspectionId, InspectionEditModel editModel)
        {
            using (var connection = new SqlConnection("Server=(localdb)\\mssqllocaldb;Database=MedicalInfoSystem;Trusted_Connection=True;"))
            {
                connection.Open();

                string updateInspectionQuery = @"
            UPDATE Inspections 
            SET anamnesis = @anamnesis, complaints = @complaints, treatment = @treatment, conclusion = @conclusion,
                nextVisitDate = @nextVisitDate, deathDate = @deathDate
            WHERE ID = @inspectionId";

                using (var updateCommand = new SqlCommand(updateInspectionQuery, connection))
                {
                    updateCommand.Parameters.AddWithValue("@inspectionId", inspectionId);
                    updateCommand.Parameters.AddWithValue("@anamnesis", editModel.anamnesis);
                    updateCommand.Parameters.AddWithValue("@complaints", editModel.complaints);
                    updateCommand.Parameters.AddWithValue("@treatment", editModel.treatment);
                    updateCommand.Parameters.AddWithValue("@conclusion", editModel.conclusion);
                    updateCommand.Parameters.AddWithValue("@nextVisitDate", editModel.nextVisitDate);
                    updateCommand.Parameters.AddWithValue("@deathDate", editModel.deathDate);

                    updateCommand.ExecuteNonQuery();
                }

                string deleteDiagnosesQuery = "DELETE FROM Diagnoses WHERE inspectionId = @inspectionId";
                using (var deleteCommand = new SqlCommand(deleteDiagnosesQuery, connection))
                {
                    deleteCommand.Parameters.AddWithValue("@inspectionId", inspectionId);
                    deleteCommand.ExecuteNonQuery();
                }

                string insertDiagnosisQuery = @"
            INSERT INTO Diagnoses (ID, inspectionId, icdDiagnosisId, description, type, createTime)
            VALUES (NEWID(), @inspectionId, @icdDiagnosisId, @description, @type, @createTime)";

                foreach (var diagnosis in editModel.diagnoses)
                {
                    using (var insertCommand = new SqlCommand(insertDiagnosisQuery, connection))
                    {
                        insertCommand.Parameters.AddWithValue("@inspectionId", inspectionId);
                        insertCommand.Parameters.AddWithValue("@icdDiagnosisId", diagnosis.icdDiagnosisId);
                        insertCommand.Parameters.AddWithValue("@description", diagnosis.description);
                        if (Enum.IsDefined(typeof(DiagnosisType), diagnosis.type))
                        {
                            insertCommand.Parameters.AddWithValue("@type", diagnosis.type.ToString());
                        }
                        else
                        {
                            throw new InvalidOperationException("Ошибка с полем type");
                        }
                        insertCommand.Parameters.AddWithValue("@createTime", DateTime.Now);
                        insertCommand.ExecuteNonQuery();
                    }
                }
            }
        }

        private InspectionPreviewModel GetInspectionChainFromDatabase(Guid initialInspectionId)
        {
            InspectionPreviewModel inspection = null;

            using (var connection = new SqlConnection("Server=(localdb)\\mssqllocaldb;Database=MedicalInfoSystem;Trusted_Connection=True;"))
            {
                connection.Open();

                string query = @"
            SELECT ins.ID, ins.createTime, ins.previousInspectionId, ins.date, ins.conclusion,
                   ins.doctorId, doc.fullName AS DoctorName, ins.patientId, pat.fullName AS PatientName
            FROM Inspections ins
            JOIN Doctors doc ON ins.doctorId = doc.ID
            JOIN Patients pat ON ins.patientId = pat.ID
            WHERE ins.previousInspectionId = @initialInspectionId";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@initialInspectionId", initialInspectionId);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var inspectionId = reader.GetGuid(reader.GetOrdinal("ID"));
                            var previousInspectionId = reader.IsDBNull(reader.GetOrdinal("previousInspectionId")) ? (Guid?)null : reader.GetGuid(reader.GetOrdinal("previousInspectionId"));
                            var conclusionString = reader.GetString(reader.GetOrdinal("conclusion"));
                            if (!Enum.TryParse<Conclusion>(conclusionString, out var conclusion))
                            {
                                throw new InvalidOperationException("Ошибка с полем conclusion");
                            }

                            inspection = new InspectionPreviewModel
                            {
                                Id = inspectionId,
                                createTime = reader.GetDateTime(reader.GetOrdinal("createTime")),
                                previousId = previousInspectionId,
                                date = reader.GetDateTime(reader.GetOrdinal("date")),
                                conclusion = conclusion,
                                doctorId = reader.GetGuid(reader.GetOrdinal("doctorId")),
                                doctor = reader.GetString(reader.GetOrdinal("DoctorName")),
                                patientId = reader.GetGuid(reader.GetOrdinal("patientId")),
                                patient = reader.GetString(reader.GetOrdinal("PatientName")),
                                diagnosis = GetDiagnosisForInspection(inspectionId, null),
                                hasChain = true,
                                hasNested = CheckHasNested(inspectionId)
                            };
                        }
                    }
                }
            }

            return inspection;
        }
        private DiagnosisModel GetDiagnosisForInspection(Guid inspectionId, string request)
        {
            using (var connection = new SqlConnection("Server=(localdb)\\mssqllocaldb;Database=MedicalInfoSystem;Trusted_Connection=True;"))
            {
                connection.Open();

                string query = @"
                SELECT diag.ID, diag.createTime, icd.code, icd.fullName, diag.description, diag.type
            FROM Diagnoses diag
                LEFT JOIN Icd10 icd ON diag.icdDiagnosisId = icd.ID
            WHERE diag.inspectionId = @inspectionId
              AND (icd.code LIKE @request OR icd.fullName LIKE @request)";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@inspectionId", inspectionId);
                    command.Parameters.AddWithValue("@request", $"%{request}%");

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var typeString = reader.GetString(reader.GetOrdinal("type"));
                            if (!Enum.TryParse<DiagnosisType>(typeString, out var type))
                            {
                                throw new InvalidOperationException("Ошибка с полем type");
                            }

                            return new DiagnosisModel
                            {
                                ID = reader.GetGuid(reader.GetOrdinal("ID")),
                                createTime = reader.GetDateTime(reader.GetOrdinal("createTime")),
                                code = reader.GetString(reader.GetOrdinal("code")),
                                name = reader.GetString(reader.GetOrdinal("fullName")),
                                description = reader.GetString(reader.GetOrdinal("description")),
                                type = type
                            };
                        }
                    }
                }
            }

            return null;
        }
        private bool CheckHasNested(Guid inspectionId)
        {
            using (var connection = new SqlConnection("Server=(localdb)\\mssqllocaldb;Database=MedicalInfoSystem;Trusted_Connection=True;"))
            {
                connection.Open();

                string query = "SELECT COUNT(*) FROM Inspections WHERE previousInspectionId = @inspectionId";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@inspectionId", inspectionId);
                    int count = (int)command.ExecuteScalar();
                    return count > 0;
                }
            }
        }
    }
}
