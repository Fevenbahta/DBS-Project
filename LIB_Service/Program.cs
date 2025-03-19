using LIB.API.Persistence;
using LIB.API.Persistence;
using LIB.API.Infrastructure;
using LIB.API.Application;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.OpenApi.Models;
using LIB.API.Persistence.Repositories;
using Microsoft.AspNetCore.Mvc;
using LIB.API.Domain;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;  // Add this





var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.ConfigureApplicationServices();
builder.Services.ConfigureInfrastructureServices(builder.Configuration);
builder.Services.ConfigurePersistenceService(builder.Configuration);




builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.IgnoreNullValues = true; // Optional:  Don't serialize null values
    });

builder.Services.AddEndpointsApiExplorer();



// Swagger Configuration
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v3", new OpenApiInfo
    {
        Version = "3.9.0",
        Title = "Transfers Connector APIs",
        Description = "These APIs define Transfers Connector endpoints consumed by DBS.",
        Contact = new OpenApiContact
        {
            Name = "Sopra Banking Software",
            Url = new Uri("https://www.soprabanking.com/")
        }
    });

    // Add Security Definition for Bearer Authentication
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' followed by a space and the JWT token."
    });

    // Add Security Requirement
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new string[] { }
        }
    });

    //  Set Base URL for Swagger UI

    //c.AddServer(new OpenApiServer
    //{
    //    Url = "https://localhost/api/v3",
    //    Description = ""
    //});
});





// Retrieve JWT settings from configuration
var jwtSettings = builder.Configuration.GetSection("JwtSettings");

// Generate a 16-byte (128-bit) random key (Consider a more robust key management strategy)
var key = new byte[16];
using (var rng = RandomNumberGenerator.Create())
{
    rng.GetBytes(key);
}

// Configure JWT authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var traceId = context.HttpContext.TraceIdentifier;
            var dbContext = context.HttpContext.RequestServices.GetService<LIBAPIDbSQLContext>();



            var controllerName = context.ActionDescriptor.RouteValues["controller"];
            var actionName = context.ActionDescriptor.RouteValues["action"];
            // Define a list of controllers where validation should be skipped
            var skipValidationControllers = new List<string> { "Refund", "Orders" , "BillGetRequest" };

            if (skipValidationControllers.Contains(controllerName))
            {
                return null;
            }
            if (!string.IsNullOrEmpty(context.HttpContext.Request.Headers["Authorization"]))
            {
                // Collect all validation errors
                var feedbacks = context.ModelState
                    .Where(e => e.Value.Errors.Count > 0)
                    .SelectMany(e => e.Value.Errors.Select(error => new
                    {
                        code = "SB_DS_001",
                        label = error.ErrorMessage,
                        severity = "ERROR",
                        source = e.Key,
                        spanId = traceId.Split('-').FirstOrDefault() ?? traceId,
                        parameters = new[] { new { code = "0", value = $"${e.Key}" } }
                    }))
                    .ToList();

                context.HttpContext.Request.EnableBuffering();
                bool simulationIndicator = bool.TryParse(context.HttpContext.Request.Query["simulation"], out var sim) && sim;

                // Log error in DB
                LogInvalidRequestAndError(dbContext, null, "SB_DS_003", "Invalid request body", simulationIndicator).Wait();

                // ? Return ALL errors, not just the second one!
                return new BadRequestObjectResult(new
                {
                    returnCode = "ERROR",
                    ticketId = Guid.NewGuid().ToString(),
                    traceId,
                    feedbacks // Return all errors
                });
            }

            else
            {
                context.HttpContext.Request.EnableBuffering();
                bool simulationIndicator = bool.TryParse(context.HttpContext.Request.Query["simulation"], out var sim) && sim;

                var feedbacks = new[]
                {
            new
            {
                code = "SB_DS_002",
                label = "Token is missing or invalid.",
                severity = "ERROR",
                source = "Authorization",
                spanId = traceId.Split('-').FirstOrDefault() ?? traceId
            }
        };

                LogInvalidRequestAndError(dbContext, null, "SB_DS_002", "Token is missing or invalid.", false).Wait();

                return new UnauthorizedObjectResult(new
                {
                    returnCode = "ERROR",
                    ticketId = Guid.NewGuid().ToString(),
                    traceId,
                    feedbacks
                });
            }
        };
    });

// Synchronous method to read the request body
static async Task<string> ReadRequestBody(HttpRequest request)
{
    using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
    return await reader.ReadToEndAsync();
}




// Add support for serving static files
//builder.Services.AddSpaStaticFiles(configuration =>
//{
//    configuration.RootPath = "wwwroot";
//});

// Configure CORS with specific options
builder.Services.AddCors(options =>
{
    options.AddPolicy("MyCorsPolicy", builder =>
    {
        builder.AllowAnyOrigin() // Allow all origins
                 .AllowAnyMethod() // Allow all HTTP methods
                 .AllowAnyHeader() // Allow all headers
           ;
    });
});

// Add HttpClientFactory

var app = builder.Build();

app.UseAuthentication();

//app.UseMiddleware<ErrorHandlingMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v3/swagger.json", "Transfers Connector APIs v3"));

}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}


//app.UseDeveloperExceptionPage();
//app.UseSwagger(); // Make sure this is present
//app.UseSwaggerUI(c =>
//{
//    c.SwaggerEndpoint("/swagger/v3/swagger.json", "Transfers Connector APIs v3"); // Correct endpoint
//    c.RoutePrefix = string.Empty; // Optional: set Swagger UI at the app's root
//});

app.UseHttpsRedirection();
//app.UseStaticFiles();

//app.UseSpaStaticFiles();

app.UseRouting();

// Use the defined CORS policy
app.UseCors("MyCorsPolicy");

app.UseAuthorization();


app.MapControllers();
//app.UseSpa(spa =>
//{
//    spa.Options.SourcePath = "wwwroot";
//    if (app.Environment.IsDevelopment())
//    {
//        spa.UseProxyToSpaDevelopmentServer("http://10.1.22.206:4200");
//    }
//});








app.Run();






static async Task LogInvalidRequestAndError(LIBAPIDbSQLContext _dbContext, TransferRequest request, string errorCode, string errorMessage, bool simulationIndicator)
{
    var requestId = Guid.NewGuid().ToString(); // Unique request identifier

    // Save the request details
    var errorLog = new ErrorLog
    {
        ticketId = GenerateRandomString(6),
        traceId = request != null ? request.ReferenceId.ToString() : "null",
        returnCode = "SB_DS_003",
        EventDate = DateTime.UtcNow,
        feedbacks = $"Error Invalid Request: {errorMessage}",
        TransactionType = simulationIndicator ? "Simulation" : "Real Transaction",
        TransactionId = ""
    };

    _dbContext.ErrorLog.Add(errorLog);
    await _dbContext.SaveChangesAsync();



    Transaction transaction = null;
    TransactionSimulation transactionsimulation = null;

    // Create a new Transaction record
    if (simulationIndicator && request != null)
    {
        transactionsimulation = new TransactionSimulation
        {
            accountId = request.AccountId,
            referenceId = request.ReferenceId,
            reservationId = Guid.NewGuid(),
            amount = request.Amount.Value,
            requestedExecutionDate = request.RequestedExecutionDate,
            paymentType = request.PaymentInformation.PaymentType,
            paymentScheme = request.PaymentInformation.PaymentScheme,
            ReciverAccountId = request.PaymentInformation.PaymentScheme,
            ReciverAccountIdType = request.PaymentInformation.Account.IdType,
            bankId = request.PaymentInformation.Bank.Id,
            bankIdType = request.PaymentInformation.Bank.IdType,
            bankName = request.Payee.Bank.Name,
            status = errorMessage,
            cbsStatusMessage = null,
            bankStatusMessage = null
        };
    }
    else if (!simulationIndicator && request != null)
    {

        transaction = new Transaction
        {
            accountId = request.AccountId,
            referenceId = request.ReferenceId,
            reservationId = Guid.NewGuid(),
            amount = request.Amount.Value,
            requestedExecutionDate = request.RequestedExecutionDate,
            paymentType = request.PaymentInformation.PaymentType,
            paymentScheme = request.PaymentInformation.PaymentScheme,
            ReciverAccountId = request.PaymentInformation.PaymentScheme,
            ReciverAccountIdType = request.PaymentInformation.Account.IdType,
            bankId = request.PaymentInformation.Bank.Id,
            bankIdType = request.PaymentInformation.Bank.IdType,
            bankName = request.Payee.Bank.Name,
            status = errorMessage,
            cbsStatusMessage = null,
            bankStatusMessage = null
        };
    }

    // Save transaction to the database
    if (simulationIndicator && request != null)
    {
        _dbContext.TransactionSimulation.Add(transactionsimulation);
        await _dbContext.SaveChangesAsync();
    }
    else if (!simulationIndicator && request != null)
    {
        _dbContext.Transaction.Add(transaction);
        await _dbContext.SaveChangesAsync();
    }


}


static string GenerateRandomString(int length)
{
    const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
    var stringBuilder = new StringBuilder(length);

    for (int i = 0; i < length; i++)
    {
        stringBuilder.Append(chars[new Random().Next(chars.Length)]);
    }

    return stringBuilder.ToString();
}







//using LIB.API.Persistence;
//using LIB.API.Infrastructure;
//using LIB.API.Application;
//using Microsoft.AspNetCore.Authentication.JwtBearer;
//using Microsoft.IdentityModel.Tokens;
//using System.Text;
//using System.Security.Cryptography;
//using System.Threading.Tasks;
//using Microsoft.OpenApi.Models;  // Add this

//var builder = WebApplication.CreateBuilder(args);

//// Add services to the container.
//builder.Services.ConfigureApplicationServices();
//builder.Services.ConfigureInfrastructureServices(builder.Configuration);
//builder.Services.ConfigurePersistenceService(builder.Configuration);
//builder.Services.AddControllers()
//    .AddJsonOptions(options =>
//    {
//        options.JsonSerializerOptions.IgnoreNullValues = true; // Optional:  Don't serialize null values
//    });

//builder.Services.AddEndpointsApiExplorer();

//// Swagger Configuration
//builder.Services.AddSwaggerGen(c =>
//{
//    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Transfers Connector APIs", Version = "v1" });

//    // Add security definition (if using authentication)
//    c.AddSecurityDefinition("bearerAuth", new OpenApiSecurityScheme
//    {
//        Type = SecuritySchemeType.Http,
//        Scheme = "bearer",
//        BearerFormat = "JWT",
//        Description = "JWT Authorization header using the Bearer scheme."
//    });
//    c.AddSecurityRequirement(new OpenApiSecurityRequirement
//    {
//        {
//            new OpenApiSecurityScheme
//            {
//                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "bearerAuth" }
//            },
//            new string[] {}
//        }
//    });
//});

//// Retrieve JWT settings from configuration
//var jwtSettings = builder.Configuration.GetSection("JwtSettings");

//// Generate a 16-byte (128-bit) random key (Consider a more robust key management strategy)
//var key = new byte[16];
//using (var rng = RandomNumberGenerator.Create())
//{
//    rng.GetBytes(key);
//}

//// Configure JWT authentication
//builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//    .AddJwtBearer(options =>
//    {
//        options.TokenValidationParameters = new TokenValidationParameters
//        {
//            ValidateIssuer = true,
//            ValidateAudience = true,
//            ValidateLifetime = true,
//            ValidateIssuerSigningKey = true,
//            ValidIssuer = jwtSettings["Issuer"],
//            ValidAudience = jwtSettings["Audience"],
//            IssuerSigningKey = new SymmetricSecurityKey(key),
//            ClockSkew = TimeSpan.Zero
//        };
//    });

//// Add support for serving static files
//builder.Services.AddSpaStaticFiles(configuration =>
//{
//    configuration.RootPath = "wwwroot";
//});

//// Configure CORS with specific options
//builder.Services.AddCors(options =>
//{
//    options.AddPolicy("MyCorsPolicy", builder =>
//    {
//        builder.WithOrigins("https://fana-sacco.anbesabank.et", "http://10.1.10.106:7070", "http://10.1.22.206:4200", "http://10.1.22.25:4200") // Allow multiple origins
//               .WithMethods("GET", "POST", "PUT", "DELETE") // Allow specific methods
//               .AllowCredentials() // Allow credentials (cookies, authorization headers, etc.)
//               .AllowAnyHeader(); // Allow any header
//    });
//});

//// Add HttpClientFactory

//var app = builder.Build();

//// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//    app.UseDeveloperExceptionPage();
//    app.UseSwagger();
//    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Transfers Connector APIs v1")); // Corrected Name
//}
//else
//{
//    app.UseExceptionHandler("/Error");
//    app.UseHsts();
//}

//app.UseHttpsRedirection();
//app.UseStaticFiles();

//app.UseSpaStaticFiles();

//app.UseRouting();

//// Use the defined CORS policy
//app.UseCors("MyCorsPolicy");

//app.UseAuthentication();
//app.UseAuthorization();

//app.UseEndpoints(endpoints =>
//{
//    endpoints.MapControllers();
//});

//app.UseSpa(spa =>
//{
//    spa.Options.SourcePath = "wwwroot";
//    if (app.Environment.IsDevelopment())
//    {
//        spa.UseProxyToSpaDevelopmentServer("http://10.1.22.206:4200");
//    }
//});

//app.Run();
