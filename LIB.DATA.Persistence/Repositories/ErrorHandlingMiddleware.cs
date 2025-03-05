using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using LIB.API.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Collections.Generic;

public class ValidateModelAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        // Check if model state is valid
        if (!context.ModelState.IsValid)
        {
            var validationErrors = new List<object>();

            foreach (var state in context.ModelState)
            {
                if (state.Value.Errors.Count > 0)
                {
                    foreach (var error in state.Value.Errors)
                    {
                        validationErrors.Add(new
                        {
                            code = "SB_DS_001", // Example error code
                            label = error.ErrorMessage,
                            severity = "ERROR",
                            type = "BUS",
                            source = state.Key,
                            origin = "TransferRequest",
                            parameters = new[]
                            {
                                new { code = "0", value = error.ErrorMessage }
                            }
                        });
                    }
                }
            }

            var errorResponse = new
            {
                returnCode = "ERROR",
                ticketId = Guid.NewGuid().ToString(),
                traceId = Guid.NewGuid().ToString(),
                feedbacks = validationErrors
            };

            // Return a 400 Bad Request response with the custom error response
            context.Result = new BadRequestObjectResult(errorResponse);
        }
    }
}

namespace LIB.API.Persistence.Repositories
{
  
    public class ValidateModelAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // Check if model state is valid
            if (!context.ModelState.IsValid)
            {
                var validationErrors = new List<object>();

                foreach (var state in context.ModelState)
                {
                    if (state.Value.Errors.Count > 0)
                    {
                        foreach (var error in state.Value.Errors)
                        {
                            validationErrors.Add(new
                            {
                                code = "SB_DS_001", // Example error code
                                label = error.ErrorMessage,
                                severity = "ERROR",
                                type = "BUS",
                                source = state.Key,
                                origin = "TransferRequest",
                                parameters = new[]
                                {
                                new { code = "0", value = error.ErrorMessage }
                            }
                            });
                        }
                    }
                }

                var errorResponse = new
                {
                    returnCode = "ERROR",
                    ticketId = Guid.NewGuid().ToString(),
                    traceId = Guid.NewGuid().ToString(),
                    feedbacks = validationErrors
                };

                // Return a 400 Bad Request response with the custom error response
                context.Result = new BadRequestObjectResult(errorResponse);
            }
        }
    }

}
