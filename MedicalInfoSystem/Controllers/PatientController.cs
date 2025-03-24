using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MedicalInfoSystem.DB;
using MedicalInfoSystem.Models;
using System.Linq.Expressions;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Identity;
using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel;
using Azure.Core;

namespace MedicalInfoSystem.Controllers
{
    [ApiController]
    [Route("api/patient")]
    public class PatientController : ControllerBase
    {
        private readonly DbConnect dbData;
        private readonly IConfiguration dbconfiguration;

        public PatientController(DbConnect context, IConfiguration configuration)
        {
            dbData = context;
            dbconfiguration = configuration;
        }

        [HttpPost]
        [SwaggerResponse(200, "Patient was registered", typeof(Guid))]
        [SwaggerResponse(400, "Invalid arguments")]
        [SwaggerResponse(401, "Unauthorized")]
        [SwaggerResponse(500, "InternalServerError", typeof(ResponseModel))]
        [EndpointSummary("Create new patient")]
        public IActionResult CreatePatient([FromBody] PatientCreateModel createModel)
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

                if (string.IsNullOrEmpty(createModel.fullName))
                {
                    return BadRequest("Ошибка 400, поле name пустое");
                }

                var newPatient = new Patient()
                {
                    ID = Guid.NewGuid(),
                    fullName = createModel.fullName,
                    birthDate = createModel.birthDate,
                    gender = createModel.gender
                };

                dbData.Patients.Add(newPatient);
                dbData.SaveChanges();

                return Ok(newPatient.ID);
            }
            catch (Exception er)
            {
                return StatusCode(500, new ResponseModel("Ошибка 500", $"{er.Message}"));
            }
        }


        [HttpPost("{id}/inspections")]
        [SwaggerResponse(200, "Success", typeof(Guid))]
        [SwaggerResponse(400, "Bad Request")]
        [SwaggerResponse(401, "Unauthorized")]
        [SwaggerResponse(500, "InternalServerError", typeof(ResponseModel))]
        [EndpointSummary("Create inspection for specified patient")]
        public IActionResult CreateInspections(Guid id, [FromBody] InspectionCreateModel inspectionModel)
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

                var patient = dbData.Patients.Find(id);
                if (patient == null)
                {
                    return BadRequest("Пациент не найден");
                }

                if (!Enum.TryParse(inspectionModel.conclusion.ToString(), out Conclusion Conclusion))
                {
                    throw new InvalidOperationException("Ошибка с полем type");
                }

                if (inspectionModel.nextVisitDate <= DateTime.Now)
                {
                    return BadRequest("Ошибка, поле nextVisitDate не должно быть в прошлом и настоящем");
                }

                if (inspectionModel.deathDate > DateTime.Now)
                {
                    return BadRequest("Ошибка, поле deathDate не должно быть в будущем");
                }

                var newInspection = new Inspection
                {
                    ID = Guid.NewGuid(),
                    patientId = id,
                    doctorId = doctorId.Value,
                    date = inspectionModel.date,
                    anamnesis = inspectionModel.anamnesis,
                    complaints = inspectionModel.complaints,
                    treatment = inspectionModel.treatment,
                    conclusion = Conclusion,
                    nextVisitDate = inspectionModel.nextVisitDate,
                    deathDate = inspectionModel.deathDate,
                    previousInspectionId = inspectionModel.previousInspectionId,
                    diagnoses = new List<Diagnosis>(),
                    consultation = null
                };

                if (inspectionModel.diagnoses != null)
                {
                    foreach (var diagnosisModel in inspectionModel.diagnoses)
                    {
                        if (!Enum.TryParse(diagnosisModel.type.ToString(), out DiagnosisType type))
                        {
                            throw new InvalidOperationException("Ошибка с полем type");
                        }
                        var diagnosis = new Diagnosis
                        {
                            ID = Guid.NewGuid(),
                            icdDiagnosisId = diagnosisModel.icdDiagnosisId,
                            description = diagnosisModel.description,
                            type = type
                        };
                        newInspection.diagnoses.Add(diagnosis);
                    }
                }

                if (inspectionModel.conclusion != null)
                {
                    var consultations = new Consultation
                    {
                        ID = Guid.NewGuid(),
                        specialityId = inspectionModel.consultation.specialityId,
                        comment = new InspectionComment
                        {
                            ID = Guid.NewGuid(),
                            content = inspectionModel.consultation.comment?.content
                        }
                    };
                    newInspection.consultation = consultations;
                }

                dbData.Inspections.Add(newInspection);
                dbData.SaveChanges();

                return Ok(newInspection.ID);
            }
            catch (Exception er)
            {
                return StatusCode(500, new ResponseModel("Ошибка 500", $"{er.Message}"));
            }

        }


        [HttpGet("{id}")]
        [SwaggerResponse(200, "Success", typeof(PatientModel))]
        [SwaggerResponse(401, "Unauthorized")]
        [SwaggerResponse(404, "Not Found")]
        [SwaggerResponse(500, "InternalServerError", typeof(ResponseModel))]
        [EndpointSummary("Get patient card")]
        public IActionResult GetPatientById(Guid id)
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

                var patient = dbData.Patients.Find(id);
                if (patient == null)
                {
                    return NotFound("Ошибка 404, пациент не найден");
                }

                var patientModel = new PatientModel
                {
                    ID = patient.ID,
                    createTime = patient.createTime,
                    fullName = patient.fullName,
                    birthDate = patient.birthDate,
                    gender = patient.gender
                };

                return Ok(patientModel);
            }
            catch (Exception er)
            {
                return StatusCode(500, new ResponseModel("Ошибка 500", $"{er.Message}"));
            }
        }

        [HttpGet]
        [SwaggerResponse(200, "Patients paged list retrieved", typeof(PatientPagedListModel))]
        [SwaggerResponse(400, "Invalid arguments for filtration/pagination/sorting")]
        [SwaggerResponse(401, "Unauthorized")]
        [SwaggerResponse(500, "InternalServerError", typeof(ResponseModel))]
        [EndpointSummary("Get patients list")]
        public IActionResult GetPatients(
            string name = null,
            SortingOptions sorting = SortingOptions.NameAsc,
            bool scheduledVisits = false,
            bool onlyMine = false,
            [FromQuery] Conclusion[] conclusions = null,
            int page = 1,
            int size = 5)
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

                var patients = GetPatientsFromDatabase(conclusions, onlyMine, doctorId.Value);

                if (!string.IsNullOrEmpty(name))
                {
                    patients = patients.Where(p => p.fullName.Contains(name, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                patients = SortPatients(patients, sorting.ToString());

                var pagedPatients = patients
                    .Skip((page - 1) * size)
                    .Take(size)
                    .ToList();

                var response = new PatientPagedListModel
                {
                    patients = pagedPatients,
                    pagination = new PageInfoModel
                    {
                        size = size,
                        count = patients.Count,
                        current = page
                    }
                };

                return Ok(response);
            }
            catch (ArgumentException)
            {
                return BadRequest("Ошибка 400");
            }
            catch (Exception er)
            {
                return StatusCode(500, new ResponseModel("Ошибка 500", $"{er.Message}"));
            }
        }


        [HttpGet("{id}/inspections/search")]
        [SwaggerResponse(200, "Patients inspections list retrieved", typeof(List<InspectionShortModel>))]
        [SwaggerResponse(401, "Unauthorized")]
        [SwaggerResponse(404, "Patient not found")]
        [SwaggerResponse(500, "Internal server error", typeof(ResponseModel))]
        [EndpointSummary("Search for patient medical inspections without child inspections")]
        public IActionResult SearchInspections(Guid id, string request = null)
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

                var inspections = GetInspectionsForPatientSearch(id, request);

                if (inspections.Count == 0 || inspections == null)
                {
                    return NotFound("Ошибка 404, Пациент не найдем");
                }

                return Ok(inspections);
            }
            catch (Exception er)
            {
                return StatusCode(500, new ResponseModel("Ошибка 500", $"{er.Message}"));
            }
        }

        [HttpGet("{id}/inspections")]
        [SwaggerResponse(200, "Patients inspections list retrieved", typeof(InspectionPagedListModel))]
        [SwaggerResponse(400, "Invalid arguments for filtration/pagination")]
        [SwaggerResponse(401, "Unauthorized")]
        [SwaggerResponse(404, "Patient not found")]
        [SwaggerResponse(500, "Internal server error", typeof(ResponseModel))]
        [EndpointSummary("Get a list of patient medical inspections")]
        public IActionResult GetPagedPatientInspections(Guid id, bool grouped = false, [FromQuery] string[] icdRoots = null, int page = 1, int size = 5)
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

                var inspections = GetPagedInspectionsFromDatabase(id);

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

        private List<PatientModel> GetPatientsFromDatabase(Conclusion[] conclusions, bool onlyMine, Guid doctorId)
        {
            var patients = new List<PatientModel>();

            try
            {
                using (var connection = new SqlConnection("Server=(localdb)\\mssqllocaldb;Database=MedicalInfoSystem;Trusted_Connection=True;"))
                {
                    connection.Open();

                    string conclusionCondition = conclusions != null && conclusions.Length > 0 ? "AND ins.conclusion IN (" + string.Join(", ", conclusions.Select(c => $"'{c}'")) + ")" : "";

                    string doctorCondition = onlyMine ? "AND ins.doctorId = @doctorId" : "";

                    string query = $@"
                        SELECT DISTINCT p.ID, p.createTime, p.fullName, p.birthDate, p.gender 
                        FROM Patients p
                        LEFT JOIN Inspections ins ON p.ID = ins.patientId
                        WHERE 1=1 {conclusionCondition} {doctorCondition}";

                    using (var command = new SqlCommand(query, connection))
                    {
                        if (onlyMine)
                        {
                            command.Parameters.AddWithValue("@doctorId", doctorId);
                        }

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var genderString = reader.GetString(reader.GetOrdinal("gender"));
                                if (!Enum.TryParse<Gender>(genderString, out var gender))
                                {
                                    throw new InvalidOperationException("Ошибка с полем gender");
                                }

                                var patient = new PatientModel
                                {
                                    ID = reader.GetGuid(reader.GetOrdinal("ID")),
                                    createTime = reader.GetDateTime(reader.GetOrdinal("createTime")),
                                    fullName = reader.GetString(reader.GetOrdinal("fullName")),
                                    birthDate = reader.GetDateTime(reader.GetOrdinal("birthDate")),
                                    gender = gender
                                };

                                patients.Add(patient);
                            }
                        }
                    }
                }
            }
            catch (Exception er)
            {
                throw new Exception($"Ошибка при получении данных пациентов: {er.Message}");
            }

            return patients;
        }

        private List<PatientModel> SortPatients(List<PatientModel> patients, string sorting)
        {
            return sorting switch
            {
                "NameAsc" => patients.OrderBy(p => p.fullName).ToList(),
                "NameDesc" => patients.OrderByDescending(p => p.fullName).ToList(),
                "CreateAsc" => patients.OrderBy(p => p.createTime).ToList(),
                "CreateDesc" => patients.OrderByDescending(p => p.createTime).ToList(),
                _ => patients.OrderBy(p => p.fullName).ToList()
            };
        }

        private List<InspectionShortModel> GetInspectionsForPatientSearch(Guid patientId, string request)
        {
            var inspections = new List<InspectionShortModel>();

            using (var connection = new SqlConnection("Server=(localdb)\\mssqllocaldb;Database=MedicalInfoSystem;Trusted_Connection=True;"))
            {
                connection.Open();

                string query = @"SELECT ins.ID, ins.createTime, ins.date FROM Inspections ins WHERE ins.patientId = @patientId";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@patientId", patientId);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var inspectionId = reader.GetGuid(reader.GetOrdinal("ID"));

                            var inspection = new InspectionShortModel
                            {
                                ID = inspectionId,
                                createTime = reader.GetDateTime(reader.GetOrdinal("createTime")),
                                date = reader.GetDateTime(reader.GetOrdinal("date")),
                                diagnosis = GetDiagnosisForInspection(inspectionId, request) 
                            };

                            if (inspection.diagnosis != null)
                            {
                                inspections.Add(inspection);
                            }
                        }
                    }
                }
            }

            return inspections;
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

        private List<InspectionPreviewModel> GetPagedInspectionsFromDatabase(Guid patientId)
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
            JOIN Patients pat ON ins.patientId = pat.ID
            WHERE ins.patientId = @patientId";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@patientId", patientId);

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

                            if (inspection.diagnosis != null)
                            {
                                inspections.Add(inspection);
                            }
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
                    inspection.hasChain = true;
                    inspection.hasNested = chainDictionary[inspection.Id].Count > 0;
                    inspection.diagnosis = inspection.diagnosis; 
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
    }
}
