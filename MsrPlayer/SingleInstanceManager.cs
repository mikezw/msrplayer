using System;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MsrPlayer;

/// <summary>
/// Ensures only one instance of the app runs at a time.
/// A named mutex guards instance creation; a named pipe lets a second
/// instance ask the first one to show its window and activate it.
/// </summary>
internal static class SingleInstanceManager
{
    private const string MutexName = "MsrPlayer_SingleInstance_Mutex";
    private const string PipeName = "MsrPlayer_SingleInstance_Pipe";
    private const string ShowWindowCommand = "ShowWindow";

    /// <summary>
    /// Tries to acquire the single-instance mutex.
    /// </summary>
    /// <param name="mutex">The acquired mutex when this process is the first instance; null otherwise.</param>
    /// <returns>True when this process is the first instance.</returns>
    public static bool TryAcquireMutex(out Mutex? mutex)
    {
        mutex = new Mutex(true, MutexName, out bool createdNew);
        if (!createdNew)
        {
            mutex.Dispose();
            mutex = null;
        }

        return createdNew;
    }

    /// <summary>
    /// Asks the running instance to show its window, then brings it to the
    /// foreground. The second instance was just launched by the user, so
    /// Windows grants it the foreground rights the first instance lacks.
    /// </summary>
    public static async Task ActivateExistingInstanceAsync()
    {
        try
        {
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            client.Connect(TimeSpan.FromSeconds(3));
            using var writer = new StreamWriter(client) { AutoFlush = true };
            // Disable BOM detection: its constructor would block reading
            // bytes before the command is sent, deadlocking both sides.
            using var reader = new StreamReader(client, Encoding.UTF8, false);

            writer.WriteLine(ShowWindowCommand);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            string? handle = await reader.ReadLineAsync(timeout.Token).ConfigureAwait(false);
            if (OperatingSystem.IsWindows() &&
                long.TryParse(handle, out long hwnd) && hwnd != 0)
            {
                SetForegroundWindow((IntPtr)hwnd);
            }
        }
        catch
        {
            // The first instance may still be starting up; ignore the failure.
        }
    }

    /// <summary>
    /// Listens for activation commands from other instances until cancelled.
    /// </summary>
    /// <param name="onShowWindow">Shows the window and returns its native handle; invoked on the thread-pool thread.</param>
    public static async Task StartListenerAsync(Func<string?> onShowWindow, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var server = new NamedPipeServerStream(PipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                await server.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);
                // Disable BOM detection: its constructor would block reading
                // bytes before the client sends the command.
                using var reader = new StreamReader(server, Encoding.UTF8, false);
                using var writer = new StreamWriter(server) { AutoFlush = true };

                string? command = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                if (command == ShowWindowCommand)
                {
                    string? handle = onShowWindow();
                    await writer.WriteLineAsync(handle ?? string.Empty).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // Keep listening despite transient pipe errors.
            }
        }
    }

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);
}
