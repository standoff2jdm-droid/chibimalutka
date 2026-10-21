using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Media;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace DesktopPetApp
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
    }

    public class PetParticle
    {
        public UIElement Element;
        public double X;
        public double Y;
        public double Vx;
        public double Vy;
        public double Rotation;
        public double Vrot;
        public double Life;
        public double MaxLife;
        public double BaseScale;
    }

    public enum PetState
    {
        Idle,
        Blink,
        Happy,
        Dragging,
        Console,
        Drawing,
        Sleep,
        Pout,
        Climbing
    }

    public enum ClimbState
    {
        None,
        WalkingToWindow,
        ClimbingWall,
        PerchedOnTitlebar,
        JumpingDown
    }

    public class TreatWindow : Window
    {
        public string TreatType;
        public string TreatName;
        public string YumPhrase;
        public double ScreenX;
        public double ScreenY;
        public double Vy;
        public bool Landed;
        public double GroundY;
        public bool IsBeingDragged = false;
        private Point dragStartOffset;
        public Action<TreatWindow> OnTreatDragged;
        public Action<TreatWindow> OnTreatReleased;
        private Image treatImage;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(out POINT lpPoint);

        public TreatWindow(string treatType, string name, string yumPhrase, BitmapSource source, double startScreenX, double startScreenY, double groundScreenY)
        {
            this.Title = "Chibi Treat";
            this.WindowStyle = WindowStyle.None;
            this.AllowsTransparency = true;
            this.Background = new SolidColorBrush(Color.FromArgb(1, 255, 255, 255));
            this.Topmost = true;
            this.ShowInTaskbar = false;
            this.ResizeMode = ResizeMode.NoResize;
            this.IsHitTestVisible = true;
            this.Cursor = Cursors.Hand;

            this.Width = 64;
            this.Height = 64;
            this.Left = startScreenX;
            this.Top = startScreenY;

            this.TreatType = treatType;
            this.TreatName = name;
            this.YumPhrase = yumPhrase;
            this.ScreenX = startScreenX;
            this.ScreenY = startScreenY;
            this.GroundY = groundScreenY - 60;
            this.Vy = -2.5;

            treatImage = new Image
            {
                Source = source,
                Width = 56,
                Height = 56,
                Stretch = Stretch.Uniform,
                Effect = new DropShadowEffect { BlurRadius = 12, ShadowDepth = 3, Opacity = 0.5, Color = Colors.Black }
            };
            RenderOptions.SetBitmapScalingMode(treatImage, BitmapScalingMode.HighQuality);

            this.Content = treatImage;

            this.MouseLeftButtonDown += (s, e) =>
            {
                IsBeingDragged = true;
                Landed = false;
                Vy = 0;
                dragStartOffset = e.GetPosition(this);
                this.CaptureMouse();
                this.Cursor = Cursors.SizeAll;
            };

            this.MouseMove += (s, e) =>
            {
                if (IsBeingDragged)
                {
                    POINT pt;
                    if (GetCursorPos(out pt))
                    {
                        this.Left = pt.X - dragStartOffset.X;
                        this.Top = pt.Y - dragStartOffset.Y;
                        this.ScreenX = this.Left;
                        this.ScreenY = this.Top;
                        if (OnTreatDragged != null) OnTreatDragged(this);
                    }
                }
            };

            this.MouseLeftButtonUp += (s, e) =>
            {
                if (IsBeingDragged)
                {
                    IsBeingDragged = false;
                    this.ReleaseMouseCapture();
                    this.Cursor = Cursors.Hand;
                    Vy = 0; // Fall with gravity from drop location
                    if (OnTreatReleased != null) OnTreatReleased(this);
                }
            };
        }

        public void UpdatePhysics()
        {
            if (IsBeingDragged) return;

            if (!Landed)
            {
                Vy += 1.3;
                ScreenY += Vy;
                if (ScreenY >= GroundY)
                {
                    ScreenY = GroundY;
                    if (Vy > 3.0)
                    {
                        Vy = -Vy * 0.35;
                    }
                    else
                    {
                        Vy = 0;
                        Landed = true;
                    }
                }
                this.Top = ScreenY;
            }
        }
    }

    public class AppReactionProfile
    {
        public string[] Keywords;
        public string CategoryName;
        public PetState State;
        public string[] Phrases;
        public string Particle;
        public Color ParticleColor;

        public AppReactionProfile(string[] keywords, string category, PetState state, string[] phrases, string particle, Color color)
        {
            Keywords = keywords;
            CategoryName = category;
            State = state;
            Phrases = phrases;
            Particle = particle;
            ParticleColor = color;
        }
    }

    public class ChatDialogWindow : Window
    {
        private TextBox inputTextBox;
        private PetWindow ownerPet;

        public ChatDialogWindow(PetWindow owner)
        {
            this.ownerPet = owner;
            this.Title = "Поговорить с Чиби";
            this.WindowStyle = WindowStyle.None;
            this.AllowsTransparency = true;
            this.Background = Brushes.Transparent;
            this.Topmost = true;
            this.ShowInTaskbar = false;
            this.Width = 330;
            this.Height = 110;

            var workArea = SystemParameters.WorkArea;
            double targetLeft = owner.Left + (owner.Width - this.Width) / 2.0;
            double targetTop = owner.Top - this.Height - 12.0;

            if (targetTop < workArea.Top + 10) targetTop = owner.Top + owner.Height + 12.0;
            if (targetLeft < workArea.Left + 10) targetLeft = workArea.Left + 10;
            if (targetLeft + this.Width > workArea.Right - 10) targetLeft = workArea.Right - this.Width - 10;

            this.Left = targetLeft;
            this.Top = targetTop;

            var card = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(255, 253, 255)),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9B59B6")),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(14),
                Effect = new DropShadowEffect
                {
                    Color = Color.FromRgb(140, 70, 180),
                    BlurRadius = 18,
                    ShadowDepth = 4,
                    Opacity = 0.35,
                    Direction = 270
                }
            };

            var stack = new StackPanel { Orientation = Orientation.Vertical };

            var titleBlock = new TextBlock
            {
                Text = "💬 Напиши что-нибудь Чиби-тян:",
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4A235A")),
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 8)
            };
            stack.Children.Add(titleBlock);

            var row = new Grid();
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            inputTextBox = new TextBox
            {
                Height = 32,
                FontSize = 13,
                VerticalContentAlignment = VerticalAlignment.Center,
                Padding = new Thickness(8, 0, 8, 0),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D2B4DE")),
                BorderThickness = new Thickness(1.5),
                Background = new SolidColorBrush(Color.FromRgb(250, 245, 255))
            };
            Grid.SetColumn(inputTextBox, 0);
            row.Children.Add(inputTextBox);

            var sendBtn = new Button
            {
                Content = "Сказать 💕",
                Height = 32,
                Padding = new Thickness(12, 0, 12, 0),
                Margin = new Thickness(8, 0, 0, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9B59B6")),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
            sendBtn.Click += (s, e) => SendMessage();
            Grid.SetColumn(sendBtn, 1);
            row.Children.Add(sendBtn);

            stack.Children.Add(row);
            card.Child = stack;
            this.Content = card;

            inputTextBox.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter) SendMessage();
                else if (e.Key == Key.Escape) this.Close();
            };

            this.Loaded += (s, e) => inputTextBox.Focus();
            this.Deactivated += (s, e) => this.Close();
        }

        private void SendMessage()
        {
            string txt = inputTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(txt))
            {
                ownerPet.HandleUserChat(txt);
            }
            this.Close();
        }
    }

    public class UpdateInfo
    {
        public string TagName;
        public string Version;
        public string Title;
        public string ReleaseNotes;
        public string DownloadUrl;
        public string HtmlUrl;
        public bool IsNewer;
        public string ErrorMessage;

        public UpdateInfo()
        {
            TagName = "";
            Version = "";
            Title = "";
            ReleaseNotes = "";
            DownloadUrl = "";
            HtmlUrl = "";
            IsNewer = false;
            ErrorMessage = "";
        }
    }

    public class PetConfig
    {
        public string Repository;
        public bool AutoCheckUpdates;
        public double CheckIntervalHours;
        public string LastCheckTime;

        public PetConfig()
        {
            Repository = "standoff2jdm-droid/chibimalutka";
            AutoCheckUpdates = true;
            CheckIntervalHours = 24.0;
            LastCheckTime = "";
        }

        public static PetConfig Load()
        {
            string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pet_config.json");
            var cfg = new PetConfig();
            if (File.Exists(path))
            {
                try
                {
                    string text = File.ReadAllText(path, Encoding.UTF8);
                    cfg.Repository = ExtractJsonValue(text, "repository", cfg.Repository);
                    string autoStr = ExtractJsonValue(text, "auto_check_updates", "true");
                    cfg.AutoCheckUpdates = autoStr.ToLower() == "true";
                    string hoursStr = ExtractJsonValue(text, "check_interval_hours", "24");
                    double h;
                    if (double.TryParse(hoursStr, NumberStyles.Any, CultureInfo.InvariantCulture, out h))
                        cfg.CheckIntervalHours = h;
                    cfg.LastCheckTime = ExtractJsonValue(text, "last_check_time", "");
                }
                catch { }
            }
            else
            {
                cfg.Save();
            }
            return cfg;
        }

        public void Save()
        {
            try
            {
                string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pet_config.json");
                var sb = new StringBuilder();
                sb.AppendLine("{");
                sb.AppendFormat("  \"repository\": \"{0}\",\r\n", Repository);
                sb.AppendFormat("  \"auto_check_updates\": {0},\r\n", AutoCheckUpdates ? "true" : "false");
                sb.AppendFormat("  \"check_interval_hours\": {0},\r\n", CheckIntervalHours.ToString(CultureInfo.InvariantCulture));
                sb.AppendFormat("  \"last_check_time\": \"{0}\"\r\n", LastCheckTime);
                sb.AppendLine("}");
                File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            }
            catch { }
        }

        private static string ExtractJsonValue(string json, string key, string defVal)
        {
            var match = Regex.Match(json, "\"" + key + "\"\\s*:\\s*\"?([^\",\\r\\n}]+)\"?");
            if (match.Success)
            {
                return match.Groups[1].Value.Trim('\"', ' ');
            }
            return defVal;
        }
    }

    public static class UpdateChecker
    {
        public const string AppVersion = "0.8.0-beta";

        public static void CheckForUpdatesAsync(string repo, Action<UpdateInfo> callback)
        {
            if (string.IsNullOrEmpty(repo) || !repo.Contains("/"))
            {
                if (callback != null)
                {
                    var errInfo = new UpdateInfo();
                    errInfo.ErrorMessage = "Репозиторий GitHub не настроен (формат: владелец/репозиторий).";
                    callback(errInfo);
                }
                return;
            }

            System.Threading.ThreadPool.QueueUserWorkItem(delegate
            {
                UpdateInfo info = new UpdateInfo();
                try
                {
                    ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072; // Tls12
                    string url = string.Format("https://api.github.com/repos/{0}/releases/latest", repo.Trim());
                    var request = (HttpWebRequest)WebRequest.Create(url);
                    request.UserAgent = "ChibiDesktopPet-Updater/" + AppVersion;
                    request.Timeout = 10000;
                    request.ReadWriteTimeout = 10000;
                    request.Accept = "application/vnd.github.v3+json";

                    using (var response = (HttpWebResponse)request.GetResponse())
                    using (var reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                    {
                        string json = reader.ReadToEnd();
                        ParseReleaseJson(json, info);
                    }

                    if (!string.IsNullOrEmpty(info.Version))
                    {
                        info.IsNewer = CompareVersions(info.Version, AppVersion) > 0;
                    }
                }
                catch (WebException wex)
                {
                    var resp = wex.Response as HttpWebResponse;
                    if (resp != null && resp.StatusCode == HttpStatusCode.NotFound)
                    {
                        info.ErrorMessage = "В репозитории " + repo + " пока нет опубликованных релизов на GitHub.";
                    }
                    else
                    {
                        info.ErrorMessage = "Не удалось связаться с GitHub (" + wex.Message + ").";
                    }
                }
                catch (Exception ex)
                {
                    info.ErrorMessage = "Ошибка при проверке обновлений: " + ex.Message;
                }

                if (callback != null)
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(delegate
                    {
                        callback(info);
                    }));
                }
            });
        }

        private static void ParseReleaseJson(string json, UpdateInfo info)
        {
            var tagMatch = Regex.Match(json, "\"tag_name\"\\s*:\\s*\"([^\"]+)\"");
            if (tagMatch.Success)
            {
                info.TagName = tagMatch.Groups[1].Value;
                info.Version = info.TagName.TrimStart('v', 'V', ' ');
            }

            var nameMatch = Regex.Match(json, "\"name\"\\s*:\\s*\"([^\"]+)\"");
            if (nameMatch.Success)
            {
                info.Title = nameMatch.Groups[1].Value;
            }

            var htmlMatch = Regex.Match(json, "\"html_url\"\\s*:\\s*\"([^\"]+)\"");
            if (htmlMatch.Success)
            {
                info.HtmlUrl = htmlMatch.Groups[1].Value;
            }

            var bodyMatch = Regex.Match(json, "\"body\"\\s*:\\s*\"((?:\\\\.|[^\"\\\\])*)\"");
            if (bodyMatch.Success)
            {
                string rawBody = bodyMatch.Groups[1].Value;
                rawBody = rawBody.Replace("\\r\\n", "\r\n").Replace("\\n", "\r\n").Replace("\\\"", "\"").Replace("\\\\", "\\");
                info.ReleaseNotes = rawBody;
            }

            var assetMatches = Regex.Matches(json, "\"browser_download_url\"\\s*:\\s*\"([^\"]+)\"");
            foreach (Match m in assetMatches)
            {
                string assetUrl = m.Groups[1].Value;
                if (assetUrl.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    info.DownloadUrl = assetUrl;
                    break;
                }
                else if (string.IsNullOrEmpty(info.DownloadUrl) && assetUrl.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    info.DownloadUrl = assetUrl;
                }
            }

            if (string.IsNullOrEmpty(info.DownloadUrl))
            {
                var zipballMatch = Regex.Match(json, "\"zipball_url\"\\s*:\\s*\"([^\"]+)\"");
                if (zipballMatch.Success)
                {
                    info.DownloadUrl = zipballMatch.Groups[1].Value;
                }
            }
        }

        public static int CompareVersions(string v1, string v2)
        {
            try
            {
                Version ver1 = NormalizeVersion(v1);
                Version ver2 = NormalizeVersion(v2);
                return ver1.CompareTo(ver2);
            }
            catch
            {
                return string.Compare(v1, v2, StringComparison.OrdinalIgnoreCase);
            }
        }

        private static Version NormalizeVersion(string v)
        {
            string[] rawParts = v.TrimStart('v', 'V', ' ').Split('-', '+')[0].Split('.');
            int major = rawParts.Length > 0 ? ParseIntSafe(rawParts[0]) : 0;
            int minor = rawParts.Length > 1 ? ParseIntSafe(rawParts[1]) : 0;
            int build = rawParts.Length > 2 ? ParseIntSafe(rawParts[2]) : 0;
            int rev = rawParts.Length > 3 ? ParseIntSafe(rawParts[3]) : 0;
            return new Version(major, minor, build, rev);
        }

        private static int ParseIntSafe(string s)
        {
            int val;
            int.TryParse(s, out val);
            return val;
        }

        public static void ApplyUpdate(string downloadUrl, Action<int> progressCallback, Action<string> completedOrErrorCallback)
        {
            System.Threading.ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
                    string tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ChibiDesktopPet_Update");
                    if (!Directory.Exists(tempDir))
                    {
                        Directory.CreateDirectory(tempDir);
                    }

                    string zipPath = System.IO.Path.Combine(tempDir, "update.zip");
                    if (File.Exists(zipPath)) File.Delete(zipPath);

                    using (var client = new WebClient())
                    {
                        client.Headers["User-Agent"] = "ChibiDesktopPet-Updater/" + AppVersion;
                        client.DownloadProgressChanged += (s, e) =>
                        {
                            if (progressCallback != null)
                            {
                                Application.Current.Dispatcher.BeginInvoke(new Action(delegate
                                {
                                    progressCallback(e.ProgressPercentage);
                                }));
                            }
                        };

                        client.DownloadFile(new Uri(downloadUrl), zipPath);
                    }

                    string appDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');
                    string batchPath = System.IO.Path.Combine(tempDir, "apply_update.bat");
                    string extractDir = System.IO.Path.Combine(tempDir, "extracted");

                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("@echo off");
                    sb.AppendLine("chcp 65001 > nul");
                    sb.AppendLine("echo ========================================");
                    sb.AppendLine("echo   🌸 Обновление Chibi Desktop Pet...    ");
                    sb.AppendLine("echo ========================================");
                    sb.AppendLine("timeout /t 2 /nobreak > nul");
                    sb.AppendLine(":WAIT_LOOP");
                    sb.AppendLine("tasklist /fi \"imagename eq DesktopPet.exe\" | find /i \"DesktopPet.exe\" > nul");
                    sb.AppendLine("if not errorlevel 1 (");
                    sb.AppendLine("    timeout /t 1 /nobreak > nul");
                    sb.AppendLine("    goto WAIT_LOOP");
                    sb.AppendLine(")");
                    sb.AppendLine("echo Распаковка обновления...");
                    sb.AppendLine("if exist \"" + extractDir + "\" rmdir /s /q \"" + extractDir + "\"");
                    sb.AppendLine("powershell -NoProfile -ExecutionPolicy Bypass -Command \"Expand-Archive -Path '" + zipPath + "' -DestinationPath '" + extractDir + "' -Force\"");
                    sb.AppendLine("echo Установка обновлённых файлов...");
                    sb.AppendLine("if exist \"" + extractDir + "\\desktop-pet\" (");
                    sb.AppendLine("    xcopy /E /Y /I \"" + extractDir + "\\desktop-pet\\*\" \"" + appDir + "\\\"");
                    sb.AppendLine(") else (");
                    sb.AppendLine("    xcopy /E /Y /I \"" + extractDir + "\\*\" \"" + appDir + "\\\"");
                    sb.AppendLine(")");
                    sb.AppendLine("echo Запуск обновлённого питомца...");
                    sb.AppendLine("start \"\" \"" + System.IO.Path.Combine(appDir, "DesktopPet.exe") + "\"");
                    sb.AppendLine("timeout /t 2 /nobreak > nul");
                    sb.AppendLine("rmdir /s /q \"" + extractDir + "\" > nul 2>&1");
                    sb.AppendLine("del \"" + zipPath + "\" > nul 2>&1");
                    sb.AppendLine("(goto) 2>nul & del \"%~f0\"");

                    File.WriteAllText(batchPath, sb.ToString(), Encoding.GetEncoding(866));

                    var psi = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = "/c \"" + batchPath + "\"",
                        CreateNoWindow = true,
                        UseShellExecute = true
                    };
                    Process.Start(psi);

                    Application.Current.Dispatcher.BeginInvoke(new Action(delegate
                    {
                        Application.Current.Shutdown();
                    }));
                }
                catch (Exception ex)
                {
                    if (completedOrErrorCallback != null)
                    {
                        Application.Current.Dispatcher.BeginInvoke(new Action(delegate
                        {
                            completedOrErrorCallback("Ошибка при установке: " + ex.Message);
                        }));
                    }
                }
            });
        }
    }

    public class UpdateDialogWindow : Window
    {
        private UpdateInfo updateInfo;
        private ProgressBar progressBar;
        private TextBlock statusText;
        private Button updateBtn;
        private Button githubBtn;
        private Button laterBtn;

        public UpdateDialogWindow(UpdateInfo info)
        {
            this.updateInfo = info;
            this.Title = "🌸 Доступно обновление!";
            this.WindowStyle = WindowStyle.None;
            this.AllowsTransparency = true;
            this.Background = Brushes.Transparent;
            this.Topmost = true;
            this.ShowInTaskbar = true;
            this.Width = 480;
            this.Height = 380;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            var card = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(254, 252, 255)),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9B59B6")),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(18),
                Padding = new Thickness(18),
                Effect = new DropShadowEffect
                {
                    Color = Color.FromRgb(120, 60, 160),
                    BlurRadius = 24,
                    ShadowDepth = 6,
                    Opacity = 0.35,
                    Direction = 270
                }
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // 1. Header (Title + Close Button)
            var headerGrid = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            headerGrid.MouseLeftButtonDown += (s, e) => { try { this.DragMove(); } catch { } };

            var titleBlock = new TextBlock
            {
                Text = "🌸 Чиби-Питомец — Найдено обновление!",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4A235A")),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(titleBlock, 0);
            headerGrid.Children.Add(titleBlock);

            var closeBtn = new Button
            {
                Content = "✕",
                Width = 26,
                Height = 26,
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Background = Brushes.Transparent,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#884EA0")),
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
            closeBtn.Click += (s, e) => this.Close();
            Grid.SetColumn(closeBtn, 1);
            headerGrid.Children.Add(closeBtn);
            Grid.SetRow(headerGrid, 0);
            grid.Children.Add(headerGrid);

            // 2. Version banner
            var verBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(245, 238, 252)),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(0, 0, 0, 10)
            };
            var verPanel = new StackPanel { Orientation = Orientation.Horizontal };
            verPanel.Children.Add(new TextBlock
            {
                Text = "Текущая: v" + UpdateChecker.AppVersion,
                FontSize = 12,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6C3483")),
                VerticalAlignment = VerticalAlignment.Center
            });
            verPanel.Children.Add(new TextBlock
            {
                Text = "  ➔  ",
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9B59B6")),
                VerticalAlignment = VerticalAlignment.Center
            });
            verPanel.Children.Add(new TextBlock
            {
                Text = "Новая: " + (string.IsNullOrEmpty(info.TagName) ? "v" + info.Version : info.TagName) + " 🎉",
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E8449")),
                VerticalAlignment = VerticalAlignment.Center
            });
            if (!string.IsNullOrEmpty(info.Title))
            {
                verPanel.Children.Add(new TextBlock
                {
                    Text = "  (" + info.Title + ")",
                    FontSize = 12,
                    FontStyle = FontStyles.Italic,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#555555")),
                    VerticalAlignment = VerticalAlignment.Center
                });
            }
            verBorder.Child = verPanel;
            Grid.SetRow(verBorder, 1);
            grid.Children.Add(verBorder);

            // 3. Release Notes
            var notesBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(252, 250, 255)),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8DAEF")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 0, 10)
            };
            var notesScroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var notesText = new TextBlock
            {
                Text = string.IsNullOrEmpty(info.ReleaseNotes) ? "Новый релиз на GitHub! Улучшения и исправления ошибок." : info.ReleaseNotes,
                FontSize = 12,
                LineHeight = 18,
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2C3E50"))
            };
            notesScroll.Content = notesText;
            notesBorder.Child = notesScroll;
            Grid.SetRow(notesBorder, 2);
            grid.Children.Add(notesBorder);

            // 4. Progress & Status
            var progressStack = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };
            progressBar = new ProgressBar
            {
                Height = 8,
                Margin = new Thickness(0, 0, 0, 4),
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                Visibility = Visibility.Collapsed,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9B59B6")),
                Background = new SolidColorBrush(Color.FromRgb(235, 225, 245))
            };
            statusText = new TextBlock
            {
                Text = "",
                FontSize = 11,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7D3C98")),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            progressStack.Children.Add(progressBar);
            progressStack.Children.Add(statusText);
            Grid.SetRow(progressStack, 3);
            grid.Children.Add(progressStack);

            // 5. Buttons Row
            var btnRow = new Grid();
            btnRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            btnRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            btnRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            updateBtn = new Button
            {
                Content = "✨ Обновить сейчас",
                Height = 36,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9B59B6")),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 13,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                Padding = new Thickness(16, 0, 16, 0),
                Margin = new Thickness(0, 0, 8, 0)
            };
            updateBtn.Click += (s, e) => StartUpdate();
            Grid.SetColumn(updateBtn, 0);
            btnRow.Children.Add(updateBtn);

            githubBtn = new Button
            {
                Content = "🌐 GitHub",
                Height = 36,
                Background = new SolidColorBrush(Color.FromRgb(240, 235, 245)),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4A235A")),
                FontWeight = FontWeights.SemiBold,
                FontSize = 12,
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D2B4DE")),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand,
                Padding = new Thickness(12, 0, 12, 0),
                Margin = new Thickness(0, 0, 8, 0)
            };
            githubBtn.Click += (s, e) =>
            {
                try
                {
                    if (!string.IsNullOrEmpty(updateInfo.HtmlUrl))
                        Process.Start(updateInfo.HtmlUrl);
                }
                catch { }
            };
            Grid.SetColumn(githubBtn, 1);
            btnRow.Children.Add(githubBtn);

            laterBtn = new Button
            {
                Content = "Позже",
                Height = 36,
                Background = Brushes.Transparent,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7F8C8D")),
                FontSize = 12,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                Padding = new Thickness(12, 0, 12, 0)
            };
            laterBtn.Click += (s, e) => this.Close();
            Grid.SetColumn(laterBtn, 2);
            btnRow.Children.Add(laterBtn);

            Grid.SetRow(btnRow, 4);
            grid.Children.Add(btnRow);

            card.Child = grid;
            this.Content = card;
        }

        private void StartUpdate()
        {
            if (string.IsNullOrEmpty(updateInfo.DownloadUrl))
            {
                MessageBox.Show("Архив релиза (.zip) не найден в релизе на GitHub.\nСейчас откроется страница релиза на GitHub.", "Обновление", MessageBoxButton.OK, MessageBoxImage.Information);
                try { if (!string.IsNullOrEmpty(updateInfo.HtmlUrl)) Process.Start(updateInfo.HtmlUrl); } catch { }
                this.Close();
                return;
            }

            updateBtn.IsEnabled = false;
            laterBtn.IsEnabled = false;
            progressBar.Visibility = Visibility.Visible;
            statusText.Text = "Скачивание файлов обновления...";

            UpdateChecker.ApplyUpdate(
                updateInfo.DownloadUrl,
                delegate(int pct)
                {
                    progressBar.Value = pct;
                    statusText.Text = string.Format("Скачивание обновления: {0}%", pct);
                },
                delegate(string error)
                {
                    progressBar.Visibility = Visibility.Collapsed;
                    updateBtn.IsEnabled = true;
                    laterBtn.IsEnabled = true;
                    statusText.Text = error;
                    MessageBox.Show(error, "Ошибка обновления", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            );
        }
    }

    public class RepoConfigDialog : Window
    {
        private TextBox repoBox;
        private PetConfig config;
        private PetWindow ownerPet;

        public RepoConfigDialog(PetWindow pet, PetConfig cfg)
        {
            this.ownerPet = pet;
            this.config = cfg;
            this.Title = "⚙ Настройка репозитория GitHub";
            this.WindowStyle = WindowStyle.None;
            this.AllowsTransparency = true;
            this.Background = Brushes.Transparent;
            this.Topmost = true;
            this.Width = 380;
            this.Height = 190;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            var card = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(254, 252, 255)),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9B59B6")),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(16),
                Effect = new DropShadowEffect
                {
                    Color = Color.FromRgb(120, 60, 160),
                    BlurRadius = 20,
                    ShadowDepth = 4,
                    Opacity = 0.35,
                    Direction = 270
                }
            };

            var stack = new StackPanel();

            var title = new TextBlock
            {
                Text = "⚙ Репозиторий GitHub",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4A235A")),
                Margin = new Thickness(0, 0, 0, 6)
            };
            stack.Children.Add(title);

            var desc = new TextBlock
            {
                Text = "Укажите репозиторий проекта в формате: владелец/репозиторий (например, standoff2jdm-droid/chibimalutka):",
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#555555")),
                Margin = new Thickness(0, 0, 0, 8)
            };
            stack.Children.Add(desc);

            repoBox = new TextBox
            {
                Text = cfg.Repository,
                Height = 32,
                FontSize = 13,
                VerticalContentAlignment = VerticalAlignment.Center,
                Padding = new Thickness(8, 0, 8, 0),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D2B4DE")),
                BorderThickness = new Thickness(1.5),
                Background = new SolidColorBrush(Color.FromRgb(250, 245, 255)),
                Margin = new Thickness(0, 0, 0, 12)
            };
            stack.Children.Add(repoBox);

            var btnRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };

            var saveBtn = new Button
            {
                Content = "💾 Сохранить",
                Height = 32,
                Padding = new Thickness(16, 0, 16, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9B59B6")),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                Margin = new Thickness(0, 0, 8, 0)
            };
            saveBtn.Click += (s, e) => SaveRepo();
            btnRow.Children.Add(saveBtn);

            var cancelBtn = new Button
            {
                Content = "Отмена",
                Height = 32,
                Padding = new Thickness(12, 0, 12, 0),
                Background = Brushes.Transparent,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7F8C8D")),
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
            cancelBtn.Click += (s, e) => this.Close();
            btnRow.Children.Add(cancelBtn);

            stack.Children.Add(btnRow);
            card.Child = stack;
            this.Content = card;

            this.Loaded += (s, e) => { repoBox.Focus(); repoBox.SelectAll(); };
        }

        private void SaveRepo()
        {
            string val = repoBox.Text.Trim();
            if (!string.IsNullOrEmpty(val) && val.Contains("/"))
            {
                config.Repository = val;
                config.Save();
                if (ownerPet != null)
                {
                    ownerPet.ShowSpeech(string.Format("Репозиторий обновлён: {0} ✨", val));
                }
                this.Close();
            }
            else
            {
                MessageBox.Show("Пожалуйста, введите репозиторий в формате: владелец/репозиторий", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

    public class AboutDialogWindow : Window
    {
        public AboutDialogWindow(string repo)
        {
            this.Title = "ℹ О программе";
            this.WindowStyle = WindowStyle.None;
            this.AllowsTransparency = true;
            this.Background = Brushes.Transparent;
            this.Topmost = true;
            this.Width = 360;
            this.Height = 220;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            var card = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(254, 252, 255)),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9B59B6")),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(16),
                Effect = new DropShadowEffect
                {
                    Color = Color.FromRgb(120, 60, 160),
                    BlurRadius = 20,
                    ShadowDepth = 4,
                    Opacity = 0.35,
                    Direction = 270
                }
            };

            var stack = new StackPanel();

            var title = new TextBlock
            {
                Text = "🌸 Chibi Desktop Pet",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4A235A")),
                Margin = new Thickness(0, 0, 0, 4)
            };
            stack.Children.Add(title);

            var ver = new TextBlock
            {
                Text = "Версия: v" + UpdateChecker.AppVersion,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9B59B6")),
                Margin = new Thickness(0, 0, 0, 8)
            };
            stack.Children.Add(ver);

            var desc = new TextBlock
            {
                Text = "Интерактивный 2D аниме чиби-маскот для Windows с физикой Shimeji, лазанием по окнам, системой кормления, динамическими глазами и автообновлениями через GitHub Releases.",
                FontSize = 11,
                LineHeight = 16,
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333333")),
                Margin = new Thickness(0, 0, 0, 14)
            };
            stack.Children.Add(desc);

            var btnRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };

            if (!string.IsNullOrEmpty(repo) && repo.Contains("/"))
            {
                var gitBtn = new Button
                {
                    Content = "🌐 GitHub",
                    Height = 30,
                    Padding = new Thickness(12, 0, 12, 0),
                    Background = new SolidColorBrush(Color.FromRgb(240, 235, 245)),
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4A235A")),
                    FontWeight = FontWeights.SemiBold,
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D2B4DE")),
                    BorderThickness = new Thickness(1),
                    Cursor = Cursors.Hand,
                    Margin = new Thickness(0, 0, 8, 0)
                };
                gitBtn.Click += (s, e) =>
                {
                    try { Process.Start("https://github.com/" + repo); } catch { }
                };
                btnRow.Children.Add(gitBtn);
            }

            var closeBtn = new Button
            {
                Content = "Закрыть",
                Height = 30,
                Padding = new Thickness(14, 0, 14, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9B59B6")),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
            closeBtn.Click += (s, e) => this.Close();
            btnRow.Children.Add(closeBtn);

            stack.Children.Add(btnRow);
            card.Child = stack;
            this.Content = card;
        }
    }

    public class PetWindow : Window
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr GetTopWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("dwmapi.dll")]
        private static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out RECT pvAttribute, int cbAttribute);
        private const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;

        private static bool GetVisibleWindowRect(IntPtr hwnd, out RECT rect)
        {
            if (hwnd != IntPtr.Zero)
            {
                try
                {
                    if (DwmGetWindowAttribute(hwnd, DWMWA_EXTENDED_FRAME_BOUNDS, out rect, Marshal.SizeOf(typeof(RECT))) == 0)
                    {
                        return true;
                    }
                }
                catch { }
                return GetWindowRect(hwnd, out rect);
            }
            rect = new RECT();
            return false;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        // Window Platform Tracking
        private IntPtr currentStandingHwnd = IntPtr.Zero;
        private IntPtr lastStandingHwnd = IntPtr.Zero;

        private Grid rootGrid;
        private Canvas spriteCanvas;
        private Image petImage;

        // Eye Tracking Elements
        private Canvas eyeCanvas;
        private Image pupilLeftImg;
        private Image pupilRightImg;
        private Image eyelashesOverlay;

        private Canvas treatCanvas;
        private Canvas particleCanvas;

        // Speech bubble container (strictly right above head!)
        private StackPanel speechContainer;
        private ScaleTransform speechScale;
        private Border speechBorder;
        private TextBlock speechBubble;
        private System.Windows.Shapes.Path speechTail;

        // Transforms for Live2D-like spring physics
        private TransformGroup transformGroup;
        private ScaleTransform scaleTransform;
        private RotateTransform rotateTransform;
        private TranslateTransform translateTransform;

        // Springs & Procedural Physics
        private double springScaleX = 1.0;
        private double springScaleY = 1.0;
        private double velScaleX = 0.0;
        private double velScaleY = 0.0;

        private double springRot = 0.0;
        private double velRot = 0.0;
        private double targetRot = 0.0;

        private double targetHoverY = 0.0;
        private double curHoverY = 0.0;

        // Real Eye Tracking variables
        private double targetPupilX = 0.0;
        private double targetPupilY = 0.0;
        private double curPupilX = 0.0;
        private double curPupilY = 0.0;

        // Native Eye Coordinates inside pet sprite (1024x1120 space mapped to petWidth x petHeight)
        private double basePupilLeftX;
        private double basePupilLeftY;
        private double basePupilRightX;
        private double basePupilRightY;
        private double pupilW;
        private double pupilH;

        // Timers
        private DispatcherTimer physicsTimer;
        private DispatcherTimer blinkTimer;
        private DispatcherTimer speechTimer;
        private DispatcherTimer windowWatchTimer;
        private DispatcherTimer idleActivityTimer;

        // Sprites Bitmaps
        private BitmapSource imgIdleSclera;
        private BitmapSource imgIdle;
        private BitmapSource imgSit;
        private BitmapSource imgWalk1;
        private BitmapSource imgWalk2;
        private BitmapSource imgBlink;
        private BitmapSource imgHappy;
        private BitmapSource imgDrag;
        private BitmapSource imgConsole;
        private BitmapSource imgDrawing;
        private BitmapSource imgSleep;
        private BitmapSource imgPout;
        private BitmapSource imgClimb1;
        private BitmapSource imgClimb2;
        private BitmapSource imgPupilL;
        private BitmapSource imgPupilR;
        private BitmapSource imgEyelashes;

        // Walking & Facing Animation
        private double facingDirection = 1.0;
        private double walkAnimTimer = 0.0;
        private bool walkFrameToggle = false;

        // Chibi Treats Bitmaps
        private BitmapSource imgTreatCake;
        private BitmapSource imgTreatBoba;
        private BitmapSource imgTreatStrawberry;
        private BitmapSource imgTreatCookie;

        private PetState currentState = PetState.Idle;

        private double petEnergy = 0.0;
        private Point lastMousePos;
        private bool isMouseDown = false;
        private bool isDragging = false;
        private Point dragStartPoint;
        private Point lastScreenDragPoint;

        private List<PetParticle> particles = new List<PetParticle>();
        private Random random = new Random();

        // Canvas dimensions:
        private double baseWidth = 280;
        private double baseHeight = 350;
        private double petWidth = 240;
        private double petHeight = 240; // 1:1 Aspect ratio matching 1024x1024 sprites
        private double currentScale = 1.0;

        // Sitting posture & Dangling Legs calibration
        private bool isSittingDangling = false; // Default: stand firmly on feet directly on taskbar/window!
        private double legDangleOffset = 65.0;  // Pixels legs hang below taskbar line when sitting

        private bool enableGravity = true;
        private bool enableSound = true;
        private bool enableAppWatch = true;
        private bool enableEyeTracking = false; // Default: crisp, beautiful, authentic anime face!

        // Affection Level (0 - 100%)
        private int affectionLevel = 25;

        private double velocityY = 0.0;
        private double simTime = 0.0;
        private double happyDuration = 0.0;
        private double inactivitySeconds = 0.0;

        // Walking
        private double walkSpeed = 0.0;
        private double walkTargetX = 0.0;
        private bool isWalking = false;

        // Window tracking
        private string lastActiveTitle = "";
        private double lastCommentTime = -50.0;
        private List<AppReactionProfile> appProfiles = new List<AppReactionProfile>();

        private string spritesDir;
        private string soundsDir;

        // Sounds
        private List<SoundPlayer> petSounds = new List<SoundPlayer>();
        private SoundPlayer clickSound;
        private SoundPlayer purrSound;
        private SoundPlayer soundNya;
        private SoundPlayer soundSqueak;
        private SoundPlayer soundPet1;

        // Window Climbing & Shimeji Physics
        private ClimbState climbState = ClimbState.None;
        private bool climbOnLeft = true;
        private double climbSpeed = 2.2;
        private double perchedTimer = 0.0;
        private double jumpVelocityX = 0.0;
        private RECT targetWindowRect;
        private IntPtr targetWindowHwnd = IntPtr.Zero;
        private double climbAnimTimer = 0.0;
        private bool climbFrameToggle = false;

        // Feeding System
        private TreatWindow activeTreat = null;

        // Auto-Update & Configuration
        private PetConfig petConfig;
        private UpdateInfo pendingUpdate = null;
        private DispatcherTimer startupUpdateTimer;

        public PetWindow()
        {
            this.Title = "Chibi Desktop Pet";
            this.WindowStyle = WindowStyle.None;
            this.AllowsTransparency = true;
            this.Background = Brushes.Transparent;
            this.Topmost = true;
            this.ShowInTaskbar = false;
            this.ResizeMode = ResizeMode.NoResize;

            this.Width = baseWidth;
            this.Height = baseHeight;

            var workArea = SystemParameters.WorkArea;
            this.Left = Math.Max(20, workArea.Right - baseWidth - 80);
            this.Top = Math.Max(20, workArea.Bottom - baseHeight + (isSittingDangling ? legDangleOffset : 6.0));

            InitAppProfiles();
            InitUI();
            InitResources();
            InitTimers();
            InitEvents();
            InitContextMenu();
            InitUpdateSystem();
        }

        private void InitAppProfiles()
        {
            // 1. Music & Audio
            appProfiles.Add(new AppReactionProfile(
                new string[] { "spotify", "music", "aimp", "fl studio", "ableton", "reaper", "soundcloud", "яндекс музыка" },
                "Музыка",
                PetState.Happy,
                new string[] {
                    "Поём вместе! Ла-ла-ла~ 🎵✨",
                    "Какой качающий трек! Я подпеваю! 🎶💖",
                    "Твой музыкальный вкус — просто космос! ⭐"
                },
                "🎵",
                Color.FromRgb(255, 105, 180)
            ));

            // 2. Video & Streams
            appProfiles.Add(new AppReactionProfile(
                new string[] { "youtube", "twitch", "vlc", "obs", "netflix", "кинопоиск", "stream" },
                "Видео и Стримы",
                PetState.Happy,
                new string[] {
                    "Киносеанс! Я сажусь рядом с тобой смотреть! 🍿✨",
                    "Ого, видосики! Покажи мне тоже! 🎥⭐",
                    "Уютный вечер за просмотром~ 💖"
                },
                "🍿",
                Color.FromRgb(255, 215, 0)
            ));

            // 3. Games
            appProfiles.Add(new AppReactionProfile(
                new string[] { "steam", "dota", "counter-strike", "minecraft", "genshin", "honkai", "valorant", "roblox", "osu!", "league of legends", "apex", "cyberpunk" },
                "Игры",
                PetState.Console,
                new string[] {
                    "Ооо, катка началась! Тащи, я верю в тебя! 🎮🔥",
                    "Главное не тильтовать! Мы победим! ⭐",
                    "Смотрю на твои клатчи! Болею всеми лапками! 💖",
                    "Я тоже играю в консольку за компанию! 🕹✨"
                },
                "⭐",
                Color.FromRgb(255, 215, 0)
            ));

            // 4. Digital Art & Design
            appProfiles.Add(new AppReactionProfile(
                new string[] { "photoshop", "krita", "paint", "blender", "aseprite", "sai", "clip studio", "figma", "illustrator" },
                "Творчество",
                PetState.Drawing,
                new string[] {
                    "Ого, ты рисуешь! Нарисуй мне звёздочку! 🎨⭐",
                    "Творческий вайб~ У тебя так красиво получается! ✨",
                    "Я твоя муза! Смотрю за каждым мазком~ 🌸",
                    "Шедевр рождается прямо сейчас! 💖"
                },
                "🎨",
                Color.FromRgb(200, 150, 255)
            ));

            // 5. Programming & Dev Tools
            appProfiles.Add(new AppReactionProfile(
                new string[] { "visual studio", "code", "pycharm", "intellij", "rider", "sublime", "git", "powershell", "terminal", "cursor", "clion" },
                "Программирование",
                PetState.Drawing,
                new string[] {
                    "Пишешь код? Я твой резиновый утёнок — расскажи алгоритм! 💻✨",
                    "Пусть скомпилируется с первого раза без единого бага! ⭐",
                    "Ты такой умный программист! Не забудь попить воды! 💖",
                    "Чистый код — залог счастья! (´꒳`)"
                },
                "💻",
                Color.FromRgb(100, 220, 255)
            ));

            // 6. Messengers & Social
            appProfiles.Add(new AppReactionProfile(
                new string[] { "telegram", "discord", "vk", "whatsapp", "slack" },
                "Общение",
                PetState.Happy,
                new string[] {
                    "С кем переписываешься? Передай привет от Чиби! 💬🌸",
                    "Дискорд звенит! Кто-то зовёт в войс? 🎧",
                    "Опять чатики~ Не забывай про меня среди друзей! 💕"
                },
                "💬",
                Color.FromRgb(255, 150, 200)
            ));

            // 7. Web Browsers
            appProfiles.Add(new AppReactionProfile(
                new string[] { "chrome", "firefox", "opera", "edge", "yandex", "browser", "brave", "vivaldi" },
                "Браузер",
                PetState.Idle,
                new string[] {
                    "Что интересного ищешь в интернете? 🌐",
                    "Смотришь мемчики или статьи? Покажи мне тоже! ✨",
                    "Не открывай сразу 100 закладок! 🌸"
                },
                "🌐",
                Color.FromRgb(120, 200, 255)
            ));
        }

        private void InitUI()
        {
            rootGrid = new Grid();
            rootGrid.ClipToBounds = false;

            // 1. Sprite Canvas (anchored at the bottom)
            spriteCanvas = new Canvas
            {
                Width = petWidth,
                Height = petHeight,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
                ClipToBounds = false
            };

            transformGroup = new TransformGroup();
            scaleTransform = new ScaleTransform(1.0, 1.0, petWidth * 0.5, petHeight * 0.95);
            rotateTransform = new RotateTransform(0.0, petWidth * 0.5, petHeight * 0.80);
            translateTransform = new TranslateTransform(0.0, 0.0);

            transformGroup.Children.Add(scaleTransform);
            transformGroup.Children.Add(rotateTransform);
            transformGroup.Children.Add(translateTransform);
            spriteCanvas.RenderTransform = transformGroup;

            // Base pet body image
            petImage = new Image
            {
                Stretch = Stretch.Uniform,
                Width = petWidth,
                Height = petHeight
            };
            RenderOptions.SetBitmapScalingMode(petImage, BitmapScalingMode.HighQuality);
            spriteCanvas.Children.Add(petImage);

            // 2. Real Eye Tracking Canvas (pupils moving smoothly behind eyelashes)
            eyeCanvas = new Canvas
            {
                Width = petWidth,
                Height = petHeight,
                ClipToBounds = false,
                IsHitTestVisible = false
            };

            // Calculate eye sizes based on 1024x1024 canvas
            pupilW = (70.0 / 1024.0) * petWidth;
            pupilH = (72.0 / 1024.0) * petHeight;

            basePupilLeftX = (447.0 / 1024.0) * petWidth - pupilW * 0.5;
            basePupilLeftY = (296.0 / 1024.0) * petHeight - pupilH * 0.5;

            basePupilRightX = (574.0 / 1024.0) * petWidth - pupilW * 0.5;
            basePupilRightY = (296.0 / 1024.0) * petHeight - pupilH * 0.5;

            pupilLeftImg = new Image
            {
                Width = pupilW,
                Height = pupilH,
                Stretch = Stretch.Uniform,
                IsHitTestVisible = false
            };
            RenderOptions.SetBitmapScalingMode(pupilLeftImg, BitmapScalingMode.HighQuality);
            Canvas.SetLeft(pupilLeftImg, basePupilLeftX);
            Canvas.SetTop(pupilLeftImg, basePupilLeftY);

            pupilRightImg = new Image
            {
                Width = pupilW,
                Height = pupilH,
                Stretch = Stretch.Uniform,
                IsHitTestVisible = false
            };
            RenderOptions.SetBitmapScalingMode(pupilRightImg, BitmapScalingMode.HighQuality);
            Canvas.SetLeft(pupilRightImg, basePupilRightX);
            Canvas.SetTop(pupilRightImg, basePupilRightY);

            eyelashesOverlay = new Image
            {
                Width = petWidth,
                Height = petHeight,
                Stretch = Stretch.Uniform,
                IsHitTestVisible = false
            };
            RenderOptions.SetBitmapScalingMode(eyelashesOverlay, BitmapScalingMode.HighQuality);

            eyeCanvas.Children.Add(pupilLeftImg);
            eyeCanvas.Children.Add(pupilRightImg);
            eyeCanvas.Children.Add(eyelashesOverlay);
            eyeCanvas.Visibility = Visibility.Collapsed;
            spriteCanvas.Children.Add(eyeCanvas);

            // 3. Treat item canvas
            treatCanvas = new Canvas
            {
                ClipToBounds = false,
                IsHitTestVisible = false
            };

            // 4. Particle canvas
            particleCanvas = new Canvas
            {
                ClipToBounds = false,
                IsHitTestVisible = false
            };

            // 5. Speech Bubble Container (Placed strictly snug above character's head!)
            speechContainer = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(6, 0, 6, petHeight + 4), // Exactly above the ahoge/horns!
                Opacity = 0,
                IsHitTestVisible = false,
                RenderTransformOrigin = new Point(0.5, 1.0)
            };

            speechScale = new ScaleTransform(1.0, 1.0);
            speechContainer.RenderTransform = speechScale;

            speechBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(254, 252, 255)),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9B59B6")),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(14, 8, 14, 8),
                MaxWidth = 250,
                Effect = new DropShadowEffect
                {
                    Color = Color.FromRgb(155, 89, 182),
                    BlurRadius = 14,
                    ShadowDepth = 3,
                    Opacity = 0.35,
                    Direction = 270
                }
            };

            speechBubble = new TextBlock
            {
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3E1A68")),
                FontFamily = new FontFamily("Segoe UI, Arial"),
                FontSize = 12.5,
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                Text = "Привет! ✨"
            };
            speechBorder.Child = speechBubble;
            speechContainer.Children.Add(speechBorder);

            // Cute balloon tail pointing down towards her face
            speechTail = new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse("M 0,0 L 14,0 L 7,9 Z"),
                Fill = new SolidColorBrush(Color.FromRgb(254, 252, 255)),
                Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9B59B6")),
                StrokeThickness = 2,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, -2, 0, 0)
            };
            speechContainer.Children.Add(speechTail);

            rootGrid.Children.Add(spriteCanvas);
            rootGrid.Children.Add(treatCanvas);
            rootGrid.Children.Add(particleCanvas);
            rootGrid.Children.Add(speechContainer);

            this.Content = rootGrid;
        }

        private void InitResources()
        {
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            spritesDir = System.IO.Path.Combine(appDir, "assets", "sprites");
            soundsDir = System.IO.Path.Combine(appDir, "assets", "sounds");

            if (!Directory.Exists(spritesDir)) Directory.CreateDirectory(spritesDir);
            if (!Directory.Exists(soundsDir)) Directory.CreateDirectory(soundsDir);

            ReloadResources();
        }

        public void ReloadResources()
        {
            // Sprites
            imgIdleSclera = LoadBitmap(System.IO.Path.Combine(spritesDir, "idle_sclera.png"));
            imgIdle = LoadBitmap(System.IO.Path.Combine(spritesDir, "idle.png"));
            imgSit = LoadBitmap(System.IO.Path.Combine(spritesDir, "sit.png")) ?? imgIdle;
            imgWalk1 = LoadBitmap(System.IO.Path.Combine(spritesDir, "walk1.png")) ?? imgIdle;
            imgWalk2 = LoadBitmap(System.IO.Path.Combine(spritesDir, "walk2.png")) ?? imgIdle;
            imgBlink = LoadBitmap(System.IO.Path.Combine(spritesDir, "blink.png")) ?? imgIdle;
            imgHappy = LoadBitmap(System.IO.Path.Combine(spritesDir, "happy.png")) ?? imgIdle;
            imgDrag = LoadBitmap(System.IO.Path.Combine(spritesDir, "drag.png")) ?? imgIdle;
            imgConsole = LoadBitmap(System.IO.Path.Combine(spritesDir, "console.png")) ?? imgIdle;
            imgDrawing = LoadBitmap(System.IO.Path.Combine(spritesDir, "drawing.png")) ?? imgIdle;
            imgSleep = LoadBitmap(System.IO.Path.Combine(spritesDir, "sleep.png")) ?? imgIdle;
            imgPout = LoadBitmap(System.IO.Path.Combine(spritesDir, "pout.png")) ?? imgIdle;
            imgClimb1 = LoadBitmap(System.IO.Path.Combine(spritesDir, "climb1.png")) ?? imgDrag;
            imgClimb2 = LoadBitmap(System.IO.Path.Combine(spritesDir, "climb2.png")) ?? imgDrag;

            imgPupilL = LoadBitmap(System.IO.Path.Combine(spritesDir, "pupil_l.png"));
            imgPupilR = LoadBitmap(System.IO.Path.Combine(spritesDir, "pupil_r.png"));
            imgEyelashes = LoadBitmap(System.IO.Path.Combine(spritesDir, "eyelashes.png"));

            // Treats
            imgTreatCake = LoadBitmap(System.IO.Path.Combine(spritesDir, "treat_cake.png"));
            imgTreatBoba = LoadBitmap(System.IO.Path.Combine(spritesDir, "treat_boba.png"));
            imgTreatStrawberry = LoadBitmap(System.IO.Path.Combine(spritesDir, "treat_strawberry.png"));
            imgTreatCookie = LoadBitmap(System.IO.Path.Combine(spritesDir, "treat_cookie.png"));

            if (pupilLeftImg != null) pupilLeftImg.Source = imgPupilL;
            if (pupilRightImg != null) pupilRightImg.Source = imgPupilR;
            if (eyelashesOverlay != null) eyelashesOverlay.Source = imgEyelashes;

            // Sounds
            petSounds.Clear();
            soundNya = CreateSoundPlayer(System.IO.Path.Combine(soundsDir, "nya.wav"));
            soundSqueak = CreateSoundPlayer(System.IO.Path.Combine(soundsDir, "squeak.wav"));
            soundPet1 = CreateSoundPlayer(System.IO.Path.Combine(soundsDir, "pet1.wav"));
            clickSound = CreateSoundPlayer(System.IO.Path.Combine(soundsDir, "click.wav"));
            purrSound = CreateSoundPlayer(System.IO.Path.Combine(soundsDir, "purr.wav"));

            if (soundNya != null) petSounds.Add(soundNya);
            if (soundSqueak != null) petSounds.Add(soundSqueak);
            if (soundPet1 != null) petSounds.Add(soundPet1);

            currentState = PetState.Idle;
            UpdateDisplay();
        }

        private SoundPlayer CreateSoundPlayer(string path)
        {
            if (File.Exists(path))
            {
                try
                {
                    var sp = new SoundPlayer(path);
                    sp.Load();
                    return sp;
                }
                catch { }
            }
            return null;
        }

        private BitmapSource LoadBitmap(string fullPath)
        {
            if (!File.Exists(fullPath)) return null;
            try
            {
                var bi = new BitmapImage();
                bi.BeginInit();
                bi.UriSource = new Uri(fullPath, UriKind.Absolute);
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.EndInit();
                bi.Freeze();
                return bi;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error loading bitmap " + fullPath + ": " + ex.Message);
                return null;
            }
        }

        private void InitTimers()
        {
            physicsTimer = new DispatcherTimer();
            physicsTimer.Interval = TimeSpan.FromMilliseconds(16);
            physicsTimer.Tick += PhysicsTimer_Tick;
            physicsTimer.Start();

            // Natural 2D Eye Blinking
            blinkTimer = new DispatcherTimer();
            blinkTimer.Interval = TimeSpan.FromSeconds(3.2);
            blinkTimer.Tick += BlinkTimer_Tick;
            blinkTimer.Start();

            // Speech bubble auto-hide
            speechTimer = new DispatcherTimer();
            speechTimer.Interval = TimeSpan.FromSeconds(3.5);
            speechTimer.Tick += (s, e) =>
            {
                speechTimer.Stop();
                var anim = new DoubleAnimation(0.0, TimeSpan.FromMilliseconds(300));
                speechContainer.BeginAnimation(UIElement.OpacityProperty, anim);
            };

            // Active window detector
            windowWatchTimer = new DispatcherTimer();
            windowWatchTimer.Interval = TimeSpan.FromSeconds(1.2);
            windowWatchTimer.Tick += WindowWatchTimer_Tick;
            windowWatchTimer.Start();

            // Idle activities
            idleActivityTimer = new DispatcherTimer();
            idleActivityTimer.Interval = TimeSpan.FromSeconds(12);
            idleActivityTimer.Tick += IdleActivityTimer_Tick;
            idleActivityTimer.Start();
        }

        private void PhysicsTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                simTime += 0.016;
                inactivitySeconds += 0.016;

            // Breathing & Hover Floating
            double breathY = 1.0;
            double breathX = 1.0;
            if (currentState == PetState.Idle || currentState == PetState.Blink)
            {
                double breathSin = Math.Sin(simTime * 2.5);
                breathY = 1.0 + breathSin * 0.025;
                breathX = 1.0 - breathSin * 0.015;
                targetHoverY = Math.Sin(simTime * 1.8) * 2.0;

                // When sitting with dangling legs on taskbar: swing legs cutely!
                if (isSittingDangling && climbState == ClimbState.None && !isWalking && !isDragging)
                {
                    targetRot = Math.Sin(simTime * 2.2) * 3.5;
                    targetHoverY += Math.Sin(simTime * 3.0) * 1.5;
                }
            }
            else if (currentState == PetState.Sleep)
            {
                double breathSin = Math.Sin(simTime * 1.6);
                breathY = 1.0 + breathSin * 0.035;
                breathX = 1.0 - breathSin * 0.02;
                targetHoverY = Math.Sin(simTime * 1.0) * 1.5;
            }
            else if (currentState == PetState.Dragging)
            {
                breathY = 1.08;
                breathX = 0.93;
                targetHoverY = 0;
            }
            else
            {
                targetHoverY = 0;
            }

            // Springs for Squash & Stretch
            double kSpring = 0.20;
            double dSpring = 0.75;

            double forceY = -kSpring * (springScaleY - breathY) - dSpring * velScaleY;
            velScaleY += forceY;
            springScaleY += velScaleY;

            double forceX = -kSpring * (springScaleX - breathX) - dSpring * velScaleX;
            velScaleX += forceX;
            springScaleX += velScaleX;

            if (climbState == ClimbState.ClimbingWall)
            {
                scaleTransform.ScaleX = (climbOnLeft ? 1.0 : -1.0) * Math.Abs(springScaleX);
            }
            else
            {
                scaleTransform.ScaleX = facingDirection * Math.Abs(springScaleX);
            }
            scaleTransform.ScaleY = springScaleY;

            // Rotation Spring
            double forceRot = -0.15 * (springRot - targetRot) - 0.70 * velRot;
            velRot += forceRot;
            springRot += velRot;
            rotateTransform.Angle = springRot;

            curHoverY += (targetHoverY - curHoverY) * 0.1;
            translateTransform.Y = curHoverY;

            // REAL EYE PUPIL TRACKING UPDATE
            UpdateEyeTracking();

            // Sleep floating 'zZZ'
            if (currentState == PetState.Sleep && random.Next(120) < 2)
            {
                SpawnParticle("z", Color.FromRgb(160, 210, 255), this.Width * 0.45, this.Height * 0.50);
            }

            // High affection golden sparkles
            if (affectionLevel >= 75 && random.Next(150) < 2)
            {
                SpawnParticle("✨", Color.FromRgb(255, 230, 110), this.Width * 0.5 + (random.NextDouble() - 0.5) * 50, this.Height * 0.65 + (random.NextDouble() - 0.5) * 40);
            }

            // Treat feeding physics
            UpdateTreatPhysics();

            // Window Climbing & Shimeji Physics
            UpdateClimbingPhysics();

            // Normal Walking update (when not climbing)
            if (climbState == ClimbState.None && activeTreat == null)
            {
                if (isWalking && !isDragging)
                {
                    if (Math.Abs(this.Left - walkTargetX) > 2.0)
                    {
                        this.Left += walkSpeed;
                        double stepSin = Math.Sin(simTime * 12.0);
                        targetHoverY = -Math.Abs(stepSin) * 6.0;
                        targetRot = stepSin * 4.0;

                        facingDirection = (walkSpeed < 0 ? -1.0 : 1.0);
                        scaleTransform.ScaleX = facingDirection * Math.Abs(springScaleX);

                        walkAnimTimer += 0.016;
                        if (walkAnimTimer >= 0.16)
                        {
                            walkAnimTimer = 0.0;
                            walkFrameToggle = !walkFrameToggle;
                            if (imgWalk1 != null && imgWalk2 != null)
                            {
                                petImage.Source = walkFrameToggle ? imgWalk1 : imgWalk2;
                            }
                        }
                    }
                    else
                    {
                        isWalking = false;
                        targetRot = 0.0;
                        scaleTransform.ScaleX = facingDirection * Math.Abs(springScaleX);
                        UpdateDisplay();
                    }
                }
            }

            // Happy state cooldown
            if (currentState == PetState.Happy)
            {
                happyDuration -= 0.016;
                if (happyDuration <= 0)
                {
                    currentState = PetState.Idle;
                    UpdateDisplay();
                }
            }

            // Pet energy decay
            if (petEnergy > 0)
            {
                petEnergy = Math.Max(0, petEnergy - 0.5);
            }

            // Gravity & Ground Landing (Window Platform & Taskbar calibration)
            if (enableGravity && climbState == ClimbState.None && !isDragging)
            {
                IntPtr groundWin;
                double effectiveGroundY = GetGroundSurfaceY(out groundWin);
                currentStandingHwnd = groundWin;

                if (this.Top < effectiveGroundY)
                {
                    velocityY += 1.6;
                    this.Top = Math.Min(effectiveGroundY, this.Top + velocityY);
                    if (this.Top >= effectiveGroundY)
                    {
                        this.Top = effectiveGroundY;
                        if (velocityY > 5.0)
                        {
                            springScaleY = 0.80;
                            springScaleX = 1.20;
                            velocityY = -velocityY * 0.35;
                        }
                        else
                        {
                            velocityY = 0;
                        }

                        if (currentState == PetState.Dragging)
                        {
                            currentState = PetState.Idle;
                            UpdateDisplay();
                        }

                        if (groundWin != IntPtr.Zero && lastStandingHwnd != groundWin)
                        {
                            ShowSpeech("Приземлилась на окошко! 🪟✨");
                            PlayNyaSound();
                        }
                        lastStandingHwnd = groundWin;
                    }
                }
                else if (this.Top > effectiveGroundY)
                {
                    // If window moves, follow smoothly
                    if (groundWin != IntPtr.Zero)
                    {
                        this.Top += (effectiveGroundY - this.Top) * 0.35;
                    }
                }
            }

            // SCREEN BOUNDS CLAMPING (Never go off-screen!)
            ClampToScreenBounds();

            UpdateParticles();
            }
            catch { }
        }

        private void UpdateEyeTracking()
        {
            if (eyeCanvas == null) return;

            if (!enableEyeTracking || currentState != PetState.Idle || isSittingDangling || isWalking || isDragging || climbState != ClimbState.None || imgPupilL == null)
            {
                if (eyeCanvas.Visibility != Visibility.Collapsed)
                {
                    eyeCanvas.Visibility = Visibility.Collapsed;
                }
                return;
            }

            eyeCanvas.Visibility = Visibility.Visible;

            // Get absolute screen cursor position
            POINT cur;
            if (GetCursorPos(out cur))
            {
                // Calculate pet's eyes screen center
                Point petScreenCenter = this.PointToScreen(new Point(this.Width * 0.5, this.Height - petHeight * 0.60));

                double dx = cur.X - petScreenCenter.X;
                double dy = cur.Y - petScreenCenter.Y;

                double dist = Math.Sqrt(dx * dx + dy * dy);
                if (dist > 1.0)
                {
                    double maxOffset = 2.4; // Maximum smooth iris travel in pixels
                    double factor = Math.Min(maxOffset, dist * 0.008);
                    double angle = Math.Atan2(dy, dx);

                    targetPupilX = Math.Cos(angle) * factor;
                    targetPupilY = Math.Sin(angle) * (factor * 0.55); // Subtle vertical eye movement
                }
                else
                {
                    targetPupilX = 0;
                    targetPupilY = 0;
                }
            }

            // Smooth spring damping for eye pupils
            curPupilX += (targetPupilX - curPupilX) * 0.25;
            curPupilY += (targetPupilY - curPupilY) * 0.25;

            Canvas.SetLeft(pupilLeftImg, basePupilLeftX + curPupilX);
            Canvas.SetTop(pupilLeftImg, basePupilLeftY + curPupilY);

            Canvas.SetLeft(pupilRightImg, basePupilRightX + curPupilX);
            Canvas.SetTop(pupilRightImg, basePupilRightY + curPupilY);
        }

        private void ClampToScreenBounds()
        {
            var workArea = SystemParameters.WorkArea;

            // Horizontal Clamp
            double minX = workArea.Left;
            double maxX = workArea.Right - this.Width;
            if (this.Left < minX)
            {
                this.Left = minX;
                if (isWalking && walkSpeed < 0) walkSpeed = Math.Abs(walkSpeed);
            }
            if (this.Left > maxX)
            {
                this.Left = maxX;
                if (isWalking && walkSpeed > 0) walkSpeed = -Math.Abs(walkSpeed);
            }

            // Vertical Clamp
            double minY = workArea.Top - 85.0;
            double maxY = workArea.Bottom - this.Height + (isSittingDangling ? 65.0 : 6.0);

            if (this.Top < minY)
            {
                this.Top = minY;
                if (velocityY < 0) velocityY = 0;
            }
            if (this.Top > maxY && climbState == ClimbState.None && !isDragging)
            {
                this.Top = maxY;
            }

            // Smart Speech Bubble Shift so text never clips on left/right screen edges!
            double screenLeftMargin = this.Left;
            double screenRightMargin = workArea.Right - (this.Left + this.Width);

            if (screenLeftMargin < 20)
            {
                speechContainer.HorizontalAlignment = HorizontalAlignment.Left;
                speechContainer.Margin = new Thickness(Math.Max(0, 20 - screenLeftMargin), 0, 0, petHeight + 4);
                speechTail.HorizontalAlignment = HorizontalAlignment.Left;
                speechTail.Margin = new Thickness(petWidth * 0.5 - 20, -2, 0, 0);
            }
            else if (screenRightMargin < 20)
            {
                speechContainer.HorizontalAlignment = HorizontalAlignment.Right;
                speechContainer.Margin = new Thickness(0, 0, Math.Max(0, 20 - screenRightMargin), petHeight + 4);
                speechTail.HorizontalAlignment = HorizontalAlignment.Right;
                speechTail.Margin = new Thickness(0, -2, petWidth * 0.5 - 20, 0);
            }
            else
            {
                speechContainer.HorizontalAlignment = HorizontalAlignment.Center;
                speechContainer.Margin = new Thickness(6, 0, 6, petHeight + 4);
                speechTail.HorizontalAlignment = HorizontalAlignment.Center;
                speechTail.Margin = new Thickness(0, -2, 0, 0);
            }
        }

        private void UpdateTreatPhysics()
        {
            if (activeTreat == null) return;

            activeTreat.UpdatePhysics();

            if (activeTreat.IsBeingDragged) return;

            if (activeTreat.Landed && !isDragging && climbState == ClimbState.None)
            {
                // In world screen space
                double petCenterX = this.Left + this.Width * 0.5;
                double treatCenterX = activeTreat.Left + activeTreat.Width * 0.5;
                double dx = treatCenterX - petCenterX;

                if (Math.Abs(dx) > 30.0)
                {
                    // Run towards the treat on desktop!
                    double moveDir = Math.Sign(dx);
                    facingDirection = moveDir;
                    scaleTransform.ScaleX = moveDir * Math.Abs(springScaleX);
                    targetRot = moveDir * 4.0;
                    targetHoverY = -Math.Abs(Math.Sin(simTime * 14.0)) * 5.0;

                    this.Left += moveDir * 2.8;

                    walkAnimTimer += 0.016;
                    if (walkAnimTimer >= 0.16)
                    {
                        walkAnimTimer = 0.0;
                        walkFrameToggle = !walkFrameToggle;
                        if (imgWalk1 != null && imgWalk2 != null)
                        {
                            petImage.Source = walkFrameToggle ? imgWalk1 : imgWalk2;
                        }
                    }
                }
                else
                {
                    // Arrived at the treat! Eat it!
                    EatActiveTreat();
                }
            }
        }

        private void EatActiveTreat()
        {
            if (activeTreat == null) return;

            string yum = activeTreat.YumPhrase;
            try { activeTreat.Close(); } catch { }
            activeTreat = null;

            isWalking = false;
            targetRot = 0.0;
            springScaleY = 0.80;
            springScaleX = 1.20;

            TriggerStateWithAnimation(PetState.Happy);
            happyDuration = 2.4;

            PlaySound(soundSqueak != null ? soundSqueak : soundNya);

            AddAffection(15);

            for (int i = 0; i < 6; i++)
            {
                SpawnParticle("💖", Color.FromRgb(255, 105, 180), this.Width * 0.5 + (random.NextDouble() - 0.5) * 35, this.Height * 0.55);
                SpawnParticle("⭐", Color.FromRgb(255, 215, 0), this.Width * 0.5 + (random.NextDouble() - 0.5) * 35, this.Height * 0.55);
            }

            ShowSpeech(yum);
        }

        private IntPtr FindForemostWindow(out RECT windowRect)
        {
            windowRect = new RECT();
            IntPtr myHwnd = IntPtr.Zero;
            try { myHwnd = new WindowInteropHelper(this).Handle; } catch { }
            IntPtr treatHwnd = activeTreat != null ? new WindowInteropHelper(activeTreat).Handle : IntPtr.Zero;

            double petCenterX = this.Left + this.Width * 0.5;
            double petCenterY = this.Top + this.Height * 0.5;

            IntPtr bestWnd = IntPtr.Zero;
            RECT bestRect = new RECT();
            double bestDist = double.MaxValue;

            IntPtr cur = GetTopWindow(IntPtr.Zero);
            int count = 0;
            while (cur != IntPtr.Zero && count < 60)
            {
                if (cur != myHwnd && cur != treatHwnd && IsWindowVisible(cur) && !IsIconic(cur))
                {
                    RECT r;
                    if (GetVisibleWindowRect(cur, out r))
                    {
                        int w = r.Right - r.Left;
                        int h = r.Bottom - r.Top;
                        // Avoid full screen desktop / invisible overlays
                        if (w > 250 && h > 180 && r.Left > -4000)
                        {
                            StringBuilder sb = new StringBuilder(256);
                            GetWindowText(cur, sb, 256);
                            string title = sb.ToString();
                            if (!string.IsNullOrEmpty(title) && title != "Program Manager" && title != "Windows Shell Experience Host" && !title.Contains("DesktopPet"))
                            {
                                // Distance from pet to nearest window border
                                double dx = Math.Max(0.0, Math.Max(r.Left - petCenterX, petCenterX - r.Right));
                                double dy = Math.Max(0.0, Math.Max(r.Top - petCenterY, petCenterY - r.Bottom));
                                double dist = Math.Sqrt(dx * dx + dy * dy);

                                if (cur == GetForegroundWindow()) dist -= 200.0;

                                if (dist < bestDist)
                                {
                                    bestDist = dist;
                                    bestWnd = cur;
                                    bestRect = r;
                                }
                            }
                        }
                    }
                }
                cur = GetWindow(cur, 2); // GW_HWNDNEXT
                count++;
            }

            if (bestWnd != IntPtr.Zero)
            {
                windowRect = bestRect;
                return bestWnd;
            }

            return IntPtr.Zero;
        }

        private double GetGroundSurfaceY(out IntPtr onWindowHwnd)
        {
            onWindowHwnd = IntPtr.Zero;
            var workArea = SystemParameters.WorkArea;
            double standOffset = isSittingDangling ? 65.0 : 6.0;
            double taskbarGroundY = workArea.Bottom - this.Height + standOffset;

            double petCenterX = this.Left + this.Width * 0.5;

            // Check foremost window beneath pet
            RECT r;
            IntPtr topWin = FindForemostWindow(out r);
            if (topWin != IntPtr.Zero)
            {
                // If pet is horizontally within this window's width
                if (petCenterX >= r.Left - 25 && petCenterX <= r.Right + 25)
                {
                    double windowSitTop = r.Top - this.Height + standOffset;
                    double minTop = workArea.Top - 85.0;
                    double windowTitlebarY = Math.Max(minTop, windowSitTop);

                    // If pet is above or sitting on the titlebar (and window is above taskbar)
                    if (this.Top <= windowTitlebarY + 40.0 && windowTitlebarY < taskbarGroundY - 30.0)
                    {
                        onWindowHwnd = topWin;
                        return windowTitlebarY;
                    }
                }
            }

            return taskbarGroundY;
        }

        private void UpdateClimbingPhysics()
        {
            if (climbState == ClimbState.None) return;

            RECT rect;
            if (climbState == ClimbState.WalkingToWindow)
            {
                if (!GetWindowRect(targetWindowHwnd, out rect) || rect.Left < -5000)
                {
                    climbState = ClimbState.None;
                    return;
                }
                targetWindowRect = rect;

                double targetX = climbOnLeft ? (rect.Left - 205.0) : (rect.Right - 75.0);
                double dx = targetX - this.Left;

                if (Math.Abs(dx) > 6.0)
                {
                    double moveDir = Math.Sign(dx);
                    this.Left += moveDir * 2.8;
                    double stepSin = Math.Sin(simTime * 12.0);
                    targetHoverY = -Math.Abs(stepSin) * 5.0;
                    targetRot = stepSin * 4.0;
                    facingDirection = moveDir;
                    scaleTransform.ScaleX = moveDir * Math.Abs(springScaleX);

                    walkAnimTimer += 0.016;
                    if (walkAnimTimer >= 0.16)
                    {
                        walkAnimTimer = 0.0;
                        walkFrameToggle = !walkFrameToggle;
                        if (imgWalk1 != null && imgWalk2 != null)
                        {
                            petImage.Source = walkFrameToggle ? imgWalk1 : imgWalk2;
                        }
                    }
                }
                else
                {
                    this.Left = targetX;
                    climbState = ClimbState.ClimbingWall;
                    currentState = PetState.Climbing;
                    facingDirection = climbOnLeft ? 1.0 : -1.0;
                    scaleTransform.ScaleX = facingDirection * Math.Abs(springScaleX);
                    UpdateDisplay();

                    ShowSpeech("Лезем наверх! 🧗✨");
                    PlaySound(soundSqueak != null ? soundSqueak : soundNya);
                }
            }
            else if (climbState == ClimbState.ClimbingWall)
            {
                if (!GetVisibleWindowRect(targetWindowHwnd, out rect) || rect.Left < -5000)
                {
                    climbState = ClimbState.JumpingDown;
                    velocityY = 0;
                    jumpVelocityX = 0;
                    return;
                }
                targetWindowRect = rect;

                // Lock horizontal position against the visible window edge
                if (climbOnLeft)
                {
                    this.Left = targetWindowRect.Left - 205.0;
                    facingDirection = 1.0;
                    scaleTransform.ScaleX = Math.Abs(springScaleX); // Facing right towards window wall
                }
                else
                {
                    this.Left = targetWindowRect.Right - 75.0;
                    facingDirection = -1.0;
                    scaleTransform.ScaleX = -Math.Abs(springScaleX); // Facing left towards window wall
                }

                // Climb UP with alternating stepping animation!
                this.Top -= climbSpeed;

                climbAnimTimer += 0.016;
                if (climbAnimTimer >= 0.16)
                {
                    climbAnimTimer = 0.0;
                    climbFrameToggle = !climbFrameToggle;
                    petImage.Source = climbFrameToggle ? imgClimb1 : imgClimb2;

                    // Footstep sparkle particle
                    if (random.Next(10) < 4)
                    {
                        SpawnParticle("✨", Color.FromRgb(255, 230, 100), this.Width * (climbOnLeft ? 0.65 : 0.35), this.Height * 0.85);
                    }
                }

                targetRot = (climbFrameToggle ? 2.5 : -2.5);
                springScaleY = 1.0 + Math.Sin(simTime * 12.0) * 0.04;

                // Titlebar height check: when reaching hand reaches window top OR hits screen top
                var workArea = SystemParameters.WorkArea;
                double upperHandScreenY = this.Top + 135.0;
                bool reachedTitlebar = (upperHandScreenY <= targetWindowRect.Top + 20.0) 
                                    || (this.Top <= workArea.Top + 10.0);

                if (reachedTitlebar)
                {
                    climbState = ClimbState.PerchedOnTitlebar; // Successfully climbed onto window!
                    perchedTimer = 30.0;
                    currentStandingHwnd = targetWindowHwnd;
                    lastStandingHwnd = targetWindowHwnd;

                    // Step onto the titlebar horizontally
                    if (climbOnLeft)
                    {
                        this.Left = targetWindowRect.Left + 25.0;
                    }
                    else
                    {
                        this.Left = Math.Max(targetWindowRect.Left + 10.0, targetWindowRect.Right - this.Width - 25.0);
                    }

                    // Sit firmly with palms on the titlebar
                    double sitTop = targetWindowRect.Top - this.Height + 93.0;
                    double minAllowedTop = workArea.Top - 85.0;
                    this.Top = Math.Max(minAllowedTop, sitTop);

                    targetRot = 0.0;
                    facingDirection = 1.0;
                    scaleTransform.ScaleX = Math.Abs(springScaleX);
                    isSittingDangling = true; // Sits dangling legs!
                    currentState = PetState.Idle;
                    UpdateDisplay(); // Switches to clean sit.png

                    PlayNyaSound();
                    SpawnParticle("💖", Color.FromRgb(255, 105, 180), this.Width * 0.5, this.Height * 0.45);

                    string[] perchedLines = new string[] {
                        "Ура, залезла на твоё окошко! Сверху такой классный вид! ✨",
                        "Сижу прямо на твоём окошке, свесив ножки! (´꒳`)🌸",
                        "Тут так уютно и высоко! 💖"
                    };
                    ShowSpeech(perchedLines[random.Next(perchedLines.Length)]);
                    return;
                }
            }
            else if (climbState == ClimbState.PerchedOnTitlebar)
            {
                perchedTimer -= 0.016;

                // Follow titlebar if user drags or resizes the window
                if (GetVisibleWindowRect(targetWindowHwnd, out rect) && rect.Left > -5000 && !IsIconic(targetWindowHwnd))
                {
                    targetWindowRect = rect;
                    double sitTop = targetWindowRect.Top - this.Height + 93.0;
                    this.Top += (sitTop - this.Top) * 0.35;

                    if (climbOnLeft)
                    {
                        this.Left = targetWindowRect.Left + 25.0;
                    }
                    else
                    {
                        this.Left = Math.Max(targetWindowRect.Left + 10.0, targetWindowRect.Right - this.Width - 25.0);
                    }
                }
                else
                {
                    // Window minimized or closed: jump down gracefully!
                    climbState = ClimbState.JumpingDown;
                    velocityY = -4.5;
                    jumpVelocityX = climbOnLeft ? -2.8 : 2.8;
                    ShowSpeech("Окошко закрылось! Спрыгиваю~ 🪂✨");
                    PlayNyaSound();
                    return;
                }

                // Dangle feet physics
                targetRot = Math.Sin(simTime * 3.2) * 3.5;
                targetHoverY = Math.Sin(simTime * 1.8) * 1.5;

                if (perchedTimer <= 0)
                {
                    // Launch gravity jump down!
                    climbState = ClimbState.JumpingDown;
                    velocityY = -5.0;
                    jumpVelocityX = climbOnLeft ? -2.8 : 2.8;
                    ShowSpeech("Спрыгиваю вниз! 🪂✨");
                    PlaySound(soundSqueak != null ? soundSqueak : soundNya);
                }
            }
            else if (climbState == ClimbState.JumpingDown)
            {
                velocityY += 1.35;
                this.Top += velocityY;
                this.Left += jumpVelocityX;
                targetRot = jumpVelocityX * 4.2;

                var workArea = SystemParameters.WorkArea;
                double effectiveGroundY = workArea.Bottom - this.Height + (isSittingDangling ? legDangleOffset : 14.0);

                if (this.Top >= effectiveGroundY)
                {
                    this.Top = effectiveGroundY;
                    velocityY = 0;
                    jumpVelocityX = 0;
                    targetRot = 0;

                    // Juicy squash impact
                    springScaleY = 0.68;
                    springScaleX = 1.32;
                    climbState = ClimbState.None;

                    TriggerStateWithAnimation(PetState.Happy);
                    happyDuration = 1.6;

                    PlaySound(soundSqueak != null ? soundSqueak : soundNya);

                    SpawnParticle("⭐", Color.FromRgb(255, 215, 0), this.Width * 0.5, this.Height * 0.85);
                    SpawnParticle("✨", Color.FromRgb(255, 200, 100), this.Width * 0.4, this.Height * 0.85);
                    SpawnParticle("💨", Color.FromRgb(230, 230, 240), this.Width * 0.6, this.Height * 0.85);
                }
            }
        }

        private void BlinkTimer_Tick(object sender, EventArgs e)
        {
            if (currentState == PetState.Idle && !isMouseDown && !isDragging && climbState == ClimbState.None && !isSittingDangling && !isWalking && activeTreat == null)
            {
                PerformNaturalBlink();
            }

            blinkTimer.Interval = TimeSpan.FromSeconds(random.Next(25, 45) / 10.0);
        }

        private void PerformNaturalBlink()
        {
            if (climbState != ClimbState.None || isDragging || activeTreat != null || isSittingDangling || isWalking) return;

            currentState = PetState.Blink;
            UpdateDisplay();

            // Closed for 140ms, then open
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(140) };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                if (currentState == PetState.Blink && climbState == ClimbState.None)
                {
                    currentState = PetState.Idle;
                    UpdateDisplay();

                    // 25% chance of a cute double blink
                    if (random.Next(10) < 3)
                    {
                        var dTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(160) };
                        dTimer.Tick += (sd, ed) =>
                        {
                            dTimer.Stop();
                            if (currentState == PetState.Idle && climbState == ClimbState.None && !isDragging) PerformNaturalBlink();
                        };
                        dTimer.Start();
                    }
                }
            };
            timer.Start();
        }

        private void UpdateParticles()
        {
            for (int i = particles.Count - 1; i >= 0; i--)
            {
                var p = particles[i];
                p.Life -= 0.016;
                if (p.Life <= 0)
                {
                    particleCanvas.Children.Remove(p.Element);
                    particles.RemoveAt(i);
                    continue;
                }

                p.X += p.Vx;
                p.Y += p.Vy;
                p.Vy += 0.025;
                p.Rotation += p.Vrot;

                Canvas.SetLeft(p.Element, p.X);
                Canvas.SetTop(p.Element, p.Y);

                double alpha = p.Life / p.MaxLife;
                p.Element.Opacity = alpha;

                var st = p.Element.RenderTransform as TransformGroup;
                if (st != null)
                {
                    var scale = (st.Children[0] as ScaleTransform);
                    var rot = (st.Children[1] as RotateTransform);
                    if (scale != null) scale.ScaleX = scale.ScaleY = p.BaseScale * (0.6 + 0.4 * alpha);
                    if (rot != null) rot.Angle = p.Rotation;
                }
            }
        }

        private void SpawnParticle(string symbol, Color color, double startX, double startY)
        {
            var tb = new TextBlock
            {
                Text = symbol,
                Foreground = new SolidColorBrush(color),
                FontSize = random.Next(18, 28),
                FontWeight = FontWeights.Bold,
                RenderTransformOrigin = new Point(0.5, 0.5)
            };

            var tg = new TransformGroup();
            tg.Children.Add(new ScaleTransform(1.0, 1.0));
            tg.Children.Add(new RotateTransform(0.0));
            tb.RenderTransform = tg;

            Canvas.SetLeft(tb, startX);
            Canvas.SetTop(tb, startY);
            particleCanvas.Children.Add(tb);

            particles.Add(new PetParticle
            {
                Element = tb,
                X = startX,
                Y = startY,
                Vx = (random.NextDouble() - 0.5) * 3.0,
                Vy = -(random.NextDouble() * 3.2 + 2.0),
                Rotation = random.Next(-30, 30),
                Vrot = (random.NextDouble() - 0.5) * 6.0,
                Life = 1.0,
                MaxLife = 1.0,
                BaseScale = 1.0
            });
        }

        private void WindowWatchTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                IntPtr hWnd = GetForegroundWindow();
                if (hWnd == IntPtr.Zero) return;

                var sb = new StringBuilder(256);
                if (GetWindowText(hWnd, sb, 256) > 0)
                {
                    string title = sb.ToString().Trim();
                    if (!string.IsNullOrEmpty(title) && title != "Chibi Desktop Pet" && title != "Поговорить с Чиби" && title != lastActiveTitle)
                    {
                        lastActiveTitle = title;
                        if (enableAppWatch) CheckAppReactions(title);
                    }
                }
            }
            catch { }
        }

        private void CheckAppReactions(string title)
        {
            if (climbState != ClimbState.None || isDragging || activeTreat != null) return;
            if (simTime - lastCommentTime < 16.0) return;

            string lower = title.ToLowerInvariant();

            int hour = DateTime.Now.Hour;
            if ((hour >= 23 || hour < 6) && random.Next(10) < 4)
            {
                lastCommentTime = simTime;
                TriggerStateWithAnimation(PetState.Sleep);
                ShowSpeech("Уже поздняя ночка... Ложись спать, не сиди до утра! 🌙💤");
                SpawnParticle("🌟", Color.FromRgb(255, 230, 100), this.Width * 0.5, this.Height * 0.60);
                return;
            }

            foreach (var profile in appProfiles)
            {
                foreach (var kw in profile.Keywords)
                {
                    if (lower.Contains(kw))
                    {
                        lastCommentTime = simTime;
                        TriggerStateWithAnimation(profile.State);

                        string phrase = profile.Phrases[random.Next(profile.Phrases.Length)];
                        ShowSpeech(phrase);
                        SpawnParticle(profile.Particle, profile.ParticleColor, this.Width * 0.6, this.Height * 0.55);
                        PlayNyaSound();
                        return;
                    }
                }
            }
        }

        private void TriggerStateWithAnimation(PetState newState)
        {
            currentState = newState;
            springScaleX = 1.10;
            springScaleY = 0.90;
            UpdateDisplay();
        }

        private void IdleActivityTimer_Tick(object sender, EventArgs e)
        {
            if (isDragging || currentState == PetState.Happy || climbState != ClimbState.None || activeTreat != null) return;

            if (inactivitySeconds >= 12.0)
            {
                if (inactivitySeconds >= 55.0 && random.Next(10) < 6)
                {
                    TriggerStateWithAnimation(PetState.Pout);
                    ShowSpeech("Хмф! Ты про меня совсем забыл... (｀ε´)");
                    return;
                }

                int roll = random.Next(14);
                if (roll < 3 && imgConsole != null)
                {
                    TriggerStateWithAnimation(PetState.Console);
                    ShowSpeech("Поиграю пока в консольку! 🕹✨");
                    SpawnParticle("⭐", Color.FromRgb(255, 215, 0), this.Width * 0.7, this.Height * 0.65);
                }
                else if (roll < 6 && imgDrawing != null)
                {
                    TriggerStateWithAnimation(PetState.Drawing);
                    ShowSpeech("Порисую на планшете~ 🎨");
                    SpawnParticle("✨", Color.FromRgb(200, 150, 255), this.Width * 0.7, this.Height * 0.65);
                }
                else if (roll < 8)
                {
                    // Shimeji Climbing action!
                    TryClimbNearestWindow();
                }
                else if (roll < 10 && imgSleep != null)
                {
                    TriggerStateWithAnimation(PetState.Sleep);
                    ShowSpeech("Сладких снов... Сплю со звёздочкой zZZ");
                    SpawnParticle("💤", Color.FromRgb(150, 200, 255), this.Width * 0.5, this.Height * 0.50);
                    if (purrSound != null && enableSound) purrSound.Play();
                }
                else
                {
                    StartWalking();
                }
            }
        }

        private void StartWalking()
        {
            if (isWalking || currentState == PetState.Dragging || climbState != ClimbState.None || activeTreat != null) return;

            var workArea = SystemParameters.WorkArea;
            double step = (random.NextDouble() - 0.5) * 180.0;
            walkTargetX = Math.Max(20, Math.Min(workArea.Right - this.Width - 20, this.Left + step));
            walkSpeed = (walkTargetX > this.Left) ? 1.5 : -1.5;
            isWalking = true;
        }

        public void TriggerPetReaction(Point mousePos)
        {
            inactivitySeconds = 0;
            isWalking = false;
            targetRot = 0.0;

            springScaleY = 0.86;
            springScaleX = 1.14;
            velScaleY += 0.05;

            currentState = PetState.Happy;
            happyDuration = 2.0;
            UpdateDisplay();

            string[] symbols = new string[] { "💖", "★", "✨", "💕", "🌸", "⭐" };
            Color[] colors = new Color[] {
                Color.FromRgb(255, 105, 180),
                Color.FromRgb(255, 215, 0),
                Color.FromRgb(180, 110, 235),
                Color.FromRgb(110, 215, 255)
            };

            string sym = symbols[random.Next(symbols.Length)];
            Color col = colors[random.Next(colors.Length)];
            SpawnParticle(sym, col, mousePos.X - 10 + (random.NextDouble() - 0.5) * 40, mousePos.Y - 20);

            PlayNyaSound();

            AddAffection(3);

            if (random.Next(10) < 4)
            {
                string[] lines;
                if (affectionLevel >= 70)
                {
                    lines = new string[] {
                        "Обожаю, когда ты меня гладишь! Ты моё чудо! 💖✨",
                        "Муррр~ С тобой так тепло и спокойно! 💕",
                        "Ты самый замечательный человек на свете! ⭐🌸",
                        "Ещё погладь, пожалуйста~ Не убирай ручку! (´꒳`)"
                    };
                }
                else
                {
                    lines = new string[] {
                        "Мур-мур~ 💖",
                        "Хе-хе, щекотно! ✨",
                        "Спасибо! ★",
                        "Ещё погладь~ (´꒳`)",
                        "Няяя~ 🌸",
                        "Ты самый лучший! ⭐",
                        "Тепло и уютно! 💕",
                        "Мяу~ Лапочка! 🌸"
                    };
                }
                ShowSpeech(lines[random.Next(lines.Length)]);
            }
        }

        private void PlayNyaSound()
        {
            if (!enableSound) return;
            try
            {
                if (petSounds.Count > 0)
                {
                    var sp = petSounds[random.Next(petSounds.Count)];
                    sp.Play();
                }
                else
                {
                    SystemSounds.Asterisk.Play();
                }
            }
            catch { }
        }

        private void PlaySound(SoundPlayer sp)
        {
            if (!enableSound || sp == null) return;
            try { sp.Play(); } catch { }
        }

        public void ShowSpeech(string text)
        {
            speechBubble.Text = text;

            // Pop animation with bouncy overshoot
            speechScale.ScaleX = 0.4;
            speechScale.ScaleY = 0.4;
            speechContainer.Opacity = 0.0;

            var scaleAnim = new DoubleAnimation(0.4, 1.0, TimeSpan.FromMilliseconds(260))
            {
                EasingFunction = new BackEase { Amplitude = 0.35, EasingMode = EasingMode.EaseOut }
            };
            var opacityAnim = new DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(180));

            speechScale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
            speechScale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);
            speechContainer.BeginAnimation(UIElement.OpacityProperty, opacityAnim);

            speechTimer.Stop();
            speechTimer.Start();
        }

        private void InitEvents()
        {
            speechContainer.MouseLeftButtonDown += (s, e) =>
            {
                if (pendingUpdate != null)
                {
                    OpenUpdateDialog(pendingUpdate);
                    e.Handled = true;
                }
            };

            this.MouseDown += (s, e) =>
            {
                inactivitySeconds = 0;
                if (e.ChangedButton == MouseButton.Left)
                {
                    // Check for double click to chat
                    if (e.ClickCount == 2)
                    {
                        OpenChatDialog();
                        return;
                    }

                    isMouseDown = true;
                    dragStartPoint = e.GetPosition(this);
                    lastScreenDragPoint = this.PointToScreen(dragStartPoint);
                    this.CaptureMouse();
                }
            };

            this.MouseMove += (s, e) =>
            {
                var curPos = e.GetPosition(this);

                // Headpat detection (upper hair/head region)
                double headTop = (this.Height - petHeight * currentScale);
                double headBottom = headTop + (petHeight * currentScale * 0.42);

                if (curPos.Y >= headTop && curPos.Y <= headBottom && !isDragging)
                {
                    double delta = Math.Abs(curPos.X - lastMousePos.X) + Math.Abs(curPos.Y - lastMousePos.Y);
                    if (delta > 3.0 && delta < 70.0)
                    {
                        petEnergy += delta * 0.5;
                        springScaleY += (random.NextDouble() - 0.5) * 0.03;

                        if (petEnergy > 20.0)
                        {
                            petEnergy = 5.0;
                            TriggerPetReaction(curPos);
                        }
                    }
                }

                // Dragging physics (picking up by the sweater collar)
                if (isMouseDown)
                {
                    double dx = curPos.X - dragStartPoint.X;
                    double dy = curPos.Y - dragStartPoint.Y;

                    if (!isDragging && (Math.Abs(dx) > 5 || Math.Abs(dy) > 5))
                    {
                        isDragging = true;
                        isWalking = false;
                        climbState = ClimbState.None;
                        currentStandingHwnd = IntPtr.Zero;
                        TriggerStateWithAnimation(PetState.Dragging);

                        string[] pickupLines = new string[] {
                            "Ой! Поймал за кофточку! (⁄ ⁄•⁄ω⁄•⁄ ⁄)✨",
                            "Куда мы летим? Держи меня крепче! 💕",
                            "Ааа, болтаю ножками в воздухе~ ⭐"
                        };
                        ShowSpeech(pickupLines[random.Next(pickupLines.Length)]);
                        PlayNyaSound();
                    }

                    if (isDragging)
                    {
                        var screenPos = this.PointToScreen(curPos);
                        double screenDx = screenPos.X - lastScreenDragPoint.X;

                        // Sway back and forth like a real dangling plush/charm
                        targetRot = Math.Max(-32.0, Math.Min(32.0, -screenDx * 2.8));

                        // Mouse holds the back of her sweater collar (centered X, collar level Y)
                        double collarOffsetX = this.Width * 0.5;
                        double collarOffsetY = (this.Height - petHeight * currentScale) + (petHeight * currentScale * 0.40);

                        this.Left = screenPos.X - collarOffsetX;
                        this.Top = screenPos.Y - collarOffsetY;
                        lastScreenDragPoint = screenPos;
                        velocityY = 0;
                    }
                }

                lastMousePos = curPos;
            };

            this.MouseUp += (s, e) =>
            {
                inactivitySeconds = 0;
                if (e.ChangedButton == MouseButton.Left)
                {
                    if (isMouseDown && !isDragging)
                    {
                        OnClickPoke(e.GetPosition(this));
                    }
                    else if (isDragging)
                    {
                        isDragging = false;
                        enableGravity = true;
                        velocityY = 1.0; // Initial downward drop momentum
                        targetRot = 0.0;
                    }

                    isMouseDown = false;
                    isDragging = false;
                    this.ReleaseMouseCapture();
                }
            };

            this.MouseLeave += (s, e) =>
            {
                if (!isDragging && climbState == ClimbState.None) targetRot = 0.0;
            };
        }

        private void OnClickPoke(Point pos)
        {
            inactivitySeconds = 0;
            isWalking = false;

            springScaleY = 0.78;
            springScaleX = 1.22;
            velScaleY += 0.08;

            currentState = PetState.Happy;
            happyDuration = 1.2;
            UpdateDisplay();

            SpawnParticle("⭐", Color.FromRgb(255, 215, 0), pos.X - 5, pos.Y - 20);

            if (enableSound && clickSound != null) clickSound.Play();
            else PlayNyaSound();

            string[] pokes = new string[] {
                "Привет! ✨",
                "Ой! Щекотно :3",
                "Я тут! Чем займёмся? ⭐",
                "Ня? (´꒳`)",
                "Слушаю тебя! 🌸"
            };
            ShowSpeech(pokes[random.Next(pokes.Length)]);
        }

        private void UpdateDisplay()
        {
            BitmapSource target = null;
            bool showEyes = false;

            switch (currentState)
            {
                case PetState.Blink:
                    target = imgBlink;
                    showEyes = false;
                    break;
                case PetState.Happy:
                    target = (isSittingDangling && imgSit != null) ? imgSit : imgHappy;
                    showEyes = false;
                    break;
                case PetState.Dragging:
                    target = imgDrag;
                    showEyes = false;
                    break;
                case PetState.Console:
                    target = imgConsole;
                    showEyes = false;
                    break;
                case PetState.Drawing:
                    target = imgDrawing;
                    showEyes = false;
                    break;
                case PetState.Sleep:
                    target = imgSleep;
                    showEyes = false;
                    break;
                case PetState.Pout:
                    target = imgPout;
                    showEyes = false;
                    break;
                case PetState.Climbing:
                    target = climbFrameToggle ? imgClimb1 : imgClimb2;
                    showEyes = false;
                    break;
                case PetState.Idle:
                default:
                    if (isSittingDangling && imgSit != null)
                    {
                        target = imgSit;
                        showEyes = false;
                    }
                    else
                    {
                        target = (enableEyeTracking && imgIdleSclera != null) ? imgIdleSclera : imgIdle;
                        showEyes = enableEyeTracking;
                    }
                    break;
            }

            petImage.Source = target ?? imgIdle;
            if (eyeCanvas != null)
            {
                bool canShowEyes = enableEyeTracking && showEyes && !isSittingDangling && !isWalking && !isDragging && currentState == PetState.Idle && climbState == ClimbState.None;
                eyeCanvas.Visibility = canShowEyes ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public void SpawnTreat(string treatType, string name, string yumPhrase)
        {
            if (activeTreat != null)
            {
                try { activeTreat.Close(); } catch { }
                activeTreat = null;
            }

            BitmapSource treatBmp = null;
            if (treatType == "cake") treatBmp = imgTreatCake;
            else if (treatType == "boba") treatBmp = imgTreatBoba;
            else if (treatType == "strawberry") treatBmp = imgTreatStrawberry;
            else if (treatType == "cookie") treatBmp = imgTreatCookie;

            var workArea = SystemParameters.WorkArea;

            // Spawn in desktop world space near the pet (offset left or right)
            double petCenterX = this.Left + this.Width * 0.5;
            double dropDist = 120.0;
            double dropX;
            if (petCenterX < workArea.Left + 160)
            {
                dropX = petCenterX + dropDist;
            }
            else if (petCenterX > workArea.Right - 160)
            {
                dropX = petCenterX - dropDist;
            }
            else
            {
                dropX = (random.Next(2) == 0) ? (petCenterX - dropDist) : (petCenterX + dropDist);
            }
            dropX = Math.Max(workArea.Left + 20, Math.Min(workArea.Right - 76, dropX));
            double dropY = Math.Max(workArea.Top + 20, this.Top + 40);
            double groundY = workArea.Bottom;

            activeTreat = new TreatWindow(treatType, name, yumPhrase, treatBmp, dropX, dropY, groundY);
            activeTreat.Owner = this;
            activeTreat.OnTreatDragged = (t) =>
            {
                double pCenterX = this.Left + this.Width * 0.5;
                double pCenterY = this.Top + this.Height * 0.65;
                double tCenterX = t.Left + t.Width * 0.5;
                double tCenterY = t.Top + t.Height * 0.5;

                double dx = tCenterX - pCenterX;
                double dy = tCenterY - pCenterY;
                double dist = Math.Sqrt(dx * dx + dy * dy);

                facingDirection = (dx < 0 ? -1.0 : 1.0);
                scaleTransform.ScaleX = facingDirection * Math.Abs(springScaleX);

                if (dist < 42.0)
                {
                    EatActiveTreat();
                }
            };

            activeTreat.OnTreatReleased = (t) =>
            {
                IntPtr gWin;
                t.GroundY = GetGroundSurfaceY(out gWin) - 60;
            };

            activeTreat.Show();

            ShowSpeech("Ого! Это для меня вкусняшка? ✨ Можно взять её мышкой и покормить меня! 🍓");
            PlayNyaSound();
        }

        public void TryClimbNearestWindow()
        {
            if (climbState != ClimbState.None || isDragging) return;

            RECT rect;
            IntPtr target = FindForemostWindow(out rect);
            if (target != IntPtr.Zero)
            {
                targetWindowHwnd = target;
                targetWindowRect = rect;

                double petCenter = this.Left + this.Width * 0.5;
                double distToLeft = Math.Abs(petCenter - rect.Left);
                double distToRight = Math.Abs(petCenter - rect.Right);
                climbOnLeft = distToLeft <= distToRight;

                double leftEdgeX = rect.Left - 205.0;
                double rightEdgeX = rect.Right - 75.0;
                double chosenEdgeX = climbOnLeft ? leftEdgeX : rightEdgeX;

                // If already near the window edge, immediately latch and climb!
                if (Math.Abs(this.Left - chosenEdgeX) < 70.0)
                {
                    this.Left = chosenEdgeX;
                    climbState = ClimbState.ClimbingWall;
                    currentState = PetState.Climbing;
                    facingDirection = climbOnLeft ? 1.0 : -1.0;
                    scaleTransform.ScaleX = facingDirection * Math.Abs(springScaleX);
                    UpdateDisplay();
                    ShowSpeech("Зацепилась за край окошка! Лезем наверх! 🧗✨");
                    PlayNyaSound();
                    return;
                }

                climbState = ClimbState.WalkingToWindow;
                isWalking = false;
                ShowSpeech("Бегу к твоему окошку! 🐾✨");
                PlayNyaSound();
                return;
            }

            ShowSpeech("Не вижу подходящего окошка рядом~ 🌸");
        }

        public void OpenChatDialog()
        {
            var chatWin = new ChatDialogWindow(this);
            chatWin.Show();
        }

        public void HandleUserChat(string message)
        {
            inactivitySeconds = 0;
            string lower = message.ToLowerInvariant();

            TriggerStateWithAnimation(PetState.Happy);
            happyDuration = 2.0;
            PlayNyaSound();
            AddAffection(4);

            SpawnParticle("💬", Color.FromRgb(255, 120, 200), this.Width * 0.5, this.Height * 0.55);

            if (lower.Contains("привет") || lower.Contains("хай") || lower.Contains("ку") || lower.Contains("здравствуй") || lower.Contains("доброе"))
            {
                ShowSpeech("Привет-привет! Рада тебя видеть! ✨ Как твои дела? (´꒳`)");
            }
            else if (lower.Contains("как дела") || lower.Contains("как ты") || lower.Contains("что делаешь") || lower.Contains("настроение"))
            {
                ShowSpeech("У меня всё просто супер! Сижу на панели задач и радуюсь каждому твоему клику! 🌸");
            }
            else if (lower.Contains("красивая") || lower.Contains("милая") || lower.Contains("няшка") || lower.Contains("умница") || lower.Contains("лапочка") || lower.Contains("хорошая"))
            {
                AddAffection(6);
                ShowSpeech("Ой, смущаешь меня... (//▽//) Спасибо огромное! Ты тоже самый лучший! 💖");
            }
            else if (lower.Contains("люблю") || lower.Contains("обожаю") || lower.Contains("сердечко"))
            {
                AddAffection(10);
                ShowSpeech("И я тебя очень-очень люблю! Ты моё самое любимое солнышко! 💕⭐✨");
                SpawnParticle("💖", Color.FromRgb(255, 105, 180), this.Width * 0.5, this.Height * 0.55);
            }
            else if (lower.Contains("устал") || lower.Contains("баг") || lower.Contains("ошибка") || lower.Contains("работа") || lower.Contains("дедлайн") || lower.Contains("сложно"))
            {
                ShowSpeech("Бедненький... Сделай перерыв, выпей чаёк! Я рядом и держу за тебя кулачки! 🍵💖");
            }
            else if (lower.Contains("игра") || lower.Contains("геншин") || lower.Contains("дота") || lower.Contains("кс") || lower.Contains("майнкрафт") || lower.Contains("стим"))
            {
                ShowSpeech("Игры — это святое! Давай тащи катцы, а я буду твоим верным талисманом на победу! 🎮⭐");
            }
            else if (lower.Contains("кушать") || lower.Contains("еда") || lower.Contains("пицца") || lower.Contains("торт") || lower.Contains("чай") || lower.Contains("кофе"))
            {
                ShowSpeech("Ооо, вкусняшки! И мне оставь кусочек тортика или бабл-ти! 🍰🧋");
            }
            else if (lower.Contains("спокойной ночи") || lower.Contains("спать") || lower.Contains("баиньки"))
            {
                TriggerStateWithAnimation(PetState.Sleep);
                ShowSpeech("Сладких и волшебных снов! Обнимаю тебя своей звёздочкой zZZ 🌙💤");
            }
            else
            {
                string[] generic = new string[] {
                    "Хе-хе, здорово! Я слушаю тебя очень внимательно! ✨",
                    "Как интересно! Расскажи ещё что-нибудь! 🌸",
                    "Ты такой классный собеседник! С тобой тепло~ 💕",
                    "Мур-мур~ Полностью с тобой согласна! (´꒳`)"
                };
                ShowSpeech(generic[random.Next(generic.Length)]);
            }
        }

        public void AddAffection(int delta)
        {
            affectionLevel = Math.Max(0, Math.Min(100, affectionLevel + delta));
        }

        private string GetAffectionStatus()
        {
            if (affectionLevel < 25) return "Стесняшка 🐾";
            if (affectionLevel < 50) return "Подружка 🌸";
            if (affectionLevel < 75) return "Любимица 💖";
            if (affectionLevel < 100) return "Влюблённая чиби 💕";
            return "Родственная душа ⭐✨";
        }

        private void InitUpdateSystem()
        {
            petConfig = PetConfig.Load();
            if (petConfig.AutoCheckUpdates)
            {
                startupUpdateTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
                startupUpdateTimer.Tick += (s, e) =>
                {
                    startupUpdateTimer.Stop();
                    CheckForUpdates(true);
                };
                startupUpdateTimer.Start();
            }
        }

        public void CheckForUpdates(bool silent)
        {
            if (petConfig == null) petConfig = PetConfig.Load();

            if (!silent)
            {
                ShowSpeech("Проверяю обновления на GitHub... 🌐");
            }

            UpdateChecker.CheckForUpdatesAsync(petConfig.Repository, delegate(UpdateInfo info)
            {
                if (!string.IsNullOrEmpty(info.ErrorMessage))
                {
                    if (!silent)
                    {
                        ShowSpeech("Ой, не удалось связаться с GitHub 📡");
                        MessageBox.Show(info.ErrorMessage, "Проверка обновлений", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    return;
                }

                if (info.IsNewer)
                {
                    pendingUpdate = info;
                    ShowSpeech(string.Format("Вышло обновление {0}! ✨ Нажмите сюда для установки~", info.TagName));
                    if (!silent)
                    {
                        OpenUpdateDialog(info);
                    }
                }
                else
                {
                    pendingUpdate = null;
                    if (!silent)
                    {
                        ShowSpeech(string.Format("У вас самая свежая версия (v{0})! 💖 Всё супер!", UpdateChecker.AppVersion));
                    }
                }
            });
        }

        public void OpenUpdateDialog(UpdateInfo info)
        {
            var dlg = new UpdateDialogWindow(info);
            dlg.Show();
        }

        public void OpenRepoConfigDialog()
        {
            if (petConfig == null) petConfig = PetConfig.Load();
            var dlg = new RepoConfigDialog(this, petConfig);
            dlg.Show();
        }

        public void OpenAboutDialog()
        {
            if (petConfig == null) petConfig = PetConfig.Load();
            var dlg = new AboutDialogWindow(petConfig.Repository);
            dlg.Show();
        }

        private void InitContextMenu()
        {
            var menu = new ContextMenu();

            var titleItem = new MenuItem { Header = string.Format("🌸 Чиби-тян  [{0}% — {1}]", affectionLevel, GetAffectionStatus()), IsEnabled = false };
            menu.Items.Add(titleItem);
            menu.Items.Add(new Separator());

            // Chat with pet
            var chatItem = new MenuItem { Header = "💬 Поговорить с Чиби-тян (Двойной клик)" };
            chatItem.Click += (s, e) => OpenChatDialog();
            menu.Items.Add(chatItem);

            // Treat / Feeding Submenu
            var treatMenu = new MenuItem { Header = "🍰 Угостить вкусняшкой" };

            var cakeItem = new MenuItem { Header = "🍰 Клубничный тортик" };
            cakeItem.Click += (s, e) => SpawnTreat("cake", "Тортик", "Ням-ням! Самый вкусный клубничный тортик на свете! 🍰💖");
            treatMenu.Items.Add(cakeItem);

            var bobaItem = new MenuItem { Header = "🧋 Таро Бабл-ти" };
            bobaItem.Click += (s, e) => SpawnTreat("boba", "Бабл-ти", "Хлюп-хлюп~ Обожаю таро бабл-ти со звёздочкой! 🧋✨");
            treatMenu.Items.Add(bobaItem);

            var berryItem = new MenuItem { Header = "🍓 Сладкая клубничка" };
            berryItem.Click += (s, e) => SpawnTreat("strawberry", "Клубничка", "Сладкая и сочная клубничка! Спасибочки! 🍓🌸");
            treatMenu.Items.Add(berryItem);

            var cookieItem = new MenuItem { Header = "⭐ Звёздное печенье" };
            cookieItem.Click += (s, e) => SpawnTreat("cookie", "Печенька", "Хрум-хрум! Самая сладкая печенька-звёздочка! ⭐💖");
            treatMenu.Items.Add(cookieItem);

            menu.Items.Add(treatMenu);

            // Eye tracking toggle
            var eyeItem = new MenuItem { Header = "👁 Слежение глазками за курсором", IsCheckable = true, IsChecked = enableEyeTracking };
            eyeItem.Click += (s, e) =>
            {
                enableEyeTracking = eyeItem.IsChecked;
                UpdateDisplay();
            };
            menu.Items.Add(eyeItem);

            // Shimeji Climbing action
            var climbItem = new MenuItem { Header = "🧗 Залезть на активное окно" };
            climbItem.Click += (s, e) => TryClimbNearestWindow();
            menu.Items.Add(climbItem);

            // Jump down to taskbar
            var jumpItem = new MenuItem { Header = "🪂 Спрыгнуть на панель задач" };
            jumpItem.Click += (s, e) =>
            {
                currentStandingHwnd = IntPtr.Zero;
                climbState = ClimbState.JumpingDown;
                velocityY = -4.5;
                jumpVelocityX = (random.Next(2) == 0 ? -2.8 : 2.8);
                ShowSpeech("Прыыыжок вниз! 🪂✨");
                PlayNyaSound();
            };
            menu.Items.Add(jumpItem);

            // Posture toggle (sitting dangling legs vs standing)
            var postureItem = new MenuItem { Header = "🪑 Сидеть, свесив ножки", IsCheckable = true, IsChecked = isSittingDangling };
            postureItem.Click += (s, e) =>
            {
                isSittingDangling = postureItem.IsChecked;
                UpdateDisplay();
                double effY = GetGroundSurfaceY(out currentStandingHwnd);
                this.Top = effY;
                ShowSpeech(isSittingDangling ? "Уселась и свесила ножки! 🌸" : "Встала на ножки! ✨");
                PlayNyaSound();
            };
            menu.Items.Add(postureItem);

            menu.Items.Add(new Separator());

            // Emotions / Activities manual trigger
            var emotionMenu = new MenuItem { Header = "🎭 Эмоции и Занятия" };

            var idleItem = new MenuItem { Header = "⭐ Обычная стойка (Idle)" };
            idleItem.Click += (s, e) => { TriggerStateWithAnimation(PetState.Idle); ShowSpeech("Я тут! ✨"); };
            emotionMenu.Items.Add(idleItem);

            var happyItem = new MenuItem { Header = "💖 Счастливая (Поглаживание)" };
            happyItem.Click += (s, e) => { TriggerPetReaction(new Point(baseWidth * 0.5, baseHeight * 0.6)); };
            emotionMenu.Items.Add(happyItem);

            var consoleItem = new MenuItem { Header = "🕹 Играть в консоль" };
            consoleItem.Click += (s, e) => { TriggerStateWithAnimation(PetState.Console); ShowSpeech("Играем! 🎮"); };
            emotionMenu.Items.Add(consoleItem);

            var drawingItem = new MenuItem { Header = "🎨 Рисовать на планшете" };
            drawingItem.Click += (s, e) => { TriggerStateWithAnimation(PetState.Drawing); ShowSpeech("Рисуем шедевр! 🎨"); };
            emotionMenu.Items.Add(drawingItem);

            var sleepItem = new MenuItem { Header = "💤 Спать со звёздочкой" };
            sleepItem.Click += (s, e) => { TriggerStateWithAnimation(PetState.Sleep); ShowSpeech("Сладких снов~ zZZ"); };
            emotionMenu.Items.Add(sleepItem);

            var poutItem = new MenuItem { Header = "😤 Надуть щёчки (Pout)" };
            poutItem.Click += (s, e) => { TriggerStateWithAnimation(PetState.Pout); ShowSpeech("Хмф! (｀ε´)"); };
            emotionMenu.Items.Add(poutItem);

            menu.Items.Add(emotionMenu);
            menu.Items.Add(new Separator());

            // Scale Submenu
            var scaleMenu = new MenuItem { Header = "🔍 Размер (Масштаб)" };
            double[] scales = new double[] { 0.75, 1.0, 1.25, 1.5, 2.0 };
            foreach (var sc in scales)
            {
                double sVal = sc;
                var item = new MenuItem { Header = string.Format("{0}%", (int)(sVal * 100)) };
                item.Click += (s, e) => SetScale(sVal);
                scaleMenu.Items.Add(item);
            }
            menu.Items.Add(scaleMenu);

            // Gravity toggle
            var gravityItem = new MenuItem { Header = "🌍 Гравитация (падать на панель задач)", IsCheckable = true, IsChecked = enableGravity };
            gravityItem.Click += (s, e) => { enableGravity = gravityItem.IsChecked; };
            menu.Items.Add(gravityItem);

            // Sound toggle
            var soundItem = new MenuItem { Header = "🔊 Звуковые няфканья", IsCheckable = true, IsChecked = enableSound };
            soundItem.Click += (s, e) => { enableSound = soundItem.IsChecked; };
            menu.Items.Add(soundItem);

            // App watch toggle
            var appWatchItem = new MenuItem { Header = "👀 Реагировать на запущенные приложения", IsCheckable = true, IsChecked = enableAppWatch };
            appWatchItem.Click += (s, e) => { enableAppWatch = appWatchItem.IsChecked; };
            menu.Items.Add(appWatchItem);

            menu.Items.Add(new Separator());

            // Reload resources
            var reloadItem = new MenuItem { Header = "🔄 Перезагрузить текстуры и звуки" };
            reloadItem.Click += (s, e) =>
            {
                ReloadResources();
                ShowSpeech("Текстуры обновлены! ✨");
            };
            menu.Items.Add(reloadItem);

            menu.Items.Add(new Separator());

            // Updates
            var updateItem = new MenuItem { Header = "🔄 Проверить обновления" };
            updateItem.Click += (s, e) => CheckForUpdates(false);
            menu.Items.Add(updateItem);

            var repoItem = new MenuItem { Header = "⚙ Репозиторий GitHub..." };
            repoItem.Click += (s, e) => OpenRepoConfigDialog();
            menu.Items.Add(repoItem);

            var aboutItem = new MenuItem { Header = string.Format("ℹ О программе (v{0})", UpdateChecker.AppVersion) };
            aboutItem.Click += (s, e) => OpenAboutDialog();
            menu.Items.Add(aboutItem);

            menu.Items.Add(new Separator());

            // Exit
            var exitItem = new MenuItem { Header = "❌ Закрыть" };
            exitItem.Click += (s, e) =>
            {
                if (activeTreat != null) { try { activeTreat.Close(); } catch { } }
                this.Close();
            };
            menu.Items.Add(exitItem);

            this.ContextMenu = menu;
        }

        private void SetScale(double scale)
        {
            currentScale = scale;
            this.Width = baseWidth * currentScale;
            this.Height = baseHeight * currentScale;

            spriteCanvas.Width = petWidth * currentScale;
            spriteCanvas.Height = petHeight * currentScale;
            petImage.Width = spriteCanvas.Width;
            petImage.Height = spriteCanvas.Height;

            scaleTransform.CenterX = spriteCanvas.Width * 0.5;
            scaleTransform.CenterY = spriteCanvas.Height * 0.95;
            rotateTransform.CenterX = spriteCanvas.Width * 0.5;
            rotateTransform.CenterY = spriteCanvas.Height * 0.80;

            // Recalculate eye positions
            pupilW = (70.0 / 1024.0) * spriteCanvas.Width;
            pupilH = (72.0 / 1120.0) * spriteCanvas.Height;

            basePupilLeftX = (436.0 / 1024.0) * spriteCanvas.Width - pupilW * 0.5;
            basePupilLeftY = (426.0 / 1120.0) * spriteCanvas.Height - pupilH * 0.5;

            basePupilRightX = (585.0 / 1024.0) * spriteCanvas.Width - pupilW * 0.5;
            basePupilRightY = (425.0 / 1120.0) * spriteCanvas.Height - pupilH * 0.5;

            pupilLeftImg.Width = pupilW;
            pupilLeftImg.Height = pupilH;
            pupilRightImg.Width = pupilW;
            pupilRightImg.Height = pupilH;
            eyelashesOverlay.Width = spriteCanvas.Width;
            eyelashesOverlay.Height = spriteCanvas.Height;
        }
    }

    public static class AppEntry
    {
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [STAThread]
        public static void Main()
        {
            string logPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug.log");
            try
            {
                File.AppendAllText(logPath, "--- START " + DateTime.Now.ToString() + " ---\n");

                AppDomain.CurrentDomain.UnhandledException += (s, args) =>
                {
                    try
                    {
                        File.AppendAllText(logPath, "AppDomain Unhandled: " + args.ExceptionObject.ToString() + "\n");
                    }
                    catch { }
                };

                IntPtr consoleWnd = GetConsoleWindow();
                if (consoleWnd != IntPtr.Zero)
                {
                    ShowWindow(consoleWnd, 0); // SW_HIDE
                }

                var app = new Application();
                app.DispatcherUnhandledException += (s, args) =>
                {
                    try
                    {
                        File.AppendAllText(logPath, "Dispatcher Unhandled: " + args.Exception.ToString() + "\n");
                    }
                    catch { }
                    args.Handled = true;
                };

                app.Exit += (s, args) =>
                {
                    File.AppendAllText(logPath, "App Exit with code: " + args.ApplicationExitCode + "\n");
                };

                File.AppendAllText(logPath, "Creating PetWindow...\n");
                var win = new PetWindow();
                File.AppendAllText(logPath, "PetWindow created. Showing...\n");
                win.Show();
                File.AppendAllText(logPath, "PetWindow shown. Running app...\n");
                app.Run(win);
                File.AppendAllText(logPath, "App.Run finished.\n");
            }
            catch (Exception ex)
            {
                File.AppendAllText(logPath, "Main Exception: " + ex.ToString() + "\n");
            }
        }
    }
}
