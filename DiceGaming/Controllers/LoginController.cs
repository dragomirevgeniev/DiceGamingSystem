using DiceGaming.Common;
using DiceGaming.Data;
using DiceGaming.Exceptions;
using DiceGaming.Models;
using DiceGaming.Requests;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using DiceGaming.Data.Entities;

namespace DiceGaming.Controllers
{
    [RoutePrefix("api/logins")]
    public class LoginController : ApiController
    {
        [AllowAnonymous]
        [HttpPost]
        [Route("")]
        public Task<HttpResponseMessage> Login([FromBody] LoginRequest loginRequest)
        {
            User user;
            using (var db = new DiceGamingDb())
            {
                user = db.Users.FirstOrDefault(u => object.Equals(u.UserName, loginRequest.Username));
                if (user == null)
                    throw new NotFoundException();

                var saltedPassword = CryptographyManager.GenerateSHA256Hash(loginRequest.Password, user.Salt);

                if (!object.Equals(user.Password, saltedPassword))
                    throw new BadRequestException();
            }

            // create token. timestamp + GUID
            byte[] time = BitConverter.GetBytes(DateTime.Now.ToBinary());
            byte[] key = Guid.NewGuid().ToByteArray();
            string token = Convert.ToBase64String(time.Concat(key).ToArray());

            var login = new LoginDTO()
            {
                UserId = user.Id,
                Token = token
            };

            Login newLogin = new Login();

            using (var db = new DiceGamingDb())
            {
                if (db.Users.FirstOrDefault(u => u.Id == login.UserId) == null)
                    throw new NotFoundException();

                newLogin.UserId = login.UserId;
                newLogin.Token = login.Token;

                db.Logins.Add(newLogin);
                db.SaveChanges();
            }

            return Task.FromResult(Request.CreateResponse(HttpStatusCode.Created, new LoginDTO()
            {
                Id = newLogin.Id,
                UserId = newLogin.UserId,
                Token = newLogin.Token
            }));
        }

        [HttpDelete]
        [Route("{id}")]
        public Task<HttpResponseMessage> Logout(int id)
        {
            string token = Request.Headers.GetValues("AuthToken").FirstOrDefault();

            using (var db = new DiceGamingDb())
            {
                var login = db.Logins.FirstOrDefault(l => l.Id == id && object.Equals(l.Token, token));
                if (login == null)
                    throw new NotFoundException();

                db.Logins.Remove(login);
                db.SaveChanges();
            }

            return Task.FromResult(Request.CreateResponse(HttpStatusCode.NoContent));
        }
    }
}
