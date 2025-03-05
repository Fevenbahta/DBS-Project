using System;
using IRepository;
using LIB.API.Application.Contracts.Persistence;

namespace LIB.API.Persistence.Repositories
{
    public class PaymentProcessorFactory
    {
        private readonly LIBAPIDbSQLContext _dbContext;
        private readonly IAwachRepositoryAPI _awachRepositoryAPI;
        private readonly IMpesaRepositoryAPI _mpesaRepositoryAPI;
        private readonly IEthswichRepositoryAPI _ethswichRepositoryAPI;
        private readonly ITelebirrRepositoryAPI _telebirrRepositoryAPI;

        public PaymentProcessorFactory(LIBAPIDbSQLContext dbContext,
            IAwachRepositoryAPI awachRepositoryAPI,
            IMpesaRepositoryAPI mpesaRepositoryAPI,
            IEthswichRepositoryAPI ethswichRepositoryAPI,ITelebirrRepositoryAPI telebirrRepositoryAPI)
        {
            _dbContext = dbContext;
           _awachRepositoryAPI = awachRepositoryAPI;
            _mpesaRepositoryAPI = mpesaRepositoryAPI;
            _ethswichRepositoryAPI = ethswichRepositoryAPI;
            _telebirrRepositoryAPI = telebirrRepositoryAPI;
        }

        public IPaymentProcessor GetPaymentProcessor(string paymentScheme)
        {
            return paymentScheme.ToUpper() switch
            {
                "AWACH" => new AwachPaymentProcessor(_dbContext, _awachRepositoryAPI),
                "MPESAWALLET" => new MpesaPaymentProcessor(_dbContext, _mpesaRepositoryAPI),
                "MPESATRUST" => new MpesaPaymentProcessor(_dbContext, _mpesaRepositoryAPI),
                "TELEBIRR" => new TelebirrPaymentProcessor(_dbContext, _telebirrRepositoryAPI),
                "ETHSWICH" => new EtswichPaymentProcessor(_dbContext, _ethswichRepositoryAPI),

                _ => throw new NotSupportedException($"Payment scheme '{paymentScheme}' is not supported.")
            };
        }
    }
}
