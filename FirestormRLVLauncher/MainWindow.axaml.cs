using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Desktop.Robot;
using Desktop.Robot.Extensions;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace FirestormRLVLauncher
{

    public class StatusMessage {
        public required string Message { get; set; }
        public required string Color { get; set; }
    }

    [SupportedOSPlatform("windows")]
    public partial class MainWindow : Window
    {
        private ObservableCollection<StatusMessage> _statusMessages;

        public string _firestormSettings;
        public LoginData? _loginData;
        public bool _running = false;

        public MainWindow()
        {
            InitializeComponent();
            _statusMessages = new ObservableCollection<StatusMessage>();
            this.StatusList.ItemsSource = _statusMessages;
            _firestormSettings = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Firestorm_x64\\user_settings\\settings.xml";
            Settings.LoadSettings();
            this.FirestormExeText.Text = Settings._firestormExe;
            if (Settings._lastLoginFile != null)
                LoadLogin(Settings._lastLoginFile);
        }

        private async void CreateProfile_Click(object? sender, RoutedEventArgs e) {
            LoginDetailsWindow detailsWindow = new LoginDetailsWindow();
            await detailsWindow.ShowDialog(this);
            if (detailsWindow._login == null)
                return;
            _loginData = detailsWindow._login;
            ShowLoginData();
        }

        private void OpenFolder_Click(object? sender, RoutedEventArgs e) {
            if (_loginData == null || _loginData._filePath == null)
                return;
            string? folderPath = Path.GetDirectoryName(_loginData._filePath);
            if (folderPath != null && Directory.Exists(folderPath)) {
                Process.Start(new ProcessStartInfo()
                {
                    FileName = folderPath,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
        }

        private async void LoadLogin_Click(object? sender, RoutedEventArgs e) {
            TopLevel? topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null)
                return;

            List<FilePickerFileType> fileTypes = new List<FilePickerFileType> {
                new FilePickerFileType("Login Files") { Patterns = new[] { "*.login.enc", "*.enc" } }
            };

            Avalonia.Platform.Storage.IStorageFolder? startFolder = null;
            if (!string.IsNullOrEmpty(Settings._lastLoginFile)) {
                string? folderPath = Path.GetDirectoryName(Settings._lastLoginFile);
                if (!string.IsNullOrEmpty(folderPath)) {
                    try {
                        startFolder = await topLevel.StorageProvider.TryGetFolderFromPathAsync(folderPath);
                    }
                    catch (Exception) { }
                }
            }

            FilePickerOpenOptions openOptions = new FilePickerOpenOptions
            {
                Title = "Load login",
                FileTypeFilter = fileTypes,
                AllowMultiple = false,
                SuggestedStartLocation = startFolder
            };

            IReadOnlyList<Avalonia.Platform.Storage.IStorageFile> files = await topLevel.StorageProvider.OpenFilePickerAsync(openOptions);
            if (files.Count == 0)
                return; // cancelled

            string? localPath = files[0].TryGetLocalPath();
            if (string.IsNullOrEmpty(localPath))
                return;

            LoadLogin(localPath);
        }

        public void LoadLogin(string filePath) {
            try {
                LoginData loginData = new LoginData(filePath) 
                { 
                    _onLoadError = AppendStatus 
                };
                _loginData = loginData;
                Settings._lastLoginFile = filePath;
                Settings.SaveSettings();
                if (loginData._userName == string.Empty)
                    AppendStatus(Path.GetFileName(filePath) + " incomplete\n  This probably means the encryption key changed.", System.Drawing.Color.DarkRed);
                else
                    AppendStatus("Loaded login from " + Path.GetFileName(filePath));
            }
            catch (Exception ex) {
                _loginData = null;
                AppendStatus("Failed to load login: " + ex.Message + "\n  Wrong file type or the encryption key changed.", System.Drawing.Color.DarkRed);
            }
            ShowLoginData();
        }

        private async void EditExe_Click(object? sender, RoutedEventArgs e) {
            TopLevel? topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) 
                return;

            Avalonia.Platform.Storage.IStorageFolder? startFolder = null;
            if (!string.IsNullOrEmpty(Settings._firestormExe)) {
                string? folderPath = Path.GetDirectoryName(Settings._firestormExe);
                if (!string.IsNullOrEmpty(folderPath)) {
                    try {
                        startFolder = await topLevel.StorageProvider.TryGetFolderFromPathAsync(folderPath);
                    }
                    catch (Exception) { }
                }
            }

            FilePickerOpenOptions openOptions = new FilePickerOpenOptions
            {
                Title = "Select Firestorm Executable",
                FileTypeFilter = new List<FilePickerFileType> { new FilePickerFileType("EXE Executable") { Patterns = new[] { "*.exe" } } },
                AllowMultiple = false,
                SuggestedStartLocation = startFolder
            };

            IReadOnlyList<Avalonia.Platform.Storage.IStorageFile> files = await topLevel.StorageProvider.OpenFilePickerAsync(openOptions);
            if (files.Count > 0) {
                Settings._firestormExe = files[0].Path.LocalPath;
                this.FirestormExeText.Text = Settings._firestormExe;
                Settings.SaveSettings();
            }
        }

        private async void LockButton_Click(object? sender, RoutedEventArgs e) {
            if (_loginData == null)
                return;

            Button lockBtn = new Button { Name = "LockConfirmButton", Content = "Lock", Width = 80 };
            Button cancelBtn = new Button { Name = "CancelButton", Content = "Cancel", Width = 80 };
            string message = "Are you sure you want to lock this login? This can't be undone until the set online time has elapsed!";
            if (_loginData._elapsedMinutes.Value > 3)
                message += "\n\nNote: Elapsed time will be reset.";
            Window confirmDialog = new Window
            {
                Title = "Confirm Lock",
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

            bool confirmed = false;
            if (lockBtn != null)
                lockBtn.Click += (s, e) => { confirmed = true; confirmDialog.Close(); };
            if (cancelBtn != null)
                cancelBtn.Click += (s, e) => confirmDialog.Close();

            await confirmDialog.ShowDialog(this);

            if (!confirmed)
                return;

            _loginData._locked.Value = 1;
            _loginData._elapsedMinutes.Value = 0;
            _loginData.SaveLogin();
            ShowLoginData();
            AppendStatus("Locked login: " + Path.GetFileName(_loginData._filePath));
        }

        private async void EditButton_Click(object sender, RoutedEventArgs e) {
            if (_loginData == null)
                return;
            LoginDetailsWindow profileWindow = new LoginDetailsWindow(_loginData);
            await profileWindow.ShowDialog(this);
            //_loginData = profileWindow._login;
            ShowLoginData();
        }

        public void ShowLoginData() {
            if (_loginData == null || _loginData._userName == string.Empty) {
                this.CurrentFile.Text = "None";
                this.LoginName.Text = "N/A";
                this.LaunchButton.IsEnabled = false;
                this.LockButton.IsEnabled = false;
                this.EditButton.IsEnabled = false;
            }
            else {
                this.CurrentFile.Text = Path.GetFileName(_loginData._filePath);
                this.LoginName.Text = _loginData._userName;
                ShowLoginTime();
                this.LaunchButton.IsEnabled = !_running;
                this.LockButton.IsEnabled = _loginData._locked.Value == 0;
                this.EditButton.IsEnabled = _loginData._locked.Value == 0;
            }
        }

        private void ShowLoginTime() {
            if (_loginData == null) return;

            if (_loginData._locked.Value != 0)
                this.TimeText.Text = "Locked: ";
            else
                this.TimeText.Text = "Time: ";
            double elapsedHours = _loginData._elapsedMinutes.Value / 60.0;
            this.TimeText.Text += elapsedHours.ToString("F1") + " / " + (_loginData._totalMinutes.Value / 60).ToString() + " hours";
        }



        private async void LaunchButton_Click(object? sender, RoutedEventArgs e)
        {
            Button? launchButton = this.FindControl<Button>("LaunchButton");
            if (launchButton == null)
                return;
            launchButton.IsEnabled = false;
            launchButton.Content = "Starting";
            _running = true;

            if (!ModifyFirestormSettings())
                goto Fail;

            Process? process = await LaunchProc();
            if (process == null)
                goto Fail;

            await Task.Delay(1000);
            IntPtr? wnd = await WaitForLoadedWindow(process);
            if (wnd == null)
                goto Fail;

            bool success = await Login(wnd.Value);
            if (!success)
                goto Fail;

            _ = MonitorProcessAsync(process);
            AppendStatus("Keep this application running to have the time count.");
            return;

        Fail:
            launchButton.IsEnabled = true;
            launchButton.Content = "Login";
            _running = false;
            
        }

        private async Task MonitorProcessAsync(Process proc)
        {
            int seconds = 0;
            Button? launchButton = this.FindControl<Button>("LaunchButton");
            if (launchButton == null)
                return;

            launchButton.Content = "Running";

            try {
                while (!proc.HasExited) {
                    await Task.Delay(1000);
                    seconds++;
                    if (seconds == 60) {
                        seconds = 0;
                        if (_loginData != null) {
                            _loginData._elapsedMinutes.Value++;
                            if (_loginData._locked.Value != 0 && _loginData._elapsedMinutes.Value >= _loginData._totalMinutes.Value) {
                                Dispatcher.UIThread.Post(() => AppendStatus("Lock duration completed. Unlocking login details."));
                                _loginData._locked.Value = 0;
                                Dispatcher.UIThread.Post(() => ShowLoginData());
                            }
                            else
                                Dispatcher.UIThread.Post(() => ShowLoginTime());
                            _loginData.SaveLogin();

                        }
                    }
                }

                // Process has exited
                Dispatcher.UIThread.Post(() => {
                    launchButton.Content = "Login";
                    launchButton.IsEnabled = true;
                    _running = false;
                    AppendStatus("Firestorm has closed");
                });
            }
            catch (Exception) {
                Dispatcher.UIThread.Post(() => {
                    AppendStatus("Firestorm Process was disposed or other error");
                    launchButton.Content = "Login";
                    launchButton.IsEnabled = true;
                    _running = false;
                });
            }
        }

        public bool ModifyFirestormSettings() {
            if (CheckFirestormSettings())
                return true;
            if (!File.Exists(_firestormSettings)) {
                AppendStatus("Settings file not found: " + _firestormSettings, Color.DarkRed);
                return false;
            }
            AppendStatus("User settings path: " + _firestormSettings);
            XmlDocument doc = new XmlDocument();
            doc.Load(_firestormSettings);
            XmlNode? root = doc.DocumentElement;
            if (root == null) {
                AppendStatus("Failed to load root", Color.DarkRed);
                return false;
            }
            root = root.SelectSingleNode("map");
            if (root == null) {
                AppendStatus("Failed to load map", Color.DarkRed);
                return false;
            }
            XmlNodeList? keys = root.SelectNodes("key");
            if (keys == null) {
                AppendStatus("Failed to load keys", Color.DarkRed);
                return false;
            }
            //Don't remember password
            SetFirestormSetting(doc, root, keys, "FSRememberUsername", "0");
            //No start location selection
            SetFirestormSetting(doc, root, keys, "ShowStartLocation", "0");
            //Enable RLV
            SetFirestormSetting(doc, root, keys, "RestrainedLove", "1");
            //No OOC chat - can still be enabled at runtime
            SetFirestormSetting(doc, root, keys, "RestrainedLoveCanOOC", "0");
            // Save changes
            try {
                doc.Save(_firestormSettings);
                AppendStatus("Modified Firestorm settings.xml");
            }
            catch (Exception ex) {
                AppendStatus("Failed to save settings.xml: " + ex.Message, Color.DarkRed);
                return false;
            }

            return true;
        }

        public void SetFirestormSetting(XmlDocument doc, XmlNode rootMap, XmlNodeList keys, string keyName, string value) {
            XmlNode? keyNode = null;
            foreach (XmlNode key in keys) {
                if (key.InnerText == keyName) {
                    keyNode = key;
                    break;
                }
            }
            if (keyNode != null) {
                AppendStatus(keyName + " already exists, removing");
                XmlNode? remMap = keyNode.NextSibling;
                if (remMap != null)
                    rootMap.RemoveChild(remMap);
                rootMap.RemoveChild(keyNode);
                keyNode = null;
            }
            if (keyNode == null) {
                keyNode = doc.CreateNode(XmlNodeType.Element, "key", null);
                keyNode.InnerText = keyName;
                XmlNode remMap = doc.CreateNode(XmlNodeType.Element, "map", null);
                XmlNode node = doc.CreateNode(XmlNodeType.Element, "key", null); node.InnerText = "Comment"; remMap.AppendChild(node);
                node = doc.CreateNode(XmlNodeType.Element, "string", null); node.InnerText = keyName; remMap.AppendChild(node);
                node = doc.CreateNode(XmlNodeType.Element, "key", null); node.InnerText = "Type"; remMap.AppendChild(node);
                node = doc.CreateNode(XmlNodeType.Element, "string", null); node.InnerText = "Boolean"; remMap.AppendChild(node);
                node = doc.CreateNode(XmlNodeType.Element, "key", null); node.InnerText = "Value"; remMap.AppendChild(node);
                node = doc.CreateNode(XmlNodeType.Element, "boolean", null); node.InnerText = value; remMap.AppendChild(node);
                rootMap.AppendChild(keyNode);
                rootMap.AppendChild(remMap);
            }
        }

        public bool CheckFirestormSettings() {
            if (!File.Exists(this._firestormSettings)) {
                AppendStatus("Settings file not found: " + _firestormSettings, Color.DarkRed);
                return false;
            }
            XmlDocument doc = new XmlDocument();
            doc.Load(_firestormSettings);
            XmlNode? root = doc.DocumentElement;
            if (root == null) {
                AppendStatus("Failed to load root", Color.DarkRed);
                return false;
            }
            root = root.SelectSingleNode("map");
            if (root == null) {
                AppendStatus("Failed to load map", Color.DarkRed);
                return false;
            }
            XmlNodeList? keys = root.SelectNodes("key");
            if (keys == null) {
                AppendStatus("Failed to load keys", Color.DarkRed);
                return false;
            }
            bool ok = true;
            ok = ok && CheckValue(keys, "FSRememberUsername", "0");
            ok = ok && CheckValue(keys, "ShowStartLocation", "0");
            ok = ok && CheckValue(keys, "RestrainedLove", "1");
            ok = ok && CheckValue(keys, "RestrainedLoveCanOOC", "0");
            return ok;
        }
        public bool CheckValue(XmlNodeList keys, string keyName, string expectedValue) {
            foreach (XmlNode key in keys) {
                if (key.InnerText == keyName) {
                    XmlNode? map = key.NextSibling;
                    if (map != null) {
                        XmlNodeList? innerKeys = map.SelectNodes("key");
                        if (innerKeys == null)
                            return false;
                        foreach (XmlNode innerKey in innerKeys) {
                            if (innerKey.InnerText == "Value") {
                                XmlNode? valueNode = innerKey.NextSibling;
                                if (valueNode != null && valueNode.InnerText == expectedValue) {
                                    return true;
                                }
                            }
                        }
                    }
                    break;
                }
            }
            AppendStatus("Firestorm setting " + keyName + " is not set to " + expectedValue, Color.DarkRed);
            return false;
        }


        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool IsWindow(IntPtr hWnd);

        public async Task<Process?> LaunchProc() {
            SHA256 sha = SHA256.Create();
            byte[] exeHash = sha.ComputeHash(File.ReadAllBytes(Settings._firestormExe));
            if (_loginData == null || !exeHash.SequenceEqual(_loginData._exeHash.Value)) {
                AppendStatus("Invalid Firestorm executable. Please use the same version as on creation of the login file.", Color.DarkRed);
                return null; 
            }
            Process? proc = null;
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = Settings._firestormExe,
                    UseShellExecute = true
                };
                proc = System.Diagnostics.Process.Start(psi);
                if (proc != null) {
                    AppendStatus("Launched");
                    
                }
                else
                    AppendStatus("Process failed to start.", Color.DarkRed);
            }
            catch (System.Exception)
            {
                AppendStatus("Launch failed", Color.DarkRed);
            }

            if (proc != null) {
                //Wait for the main window handle to be available
                int waitTime = 0;
                while (waitTime < 4000)
                {
                    if (proc.MainWindowHandle != IntPtr.Zero)
                    {
                        AppendStatus("Main window handle obtained after " + waitTime.ToString() + ".");
                        break;
                    }
                    await Task.Delay(100);
                    waitTime += 100;
                }

                //Wait for the loading floater to close
                waitTime = 0;
                while (waitTime < 20000)
                {
                    if (!IsWindow(proc.MainWindowHandle))
                    {
                        AppendStatus("Loading floater closed after " + waitTime.ToString() + ".");
                        break;
                    }
                    await Task.Delay(100);
                    waitTime += 100;
                }
                if (!CheckFirestormSettings()) {
                    File.Copy(_firestormSettings, _firestormSettings + ".bak", true);
                    return null;
                }
            }

            return proc;
        }



        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        public IntPtr? GetProcessWindow(Process process) {
            IntPtr found = IntPtr.Zero;
            List<IntPtr> windows = new List<IntPtr>();
            List<int> widths = new List<int>();
            List<int> colors = new List<int>();
            int maxColorCount = 0;

            EnumWindows(delegate (IntPtr wnd, IntPtr param)
            {
                uint wndProcessId;
                GetWindowThreadProcessId(wnd, out wndProcessId);
                if (wndProcessId == process.Id) {
                    RECT rc;
                    GetWindowRect(wnd, out rc);
                    if (rc.Width >= 100) {
                        int colorCount = WindowColors(wnd);
                        if (colorCount > maxColorCount) {
                            AppendStatus("Window " + wnd.ToString() + " for process " + process.Id.ToString() + " width: " + rc.Width + " colors " + colorCount);
                            maxColorCount = colorCount;
                            windows.Add(wnd);
                            widths.Add(rc.Width);
                            colors.Add(colorCount);
                        }
                        else
                            AppendStatus("Other window " + wnd.ToString() + " for process " + process.Id.ToString() + " width: " + rc.Width + " colors " + colorCount);
                    }
                }
                return true; //return true here so that we iterate all windows
            }, IntPtr.Zero);

            if (windows.Count > 0)
                return windows[windows.Count - 1];
            return null;
        }



        public async Task<IntPtr?> WaitForLoadedWindow(Process process) {
            IntPtr? hwnd = GetProcessWindow(process);
            if (hwnd == null)
                return null;

            int waitTime = 0;
            int shouldHaveColorsTime = 3000;
            int previousDifferentColors = 0;
            int unchanged = 0;
            int errors = 0;
            while (waitTime < 20000 && errors < 5) {
                int differentColors = WindowColors(hwnd.Value);
                if (differentColors == 0) {
                    AppendStatus("Window appears to be closed after " + waitTime.ToString() + "ms");
                    hwnd = GetProcessWindow(process);
                    errors++;
                }
                else if (differentColors < 50 && waitTime >= shouldHaveColorsTime) {
                    AppendStatus("Window empty after " + waitTime.ToString() + "ms");
                    hwnd = GetProcessWindow(process);
                    errors++;
                    shouldHaveColorsTime += 1000;
                }
                else if (differentColors != previousDifferentColors) {
                    AppendStatus("Different colors: " + differentColors.ToString() + " at " + waitTime.ToString() + "ms");
                    previousDifferentColors = differentColors;
                    unchanged = 0;
                }
                else {
                    unchanged++;
                    if (differentColors > 50 && unchanged > 5) {
                        AppendStatus("Window appears to be loaded after " + waitTime.ToString() + "ms");
                        return hwnd;
                    }
                }
                //bmp.Save("window_capture_" + waitTime.ToString() + ".png", ImageFormat.Png);
                await Task.Delay(100);
                waitTime += 100;
            }
            AppendStatus("Failed to find the Firestorm window", Color.DarkRed);
            return null;
        }

        public int WindowColors(IntPtr hwnd) {
            try {
                System.Drawing.Bitmap bmp = PrintWindow(hwnd);
                Color lastColor = Color.Green;
                int differentColors = 0;
                for (int y = 2; y < bmp.Height - 2; y++) {
                    Color color = bmp.GetPixel(bmp.Width / 2, y);
                    if (color != lastColor)
                        differentColors++;
                    lastColor = color;
                }
                return differentColors;
            }
            catch (Exception) {
                AppendStatus("Failed to capture screen", Color.DarkRed);
                return 0;
            }
        }

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
        [DllImport("user32.dll")]
        public static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, int nFlags);
        public static System.Drawing.Bitmap PrintWindow(IntPtr hwnd)
        {
            RECT rc;
            GetWindowRect(hwnd, out rc);

            System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(rc.Width, rc.Height, PixelFormat.Format32bppArgb);
            Graphics gfxBmp = Graphics.FromImage(bmp);
            IntPtr hdcBitmap = gfxBmp.GetHdc();

            PrintWindow(hwnd, hdcBitmap, 0);

            gfxBmp.ReleaseHdc(hdcBitmap);
            gfxBmp.Dispose();

            return bmp;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        [DllImport("USER32.DLL")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        public bool CheckForegroundWindow(IntPtr targetWnd) {
            IntPtr wnd = GetForegroundWindow();
            //Check process ID
            if (wnd != targetWnd) {
                AppendStatus("Foreground window is not correct - keep the Firestorm window selected while starting!", Color.DarkRed);
                return false;
            }

            if (IsLoggedIn(wnd)) {
                AppendStatus("Already logged in!", Color.DarkRed);
                return false;
            }
            return true;
        }

        public bool IsLoggedIn(IntPtr wnd) {
            StringBuilder sb = new StringBuilder(100);
            GetWindowText(wnd, sb, sb.Capacity);
            if (sb.ToString().Contains(" - ")) {
                return true;
            }
            return false;
        }


        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        public async Task<bool> Login(IntPtr firestormWnd) {
            int i = 0;
            while (i < 5 && GetForegroundWindow() != firestormWnd) {
                AppendStatus("Waiting for Firestorm window to be in foreground...");
                SetForegroundWindow(firestormWnd);
                await Task.Delay(300);
                i++;
            }
            if (!CheckFirestormSettings())
                return false;
            if (_loginData == null)
                return false;
            if (Clipboard == null) {
                AppendStatus("Clipboard is not available.", Color.DarkRed);
                return false;
            }
            if (!CheckForegroundWindow(firestormWnd))
                return false;
            Robot robot = new Robot();
            await Clipboard.SetTextAsync(_loginData._userName);
            await Task.Delay(10); //test to see if it fixes empty name
            robot.CombineKeys(Key.Control, Key.V);
            await Task.Delay(70);
            robot.KeyPress(Key.Tab);
            if (!CheckForegroundWindow(firestormWnd))
                return false;
            await Clipboard.SetTextAsync(_loginData.GetPassword());
            robot.CombineKeys(Key.Control, Key.V);
            await Task.Delay(60);
            await Clipboard.ClearAsync();
            robot.KeyPress(Key.Enter);
            await Task.Delay(10);
            robot.KeyPress(Key.Enter);
            AppendStatus("Logging in.");
            await Task.Delay(500);
            if (!IsLoggedIn(GetForegroundWindow())) {
                AppendStatus("Login seems to have failed.", Color.DarkRed);
                return false;
            }
            return true;
        }

        // Append a status message to the bottom ListBox
        public void AppendStatus(string message) {
            AppendStatus(message, Color.Black);
        }
        public void AppendStatus(string message, Color severity) {
            var text = $"[{DateTime.Now:HH:mm:ss}] {message}";
            Dispatcher.UIThread.Post(() =>
            {
                StatusMessage status = new StatusMessage { Message = text, Color = severity.Name };
                _statusMessages.Add(status);
                this.StatusList.ScrollIntoView(status);
            });
        }

        private async void CopyStatusMessage_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (StatusList.SelectedItem is StatusMessage status)
            {
                var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
                if (clipboard is not null)
                {
                    await clipboard.SetTextAsync(status.Message);
                }
            }
        }

        private async void CopyAllStatusMessage_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (_statusMessages.Count > 0)
            {
                var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
                if (clipboard is not null)
                {
                    var allText = string.Join(Environment.NewLine, _statusMessages.Select(m => m.Message));
                    await clipboard.SetTextAsync(allText);
                }
            }
        }
    }
}
