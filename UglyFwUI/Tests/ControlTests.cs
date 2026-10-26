using System.Net;
using System.Text;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using UglyAppFramework.DependencyManager;
using UglyAppFramework.DependencyManager.Attributes;
using UglyAppFramework.StateManage;
using UglyFwUI.Forms;
using UglyFwUI.Models;
using UglyFwUI.Rendering;
using UglyFwUI.ViewModels;
using UglyFwUI.Views;
using UglyTgApplication;
using UglyTgApplication.Attributes;
using UglyTgApplication.States;
using UglyTgApplication.View;

namespace UglyFwUI.Tests;

public class ControlTests
{
    [Test]
    public async Task TextValidationKeepsDialogOpenAndSuccessRendersUpdatedParent()
    {
        using var bot = new Harness();
        await bot.Start();
        var page = bot.Page;
        await bot.Click(page.Name.CallbackData);
        var dialog = (TextDialogViewModel)bot.Current;
        await bot.Text(" ");
        Assert.That(bot.Current, Is.SameAs(dialog));
        Assert.That(dialog.ErrorMessage, Is.EqualTo("Required <name>"));
        Assert.That(bot.Transport.LastText, Does.Contain("Required &lt;name&gt;"));
        Assert.That(page.Changes, Is.Zero);
        await bot.Text("<Ana&>");
        Assert.That(bot.Current, Is.SameAs(page));
        Assert.That(page.Name.Text, Is.EqualTo("<Ana&>"));
        Assert.That(page.Changes, Is.EqualTo(1));
        Assert.That(bot.Transport.LastText, Does.Contain("&lt;Ana&amp;&gt;"));
    }

    [Test]
    public async Task BackButtonAndBackCommandCancelWithoutSubmitting()
    {
        using var bot = new Harness();
        await bot.Start();
        var page = bot.Page;
        await bot.Click(page.Name.CallbackData);
        await bot.Click(((TextDialogViewModel)bot.Current).BackCallbackData);
        Assert.That(bot.Current, Is.SameAs(page));
        await bot.Click(page.Name.CallbackData);
        await bot.Text("/back");
        Assert.That(bot.Current, Is.SameAs(page));
        Assert.That(page.Name.Text, Is.Empty);
        Assert.That(page.Changes, Is.Zero);
    }

    [Test]
    public async Task PickerKeepsTypedValuesAndDuplicateLabelsAndSnapshotsOptions()
    {
        using var bot = new Harness();
        await bot.Start();
        var page = bot.Page;
        page.SourceOptions.Clear();
        await bot.Click(page.Choice.CallbackData);
        var picker = (PickerDialogViewModel<int>)bot.Current;
        Assert.That(picker.Options.Count, Is.EqualTo(2));
        await bot.Click(ChoiceCallback(picker, 1));
        Assert.That(bot.Current, Is.SameAs(page));
        Assert.That(page.Choice.HasSelection, Is.True);
        Assert.That(page.Choice.SelectedValue, Is.EqualTo(20));
        Assert.That(page.Choice.SelectedText, Is.EqualTo("<Choice>"));
        Assert.That(bot.Transport.LastText, Does.Contain("&lt;Choice&gt;"));
    }

    [Test]
    public void EmptyPickerStillHasPromptAndBackButton()
    {
        var picker = new PickerDialogViewModel<int>("<Empty>", Array.Empty<PickerOption<int>>(),
            (_, _) => Task.CompletedTask);
        var response = new PickerDialogView().Display(picker).ResponseMessages.Single();
        Assert.That(response.text, Does.Contain("&lt;Empty&gt;").And.Contain("No options available"));
        Assert.That(response.replyMarkup!.InlineKeyboard.Single().Single().CallbackData,
            Is.EqualTo(picker.BackCallbackData));
    }

    [Test]
    public async Task OldPickerKeyboardCannotSelectFromNewDialog()
    {
        using var bot = new Harness();
        await bot.Start();
        var page = bot.Page;
        await bot.Click(page.Choice.CallbackData);
        var first = (PickerDialogViewModel<int>)bot.Current;
        var stale = ChoiceCallback(first, 0);
        await bot.Click(first.BackCallbackData);
        await bot.Click(page.Choice.CallbackData);
        var second = (PickerDialogViewModel<int>)bot.Current;
        await bot.Click(stale);
        Assert.That(bot.Current, Is.SameAs(second));
        Assert.That(page.Choice.HasSelection, Is.False);
        await bot.Click(ChoiceCallback(second, 0));
        Assert.That(bot.Current, Is.SameAs(page));
        Assert.That(page.Choice.SelectedValue, Is.EqualTo(10));
    }

    [Test]
    public async Task FailedTextCallbackIsObservedAndCanBeRetriedWithoutLosingDialog()
    {
        using var bot = new Harness();
        await bot.Start();
        var page = bot.Page;
        page.FailChange = true;
        await bot.Click(page.Name.CallbackData);
        Assert.ThrowsAsync<InvalidOperationException>(async () => await bot.Text("Alice"));
        Assert.That(bot.Current, Is.TypeOf<TextDialogViewModel>());
        Assert.That(page.Name.Text, Is.Empty);
        page.FailChange = false;
        await bot.Text("Alice");
        Assert.That(bot.Current, Is.SameAs(page));
        Assert.That(page.Changes, Is.EqualTo(1));
    }

    [Test]
    public async Task FormRenderingDoesNotRegisterNewCallbacksAndButtonTasksAreAwaited()
    {
        using var bot = new Harness();
        await bot.Start();
        var page = bot.Page;
        var view = new FormPageView();
        var first = view.Display(page).ResponseMessages.Single();
        var second = view.Display(page).ResponseMessages.Single();
        Assert.That(second.replyMarkup!.InlineKeyboard.SelectMany(r => r).Select(b => b.CallbackData),
            Is.EqualTo(first.replyMarkup!.InlineKeyboard.SelectMany(r => r).Select(b => b.CallbackData)));
        var callbacks = page.Fields.Select(f => f.CallbackData).ToArray();
        Assert.That(callbacks.Distinct().Count(), Is.EqualTo(callbacks.Length));
        Assert.That(callbacks.All(c => Encoding.UTF8.GetByteCount(c) <= 64), Is.True);
        await bot.Click(page.Save.CallbackData);
        Assert.That(page.Clicks, Is.EqualTo(1));
        page.FailButton = true;
        Assert.ThrowsAsync<InvalidOperationException>(async () => await bot.Click(page.Save.CallbackData));
        Assert.That(page.Clicks, Is.EqualTo(1));
        Assert.That(bot.Current, Is.SameAs(page));
    }

    [Test]
    public async Task NonTextInputDoesNotSubmitTextDialog()
    {
        using var bot = new Harness();
        await bot.Start();
        var page = bot.Page;
        await bot.Click(page.Name.CallbackData);
        var dialog = bot.Current;
        await bot.Text(null);
        Assert.That(bot.Current, Is.SameAs(dialog));
        Assert.That(page.Changes, Is.Zero);
    }

    [Test]
    public void ScrollBoxFollowsNewItemsUntilScrolledThenStaysPut()
    {
        var items = Enumerable.Range(1, 10).ToList();
        var box = new FormScrollBox<int>(new FormPage(), () => items, pageSize: 4);
        var first = box.GetWindow();
        Assert.That((first.Older, first.Newer, first.AutoScroll), Is.EqualTo((6, 0, true)));
        Assert.That(box.GetWindow().Items, Is.EqualTo(new[] { 7, 8, 9, 10 }));
        items.Add(11);
        Assert.That(box.GetWindow().Items, Is.EqualTo(new[] { 8, 9, 10, 11 }));

        box.ScrollOlder();
        Assert.That(box.GetWindow().Items, Is.EqualTo(new[] { 4, 5, 6, 7 }));
        items.Add(12);
        var window = box.GetWindow();
        Assert.That(window.Items, Is.EqualTo(new[] { 4, 5, 6, 7 }));
        Assert.That((window.Older, window.Newer, window.AutoScroll), Is.EqualTo((3, 5, false)));

        box.ScrollOlder();
        box.ScrollOlder();
        Assert.That(box.GetWindow().Items, Is.EqualTo(new[] { 1, 2, 3, 4 }));
        box.ScrollNewer();
        Assert.That(box.GetWindow().Items, Is.EqualTo(new[] { 5, 6, 7, 8 }));
        Assert.That(box.GetWindow().AutoScroll, Is.False);
        box.ScrollNewer();
        Assert.That(box.GetWindow().AutoScroll, Is.True, "reaching the bottom sticks to it again");

        box.ToggleAutoScroll();
        items.Add(13);
        Assert.That(box.GetWindow().Items, Is.EqualTo(new[] { 9, 10, 11, 12 }));
        box.ScrollToLatest();
        Assert.That(box.GetWindow().Items, Is.EqualTo(new[] { 10, 11, 12, 13 }));
        box.ScrollToOldest();
        Assert.That(box.GetWindow().Items.First(), Is.EqualTo(1));
    }

    [Test]
    public async Task ScrollButtonsRenderTheOwnerAndLayoutEscapesAndBoundsItems()
    {
        using var bot = new Harness();
        await bot.Start();
        var page = bot.Page;
        page.Log.AddRange(Enumerable.Range(1, 6).Select(i => $"<{i}>"));
        await bot.Click(page.Scroll.OlderCallbackData);
        Assert.That(bot.Transport.LastText, Does.Contain("&lt;1&gt;").And.Contain("▼ 3 newer"));

        var layout = FormLayout.Create("t").Scroll(page.Scroll, HtmlText.Encode, maxLength: 8).Build()
            .ResponseMessages.Single();
        // The newest visible line always shows; lines over the budget count as earlier.
        Assert.That(layout.text, Does.Contain("▲ 2 earlier").And.Contain("&lt;3&gt;").And.Not.Contain("&lt;2&gt;"));
        Assert.That(layout.replyMarkup!.InlineKeyboard.Single().Select(b => b.CallbackData), Is.EqualTo(new[]
        {
            page.Scroll.OldestCallbackData, page.Scroll.OlderCallbackData, page.Scroll.NewerCallbackData,
            page.Scroll.LatestCallbackData, page.Scroll.AutoScrollCallbackData
        }));
        page.Log.Clear();
        Assert.That(FormLayout.Create("t").Scroll(page.Scroll, HtmlText.Encode, "<empty>").Build()
            .ResponseMessages.Single().text, Does.Contain("&lt;empty&gt;"));
    }

    [Test]
    public async Task ToggleAwaitsChangeBeforeFlippingAndRenders()
    {
        using var bot = new Harness();
        await bot.Start();
        var page = bot.Page;
        page.FailChange = true;
        Assert.ThrowsAsync<InvalidOperationException>(async () => await bot.Click(page.Toggle.CallbackData));
        Assert.That(page.Toggle.Value, Is.False);
        page.FailChange = false;
        await bot.Click(page.Toggle.CallbackData);
        Assert.That(page.Toggle.Value, Is.True);
        Assert.That(page.Toggle.DisplayValue, Is.EqualTo("Yes"));
        Assert.That(bot.Transport.LastText, Does.Contain("Toggle on"));
    }

    [Test]
    public async Task InputButtonSubmitsOnceWithoutStoringAndValidatesInPlace()
    {
        using var bot = new Harness();
        await bot.Start();
        var page = bot.Page;
        await bot.Click(page.Input.CallbackData);
        var dialog = (TextDialogViewModel)bot.Current;
        await bot.Text(" ");
        Assert.That(bot.Current, Is.SameAs(dialog));
        Assert.That(page.Submitted, Is.Empty);
        await bot.Text("one");
        Assert.That(bot.Current, Is.SameAs(page));
        await bot.Click(page.Input.CallbackData);
        await bot.Text("two");
        Assert.That(page.Submitted, Is.EqualTo(new[] { "one", "two" }));
        Assert.That(page.Input.DisplayValue, Is.Null);
    }

    [Test]
    public void PickerPreselectionAndTextClearAndMinimalEscaping()
    {
        var page = new FormPage();
        Assert.That(page.Choice.Select(30), Is.False);
        Assert.That(page.Choice.HasSelection, Is.False);
        Assert.That(page.Choice.Select(20), Is.True);
        Assert.That((page.Choice.SelectedValue, page.Choice.SelectedText), Is.EqualTo((20, "<Choice>")));
        page.Name.Clear();
        Assert.That(page.Name.Text, Is.Empty);
        Assert.That(HtmlText.Encode("🐦 <a href=\"x\">&</a> ¯\\_(ツ)_/¯"),
            Is.EqualTo("🐦 &lt;a href=&quot;x&quot;&gt;&amp;&lt;/a&gt; ¯\\_(ツ)_/¯"));
    }

    private static string ChoiceCallback<T>(PickerDialogViewModel<T> picker, int index) =>
        new PickerDialogView().Display(picker).ResponseMessages.Single().replyMarkup!
            .InlineKeyboard.ElementAt(index).Single().CallbackData!;

    private sealed class Harness : IDisposable
    {
        public FakeTransport Transport { get; } = new();
        private readonly HttpClient _http;
        private readonly TelegramBotClient _client;
        private readonly UglyTgApp _app;
        private readonly SessionManager _sessions;
        public TgState Current => _sessions.GetCurrentContext(42, 42).CurrentState!;
        public FormPage Page => (FormPage)Current;

        public Harness()
        {
            _http = new HttpClient(Transport);
            _client = new TelegramBotClient("123456:offline-test-token", _http);
            var dependencies = new DependencyManager();
            _app = new UglyTgApp("123456:offline-test-token", typeof(FormPage));
            dependencies.InjectDependencies(_app);
            ((TelegramInteractionManager)dependencies.GetDependencyInstance(typeof(TelegramInteractionManager), new InjectAttribute()))
                ._telegramBotClient = _client;
            _sessions = (SessionManager)dependencies.GetDependencyInstance(typeof(SessionManager), new InjectAttribute());
        }

        public Task Start() => Text("hello");
        public Task Text(string? value) => _app.HandleUpdateAsync(_client, new Update
        {
            Message = new Message { MessageId = 99, Chat = new Chat { Id = 42 }, From = new User { Id = 42 }, Text = value }
        }, CancellationToken.None);
        public Task Click(string data) => _app.HandleUpdateAsync(_client, new Update
        {
            CallbackQuery = new CallbackQuery
            {
                Id = "button", From = new User { Id = 42 }, Data = data,
                Message = new Message { Chat = new Chat { Id = 42 } }
            }
        }, CancellationToken.None);
        public void Dispose() => _http.Dispose();
    }

    private sealed class FakeTransport : HttpMessageHandler
    {
        private int _messageId;
        public string LastText { get; private set; } = "";

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            var method = request.RequestUri!.Segments.Last();
            var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
            if (body.RootElement.TryGetProperty("text", out var text)) LastText = text.GetString()!;
            var result = method is "answerCallbackQuery" or "deleteMessage" ? "true"
                : "{\"message_id\":" + ++_messageId + ",\"date\":0,\"chat\":{\"id\":42,\"type\":\"private\"}}";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"ok\":true,\"result\":" + result + "}", Encoding.UTF8, "application/json")
            };
        }
    }

    [BindView(typeof(FormPageView))]
    public class FormPage : UglyTgApplication.States.ViewModel
    {
        public FormTextBox Name { get; }
        public FormPickerButton<int> Choice { get; }
        public FormButton<int> Save { get; }
        public FormToggle Toggle { get; }
        public FormInputButton Input { get; }
        public List<string> Submitted { get; } = new();
        public List<string> Log { get; } = new();
        public FormScrollBox<string> Scroll { get; }
        public IReadOnlyList<IFormElement> Fields { get; }
        public int Changes { get; private set; }
        public int Clicks { get; private set; }
        public bool FailChange { get; set; }
        public bool FailButton { get; set; }
        public List<PickerOption<int>> SourceOptions { get; } = new()
        {
            new("<Choice>", 10), new("<Choice>", 20)
        };

        public FormPage()
        {
            RemoveUserMessagesPolicyEnabled = false;
            Name = new FormTextBox(this, "<Name>", validate: value => string.IsNullOrWhiteSpace(value) ? "Required <name>" : null,
                onChanged: async (_, _) =>
                {
                    await Task.Yield();
                    if (FailChange) throw new InvalidOperationException("change failed");
                    Changes++;
                });
            Choice = new FormPickerButton<int>(this, "Choice", SourceOptions);
            Save = new FormButton<int>(this, "Save", 1, async (value, token) =>
            {
                await Task.Yield();
                if (FailButton) throw new InvalidOperationException("save failed");
                Clicks += value;
                await UpdateViewAsync(token);
            });
            Toggle = new FormToggle(this, "Toggle", onChanged: async (_, _) =>
            {
                await Task.Yield();
                if (FailChange) throw new InvalidOperationException("toggle failed");
            }, onText: "Yes");
            Scroll = new FormScrollBox<string>(this, () => Log, pageSize: 3);
            Input = new FormInputButton(this, "Input", "Say:", (text, _) =>
            {
                Submitted.Add(text);
                return Task.CompletedTask;
            }, text => string.IsNullOrWhiteSpace(text) ? "Required" : null);
            Fields = new IFormElement[] { Name, Choice, Save };
        }
    }

    [Managed]
    public class FormPageView : IView
    {
        public ViewResponse Display(IState state)
        {
            var model = (FormPage)state;
            return model.Log.Count > 0
                ? FormLayout.Create("Log").Scroll(model.Scroll, HtmlText.Encode).Build()
                : FormRenderer.Render("Form " + model.Clicks + (model.Toggle.Value ? " Toggle on" : ""), model.Fields);
        }
    }
}
