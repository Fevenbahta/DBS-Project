using System;
using IRepository;
using LIB.API.Application.Contracts.Persistence;
using LIB.API.Persistence.Repositories.ExternalAPI;

namespace LIB.API.Persistence.Repositories
{
    public class PaymentProcessorFactory
    {
        private readonly LIBAPIDbSQLContext _dbContext;
        private readonly IAwachRepositoryAPI _awachRepositoryAPI;
        private readonly IMpesaRepositoryAPI _mpesaRepositoryAPI;
        private readonly IEthswichRepositoryAPI _ethswichRepositoryAPI;
        private readonly ITelebirrRepositoryAPI _telebirrRepositoryAPI;
        private readonly IRtgRepositoryAPI _rtgRepositoryAPI;
        private readonly IHellocashRepositoryAPI _hellocashRepositoryAPI;

        public PaymentProcessorFactory(LIBAPIDbSQLContext dbContext,
            IAwachRepositoryAPI awachRepositoryAPI,
            IMpesaRepositoryAPI mpesaRepositoryAPI,
            IEthswichRepositoryAPI ethswichRepositoryAPI,ITelebirrRepositoryAPI telebirrRepositoryAPI,IRtgRepositoryAPI rtgRepositoryAPI,
            IHellocashRepositoryAPI hellocashRepositoryAPI)
        {
            _dbContext = dbContext;
           _awachRepositoryAPI = awachRepositoryAPI;
            _mpesaRepositoryAPI = mpesaRepositoryAPI;
            _ethswichRepositoryAPI = ethswichRepositoryAPI;
            _telebirrRepositoryAPI = telebirrRepositoryAPI;
            _rtgRepositoryAPI = rtgRepositoryAPI;
            _hellocashRepositoryAPI = hellocashRepositoryAPI;
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
                "RTGS" => new RtgsPaymentProcessor(_dbContext, _rtgRepositoryAPI),
                "HELLOCASH" => new HelloCashPaymentProcessor(_dbContext, _hellocashRepositoryAPI),

                _ => throw new NotSupportedException($"Payment scheme '{paymentScheme}' is not supported.")
            };
        }
    }
}
