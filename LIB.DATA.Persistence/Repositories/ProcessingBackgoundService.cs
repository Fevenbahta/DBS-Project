using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace LIB.API.Persistence.Repositories
{
    public class ProcessingBackgroundService : IHostedService, IDisposable
    {
        private readonly ILogger<ProcessingBackgroundService> _logger;
        private readonly IServiceProvider _serviceProvider;  // Add IServiceProvider to create a scope
        private Timer _timer;
        private Timer _timer2;

        // Modify constructor to include IServiceProvider
        public ProcessingBackgroundService(IServiceProvider serviceProvider, ILogger<ProcessingBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Refund processing service started.");

            // Run ProcessRefundsAsync every 5 minutes (300000 milliseconds)
            _timer = new Timer(ExecuteRefundProcessing, null, TimeSpan.Zero, TimeSpan.FromMinutes(5));
            _timer2 = new Timer(ExecuteOrderProcessing, null, TimeSpan.Zero, TimeSpan.FromMinutes(5));
            return Task.CompletedTask;
        }

        private async void ExecuteRefundProcessing(object state)
        {
            try
            {
                // Create a scope to resolve scoped services
                using (var scope = _serviceProvider.CreateScope())
                {
                    var taskRefundService = scope.ServiceProvider.GetRequiredService<TaskRefundService>();

                    _logger.LogInformation("Processing refunds...");

                    // Call the method to process refunds
                    var success = await taskRefundService.ProcessRefundsAsync();

                    if (success)
                    {
                        _logger.LogInformation("Refunds processed successfully.");
                    }
                    else
                    {
                        _logger.LogWarning("No refunds to process or an error occurred.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error occurred while processing refunds: {ex.Message}");
            }
        }

        private async void ExecuteOrderProcessing(object state)
        {
            try
            {
                // Create a scope to resolve scoped services
                using (var scope = _serviceProvider.CreateScope())
                {
                    var taskConfirmOrderService = scope.ServiceProvider.GetRequiredService<TaskConfirmOrderService>();

                    _logger.LogInformation("Processing orders...");

                    // Call the method to process orders
                    var success = await taskConfirmOrderService.ProcessConfirmOrdersAsync();

                    if (success)
                    {
                        _logger.LogInformation("Order processed successfully.");
                    }
                    else
                    {
                        _logger.LogWarning("No orders to process or an error occurred.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error occurred while processing orders: {ex.Message}");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Processing service stopped.");
            _timer?.Change(Timeout.Infinite, 0);
            _timer2?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
            _timer2?.Dispose();
        }
    }
}
