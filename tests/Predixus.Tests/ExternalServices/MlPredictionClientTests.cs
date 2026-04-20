using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Predixus.Application.Exceptions;
using Predixus.Application.Interfaces;
using Predixus.Infrastructure.ExternalServices;

namespace Predixus.Tests.ExternalServices;

/// <summary>
/// MlPredictionClient testleri.
/// Gerçek ML servisi çalışmak zorunda değil — HTTP çağrısı sahte handler ile yakalanır.
/// </summary>
public class MlPredictionClientTests
{
    // ML servisinin döndürdüğü gerçek yanıt formatı
    private const string ValidMlResponse = """{"prediction":"1234.56"}""";

    private static readonly List<StockPricePoint> SampleData = Enumerable
        .Range(0, 30)
        .Select(i => new StockPricePoint(
            Date: DateOnly.FromDateTime(DateTime.Today.AddDays(-30 + i)),
            Open: 100 + i,
            High: 105 + i,
            Low: 95 + i,
            Close: 102 + i,
            Volume: 1_000_000L
        ))
        .ToList();

    // ─── Yardımcı: client oluştur ───────────────────────────────────────────

    private static (MlPredictionClient client, FakeHttpHandler handler) CreateClient(
        HttpResponseMessage response, int retryCount = 0)
    {
        var handler = new FakeHttpHandler(response);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8000") };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MlService:RetryCount"] = retryCount.ToString()
            })
            .Build();

        var logger = new Mock<ILogger<MlPredictionClient>>().Object;

        return (new MlPredictionClient(httpClient, config, logger), handler);
    }

    // ─── Testler ────────────────────────────────────────────────────────────

    [Fact]
    public async Task PredictAsync_Basarili_TahminiDogruParseEder()
    {
        // Arrange: ML servisi 1234.56 döndürüyor
        var (client, _) = CreateClient(OkResponse(ValidMlResponse));

        // Act
        var result = await client.PredictAsync(new MlPredictionInput(SampleData));

        // Assert: decimal olarak doğru parse edilmeli
        result.PredictedPrice.Should().Be(1234.56m);
    }

    [Fact]
    public async Task PredictAsync_Basarili_IstegiMultipartFormDataOlarakGonderir()
    {
        // Arrange
        var (client, handler) = CreateClient(OkResponse(ValidMlResponse));

        // Act
        await client.PredictAsync(new MlPredictionInput(SampleData));

        // Assert: istek multipart/form-data olmalı
        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.Content.Should().BeOfType<MultipartFormDataContent>();
    }

    [Fact]
    public async Task PredictAsync_Basarili_CsvDogru_SutunBasliklariniIcerir()
    {
        // Arrange
        var (client, handler) = CreateClient(OkResponse(ValidMlResponse));

        // Act
        await client.PredictAsync(new MlPredictionInput(SampleData));

        // Assert: CSV içeriği "Date,Open,High,Low,Close,Volume" başlığını içermeli
        handler.LastCsvContent.Should().Contain("Date,Open,High,Low,Close,Volume");
    }

    [Fact]
    public async Task PredictAsync_Basarili_CsvVeriSatirlariniIcerir()
    {
        // Arrange
        var (client, handler) = CreateClient(OkResponse(ValidMlResponse));
        var tek = new List<StockPricePoint>
        {
            new(DateOnly.Parse("2024-01-15"), 100m, 105m, 95m, 102m, 500_000L)
        };

        // Act
        await client.PredictAsync(new MlPredictionInput(tek));

        // Assert: tarih ve fiyatlar CSV'de doğru formatlanmalı
        handler.LastCsvContent.Should().Contain("2024-01-15");
        handler.LastCsvContent.Should().Contain("100");
        handler.LastCsvContent.Should().Contain("500000");
    }

    [Fact]
    public async Task PredictAsync_Basarili_CsvTariheSoreArtan()
    {
        // Arrange: ters sıralı veri ver
        var (client, handler) = CreateClient(OkResponse(ValidMlResponse));
        var tersSirali = SampleData.OrderByDescending(x => x.Date).ToList();

        // Act
        await client.PredictAsync(new MlPredictionInput(tersSirali));

        // Assert: CSV'de veriler tarihe göre artan sırada olmalı
        var satirlar = handler.LastCsvContent!
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Skip(1) // başlığı atla
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        var tarihler = satirlar
            .Select(s => DateOnly.Parse(s.Split(',')[0]))
            .ToList();

        tarihler.Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task PredictAsync_ServisHata_ExternalServiceExceptionFirlatir()
    {
        // Arrange: ML servisi 500 döndürüyor
        var (client, _) = CreateClient(new HttpResponseMessage(HttpStatusCode.InternalServerError));

        // Act
        var act = () => client.PredictAsync(new MlPredictionInput(SampleData));

        // Assert
        await act.Should().ThrowAsync<ExternalServiceException>()
            .WithMessage("*MlService*");
    }

    [Fact]
    public async Task PredictAsync_RetryCount1_1KezDener()
    {
        // Arrange: her zaman başarısız, retry=1 → toplam 2 deneme
        var (client, handler) = CreateClient(
            new HttpResponseMessage(HttpStatusCode.InternalServerError),
            retryCount: 1);

        // Act
        var act = () => client.PredictAsync(new MlPredictionInput(SampleData));

        // Assert: exception fırlatmalı
        await act.Should().ThrowAsync<ExternalServiceException>();

        // Ve toplam 2 kez istek atılmalı (1 ilk + 1 retry)
        handler.CallCount.Should().Be(2);
    }

    [Fact]
    public async Task PredictAsync_IlkDenemeBas\u0131lars\u0131z_IkinciDenemede_BasariDondurur()
    {
        // Arrange: ilk istek başarısız, ikinci başarılı
        var sıra = 0;
        var handler = new FakeHttpHandler(_ =>
        {
            sıra++;
            return sıra == 1
                ? new HttpResponseMessage(HttpStatusCode.InternalServerError)
                : OkResponse(ValidMlResponse);
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8000") };
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["MlService:RetryCount"] = "2" })
            .Build();

        var client = new MlPredictionClient(httpClient, config, new Mock<ILogger<MlPredictionClient>>().Object);

        // Act
        var result = await client.PredictAsync(new MlPredictionInput(SampleData));

        // Assert
        result.PredictedPrice.Should().Be(1234.56m);
        handler.CallCount.Should().Be(2);
    }

    // ─── Yardımcılar ────────────────────────────────────────────────────────

    private static HttpResponseMessage OkResponse(string json) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
}

/// <summary>
/// HTTP isteklerini yakalayan sahte handler.
/// </summary>
public class FakeHttpHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _factory;

    public HttpRequestMessage? LastRequest { get; private set; }
    public string? LastCsvContent { get; private set; }
    public int CallCount { get; private set; }

    public FakeHttpHandler(HttpResponseMessage fixedResponse)
        : this(_ => fixedResponse) { }

    public FakeHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> factory)
    {
        _factory = factory;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        CallCount++;
        LastRequest = request;

        // Multipart içindeki CSV dosyasını oku
        if (request.Content is MultipartFormDataContent multipart)
        {
            foreach (var part in multipart)
            {
                LastCsvContent = await part.ReadAsStringAsync(cancellationToken);
                break;
            }
        }

        return _factory(request);
    }
}
