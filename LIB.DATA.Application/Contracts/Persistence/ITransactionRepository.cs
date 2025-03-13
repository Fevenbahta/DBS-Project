
using LIB.API.Application.Contracts.Persistent;
using LIB.API.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LIB.API.Application.Contracts.Persistent
{
    public interface IDetailRepository : IGenericRepositoryOracle<AccountInfos>
    {
        Task<AccountInfos> GetUserDetailsByAccountNumberAsync(string accountNumber);
 


    }
}
