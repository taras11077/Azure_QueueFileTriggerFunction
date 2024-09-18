using Azure;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace QueueFileTriggerFunction;

/// <summary>
/// �������� ���� ��� ����, ��� ����� ���� ��� ��������� ����� ������ ��� 
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
        _logger.LogInformation($"�������� ����������� � �����: {queueMessage}");

        try
        {
            ShareClient shareClient = new ShareClient(connectionString, shareName);

            if (await shareClient.ExistsAsync())
            {
                // ��������� �볺��� �������� �� �������� �����
                ShareDirectoryClient rootDir = shareClient.GetRootDirectoryClient();
                ShareFileClient fileClient = rootDir.GetFileClient(queueMessage);

                if (await fileClient.ExistsAsync()) // ��������, �� ���� ���� � File Shares
                {
                    var response = await fileClient.DownloadAsync(); // ���������� ����� � File Shares
                    ShareFileDownloadInfo info = response.Value; // ��������� ���������� ��� �������� ����

                    if (!Directory.Exists(downloadsPath))
                        Directory.CreateDirectory(downloadsPath); // ��������� ����� ����������� ���� �� ����

                    string filePath = Path.Combine(downloadsPath, queueMessage); // ���� ���������� ����� � �������� ���������

                    // ����� ����� � �������� ���������
                    using var ms = new MemoryStream();
                    info.Content.CopyTo(ms); // ���������� ������ � ���������� ��� ���� �� MemoryStream
                    File.WriteAllBytes(filePath, ms.ToArray()); //��������� �����, ��������� � MemoryStream

                    _logger.LogInformation($"���� {queueMessage} ������ ������������ � {filePath}");
                }
                else
                    _logger.LogWarning($"���� {queueMessage} �� �������� � File Shares.");
            }
            else
                _logger.LogError("���������� File Share �� ����.");
        }
        catch (Exception ex)
        {
            _logger.LogError($"������� �� ��� ������� �����������: {ex.Message}");
        }
    }
}

