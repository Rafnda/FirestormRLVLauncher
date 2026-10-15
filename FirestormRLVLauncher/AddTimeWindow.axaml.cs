using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System.IO;

namespace FirestormRLVLauncher;

public partial class AddTimeWindow : Window
{
    public LoginData? _login { get; set; }
    public bool accepted = false;
    public AddTimeWindow()
    {
        InitializeComponent();
    }
    public AddTimeWindow(LoginData login) {
        _login = login;
        InitializeComponent();
        // use the window itself as the binding context so compiled bindings to HidePassword work
        this.DataContext = this;

        TextBlock? fileLabel = this.FindControl<TextBlock>("ProfileFileText");
        if (fileLabel != null)
            fileLabel.Text = Path.GetFileName(_login._filePath);

        TextBlock? timeText = this.FindControl<TextBlock>("TimeText");
        if (timeText != null) {
            double elapsedHours = _login._elapsedMinutes.Value / 60.0;
            this.TimeText.Text = elapsedHours.ToString("F1") + " / " + (_login._totalMinutes.Value / 60).ToString() + " hours";
        }

        NumericUpDown? durationNum = this.FindControl<NumericUpDown>("DurationNum");
        if (durationNum != null) {
            durationNum.Value = (decimal)(_login._totalMinutes.Value / 60.0);
            durationNum.Minimum = durationNum.Value.Value;
        }

        SetPreview(null, null);
    }

    private void SetPreview(object? sender, NumericUpDownValueChangedEventArgs e) {
        if (_login == null) return;
        NumericUpDown? durationNum = this.FindControl<NumericUpDown>("DurationNum");
        if (durationNum != null) {
            double newTotalHours = (double)durationNum.Value;
            double newTotalMinutes = newTotalHours * 60.0;
            double elapsedHours = (_login._elapsedMinutes.Value - 600) / 60.0;
            if (elapsedHours < 0) elapsedHours = 0;
            this.PreviewTimeText.Text = elapsedHours.ToString("F1") + " / " + newTotalHours.ToString() + " hours";
        }
    }

    private async void ApplyButton_Click(object? sender, RoutedEventArgs e) {
        if (_login == null)
            return;
        NumericUpDown? durationNum = this.FindControl<NumericUpDown>("DurationNum");
        if (durationNum == null)
            return;
        Button lockBtn = new Button { Name = "LockConfirmButton", Content = "Confirm", Width = 80 };
        Button cancelBtn = new Button { Name = "CancelButton", Content = "Cancel", Width = 80 };
        string message = "Are you sure you want to increase the lock time to " + durationNum.Value.ToString() + " hours? It can't be decreased!";
        Window confirmDialog = new Window
        {
            Title = "Confirm Lock Time Change",
            Width = 300,
            Height = 150,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new StackPanel
            {
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Spacing = 16,
                Margin = new Avalonia.Thickness(10),
                Children =
                    {
                        new TextBlock {
                            Text = message,
                            TextWrapping = Avalonia.Media.TextWrapping.Wrap
                        },
                        new StackPanel
                        {
                            Orientation = Avalonia.Layout.Orientation.Horizontal,
                            Spacing = 8,
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Bottom,
                            Children =
                            {
                                lockBtn, cancelBtn
                            }
                        }
                    }
            }
        };

        if (lockBtn != null)
            lockBtn.Click += (s, e) => { accepted = true; confirmDialog.Close(); };
        if (cancelBtn != null)
            cancelBtn.Click += (s, e) => confirmDialog.Close();

        await confirmDialog.ShowDialog(this);

        if (!accepted)
            return;

        _login._elapsedMinutes.Value = _login._elapsedMinutes.Value - 10;
        if (_login._elapsedMinutes.Value < 0)
            _login._elapsedMinutes.Value = 0;
        if (durationNum.Value * 60 > _login._totalMinutes.Value) {
            _login._totalMinutes.Value = (int)(durationNum.Value * 60);
        }
        _login.SaveLogin();
        this.Close();
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e) {
        _login = null;
        this.Close();
    }
}