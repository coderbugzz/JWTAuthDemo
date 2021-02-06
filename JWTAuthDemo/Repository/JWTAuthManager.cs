using Dapper;
using JWTAuthDemo.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace JWTAuthDemo.Repository
{
    public class JWTAuthManager : IJWTAuthManager
    {
        private readonly IConfiguration _configuration;

        public JWTAuthManager(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public Response<string> GenerateJWT(User user)
        {

            //var currentUser = HttpContext.User;
            //DateTime TokenDate = new DateTime();

            //if (currentUser.HasClaim(c => c.Type == "Date"))
            //{
            //    TokenDate = DateTime.Parse(currentUser.Claims.FirstOrDefault(c => c.Type == "Date").Value);

            //}
            Response<string> response = new Response<string>();

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtAuth:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            //claim is used to add identity to JWT token
            var claims = new[] {
                new Claim(JwtRegisteredClaimNames.Sub, user.Username),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("roles", user.Role),
                new Claim("Date", DateTime.Now.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(_configuration["JwtAuth:Issuer"],
              _configuration["JwtAuth:Issuer"],
              claims,    //null original value
              expires: DateTime.Now.AddMinutes(120),
              signingCredentials: credentials);

            response.Data = new JwtSecurityTokenHandler().WriteToken(token); //return access token
            response.code = 200;
            response.message = "Token generated";
            return response;

        }

        public Response<T> Execute_Command<T>(string query, DynamicParameters sp_params)
        {
            Response<T> response = new Response<T>();
            using (IDbConnection dbConnection = new SqlConnection(_configuration.GetConnectionString("default")))
            {
                if (dbConnection.State == ConnectionState.Closed)
                    dbConnection.Open();
               using var transaction = dbConnection.BeginTransaction();
                try
                {
                    response.Data = dbConnection.Query<T>(query, sp_params, commandType: CommandType.StoredProcedure, transaction: transaction).FirstOrDefault();
                    response.code = sp_params.Get<int>("retVal"); //get output parameter value
                    response.message = "Success";
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    response.code = 500;
                    response.message = ex.Message;
                }
            }
            return response;
        }

        public Response<List<T>> getUserList<T>()
        {
            Response<List<T>> response = new Response<List<T>>();
            using IDbConnection db = new SqlConnection(_configuration.GetConnectionString("default"));
            string query = "Select userid,username,email,[role],reg_date FROM tbl_users";
            response.Data = db.Query<T>(query, null, commandType: CommandType.Text).ToList();
            return response;
        }
    }
}
