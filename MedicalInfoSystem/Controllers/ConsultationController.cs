using Microsoft.AspNetCore.Mvc;
using MedicalInfoSystem.DB;
using MedicalInfoSystem.Models;
using System.Linq;
using Microsoft.Data.SqlClient;
using System.IdentityModel.Tokens.Jwt;
using Swashbuckle.AspNetCore.Annotations;


namespace MedicalInfoSystem.Controllers
{
    [ApiController]
    [Route("api/consultation")]
    public class ConsultationController : ControllerBase
    {
        private readonly DbConnect dbData;
        private readonly IConfiguration dbconfiguration;
        public ConsultationController(DbConnect context, IConfiguration configuration)
        {
            dbData = context;
            dbconfiguration = configuration;
        }

        [HttpGet]
        [SwaggerResponse(200, "Inspection for consultation list retrieved", typeof(InspectionPagedListModel))]
        [SwaggerResponse(400, "Invalid arguments for filtration/pagination")]
        [SwaggerResponse(401, "Unauthorized")]
        [SwaggerResponse(404, "Not found")]
        [SwaggerResponse(500, "Internal server error", typeof(ResponseModel))]
        [EndpointSummary("Get a list of medical inspections for consultation")]
        public IActionResult GetMedicalInspectionList(bool grouped = false, [FromQuery] string[] icdRoots = null, int page = 1, int size = 5)
        {
            if (page <= 0 || size <= 0)
            {
                return BadRequest("Ошибка 400");
            }

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

                var inspections = GetAllInspectionsFromDatabase();

                if (grouped)
                {
                    inspections = GroupInspectionsByChain(inspections);
                }

                var pagedInspections = inspections
                    .Skip((page - 1) * size)
                    .Take(size)
                    .ToList();

                var result = new InspectionPagedListModel
                {
                    inspections = pagedInspections,
                    pagination = new PageInfoModel
                    {
                        size = size,
                        count = inspections.Count,
                        current = page
                    }
                };

                return Ok(result);
            }
            catch (Exception er)
            {
                return StatusCode(500, new ResponseModel("Ошибка 500", $"{er.Message}"));
            }
        }

        [HttpGet("{id}")]
        [SwaggerResponse(200, "Success", typeof(ConsultationModel))]
        [SwaggerResponse(401, "Unauthorized")]
        [SwaggerResponse(404, "Not found")]
        [SwaggerResponse(500, "Internal server error", typeof(ResponseModel))]
        [EndpointSummary("Get concrete consultation")]
        public IActionResult GetConcreteConsultation(Guid id)
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

                var consultation = GetConsultationFromDatabase(id, doctorId.Value);
                if (consultation == null)
                {
                    return NotFound("Ошибка 404, консультация не найдена");
                }

                return Ok(consultation);
            }
            catch (Exception er)
            {
                return StatusCode(500, new ResponseModel("Ошибка 500", $"{er.Message}"));
            }
        }


        [HttpPost("{id}/comment")]
        [SwaggerResponse(200, "Success", typeof(Guid))]
        [SwaggerResponse(400, "Invalid arguments")]
        [SwaggerResponse(401, "Unauthorized")]
        [SwaggerResponse(403, "User doesn't have add comment to consultation (unsuitable specialty and not the inspection author)")]
        [SwaggerResponse(404, "Consultation or parent comment not found")]
        [SwaggerResponse(500, "Internal server error", typeof(ResponseModel))]
        [EndpointSummary("Add comment to concrete consultation")]
        public IActionResult AddConcreteComment(Guid id, [FromBody] CommentCreateModel commentModel)
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

                if (string.IsNullOrEmpty(commentModel.content))
                {
                    return BadRequest("Ошибка 400");
                }

                var consultationExists = CheckConsultationExists(id);
                if (!consultationExists)
                {
                    return NotFound("Ошибка 404, консультация не найдена");
                }

                var newComment = AddCommentToDatabase(id, commentModel);

                return Ok(newComment);

            }
            catch (Exception er)
            {
                return StatusCode(500, new ResponseModel("Ошибка 500", $"{er.Message}"));
            }
        }


        [HttpPut("comment/{id}")]
        [SwaggerResponse(200, "Success")]
        [SwaggerResponse(400, "Invalid arguments")]
        [SwaggerResponse(401, "Unauthorized")]
        [SwaggerResponse(403, "User is not the author of the comment")]
        [SwaggerResponse(404, "Comment not found")]
        [SwaggerResponse(500, "Internal server error", typeof(ResponseModel))]
        [EndpointSummary("Edit comment")]
        public IActionResult EditComment(Guid id, [FromBody] InspectionCommentCreateModel commentModel)
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

                if (string.IsNullOrEmpty(commentModel.content))
                {
                    return BadRequest("Ошибка 400");
                }

                var editedComment = UpdateComment(id, commentModel);

                if (editedComment == null)
                {
                    return NotFound("Ошибка 404, комментарий не найден");
                }

                return Ok(editedComment);
            }
            catch(Exception er)
            {
                return StatusCode(500, new ResponseModel("Ошибка 500", $"{er.Message}"));
            }
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
        private List<InspectionPreviewModel> GetAllInspectionsFromDatabase()
        {
            var inspections = new List<InspectionPreviewModel>();

            using (var connection = new SqlConnection("Server=(localdb)\\mssqllocaldb;Database=MedicalInfoSystem;Trusted_Connection=True;"))
            {
                connection.Open();

                string query = @"
                SELECT ins.ID, ins.createTime, ins.previousInspectionId, ins.date, ins.conclusion, 
                       ins.doctorId, doc.fullName AS DoctorName, ins.patientId, pat.fullName AS PatientName
                FROM Inspections ins
                JOIN Doctors doc ON ins.doctorId = doc.ID
                JOIN Patients pat ON ins.patientId = pat.ID";

                using (var command = new SqlCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var inspectionId = reader.GetGuid(reader.GetOrdinal("ID"));
                            var previousInspectionId = reader.IsDBNull(reader.GetOrdinal("previousInspectionId")) ? (Guid?)null : reader.GetGuid(reader.GetOrdinal("previousInspectionId"));
                            var conclusionString = reader.GetString(reader.GetOrdinal("conclusion"));
                            if (!Enum.TryParse<Conclusion>(conclusionString, out var conclusion))
                            {
                                throw new InvalidOperationException("Ошибка с полем conclusion");
                            }

                            var inspection = new InspectionPreviewModel
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
                                hasChain = previousInspectionId.HasValue, 
                                hasNested = CheckHasNested(inspectionId) 
                            };

                            inspections.Add(inspection);
                        }
                    }
                }
            }

            return inspections;
        }
        private List<InspectionPreviewModel> GroupInspectionsByChain(List<InspectionPreviewModel> inspections)
        {
            var groupedInspections = new List<InspectionPreviewModel>();
            var chainDictionary = new Dictionary<Guid, List<InspectionPreviewModel>>();

            foreach (var inspection in inspections)
            {
                if (inspection.previousId.HasValue)
                {
                    if (!chainDictionary.ContainsKey(inspection.previousId.Value))
                    {
                        chainDictionary[inspection.previousId.Value] = new List<InspectionPreviewModel>();
                    }
                    chainDictionary[inspection.previousId.Value].Add(inspection);
                }
                else
                {
                    groupedInspections.Add(inspection);
                }
            }

            foreach (var inspection in groupedInspections)
            {
                if (chainDictionary.ContainsKey(inspection.Id))
                {
                    inspection.hasNested = true;
                    groupedInspections.AddRange(chainDictionary[inspection.Id]);
                }
            }

            return groupedInspections;
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
        private ConsultationModel GetConsultationFromDatabase(Guid consultationId, Guid doctorId)
        {
            ConsultationModel consultation = null;
            using (var connection = new SqlConnection("Server=(localdb)\\mssqllocaldb;Database=MedicalInfoSystem;Trusted_Connection=True;"))
            {
                connection.Open();

                var query = @"
                SELECT c.ID, c.createTime, c.inspectionId, 
                    s.ID AS SpecialityId, s.createTime AS SpecialityCreateTime, s.fullName AS SpecialityName,
                    com.ID AS CommentId, com.createTime AS CommentCreateTime, com.content AS CommentContent,
                    d.fullName AS DoctorName
                FROM Consultations c
                JOIN Speciality s ON c.specialityId = s.ID
                LEFT JOIN InspectionComments com ON com.consultationId = c.ID
                LEFT JOIN Doctors d ON d.ID = @doctorId
                WHERE c.ID = @consultationId";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@consultationId", consultationId);
                    command.Parameters.AddWithValue("@doctorId", doctorId);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (consultation == null)
                            {
                                consultation = new ConsultationModel
                                {
                                    id = reader.GetGuid(reader.GetOrdinal("ID")),
                                    createTime = reader.GetDateTime(reader.GetOrdinal("createTime")),
                                    inspectionId = reader.GetGuid(reader.GetOrdinal("inspectionId")),
                                    speciality = new SpecialityModel
                                    {
                                        ID = reader.GetGuid(reader.GetOrdinal("SpecialityId")),
                                        createTime = reader.GetDateTime(reader.GetOrdinal("SpecialityCreateTime")),
                                        fullName = reader.GetString(reader.GetOrdinal("SpecialityName"))
                                    },
                                    comments = new List<CommentModel>()
                                };
                            }

                            if (!reader.IsDBNull(reader.GetOrdinal("CommentId")))
                            {
                                consultation.comments.Add(new CommentModel
                                {
                                    id = reader.GetGuid(reader.GetOrdinal("CommentId")),
                                    createTime = reader.GetDateTime(reader.GetOrdinal("CommentCreateTime")),
                                    content = reader.GetString(reader.GetOrdinal("CommentContent")),
                                    authorId = doctorId,
                                    author = reader.GetString(reader.GetOrdinal("DoctorName"))
                                });
                            }
                        }
                    }
                }
            }
            return consultation;

        }
        private bool UpdateComment(Guid commentId, InspectionCommentCreateModel commentModel)
        {
            using (var connection = new SqlConnection("Server=(localdb)\\mssqllocaldb;Database=MedicalInfoSystem;Trusted_Connection=True;"))
            {
                connection.Open();

                string query = @"UPDATE InspectionComments SET content = @content WHERE ID = @commentId";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@commentId", commentId);
                    command.Parameters.AddWithValue("@content", commentModel.content);

                    int rowsAffected = command.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            }
        }
        private Guid AddCommentToDatabase(Guid consultationId, CommentCreateModel commentModel)
        {
            var newCommentId = Guid.NewGuid();

            using (var connection = new SqlConnection("Server=(localdb)\\mssqllocaldb;Database=MedicalInfoSystem;Trusted_Connection=True;"))
            {
                connection.Open();

                string query = @"
            INSERT INTO InspectionComments (ID, consultationId, content, createTime, parentId)
            VALUES (@id, @consultationId, @content, @createTime, @parentId)";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@id", newCommentId);
                    command.Parameters.AddWithValue("@consultationId", consultationId);
                    command.Parameters.AddWithValue("@content", commentModel.content);
                    command.Parameters.AddWithValue("@createTime", DateTime.Now);
                    command.Parameters.AddWithValue("@parentId", (object)commentModel.parentId ?? DBNull.Value);

                    command.ExecuteNonQuery();
                }
            }

            return newCommentId;
        }
        private bool CheckConsultationExists(Guid consultationId)
        {
            using (var connection = new SqlConnection("Server=(localdb)\\mssqllocaldb;Database=MedicalInfoSystem;Trusted_Connection=True;"))
            {
                connection.Open();

                string query = "SELECT COUNT(1) FROM Consultations WHERE ID = @consultationId";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@consultationId", consultationId);

                    int count = (int)command.ExecuteScalar();
                    return count > 0;
                }
            }
        }

    }

}
