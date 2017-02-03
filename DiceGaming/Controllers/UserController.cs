using DiceGaming.Common;
using DiceGaming.Data;
using DiceGaming.Exceptions;
using DiceGaming.Models;
using DiceGaming.Requests;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using DiceGaming.Data.Entities;

namespace DiceGaming.Controllers
{
    [RoutePrefix("api/users")]
    public class UserController : ApiController
    {
        private bool IsTokenCorrect(int userId, string token)
        {
            using (var db = new DiceGamingDb())
            {
                if (db.Logins.FirstOrDefault(l => l.UserId == userId && object.Equals(l.Token, token)) == null)
                    return false;
            }
            return true;
        }

        [AllowAnonymous]
        [HttpPost]
        [Route("")]
        public Task<HttpResponseMessage> Register([FromBody] RegisterRequest registerRequest)
        {
            if (registerRequest.Username == null
                || registerRequest.Password == null
                || registerRequest.FullName == null
                || registerRequest.Email == null
                || registerRequest.Password.Length < 6
                || registerRequest.Password.Length > 20)
                throw new BadRequestException();

            User newUser;
            using (var db = new DiceGamingDb())
            {
                if (db.Users.FirstOrDefault(u => object.Equals(u.UserName, registerRequest.Username)) != null)
                    throw new ConflictException();


                var salt = CryptographyManager.GenerateSalt();
                var saltedPass = CryptographyManager.GenerateSHA256Hash(registerRequest.Password, salt);

                newUser = new User()
                {
                    UserName = registerRequest.Username,
                    Password = saltedPass,
                    Salt = salt,
                    FullName = registerRequest.FullName,
                    Email = registerRequest.Email,
                    VirtualMoney = 0
                };

                db.Users.Add(newUser);
                db.SaveChanges();
            }

            return Task.FromResult(Request.CreateResponse(HttpStatusCode.Created, new UserDTO()
            {
                Id = newUser.Id,
                Username = newUser.UserName,
                FullName = newUser.FullName,
                Email = newUser.Email
            }));
        }

        [HttpGet]
        [Route("{id}")]
        public Task<HttpResponseMessage> GetProfileData(int id)
        {
            string token = Request.Headers.GetValues("AuthToken").FirstOrDefault();
            if (!IsTokenCorrect(id, token))
                throw new ForbiddenException();

            User user;

            using (var db = new DiceGamingDb())
            {
                user = db.Users.FirstOrDefault(u => u.Id == id);

                if (user == null)
                    throw new NotFoundException();
            }

            return Task.FromResult(Request.CreateResponse(HttpStatusCode.OK, new UserDTO()
            {
                Id = user.Id,
                Username = user.UserName,
                FullName = user.FullName,
                Email = user.Email
            }));
        }

        [HttpPut]
        [Route("{id}/profile")]
        public Task<HttpResponseMessage> UpdataProfileData(int id, [FromBody] UpdateProfileRequest updProfileRequest)
        {
            string token = Request.Headers.GetValues("AuthToken").FirstOrDefault();
            if (!IsTokenCorrect(id, token))
                throw new ForbiddenException();

            if (updProfileRequest.FullName == null
                && updProfileRequest.Email == null)
                throw new BadRequestException();

            User user;

            using (var db = new DiceGamingDb())
            {
                user = db.Users.FirstOrDefault(u => u.Id == id);

                if (user == null)
                    throw new NotFoundException();

                if (!string.IsNullOrEmpty(updProfileRequest.FullName))
                    user.FullName = updProfileRequest.FullName;

                if (!string.IsNullOrEmpty(updProfileRequest.Email))
                    user.Email = updProfileRequest.Email;

                user.FullName = user.FullName;
                user.Email = user.Email;

                db.Users.AddOrUpdate(user);
                db.SaveChanges();
            }

            var response = new
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email
            };

            return Task.FromResult(Request.CreateResponse(HttpStatusCode.OK, response));
        }

        [HttpPut]
        [Route("{id}/password")]
        public Task<HttpResponseMessage> ChangePassword(int id, [FromBody] ChangePasswordRequest chgPasswordRequest)
        {
            string token = Request.Headers.GetValues("AuthToken").FirstOrDefault();
            if (!IsTokenCorrect(id, token))
                throw new ForbiddenException();

            if (chgPasswordRequest.OldPassword == null
                || chgPasswordRequest.NewPassword == null
                || chgPasswordRequest.OldPassword.Length < 6
                || chgPasswordRequest.OldPassword.Length > 20
                || chgPasswordRequest.NewPassword.Length < 6
                || chgPasswordRequest.NewPassword.Length > 20)
                throw new BadRequestException();

            User user;
            using (var db = new DiceGamingDb())
            {
                user = db.Users.FirstOrDefault(u => u.Id == id);

                if (user == null)
                    throw new NotFoundException();
            }

            using (var db = new DiceGamingDb())
            {
                var userForUpdate = db.Users.FirstOrDefault(u => u.Id == id);
                if (userForUpdate == null)
                    throw new NotFoundException();

                var saltedOldPassword = CryptographyManager.GenerateSHA256Hash(chgPasswordRequest.OldPassword, userForUpdate.Salt);

                if (!object.Equals(userForUpdate.Password, saltedOldPassword))
                    throw new BadRequestException();

                var saltedNewPassword = CryptographyManager.GenerateSHA256Hash(chgPasswordRequest.NewPassword, userForUpdate.Salt);

                userForUpdate.Password = saltedNewPassword;

                db.Users.AddOrUpdate(userForUpdate);
                db.SaveChanges();
            }

            return Task.FromResult(Request.CreateResponse(HttpStatusCode.NoContent));
        }

        [HttpDelete]
        [Route("{id}")]
        public Task<HttpResponseMessage> DeleteAccount(int id, [FromBody] DeleteAccountRequest delAccountRequest)
        {
            string token = Request.Headers.GetValues("AuthToken").FirstOrDefault();
            if (!IsTokenCorrect(id, token))
                throw new ForbiddenException();

            if (delAccountRequest.Password == null
                || delAccountRequest.Password.Length < 6
                || delAccountRequest.Password.Length > 20)
                throw new BadRequestException();

            User user;
            using (var db = new DiceGamingDb())
            {
                user = db.Users.FirstOrDefault(u => u.Id == id);
                if (user == null)
                    throw new BadRequestException();
            }

            var saltedPassword = CryptographyManager.GenerateSHA256Hash(delAccountRequest.Password, user.Salt);
            if (!object.Equals(user.Password, saltedPassword))
                throw new ForbiddenException();

            using (var db = new DiceGamingDb())
            {
                user = db.Users.FirstOrDefault(u => u.Id == id);

                if (user == null)
                    throw new NotFoundException();

                db.Users.Remove(user);
                db.SaveChanges();
            }

            return Task.FromResult(Request.CreateResponse(HttpStatusCode.NoContent));
        }

        [HttpPut]
        [Route("{id}/wallet")]
        public Task<HttpResponseMessage> AddMoney(int id, [FromBody] AddMoneyRequest addMoneyRequest)
        {
            string token = Request.Headers.GetValues("AuthToken").FirstOrDefault();
            if (!IsTokenCorrect(id, token))
                throw new ForbiddenException();

            if (addMoneyRequest.AddMoney == null)
                throw new BadRequestException();

            decimal money;
            if (!decimal.TryParse(addMoneyRequest.AddMoney, out money))
                throw new BadRequestException();

            User user;
            using (var db = new DiceGamingDb())
            {
                user = db.Users.FirstOrDefault(u => u.Id == id);
                if (user == null)
                    throw new NotFoundException();

                user.VirtualMoney += money;

                db.Users.AddOrUpdate(user);
                db.SaveChanges();
            }

            var response = new
            {
                Balance = user.VirtualMoney
            };

            return Task.FromResult(Request.CreateResponse(HttpStatusCode.OK, response));
        }

        [HttpGet]
        [Route("{id}/wallet")]
        public Task<HttpResponseMessage> GetBalance(int id)
        {
            string token = Request.Headers.GetValues("AuthToken").FirstOrDefault();
            if (!IsTokenCorrect(id, token))
                throw new ForbiddenException();

            User user;

            using (var db = new DiceGamingDb())
            {
                user = db.Users.FirstOrDefault(u => u.Id == id);

                if (user == null)
                    throw new NotFoundException();
            }

            var response = new
            {
                Balance = user.VirtualMoney
            };

            return Task.FromResult(Request.CreateResponse(HttpStatusCode.OK, response));
        }
    }
}
