using Telegram.Bot.Types.ReplyMarkups;
using UglyAppFramework.DependencyManager.Attributes;
using UglyAppFramework.StateManage;
using UglyFwUI.Rendering;
using UglyFwUI.ViewModels;
using UglyTgApplication.View;

namespace UglyFwUI.Views;

[Managed]
public class PickerDialogView : IView
{
    public ViewResponse Display(IState viewModel)
    {
        var model = (IPickerDialogState)viewModel;
        var rows = model.Choices.Select(choice => new[]
        {
            InlineKeyboardButton.WithCallbackData(choice.Label, choice.CallbackData)
        }).ToList();
        rows.Add(new[] { InlineKeyboardButton.WithCallbackData("Back", model.BackCallbackData) });
        var text = HtmlText.Encode(model.Prompt);
        if (model.Choices.Count == 0) text += "\n\nNo options available.";
        return new ViewSimpleResponse(text, new InlineKeyboardMarkup(rows));
    }
}
