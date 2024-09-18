using Azure;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace QueueFileTriggerFunction;

/// <summary>
/// Коментарі пишу для себе, щоб легше було все пригадати через деякий час 
/// </summary>


public class Function1
{
    private readonly string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
    private readonly string shareName = "testfileshares1";
    private readonly string downloadsPath = @"C:\DownloadedFiles\";
    private readonly ILogger _logger;

    public Function1(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<Function1>();
    }

    [Function("Function1")]
    public async Task Run(
        [QueueTrigger("myfirstqueue")] string queueMessage)
    {
        _logger.LogInformation($"Отримано повідомлення з черги: {queueMessage}");

        try
        {
            ShareClient shareClient = new ShareClient(connectionString, shareName);

            if (await shareClient.ExistsAsync())
            {
                // отримання клієнта директорії та перевірка файлу
                ShareDirectoryClient rootDir = shareClient.GetRootDirectoryClient();
                ShareFileClient fileClient = rootDir.GetFileClient(queueMessage);

                if (await fileClient.ExistsAsync()) // перевірка, чи існує файл у File Shares
                {
                    var response = await fileClient.DownloadAsync(); // скачування файлу з File Shares
                    ShareFileDownloadInfo info = response.Value; // отримання інформації про скачаний файл

                    if (!Directory.Exists(downloadsPath))
                        Directory.CreateDirectory(downloadsPath); // створення папки завантажень якщо не існує

                    string filePath = Path.Combine(downloadsPath, queueMessage); // шлях збереження файлу в локальну директорію

                    // запис файлу в локальну директорію
                    using var ms = new MemoryStream();
                    info.Content.CopyTo(ms); // копиювання потоку з інформації про файл до MemoryStream
                    File.WriteAllBytes(filePath, ms.ToArray()); //створення файлу, побайтово з MemoryStream

                    _logger.LogInformation($"Файл {queueMessage} успішно завантажений у {filePath}");
                }
                else
                    _logger.LogWarning($"Файл {queueMessage} не знайдено в File Shares.");
            }
            else
                _logger.LogError("Зазначений File Share не існує.");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Помилка під час обробки повідомлення: {ex.Message}");
        }
    }
}

