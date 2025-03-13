
using LIB.API.Application.Contracts.Persistent;
using LIB.API.Domain;
using LIB.API.Persistence;
using LIBPROPERTY.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using Org.BouncyCastle.Asn1.X509;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace LIB.API.Persistence.Repositories
{
    public class DetailRepository : GenericRepositoryOracle<AccountInfos>, IDetailRepository
    {
        private readonly LIBAPIDbContext _context;
  
        private readonly HttpClient _httpClient;
        public DetailRepository(LIBAPIDbContext context) : base(context)
        {
            _context = context;
         
        }

   

        public async Task<AccountInfos> GetUserDetailsByAccountNumberAsync(string accountNumber)
        {


            var query2 = @"
SELECT *
FROM anbesaprod.valid_accounts
WHERE ACCOUNTNUMBER = :accountNumber";

            var accountNumberParameter = new OracleParameter("accountNumber", accountNumber);
            var userDetails2 = await _context.AccountInfos
                .FromSqlRaw(query2, accountNumberParameter)
                .FirstOrDefaultAsync();




            return (userDetails2);
        }


    }
}
