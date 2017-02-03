using DiceGaming.Data;
using DiceGaming.Exceptions;
using DiceGaming.Models;
using DiceGaming.Requests;
using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using DiceGaming.Data.Entities;

namespace DiceGaming.Controllers
{
    [RoutePrefix("api/users/{userId}/games")]
    public class GameController : ApiController
    {
        private static readonly Random random = new Random();

        private bool IsTokenCorrect(int userId, string token)
        {
            using (var db = new DiceGamingDb())
            {
                if (db.Logins.FirstOrDefault(l => l.UserId == userId && object.Equals(l.Token, token)) == null)
                    return false;
            }
            return true;
        }

        private int DiceRoll()
        {
            int dice1 = random.Next(1, 7);
            int dice2 = random.Next(1, 7);

            return dice1 + dice2;
        }

        private decimal StakeMultiplier(int bet)
        {
            // Sum | Chances | Multiplier
            //  2  |  1/36   | 36/1 = 36
            //  3  |  2/36   | 36/2 = 18
            //  4  |  3/36   | 36/3 = 12
            //  5  |  4/36   | 36/4 = 9
            //  6  |  5/36   | 36/5 = 7.2
            //  7  |  6/36   | 36/6 = 6
            //  8  |  5/36   | 36/5 = 7.2
            //  9  |  4/36   | 36/4 = 9
            // 10  |  3/36   | 36/3 = 12
            // 11  |  2/36   | 36/2 = 18
            // 12  |  1/36   | 36/1 = 36
            switch (bet)
            {
                case 2:
                case 12:
                    return 36;
                case 3:
                case 11:
                    return 18;
                case 4:
                case 10:
                    return 12;
                case 5:
                case 9:
                    return 9;
                case 6:
                case 8:
                    return 7.2M;
                case 7:
                    return 6;
                default:
                    return 0;
            }
        }

        [HttpPost]
        [Route("")]
        public Task<HttpResponseMessage> CreateGame(int userId, [FromBody] GameRequest gameRequest)
        {
            string token = Request.Headers.GetValues("AuthToken").FirstOrDefault();
            if (!IsTokenCorrect(userId, token))
                throw new ForbiddenException();

            if (gameRequest.Bet == null
                || gameRequest.Stake == null)
                throw new BadRequestException();

            int userBet;
            if (!int.TryParse(gameRequest.Bet, out userBet))
                throw new BadRequestException();

            decimal stake;
            if (!decimal.TryParse(gameRequest.Stake, out stake))
                throw new BadRequestException();

            User user;
            using (var db = new DiceGamingDb())
            {
                user = db.Users.FirstOrDefault(u => u.Id == userId);

                if (user == null)
                    throw new NotFoundException();
            }

            if (userBet < 2 || userBet > 12)
                throw new BadRequestException();

            if (user.VirtualMoney < stake)
                throw new BadRequestException();

            decimal win = 0;
            int roll = DiceRoll();

            if (roll == userBet)
            {
                decimal multiplier = StakeMultiplier(userBet);
                win = stake * multiplier;
                win -= win / 10; // taxes :D
            }

            var game = new Game()
            {
                UserId = userId,
                DiceSumBet = userBet,
                DiceSumResult = roll,
                Stake = stake,
                Win = win,
                CreationDate = DateTime.Now
            };

            using (var db = new DiceGamingDb())
            {
                db.Games.Add(game);
                db.SaveChanges();
            }

            decimal newMoney = user.VirtualMoney - stake + win;

            using (var db = new DiceGamingDb())
            {
                user = db.Users.FirstOrDefault(u => u.Id == userId);
                if (user == null)
                    throw new NotFoundException();

                user.VirtualMoney = newMoney;

                db.Users.AddOrUpdate(user);
                db.SaveChanges();
            }

            var response = new
            {
                BetId = game.Id,
                Bet = game.DiceSumBet,
                Stake = game.Stake,
                Win = game.Win,
                Timestamp = game.CreationDate
            };

            return Task.FromResult(Request.CreateResponse(HttpStatusCode.OK, response));
        }

        [HttpGet]
        [Route("")]
        public Task<HttpResponseMessage> GetGames(int userId, [FromUri] string skip, [FromUri] string take, [FromUri] string orderBy, [FromUri] string filter = "none")
        {
            string token = Request.Headers.GetValues("AuthToken").FirstOrDefault();
            if (!IsTokenCorrect(userId, token))
                throw new ForbiddenException();

            int skipNum;
            if (!int.TryParse(skip, out skipNum))
                throw new BadRequestException();

            int takeNum;
            if (!int.TryParse(take, out takeNum))
                throw new BadRequestException();

            if (skipNum < 0 || takeNum < 0)
                throw new BadRequestException();

            if (!object.Equals(orderBy, "time")
                && !object.Equals(orderBy, "win"))
                throw new BadRequestException();

            if (!object.Equals(filter, "win")
                && !object.Equals(filter, "lose")
                && !object.Equals(filter, "none"))
                throw new BadRequestException();

            Func<Game, bool> betFilter;
            if (object.Equals(filter, "win"))
                betFilter = new Func<Game, bool>(g => g.UserId == userId && g.Win != 0);
            else if (object.Equals(filter, "lose"))
                betFilter = new Func<Game, bool>(g => g.UserId == userId && g.Win == 0);
            else betFilter = new Func<Game, bool>(g => g.UserId == userId);

            List<Game> games;
            using (var db = new DiceGamingDb())
            {
                var user = db.Users.FirstOrDefault(u => u.Id == userId);
                if (user == null)
                    throw new NotFoundException();

                games = db.Games.Where(betFilter).Skip(skipNum).Take(takeNum).ToList();
            }

            if (object.Equals(orderBy, "win"))
            {
                games = (from g in games.ToList()
                         orderby g.Win descending
                         select g).ToList();
            }
            else
            {
                games = (from g in games.ToList()
                         orderby g.CreationDate descending
                         select g).ToList();
            }

            return Task.FromResult(Request.CreateResponse(HttpStatusCode.OK, from g in games
                                                                             select new
                                                                             {
                                                                                 CreationDate = g.CreationDate,
                                                                                 Stake = g.Stake,
                                                                                 Win = g.Win
                                                                             }));
        }

        [HttpGet]
        [Route("{id}")]
        public Task<HttpResponseMessage> GetGame(int userId, int id)
        {
            string token = Request.Headers.GetValues("AuthToken").FirstOrDefault();
            if (!IsTokenCorrect(userId, token))
                throw new ForbiddenException();

            Game game;
            using (var db = new DiceGamingDb())
            {
                game = db.Games.FirstOrDefault(b => b.Id == id);

                if (game == null)
                    throw new NotFoundException();
            }

            if (game.UserId != userId)
                throw new ForbiddenException();

            return Task.FromResult(Request.CreateResponse(HttpStatusCode.OK, new GameDTO()
            {
                Id = game.Id,
                UserId = game.UserId,
                Bet = game.DiceSumBet,
                Stake = game.Stake,
                Win = game.Win,
                ActualRoll = game.DiceSumResult,
                Timestamp = game.CreationDate
            }));
        }

        [HttpDelete]
        [Route("{id}")]
        public Task<HttpResponseMessage> DeleteGame(int userId, int id)
        {
            string token = Request.Headers.GetValues("AuthToken").FirstOrDefault();
            if (!IsTokenCorrect(userId, token))
                throw new ForbiddenException();

            using (var db = new DiceGamingDb())
            {
                var game = db.Games.FirstOrDefault(b => b.Id == id);
                if (game == null)
                    throw new NotFoundException();

                if (game.UserId != userId)
                    throw new ForbiddenException();

                if (DateTime.Now.AddMinutes(-1) > game.CreationDate)
                    throw new ForbiddenException();

                var user = game.User;
                user.VirtualMoney = user.VirtualMoney - game.Win + game.Stake;

                db.Users.AddOrUpdate(user);
                db.Games.Remove(game);
                db.SaveChanges();
            }

            return Task.FromResult(Request.CreateResponse(HttpStatusCode.NoContent));
        }
    }
}
