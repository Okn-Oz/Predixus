using System.Net.Http.Json;
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

    public async Task<MlPredictionOutput> PredictAsync(MlPredictionInput input, CancellationToken ct = default)
    {
        logger.LogInformation(
            "ML servisine tahmin isteği gönderiliyor: {Symbol}, {Days} gün",
            input.Symbol, input.ForecastDays);

        Exception? lastException = null;

        for (int attempt = 1; attempt <= _retryCount + 1; attempt++)
        {
            try
            {
                var response = await httpClient.PostAsJsonAsync("/predict", input, ct);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<MlPredictionOutput>(ct);
                if (result is null)
                    throw new ExternalServiceException("MlService", "Boş yanıt döndü.");

                logger.LogInformation(
                    "ML tahmini alındı: {Symbol}, confidence={Confidence}",
                    result.Symbol, result.Confidence);

                return result;
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
}
