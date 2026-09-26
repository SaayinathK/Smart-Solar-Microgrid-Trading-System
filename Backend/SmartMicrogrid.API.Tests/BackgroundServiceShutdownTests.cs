using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SmartMicrogrid.API.Models.M1;
using SmartMicrogrid.API.Repositories.Interfaces;
using SmartMicrogrid.API.Services.Implementation;
using Xunit;

namespace SmartMicrogrid.API.Tests;

public class BackgroundServiceShutdownTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SlotCleanupCompletesCleanlyWhenStoppedOrStartupIsAborted(bool abortStartup)
    {
        var slots = new Mock<IEnergySlotRepository>();
        slots.Setup(repository => repository.GetAllAsync(null, "Available", null, null, null))
            .ReturnsAsync(Array.Empty<EnergySlot>());
        using var provider = new ServiceCollection()
            .AddSingleton(slots.Object)
            .AddSingleton(Mock.Of<IMicrogridRepository>())
            .BuildServiceProvider();
        using var worker = new EnergySlotCleanupService(NullLogger<EnergySlotCleanupService>.Instance, provider);

        await VerifyCleanShutdown(worker, abortStartup);
        slots.Verify(repository => repository.GetAllAsync(null, "Available", null, null, null), Times.Once);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReservationExpiryCompletesCleanlyWhenStoppedOrStartupIsAborted(bool abortStartup)
    {
        using var provider = new ServiceCollection().BuildServiceProvider();
        using var worker = new ReservationExpiryService(provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<ReservationExpiryService>.Instance);

        await VerifyCleanShutdown(worker, abortStartup);
    }

    private static async Task VerifyCleanShutdown(BackgroundService worker, bool abortStartup)
    {
        await worker.StartAsync(CancellationToken.None);
        var execution = Assert.IsAssignableFrom<Task>(worker.ExecuteTask);
        Assert.False(execution.IsCompleted);
        if (abortStartup)
            worker.Dispose(); // Host disposal after a bind failure cancels workers without StopAsync.
        else
            await worker.StopAsync(CancellationToken.None);

        await execution.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.True(execution.IsCompletedSuccessfully);
    }
}
