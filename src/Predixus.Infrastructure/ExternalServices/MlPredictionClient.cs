using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Predixus.Application.Exceptions;
using Predixus.Application.Interfaces;

namespace Predixus.Infrastructure.ExternalServices;

public class MlPredictionClient(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<MlPredictionClient> logger) : IMlPredictionClient
{
    private readonly int _retryCount = int.TryParse(configuration["MlService:RetryCount"], out var retry) ? retry : 2;

    // ML servisi { "prediction": "1234.56" } formatında yanıt döner
    private record MlRawResponse(string Prediction);

    public async Task<MlPredictionOutput> PredictAsync(MlPredictionInput input, CancellationToken ct = default)
    {
        logger.LogInformation(
            "ML servisine tahmin isteği gönderiliyor. Veri sayısı: {Count}",
            input.HistoricalData.Count);

        // Geçmiş veriyi CSV formatına çevir
        var csvBytes = BuildCsv(input.HistoricalData);

        Exception? lastException = null;

        for (int attempt = 1; attempt <= _retryCount + 1; attempt++)
        {
            try
            {
                // Multipart form-data ile CSV dosyası gönder
                using var content = new MultipartFormDataContent();
                using var fileContent = new ByteArrayContent(csvBytes);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
                content.Add(fileContent, "file", "data.csv");

                var response = await httpClient.PostAsync("/predict", content, ct);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<MlRawResponse>(ct);
                if (result is null)
                    throw new ExternalServiceException("MlService", "Boş yanıt döndü.");

                var predictedPrice = decimal.Parse(result.Prediction, CultureInfo.InvariantCulture);

                logger.LogInformation("ML tahmini alındı: {PredictedPrice}", predictedPrice);

                return new MlPredictionOutput(predictedPrice);
            }
            catch (ExternalServiceException)
            {
                throw;
            }
            catch (Exception ex)
            {
                lastException = ex;
                logger.LogWarning(
                    "ML servis denemesi {Attempt}/{Total} başarısız: {Message}",
                    attempt, _retryCount + 1, ex.Message);

                if (attempt <= _retryCount)
                    await Task.Delay(1000 * attempt, ct);
            }
        }

        throw new ExternalServiceException("MlService", lastException?.Message ?? "Bilinmeyen hata.");
    }

    // OHLCV verilerini CSV string'e çevirir
    // Sütun sırası: Date,Open,High,Low,Close,Volume
    private static byte[] BuildCsv(List<StockPricePoint> data)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Date,Open,High,Low,Close,Volume");

        foreach (var p in data.OrderBy(x => x.Date))
        {
            sb.AppendLine(string.Format(
                CultureInfo.InvariantCulture,
                "{0:yyyy-MM-dd},{1},{2},{3},{4},{5}",
                p.Date, p.Open, p.High, p.Low, p.Close, p.Volume));
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }
}
