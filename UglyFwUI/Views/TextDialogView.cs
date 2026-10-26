using Telegram.Bot.Types.ReplyMarkups;
using UglyAppFramework.DependencyManager.Attributes;
using UglyAppFramework.StateManage;
using UglyFwUI.Rendering;
using UglyFwUI.ViewModels;
using UglyTgApplication.View;

namespace UglyFwUI.Views;

[Managed]
public class TextDialogView : IView
{
    public ViewResponse Display(IState viewModel)
    {
        var model = (TextDialogViewModel)viewModel;
        var text = HtmlText.Encode(model.Prompt);
        if (!string.IsNullOrEmpty(model.ErrorMessage))
            text += "\n\n" + HtmlText.Encode(model.ErrorMessage);
        return new ViewSimpleResponse(text, new InlineKeyboardMarkup(
            InlineKeyboardButton.WithCallbackData("Back", model.BackCallbackData)));
    }
}
