using Azure.Core;
using MedicalInfoSystem.DB;
using MedicalInfoSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Swashbuckle.AspNetCore.Annotations;

namespace MedicalInfoSystem.Controllers
{
    [ApiController]
    [Route("api/dictionary")]
    public class DictionaryController : ControllerBase
    {
        private readonly DbConnect dbData;
        private readonly IConfiguration dbconfiguration;

        public DictionaryController(DbConnect context, IConfiguration configuration)
        {
            dbData = context;
            dbconfiguration = configuration;
        }

        [HttpGet("speciality")]
        [SwaggerResponse(200, "Specialties paged list retrieved", typeof(SpecialtiesPagedListModel))]
        [SwaggerResponse(400, "Invalid arguments for filtration/pagination")]
        [SwaggerResponse(500, "InternalServerError", typeof(ResponseModel))]
        [EndpointSummary("Get specialities list")]
        public IActionResult GetSpeciality(
             string name = null,
             int page = 1,
             int size = 5)
        {
            if (page <= 0 || size <= 0)
            {
                return BadRequest("Ошибка 400");
            }

            try
            {
                var speciality = GetSpecialityFromDatabase();

                if (!string.IsNullOrEmpty(name))
                {
                    speciality = speciality.Where(p => p.fullName.Contains(name, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                var paged = speciality
                    .Skip((page - 1) * size)
                    .Take(size)
                    .ToList();

                var responce = new SpecialtiesPagedListModel
                {
                    specialties = paged,
                    pagination = new PageInfoModel
                    {
                        size = size,
                        count = speciality.Count(),
                        current = page
                    }
                };

                return Ok(responce);
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

        [HttpGet("icd10")]
        [SwaggerResponse(200, "Searching result extracted", typeof(Icd10SearchModel))]
        [SwaggerResponse(400, "Some fields in request are invalid")]
        [SwaggerResponse(500, "InternalServerError", typeof(ResponseModel))]
        [EndpointSummary("Search for diagnoses in ICD-10 dictionary")]
        public IActionResult GetIcd10(
             string request = null,
             int page = 1,
             int size = 5)
        {
            if (page <= 0 || size <= 0)
            {
                return BadRequest("Ошибка 400");
            }

            try
            {
                var icd10 = GetIcd10FromDatabase(request);

                var pagedRecords = icd10
                    .Skip((page - 1) * size)
                    .Take(size)
                    .ToList();

                var responce = new Icd10SearchModel
                {
                    records = pagedRecords,
                    pagination = new PageInfoModel
                    {
                        size = size,
                        count = icd10.Count(),
                        current = page
                    }
                };

                return Ok(responce);
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

        [HttpGet("icd/roots")]
        [SwaggerResponse(200, "Root ICD-10 elements retrieved", typeof(List<Icd10RecordModel>))]
        [SwaggerResponse(500, "InternalServerError", typeof(ResponseModel))]
        [EndpointSummary("Get root ICD-10 elements")]
        public IActionResult GetIcdRootDiagnoses()
        {
            try
            {

                var roots = GetIcd10RootsFromDatabase();

                return Ok(roots);
            }
            catch (Exception er)
            {
                return StatusCode(500, new ResponseModel("Ошибка 500", $"{er.Message}"));
            }
        }

        private List<SpecialityModel> GetSpecialityFromDatabase()
        {
            var speciality = new List<SpecialityModel>();

            try
            {
                using (var connection = new SqlConnection("Server=(localdb)\\mssqllocaldb;Database=MedicalInfoSystem;Trusted_Connection=True;"))
                {
                    connection.Open();
                    string query = "SELECT fullName, ID, createTime FROM Speciality";

                    using (var command = new SqlCommand(query, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var model = new SpecialityModel
                                {
                                    fullName = reader.GetString(reader.GetOrdinal("fullName")),
                                    ID = reader.GetGuid(reader.GetOrdinal("ID")),
                                    createTime = reader.GetDateTime(reader.GetOrdinal("createTime")),
                                };

                                speciality.Add(model);
                            }
                        }
                    }
                }
            }
            catch (Exception er)
            {
                throw new Exception($"Ошибка при получении данных специальностей: {er.Message}");
            }

            return speciality;
        }
        private List<Icd10RecordModel> GetIcd10FromDatabase(string request)
        {
            var icd10 = new List<Icd10RecordModel>();

            try
            {
                using (var connection = new SqlConnection("Server=(localdb)\\mssqllocaldb;Database=MedicalInfoSystem;Trusted_Connection=True;"))
                {
                    connection.Open();

                    string query = @"SELECT ID, createTime, code, fullName FROM Icd10 WHERE code LIKE @request OR fullName LIKE @request";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@request", $"%{request}%");

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var model = new Icd10RecordModel
                                {
                                    ID = reader.GetGuid(reader.GetOrdinal("ID")),
                                    createTime = reader.GetDateTime(reader.GetOrdinal("createTime")),
                                    code = reader.GetString(reader.GetOrdinal("code")),
                                    fullName = reader.GetString(reader.GetOrdinal("fullName"))
                                };

                                icd10.Add(model);
                            }
                        }
                    }
                }
            }
            catch (Exception er)
            {
                throw new Exception($"Ошибка при получении данных ICD-10: {er.Message}");
            }

            return icd10;
        }
        private List<Icd10RecordModel> GetIcd10RootsFromDatabase()
        {
            var roots = new List<Icd10RecordModel>();

            try
            {
                using (var connection = new SqlConnection("Server=(localdb)\\mssqllocaldb;Database=MedicalInfoSystem;Trusted_Connection=True;"))
                {
                    connection.Open();

                    string query = @"SELECT ID, createTime, code, fullName FROM Icd10 WHERE parentId IS NULL ORDER BY CASE
                                WHEN code = 'I' THEN 1
                                WHEN code = 'II' THEN 2
                                WHEN code = 'III' THEN 3
                                WHEN code = 'IV' THEN 4
                                WHEN code = 'V' THEN 5
                                WHEN code = 'VI' THEN 6
                                WHEN code = 'VII' THEN 7
                                WHEN code = 'VIII' THEN 8
                                WHEN code = 'IX' THEN 9
                                WHEN code = 'X' THEN 10
                                WHEN code = 'XI' THEN 11
                                WHEN code = 'XII' THEN 12
                                WHEN code = 'XIII' THEN 13
                                WHEN code = 'XIV' THEN 14
                                WHEN code = 'XV' THEN 15
                                WHEN code = 'XVI' THEN 16
                                WHEN code = 'XVII' THEN 17
                                WHEN code = 'XVIII' THEN 18
                                WHEN code = 'XIX' THEN 19
                                WHEN code = 'XX' THEN 20
                                WHEN code = 'XXI' THEN 21
                                WHEN code = 'XXII' THEN 22 END";

                    using (var command = new SqlCommand(query, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var model = new Icd10RecordModel
                                {
                                    ID = reader.GetGuid(reader.GetOrdinal("ID")),
                                    createTime = reader.GetDateTime(reader.GetOrdinal("createTime")),
                                    code = reader.GetString(reader.GetOrdinal("code")),
                                    fullName = reader.GetString(reader.GetOrdinal("fullName"))
                                };

                                model.code = model.code switch
                                {
                                    "I" => "A00-B99",
                                    "II" => "C00-D48",
                                    "III" => "D50-D89",
                                    "IV" => "E00-E90",
                                    "V" => "F00-F99",
                                    "VI" => "G00-G99",
                                    "VII" => "H00-H59",
                                    "VIII" => "H60-H95",
                                    "IX" => "I00-I99",
                                    "X" => "J00-J99",
                                    "XI" => "K00-K93",
                                    "XII" => "L00-L99",
                                    "XIII" => "M00-M99",
                                    "XIV" => "N00-N99",
                                    "XV" => "O00-O99",
                                    "XVI" => "P00-P96",
                                    "XVII" => "Q00-Q99",
                                    "XVIII" => "R00-R99",
                                    "XIX" => "S00-T98",
                                    "XX" => "U00-U85",
                                    "XXI" => "V01-Y98",
                                    "XXII" => "Z00-Z99",
                                    _ => model.code 
                                };

                                roots.Add(model);
                            }
                        }
                    }
                }
            }
            catch (Exception er)
            {
                throw new Exception($"Ошибка при получении корневых диагнозов: {er.Message}");
            }

            return roots;
        }

    }
}
