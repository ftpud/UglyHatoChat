using TelegramTestBot.ViewModels;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using UglyTgApplication;
using UglyTgApplication.Services;

namespace TelegramTestBot.Hosting;

// This example accepts private text chats; polling, sessions and dispatch live in the framework.
public class TestBotHost : UglyTgApp
{
    public TestBotHost(string token, CancellationToken shutdown = default, IUiMessageStore? messageStore = null)
        : base(token, typeof(LobbyViewModel), shutdown, messageStore)
    {
        RemoveUserMessagesPolicyOverride = true;
    }

    public override Task HandleUpdateAsync(ITelegramBotClient client, Update update,
        CancellationToken cancellationToken)
    {
        var message = update.CallbackQuery?.Message ?? update.Message;
        var text = update.CallbackQuery?.Data ?? update.Message?.Text;
        // Whitespace is still text: dialogs need it to display validation feedback.
        if (message?.Chat.Type != ChatType.Private || text == null)
            return Task.CompletedTask;
        return base.HandleUpdateAsync(client, update, cancellationToken);
    }
}
