using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MedicalInfoSystem.DB;
using MedicalInfoSystem.Models;
using System.Linq.Expressions;
using Swashbuckle.AspNetCore.Annotations;

namespace MedicalInfoSystem.Controllers
{
    [ApiController]
    [Route("api/doctor")]
    public class DoctorController : ControllerBase
    {
        private readonly DbConnect dbData;
        private readonly IConfiguration dbconfiguration;

        public DoctorController(DbConnect context, IConfiguration configuration)
        {
            dbData = context;
            dbconfiguration = configuration;
        }

        
        [HttpPost("register")]
        [SwaggerResponse(200, "Doctor was registered", typeof(TokenResponseModel))]
        [SwaggerResponse(400, "Invalid arguments")]
        [SwaggerResponse(500, "InternalServerError", typeof(ResponseModel))]
        [EndpointSummary("Register new user")]
        public IActionResult Register([FromBody] DoctorRegisterModel registerModel)
        {
            try
            {
                if (dbData.Doctors.Any(e => e.email == registerModel.email))
                {
                    return BadRequest("Доктор с таким email уже существует");
                }

                var newDoctor = registerModel.ToDoctor();

                dbData.Doctors.Add(newDoctor);
                dbData.SaveChanges();

                var token = GenerateJwtToken(newDoctor);
                var tokenResponse = new TokenResponseModel(token);
                return Ok(tokenResponse);
            }
            catch(Exception er)
            {
                return StatusCode(500, new ResponseModel("Ошибка 500", $"{er.Message}"));
            }
        }

        [HttpPost("login")]
        [SwaggerResponse(200, "Doctor was registered", typeof(TokenResponseModel))]
        [SwaggerResponse(400, "Invalid arguments")]
        [SwaggerResponse(500, "InternalServerError", typeof(ResponseModel))]
        [EndpointSummary("Log in to the system")]
        public IActionResult Login([FromBody] LoginCredentialsModel loginModel)
        {
            try
            {
                if (string.IsNullOrEmpty(loginModel.email) || string.IsNullOrEmpty(loginModel.password))
                {
                    return BadRequest("Поле login или password пустое");
                }
                var doctor = dbData.Doctors.FirstOrDefault(e => e.email == loginModel.email);
                if (doctor == null)
                {
                    return BadRequest("Введите корректный email");
                }

                if (doctor.password != loginModel.password)
                {
                    return BadRequest("Введите корректный password");
                }

               
                var token = GenerateJwtToken(doctor);
                var tokenResponse = new TokenResponseModel(token);
                return Ok(tokenResponse);
            }
            catch (Exception er)
            {
                return StatusCode(500, new ResponseModel("Ошибка 500", $"{er.Message}"));
            }
        }

        [HttpPost("logout")]
        [SwaggerResponse(200, "Success", typeof(ResponseModel))]
        [SwaggerResponse(401, "Unauthorized")]
        [SwaggerResponse(500, "InternalServerError", typeof(ResponseModel))]
        [EndpointSummary("Log out system user")]
        public IActionResult Logout()
        {
            var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized("Ошибка 401");
            }

            if (TokenBlackList.IsTokenDeactivated(token))
            {
                return Ok(new ResponseModel("Информация", "Вы уже вышли из системы"));
            }

            try
            {
                TokenBlackList.DeactivateToken(token);

                var response = new ResponseModel("Успешно", "Вы вышли из системы");
                return Ok(response);
            }
            catch (Exception er)
            {
                return StatusCode(500, new ResponseModel("Ошибка 500", $"{er.Message}"));
            }
        }

        [HttpGet("profile")]
        [SwaggerResponse(200, "Success", typeof(DoctorModel))]
        [SwaggerResponse(401, "Unauthorized")]
        [SwaggerResponse(404, "Not Found")]
        [SwaggerResponse(500, "InternalServerError", typeof(ResponseModel))]
        [EndpointSummary("Get user profile")]
        public IActionResult GetProfile()
        {
            try
            {
                var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                if (TokenBlackList.IsTokenDeactivated(token))
                {
                    return Unauthorized("Этот токен деактивирован");
                }

                if (string.IsNullOrEmpty(token))
                {
                    return Unauthorized("Ошибка 401, токен не найден");
                }

                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);
                var doctorId = jwtToken.Claims.FirstOrDefault(id => id.Type == "ID");

                if (doctorId == null)
                {
                    return Unauthorized("Ошибка 401");
                }

                Guid id = Guid.Parse(doctorId.Value);
                var doctor = dbData.Doctors.Find(id);

                if (doctor == null)
                {
                    return NotFound("Ошибка 404, доктор не найдег");
                }

                var doctorModel = new DoctorModel
                {
                    Id = doctor.ID,
                    CreateTime = doctor.createTime,
                    Name = doctor.fullName,
                    BirthDate = doctor.birthDate,
                    Gender = doctor.gender, 
                    Phone = doctor.phoneNumber,
                    Email = doctor.email,
                    Speciality = doctor.speciality
                };

                return Ok(doctorModel);
            }
            catch (Exception er)
            {
                return StatusCode(500, new ResponseModel("Ошибка 500", $"{er.Message}"));
            }
        }

        [HttpPut("profile")]
        [SwaggerResponse(200, "Success")]
        [SwaggerResponse(400, "Bad Request")]
        [SwaggerResponse(401, "Unauthorized")]
        [SwaggerResponse(404, "Not Found")]
        [SwaggerResponse(500, "InternalServerError", typeof(ResponseModel))]
        [EndpointSummary("Edit user Profile")]
        public IActionResult UpdateProfile([FromBody] DoctorEditModel editModel)
        {
            try
            {
                var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                if (string.IsNullOrEmpty(token))
                {
                    return Unauthorized("Ошибка 401, токен не найден");
                }

                if (TokenBlackList.IsTokenDeactivated(token))
                {
                    return Unauthorized("Этот токен деактивирован");
                }

                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);
                var doctorId = jwtToken.Claims.FirstOrDefault(id => id.Type == "ID");

                if (doctorId == null)
                {
                    return Unauthorized("Ошибка 401");
                }

                Guid id = Guid.Parse(doctorId.Value);

                if (string.IsNullOrEmpty(editModel.email))
                {
                    return BadRequest("Ошибка 400, данных email нет или же они пустые");
                }

                var doctor = dbData.Doctors.Find(id);
                if (doctor == null)
                {
                    return NotFound("Ошибка 404, доктор не найдег");
                }

                doctor.email = editModel.email;
                doctor.fullName = editModel.name;
                doctor.birthDate = editModel.birthDate;
                doctor.gender = editModel.gender;
                doctor.phoneNumber = editModel.phone;

                dbData.SaveChanges();
                return Ok("Изменения успешно произведены");
            }
            catch(Exception er)
            {
                return StatusCode(500, new ResponseModel("Ошибка 500", $"{er.Message}"));
            }
        }

        private string GenerateJwtToken(Doctor doctor)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(dbconfiguration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim("ID", doctor.ID.ToString()),
                new Claim(ClaimTypes.Email, doctor.email)
            };

            var token = new JwtSecurityToken(
                issuer: dbconfiguration["Jwt:Issuer"],
                audience: dbconfiguration["Jwt:Issuer"],
                claims: claims,
                expires: DateTime.Now.AddHours(1),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

    }
}
