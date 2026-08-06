using MsrPlayer;
using System.Threading;
using System.Threading.Tasks;

namespace MsrPlayer.Tests;

public class SingleInstanceManagerTests
{
    [Fact]
    public void TryAcquireMutex_FirstInstance_ReturnsTrueAndOwnsMutex()
    {
        bool acquired = SingleInstanceManager.TryAcquireMutex(out Mutex? mutex);

        Assert.True(acquired);
        Assert.NotNull(mutex);

        mutex!.Dispose();
    }

    [Fact]
    public void TryAcquireMutex_SecondInstanceWhileHeld_ReturnsFalse()
    {
        Assert.True(SingleInstanceManager.TryAcquireMutex(out Mutex? first));
        try
        {
            bool acquired = SingleInstanceManager.TryAcquireMutex(out Mutex? second);

            Assert.False(acquired);
            Assert.Null(second);
        }
        finally
        {
            first!.Dispose();
        }
    }

    [Fact]
    public void TryAcquireMutex_AfterRelease_CanAcquireAgain()
    {
        Assert.True(SingleInstanceManager.TryAcquireMutex(out Mutex? first));
        first!.Dispose();

        Assert.True(SingleInstanceManager.TryAcquireMutex(out Mutex? second));
        second!.Dispose();
    }

    [Fact]
    public async Task ActivateExistingInstance_WhenListenerRunning_InvokesCallback()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var callback = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task listener = SingleInstanceManager.StartListenerAsync(
            () => { callback.TrySetResult(); return null; }, cts.Token);

        try
        {
            await SingleInstanceManager.ActivateExistingInstanceAsync();

            await callback.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }
        finally
        {
            cts.Cancel();
            await listener;
        }
    }

    [Fact]
    public async Task ActivateExistingInstance_WithoutListener_DoesNotThrow()
    {
        await SingleInstanceManager.ActivateExistingInstanceAsync();
    }
}
