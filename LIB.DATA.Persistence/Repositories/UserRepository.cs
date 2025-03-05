using LIB.API.Application.Contracts.Persistence;
using LIB.API.Domain;
using LIBPROPERTY.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;
using System.Threading.Tasks;

namespace LIB.API.Persistence.Repositories
{
    public class UserRepository : GenericRepository<Users>, IUserRepository
    {
        private readonly LIBAPIDbSQLContext _context;

        public UserRepository(LIBAPIDbSQLContext context) : base(context)
        {
            _context = context;
        }

 /*       public async Task<Users> CheckCredentials(string userName, string password)
        {

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName && u.Password == password);
            return user;
        }*/

        public async Task<Users> CheckCredentials(string userName, string password)
        {
            // Fetch the user by username
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);

            if (user == null)
            {
                // User not found
                return null;
            }

            // Verify the provided password with the stored hashed password
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(password, user.Password);

            if (isPasswordValid)
            {
                // Password is valid, return user
                return user;
            }

            // Password is invalid, return null or handle accordingly
            return null;
        }


    }
}
