using TelegramTestBot.Hosting;
using UglyAppFramework;
using UglyTgApplication.Services;

var token = Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN")?.Trim();
if (string.IsNullOrWhiteSpace(token))
{
    Console.Error.WriteLine("Set TELEGRAM_BOT_TOKEN to the token from Telegram's @BotFather.");
    return 1;
}

using var shutdown = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    shutdown.Cancel();
};

try
{
    var dataDirectory = Environment.GetEnvironmentVariable("TELEGRAM_BOT_DATA_DIR")
        ?? Path.Combine(AppContext.BaseDirectory, ".data");
    UglyApp.Start(new TestBotHost(token, shutdown.Token,
        new JsonUiMessageStore(Path.Combine(dataDirectory, "ui-messages"))));
    Console.WriteLine("Test bot is running. Open a private chat and send /start. Press Ctrl+C to stop.");
    await Task.Delay(Timeout.Infinite, shutdown.Token);
}
catch (OperationCanceledException) when (shutdown.IsCancellationRequested)
{
    // Normal Ctrl+C shutdown.
}
catch (Exception)
{
    Console.Error.WriteLine("Bot startup failed. Check TELEGRAM_BOT_TOKEN and your network connection.");
    return 1;
}

return 0;
