using LIB.API.Application.Contracts.Persistent;
using LIB.API.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LIB.API.Application.Contracts.Persistence
{
    public interface IUserRepository : IGenericRepository<Users>
    {
        Task<Users?> CheckCredentials(string userName, string password);
 
    }
}
