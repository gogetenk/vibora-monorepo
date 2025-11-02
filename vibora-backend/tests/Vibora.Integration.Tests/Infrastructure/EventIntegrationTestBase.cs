using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Vibora.Integration.Tests.Infrastructure;

/// <summary>
/// Base class for integration tests that verify MassTransit events
/// Automatically manages Test Harness lifecycle (Start/Stop)
/// Inherits from IntegrationTestBaseImproved to get Seeder and other improvements
/// </summary>
public abstract class EventIntegrationTestBase : IntegrationTestBaseImproved, IAsyncLifetime
{
    protected ITestHarness Harness { get; private set; } = null!;

    protected EventIntegrationTestBase(ViboraWebApplicationFactory factory) : base(factory)
    {
    }

    /// <summary>
    /// Initialize Test Harness before each test
    /// </summary>
    public new async Task InitializeAsync()
    {
        await base.InitializeAsync();

        Harness = Factory.Services.GetRequiredService<ITestHarness>();
        await Harness.Start();
    }

    /// <summary>
    /// Stop Test Harness and clean up after each test
    /// </summary>
    public new async Task DisposeAsync()
    {
        if (Harness != null)
        {
            await Harness.Stop();
        }

        await base.DisposeAsync();
    }

    /// <summary>
    /// Wait for a specific event to be published with polling and timeout
    /// Avoids flaky tests from Task.Delay
    /// </summary>
    protected async Task<bool> WaitForEventAsync<T>(
        Func<IPublishedMessage<T>, bool>? predicate = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var maxWait = timeout ?? TimeSpan.FromSeconds(10);
        var pollInterval = TimeSpan.FromMilliseconds(100);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        while (stopwatch.Elapsed < maxWait)
        {
            var published = await Harness.Published.Any<T>(cancellationToken);
            
            if (published)
            {
                if (predicate == null)
                {
                    return true;
                }

                var messages = Harness.Published.Select<T>().ToList();
                if (messages.Any(predicate))
                {
                    return true;
                }
            }

            await Task.Delay(pollInterval, cancellationToken);
        }

        return false;
    }

    /// <summary>
    /// Wait for a consumer to process a message with polling
    /// </summary>
    protected async Task<bool> WaitForConsumerAsync<TConsumer, TMessage>(
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
        where TConsumer : class, IConsumer
        where TMessage : class
    {
        var maxWait = timeout ?? TimeSpan.FromSeconds(10);
        var pollInterval = TimeSpan.FromMilliseconds(100);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        while (stopwatch.Elapsed < maxWait)
        {
            var consumed = await Harness.Consumed.Any<TMessage>(cancellationToken);
            
            if (consumed)
            {
                return true;
            }

            await Task.Delay(pollInterval, cancellationToken);
        }

        return false;
    }
}
