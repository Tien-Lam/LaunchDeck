using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace LaunchDeck.Companion;

internal sealed class FocusStealSimulator : IDisposable
{
    private readonly ManualResetEventSlim _shown = new(false);
    private readonly Thread _thread;
    private Form? _form;

    private FocusStealSimulator(int afterMs, int durationMs)
    {
        _thread = new Thread(() => Run(afterMs, durationMs))
        {
            IsBackground = true,
            Name = "LaunchDeck focus-steal simulator"
        };
        _thread.SetApartmentState(ApartmentState.STA);
    }

    internal static FocusStealSimulator Start(int afterMs, int durationMs)
    {
        var simulator = new FocusStealSimulator(afterMs, durationMs);
        simulator._thread.Start();
        return simulator;
    }

    private void Run(int afterMs, int durationMs)
    {
        try
        {
            if (afterMs > 0)
                Thread.Sleep(afterMs);

            using var form = new Form
            {
                Text = "LaunchDeck Focus Simulation",
                StartPosition = FormStartPosition.CenterScreen,
                Size = new Size(420, 140),
                TopMost = false,
                ShowInTaskbar = false
            };
            form.Controls.Add(new Label
            {
                Text = "Simulating Game Bar focus returning after launch.",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            });
            _form = form;
            form.Shown += (_, _) =>
            {
                form.Activate();
                form.BringToFront();
                _shown.Set();
            };

            if (durationMs > 0)
            {
                var timer = new System.Windows.Forms.Timer { Interval = durationMs };
                timer.Tick += (_, _) =>
                {
                    timer.Stop();
                    form.Close();
                };
                form.Shown += (_, _) => timer.Start();
            }

            Application.Run(form);
        }
        catch (Exception ex)
        {
            Log.Write($"focus-steal-simulator: {ex.GetType().Name}: {ex.Message}");
            _shown.Set();
        }
    }

    internal bool WaitUntilShown(int timeoutMs)
    {
        return _shown.Wait(timeoutMs);
    }

    public void Dispose()
    {
        try
        {
            var form = _form;
            if (form != null && !form.IsDisposed)
                form.BeginInvoke(new Action(() => form.Close()));
        }
        catch { }
    }
}
