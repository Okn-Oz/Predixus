using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Predixus.Application.DTOs;
using Predixus.Application.Exceptions;
using Predixus.Application.Interfaces;
using Predixus.Application.Services;
using Predixus.Domain.Entities;
using Predixus.Domain.Interfaces;

namespace Predixus.Tests.Services;

/// <summary>
/// PredictionService testleri.
/// Sistem her zaman BIST100 (XU100) endeksini tahmin eder — sembol parametresi yok.
/// </summary>
public class PredictionServiceTests
{
    private readonly Mock<IStockRepository> _stockRepo = new();
    private readonly Mock<IStockPriceRepository> _priceRepo = new();
    private readonly Mock<IPredictionRepository> _predRepo = new();
    private readonly Mock<ICacheService> _cache = new();
    private readonly Mock<IMlPredictionClient> _mlClient = new();
    private readonly Mock<ILogger<PredictionService>> _logger = new();

    private PredictionService CreateService() => new(
        _stockRepo.Object, _priceRepo.Object, _predRepo.Object,
        _cache.Object, _mlClient.Object, _logger.Object);

    private static readonly Guid UserId = Guid.NewGuid();
    private const string CacheKey = "prediction:bist100:1d";

    private static Stock CreateXu100() => Stock.Create("XU100", "BIST 100 Endeksi", "Endeks");

    private static List<StockPrice> FakePrices(int count = 30)
    {
        var stock = CreateXu100();
        return Enumerable.Range(0, count)
            .Select(i => StockPrice.Create(
                stockId: stock.Id,
                date: DateOnly.FromDateTime(DateTime.Today.AddDays(-count + i)),
                open: 10000 + i * 10,
                high: 10050 + i * 10,
                low: 9950 + i * 10,
                close: 10020 + i * 10,
                volume: 5_000_000_000L))
            .ToList();
    }

    [Fact]
    public async Task PredictAsync_CacheHit_MlCagrilmaz()
    {
        var cached = new PredictionResponseDto(Guid.NewGuid(), 10500m, DateTime.UtcNow, FromCache: false);

        _cache.Setup(c => c.GetAsync<PredictionResponseDto>(CacheKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cached);

        var result = await CreateService().PredictAsync(UserId);

        result.FromCache.Should().BeTrue();
        result.PredictedPrice.Should().Be(10500m);
        _mlClient.Verify(m => m.PredictAsync(It.IsAny<MlPredictionInput>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PredictAsync_Bist100DbdeYok_NotFoundExceptionFirlatir()
    {
        _cache.Setup(c => c.GetAsync<PredictionResponseDto>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PredictionResponseDto?)null);
        _stockRepo.Setup(r => r.GetBySymbolAsync("XU100", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Stock?)null);

        var act = () => CreateService().PredictAsync(UserId);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("*XU100*");
    }

    [Fact]
    public async Task PredictAsync_YetersizVeri_InsufficientDataExceptionFirlatir()
    {
        _cache.Setup(c => c.GetAsync<PredictionResponseDto>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PredictionResponseDto?)null);
        _stockRepo.Setup(r => r.GetBySymbolAsync("XU100", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateXu100());
        _priceRepo.Setup(r => r.GetBySymbolAsync("XU100", 60, It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakePrices(count: 10));

        var act = () => CreateService().PredictAsync(UserId);

        await act.Should().ThrowAsync<InsufficientDataException>().WithMessage("*24*");
    }

    [Fact]
    public async Task PredictAsync_HappyPath_TahminFiyatiniDondurur()
    {
        SetupHappyPath(predictedPrice: 10750m);

        var result = await CreateService().PredictAsync(UserId);

        result.PredictedPrice.Should().Be(10750m);
        result.FromCache.Should().BeFalse();
    }

    [Fact]
    public async Task PredictAsync_HappyPath_DbKaydetCagrilir()
    {
        SetupHappyPath(predictedPrice: 10750m);

        await CreateService().PredictAsync(UserId);

        _predRepo.Verify(r => r.AddAsync(It.IsAny<Prediction>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PredictAsync_HappyPath_CacheSetCagrilir()
    {
        SetupHappyPath(predictedPrice: 10750m);

        await CreateService().PredictAsync(UserId);

        _cache.Verify(c => c.SetAsync(
            CacheKey, It.IsAny<PredictionResponseDto>(),
            It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetHistoryAsync_BosListe_BosDondurur()
    {
        var userId = Guid.NewGuid();
        _predRepo.Setup(r => r.GetByUserAsync(userId, "XU100", 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await CreateService().GetHistoryAsync(userId, count: 10);

        result.Should().BeEmpty();
    }

    private void SetupHappyPath(decimal predictedPrice)
    {
        _cache.Setup(c => c.GetAsync<PredictionResponseDto>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PredictionResponseDto?)null);
        _stockRepo.Setup(r => r.GetBySymbolAsync("XU100", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateXu100());
        _priceRepo.Setup(r => r.GetBySymbolAsync("XU100", 60, It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakePrices());
        _mlClient.Setup(m => m.PredictAsync(It.IsAny<MlPredictionInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MlPredictionOutput(predictedPrice));
    }
}
