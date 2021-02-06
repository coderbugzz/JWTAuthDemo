using Dapper;
using JWTAuthDemo.Models;
using JWTAuthDemo.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace JWTAuthDemo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    
    public class AccountController : ControllerBase
    {
        private readonly IJWTAuthManager _authentication;

        public AccountController(IJWTAuthManager authentication)
        {
            _authentication = authentication;
        }

        [HttpPost("Login")]
        [AllowAnonymous]
        public IActionResult login([FromBody]LoginModel user)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Parameter is missing");
            }

            DynamicParameters dp_param = new DynamicParameters();
            dp_param.Add("email",user.email,DbType.String);
            dp_param.Add("password",user.password,DbType.String);
            dp_param.Add("retVal", DbType.String, direction: ParameterDirection.Output);
            var result = _authentication.Execute_Command<User>("sp_loginUser",dp_param);
            if (result.code == 200)
            {
                var token = _authentication.GenerateJWT(result.Data);
                return Ok(token);
            }
            return NotFound(result.Data);
        }

        [HttpGet("UserList")]
        [Authorize(Roles = "User")]
        public IActionResult getAllUsers()
        {
            var result = _authentication.getUserList<User>();
            return Ok(result);
        }

        [HttpPost("Register")]
        [Authorize(Roles = "Admin")]
        public IActionResult Register([FromBody]User user)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Parameter is missing");
            }

            DynamicParameters dp_param = new DynamicParameters();
            dp_param.Add("email", user.Email, DbType.String);
            dp_param.Add("username", user.Username, DbType.String);
            dp_param.Add("password", user.Password, DbType.String);
            dp_param.Add("role", user.Role, DbType.String);
            dp_param.Add("retVal", DbType.String, direction: ParameterDirection.Output);
            var result = _authentication.Execute_Command<User>("sp_registerUser", dp_param);
            if (result.code == 200)
            {
                return Ok(result);
            }
            
           
            return BadRequest(result);
        }

        [HttpDelete("Delete")]
        [Authorize(Roles = "Admin")]
        public IActionResult Delete(string id)
        {
            if (id == string.Empty)
            {
                return BadRequest("Parameter is missing");
            }

            DynamicParameters dp_param = new DynamicParameters();
            dp_param.Add("userid", id, DbType.String);
          
            dp_param.Add("retVal", DbType.String, direction: ParameterDirection.Output);
            var result = _authentication.Execute_Command<User>("sp_deleteUser", dp_param);
            if (result.code == 200)
            {
                return Ok(result);
            }
            return NotFound(result);
        }
    }
}
