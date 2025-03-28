
using LIB.API.Application.Contracts.Persistence;
using LIB.API.Application.Contracts.Persistent;
using LIB.API.Persistence.Repositories;

using LIBPROPERTY.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PERSISTANCE.Services;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using LIB.API.Interfaces;
using LIB.API.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using IRepository;
using Repository;
using LIB.API.Persistence.Repositories.ExternalAPI;
using LIB.API.Application.Contracts.Persistence.LIB.API.Repositories;
using Microsoft.Extensions.Hosting;



namespace LIB.API.Persistence
{
    public static partial class PersistenceServiceRegistrtion
    {
        public static IServiceCollection ConfigurePersistenceService(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<LIBAPIDbContext>(options => options.UseOracle(configuration.GetConnectionString("LIBAPIConnectionString")));

            services.AddDbContext<LIBAPIDbSQLContext>(options => options.UseNpgsql(configuration.GetConnectionString("LIBAPISQLConnectionString")));
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));


         

            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<JwtService>();
            // Inside ConfigureServices method of Startup.cs
            services.AddScoped<UpdateLogService>();

       
          
            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var errors = context.ModelState
                        .Where(e => e.Value.Errors.Count > 0)
                        .Select(e => new { field = e.Key, errors = e.Value.Errors.Select(x => x.ErrorMessage).ToArray() })
                        .ToList();

                    var response = new
                    {
                        status = 400,
                        message = "Validation failed",
                        errors = errors
                    };

                    return new BadRequestObjectResult(response);
                };
            });




            services.AddScoped<ITransferService, TransferService>();


            services.AddScoped<IPaymentProcessor, RtgsPaymentProcessor>();
            services.AddScoped<IPaymentProcessor, AwachPaymentProcessor>();
            services.AddScoped<IPaymentProcessor, MpesaPaymentProcessor>();
            services.AddScoped<IPaymentProcessor, TelebirrPaymentProcessor>();
            services.AddScoped<IPaymentProcessor, EtswichPaymentProcessor>();
            services.AddScoped<IPaymentProcessor, HelloCashPaymentProcessor>();


            services.AddScoped<IAwachRepositoryAPI, AwachRepositoryAPI>();
            services.AddScoped<IEthswichRepositoryAPI,EthswichRepositoryAPI>();
            services.AddScoped<IMpesaRepositoryAPI, MpesaRepositoryAPI>();
            services.AddScoped<ITelebirrRepositoryAPI, TelebirrRepositoryAPI>();
            services.AddScoped<IRtgRepositoryAPI, RtgRepositoryAPI>();
            services.AddScoped<IHellocashRepositoryAPI, HellocashRepositoryAPI>();
            services.AddScoped<PaymentProcessorFactory>(); // Change this to Scoped

           services.AddScoped<IAirlinesOrderRepository, AirlinesOrderRepository>();
           services.AddScoped<IAirlinesOrderService, AirlinesOrderService>();
            services.AddScoped<IConfirmOrderRepository, ConfirmOrderRepository>();
            services.AddScoped<IConfirmOrderService, ConfirmOrderService>();
            services.AddScoped<IRefundRepository, RefundRepository>();
            services.AddScoped<IDetailRepository, DetailRepository>();
            services.AddScoped<IBillGetRequestRepository, BillGetRequestRepository>();
            services.AddScoped<IECPaymentRepository, ECPaymentRepository>();
            services.AddHttpClient<SoapClient>();
            services.AddScoped<TaskRefundService>();
            services.AddScoped<TaskConfirmOrderService>();
            services.AddHttpClient();
      

            // Register your IHostedService as a concrete implementation
            services.AddHostedService<ProcessingBackgroundService>();

            string connectionString = configuration.GetConnectionString("LIBAPIConnectionString");
            string connectionSqlString = configuration.GetConnectionString("LIBAPISQLConnectionString");

            // Register IDbConnection with a SqlConnection instance using the retrieved connection string
            //   services.AddScoped<IDbConnection>(_ => new SqlConnection(connectionString));

            services.AddScoped<IDbConnection>(_ => new OracleConnection(connectionString));
            services.AddScoped<IDbConnection>(_ => new NpgsqlConnection(connectionSqlString));


            return services;
        }
    }
}
