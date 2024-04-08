using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;
using Vonage.Messaging;
using Vonage;
using Microsoft.EntityFrameworkCore;

namespace RingServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TestController : Controller
    {
        private readonly RingDBContext _dbContext;

        public TestController(RingDBContext dbContext)
        {
            _dbContext = dbContext;
        }

        public class DecryptedRequest
        {
            public string PKey { get; set; }
            public string Number { get; set; }
            public string Id { get; set; }

            public DecryptedRequest(string pKey, string number, string id)
            {
                PKey = pKey;
                Number = number;
                Id = id;
            }
        }

        public class VerifyRequest
        {
            public string Code { get; set; }
            public string Number { get; set; }

            public VerifyRequest(string code, string number)
            {
                Code = code;
                Number = number;
            }
        }

        [HttpPost]
        [Route("/api/v1/auth/fakesignin")]
        public async Task<IActionResult> FakeSignIn([FromBody] DecryptedRequest requestBody)
        {
            if (requestBody == null)
            {
                return BadRequest("Outer invalid request payload");
            }
            else
            {
                if (requestBody != null)
                {
                    var userInfo = requestBody;
                    if (userInfo != null)
                    {
                        if (_dbContext.Users.All(u => u.phoneNumber != userInfo.Number))
                        {
                            _dbContext.Users.Add(new User
                            {
                                phoneNumber = userInfo.Number,
                                deviceId = userInfo.Id,
                                verificationCode = string.Empty,
                                verificationExpire = DateTime.MinValue,
                                lastLogin = DateTime.MinValue,
                                publicKey = userInfo.PKey
                            });
                        }
                        else
                        {
                            _dbContext.Users.Where(u => u.phoneNumber == userInfo.Number).First().verificationCode = string.Empty;
                            _dbContext.Users.Where(u => u.phoneNumber == userInfo.Number).First().lastLogin = DateTime.MinValue;
                        }
                        try
                        {
                            await _dbContext.SaveChangesAsync();
                        }
                        catch (Exception ex)
                        {
                            return StatusCode(500, "An error occurred while saving the user to the database");
                        }
                        return Ok();
                    }
                    else
                    {
                        return BadRequest("Invalid request payload");
                    }
                }
                else
                {
                    return BadRequest("Invalid request payload");
                }
            }
        }


        [HttpPost]
        [Route("/api/v1/auth/fakesendcode")]
        public async Task<IActionResult> SendSMS([FromBody] string requestBody)
        {
            if (requestBody == null)
            {
                return BadRequest("Invalid request payload");
            }
            else
            {
                try
                {
                    var to = requestBody;

                    string verificationCode = new Random().Next(10000000, 99999999).ToString();
                    _dbContext.Users.Where(u => u.phoneNumber == to).First().verificationCode = verificationCode;
                    _dbContext.Users.Where(u => u.phoneNumber == to).First().verificationExpire = DateTime.Now.AddMinutes(5);
                    try
                    {
                        await _dbContext.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        return StatusCode(500, "An error occurred while saving the user to the database");
                    }

                    string message = "Your verification code is: " + verificationCode;

                    return Ok(message);
                }
                catch (Exception e)
                {
                    return BadRequest("SMS not sent: " + e.Message);
                }
            }
        }

        [HttpPost]
        [Route("/api/v1/auth/fakeverifycode")]
        public async Task<IActionResult> VerifyCode([FromBody] VerifyRequest requestBody)
        {
            if (requestBody == null)
            {
                return BadRequest("Invalid request payload");
            }
            else
            {
                var info = requestBody;
                if (info == null)
                {
                    return BadRequest("Invalid request payload");
                }

                string code = info.Code;
                string number = info.Number;

                string verificationCode = _dbContext.Users.Where(u => u.phoneNumber == number).Select(u => u.verificationCode).First();
                DateTime verificationExpire = _dbContext.Users.Where(u => u.phoneNumber == number).Select(u => u.verificationExpire).First();

                if (DateTime.Now > verificationExpire)
                {
                    return BadRequest("Verification code expired");
                }
                if (code == verificationCode)
                {
                    _dbContext.Users.Where(u => u.phoneNumber == info.Number).First().lastLogin = DateTime.Now;
                    try
                    {
                        await _dbContext.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        return StatusCode(500, "An error occurred while saving the user to the database");
                    }
                    return Ok("info");
                }
                else
                {
                    return BadRequest("Invalid code");
                }
            }
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            var users = await _dbContext.Users.ToListAsync();
            if (users == null)
            {
                return NotFound();
            }
            return users;
        }
    }
}
