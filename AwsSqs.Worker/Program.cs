using Amazon.SQS;
using AwsSqs.Worker;
using Polly;

IAsyncPolicy CreateRateLimitingPolicy()
{
    // Defina aqui a sua taxa máxima permitida de execução por segundo
    int maxExecutionsPerSecond = 5;

    return Policy
        .RateLimitAsync(maxExecutionsPerSecond, TimeSpan.FromSeconds(1));
}

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        // Configurar a política de rate limiting usando Polly
        services.AddSingleton(CreateRateLimitingPolicy());
        
        services.AddHostedService<Worker>();
        services.AddSingleton<IAmazonSQS>(sp =>
        {
            var awsOptions = hostContext.Configuration.GetAWSOptions();
            return awsOptions.CreateServiceClient<IAmazonSQS>();
        });
    })
    .Build();

host.Run();