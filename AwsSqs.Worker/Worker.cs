using Amazon.SQS;
using Amazon.SQS.Model;
using Polly;

namespace AwsSqs.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IAmazonSQS _sqsClient;
    private readonly IAsyncPolicy _rateLimitingPolicy;

    public Worker(ILogger<Worker> logger, IAmazonSQS sqsClient, IAsyncPolicy rateLimitingPolicy)
    {
        _logger = logger;
        _sqsClient = sqsClient;
        _rateLimitingPolicy = rateLimitingPolicy;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Substitua "YourQueueUrl" pelo URL da sua fila SQS
                string queueUrl = "YourQueueUrl";

                // Configure a solicitação de recebimento de mensagens
                var request = new ReceiveMessageRequest
                {
                    QueueUrl = queueUrl,
                    MaxNumberOfMessages = 10, // Receba até 10 mensagens por vez
                    WaitTimeSeconds = 20, // Tempo máximo de espera para receber mensagens
                };

                // Receba mensagens da fila
                var response = await _sqsClient.ReceiveMessageAsync(request, stoppingToken);

                // Crie uma lista de tarefas para processar as mensagens em paralelo
                var tasks = new List<Task>();

                foreach (var message in response.Messages)
                {
                    var task = Task.Run(async () =>
                    {
                        await _rateLimitingPolicy.ExecuteAsync(async () =>
                        {
                            _logger.LogInformation("Mensagem recebida: {Message}", message.Body);

                            // Processar a mensagem aqui

                            // Remova a mensagem da fila depois de processá-la (caso contrário, ela será recebida novamente após o tempo de visibilidade)
                            await _sqsClient.DeleteMessageAsync(queueUrl, message.ReceiptHandle, stoppingToken);
                        });
                    });

                    tasks.Add(task);
                }

                // Aguarde todas as tarefas de processamento serem concluídas
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ocorreu um erro ao receber ou processar mensagens da fila SQS.");
            }

            // Aguarde um intervalo de tempo antes de verificar novamente a fila
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }

}