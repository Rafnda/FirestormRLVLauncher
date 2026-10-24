using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using System;
using System.IO;
using System.ComponentModel;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Platform.Storage;

namespace FirestormRLVLauncher
{
    public partial class LoginDetailsWindow : Window, INotifyPropertyChanged
    {
        private bool _hidePassword = true;

        public LoginData? _login { get; set; }

        public bool HidePassword
        {
            get => _hidePassword;
            set
            {
                if (_hidePassword == value) return;
                _hidePassword = value;
                OnPropertyChanged(nameof(HidePassword));
                var passwordBox = this.FindControl<TextBox>("PasswordBox");
                if (passwordBox != null)
                    passwordBox.PasswordChar = _hidePassword ? '*' : '\0';
                
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public LoginDetailsWindow()
        {
            InitializeComponent();
            // use the window itself as the binding context so compiled bindings to HidePassword work
            this.DataContext = this;
        }

        public LoginDetailsWindow(LoginData login) {
            _login = login;
            InitializeComponent();
            // use the window itself as the binding context so compiled bindings to HidePassword work
            this.DataContext = this;

            // Fill UI from login data
            TextBox? usernameBox = this.FindControl<TextBox>("UsernameText");
            if (usernameBox != null)
                usernameBox.Text = _login._userName;

            TextBox? passwordBox = this.FindControl<TextBox>("PasswordBox");
            if (passwordBox != null)
                passwordBox.Text = _login.GetPassword();

            NumericUpDown? durationNum = this.FindControl<NumericUpDown>("DurationNum");
            if (durationNum != null)
                durationNum.Value = (decimal)(_login._totalMinutes.Value / 60.0);

            TextBlock? fileLabel = this.FindControl<TextBlock>("ProfileFileText");
            if (fileLabel != null)
                fileLabel.Text = Path.GetFileName(_login._filePath);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }


        private async void SaveButton_Click(object? sender, RoutedEventArgs e)
        {
            TextBox? usernameBox = this.FindControl<TextBox>("UsernameText");
            TextBox? passwordBox = this.FindControl<TextBox>("PasswordBox");
            NumericUpDown? durationNum = this.FindControl<NumericUpDown>("DurationNum");

            string? username = usernameBox?.Text;
            string? password = passwordBox?.Text;
            int hours = (int)(durationNum?.Value ?? 50);
            int totalMinutes = Math.Max(1, hours) * 60;

            if (username == null || username.Length == 0)
                return;

            if (_login == null) {

                // ask user where to save the profile
                TopLevel? top = TopLevel.GetTopLevel(this);
                if (top == null)
                    return;

                Avalonia.Platform.Storage.IStorageFolder? startFolder = null;
                if (!string.IsNullOrEmpty(Settings._lastLoginFile)) {
                    string? folderPath = Path.GetDirectoryName(Settings._lastLoginFile);
                    if (!string.IsNullOrEmpty(folderPath)) {
                        try {
                            startFolder = await top.StorageProvider.TryGetFolderFromPathAsync(folderPath);
                        }
                        catch (Exception) { }
                    }
                }

                FilePickerSaveOptions saveOptions = new FilePickerSaveOptions
                {
                    Title = "Save login",
                    SuggestedFileName = username + ".login.enc",
                    FileTypeChoices = new System.Collections.Generic.List<FilePickerFileType> {
                        new FilePickerFileType("Profile") { Patterns = new[] { "*.login.enc", "*.enc" } } },
                    SuggestedStartLocation = startFolder
                };

                IStorageFile? picked = await top.StorageProvider.SaveFilePickerAsync(saveOptions);
                if (picked == null)
                    return; // cancelled

                string? localPath = picked.TryGetLocalPath();
                if (string.IsNullOrEmpty(localPath))
                    return;

                // create and populate ProfileData
                LoginData profile = new LoginData(localPath, username ?? string.Empty, password ?? string.Empty);
                profile._totalMinutes.Value = totalMinutes;
                profile._elapsedMinutes.Value = 0;

                try {
                    bool saved = profile.SaveLogin();
                    if (saved) {
                        Settings._lastLoginFile = localPath;
                        Settings.SaveSettings();
                        _login = profile;
                    }
                }
                catch (Exception) { }
            }
            else {
                // Update existing login
                _login._userName = username ?? string.Empty;
                _login.SetPassword(password ?? string.Empty);
                _login._totalMinutes.Value = totalMinutes;

                _login.SaveLogin();
            }

            this.Close();
        }

        private void CancelButton_Click(object? sender, RoutedEventArgs e) {
            _login = null;
            this.Close();
        }

        private void HidePasswordBox_Toggle(object? sender, RoutedEventArgs e)
        {
            var hideBox = this.FindControl<CheckBox>("HidePasswordBox");
            var passwordBox = this.FindControl<TextBox>("PasswordBox");
            if (passwordBox == null || hideBox == null)
                return;

            passwordBox.PasswordChar = hideBox.IsChecked == true ? '*' : '\0';
        }

    }
}