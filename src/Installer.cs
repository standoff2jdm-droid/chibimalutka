using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace ChibiInstaller
{
    public class InstallerWindow : Window
    {
        private TextBox pathBox;
        private CheckBox desktopShortcutCheck;
        private CheckBox startMenuCheck;
        private CheckBox launchAfterCheck;
        private ProgressBar progressBar;
        private TextBlock statusText;
        private Button installBtn;
        private Button browseBtn;
        private StackPanel optionsPanel;
        private StackPanel progressPanel;
        private StackPanel finishedPanel;

        public InstallerWindow()
        {
            this.Title = "🌸 Установка Chibi Desktop Pet";
            this.WindowStyle = WindowStyle.None;
            this.AllowsTransparency = true;
            this.Background = Brushes.Transparent;
            this.Width = 540;
            this.Height = 440;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            var card = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(254, 252, 255)),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9B59B6")),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(18),
                Padding = new Thickness(24),
                Effect = new DropShadowEffect
                {
                    Color = Color.FromRgb(120, 60, 160),
                    BlurRadius = 26,
                    ShadowDepth = 6,
                    Opacity = 0.35,
                    Direction = 270
                }
            };

            var rootGrid = new Grid();
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Content
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Footer

            // 1. Header
            var headerGrid = new Grid { Margin = new Thickness(0, 0, 0, 16) };
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            headerGrid.MouseLeftButtonDown += (s, e) => { try { this.DragMove(); } catch { } };

            var headerStack = new StackPanel();
            var titleBlock = new TextBlock
            {
                Text = "🌸 Установка Chibi Desktop Pet",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4A235A"))
            };
            var subTitleBlock = new TextBlock
            {
                Text = "Версия 0.8.0-beta • Твой милый интерактивный 2D маскот",
                FontSize = 12,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8E44AD")),
                Margin = new Thickness(0, 2, 0, 0)
            };
            headerStack.Children.Add(titleBlock);
            headerStack.Children.Add(subTitleBlock);
            Grid.SetColumn(headerStack, 0);
            headerGrid.Children.Add(headerStack);

            var closeBtn = new Button
            {
                Content = "✕",
                Width = 28,
                Height = 28,
                FontSize = 14,
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
            rootGrid.Children.Add(headerGrid);

            // 2. Main Content
            var contentGrid = new Grid();

            // Panel A: Options View
            optionsPanel = new StackPanel { VerticalAlignment = VerticalAlignment.Top };

            var descBox = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(245, 238, 252)),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(14, 10, 14, 10),
                Margin = new Thickness(0, 0, 0, 16)
            };
            var descText = new TextBlock
            {
                Text = "Мастер установит Chibi Desktop Pet на ваш компьютер. Персонаж умеет ходить по экрану, лазать по окнам, кушать сладости и общаться!",
                FontSize = 12,
                LineHeight = 17,
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4A235A"))
            };
            descBox.Child = descText;
            optionsPanel.Children.Add(descBox);

            var pathLabel = new TextBlock
            {
                Text = "Папка установки:",
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2C3E50")),
                Margin = new Thickness(0, 0, 0, 6)
            };
            optionsPanel.Children.Add(pathLabel);

            var pathRow = new Grid { Margin = new Thickness(0, 0, 0, 16) };
            pathRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            pathRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            string defaultDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "ChibiDesktopPet");
            pathBox = new TextBox
            {
                Text = defaultDir,
                Height = 32,
                FontSize = 12,
                VerticalContentAlignment = VerticalAlignment.Center,
                Padding = new Thickness(8, 0, 8, 0),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D2B4DE")),
                BorderThickness = new Thickness(1.5),
                Background = new SolidColorBrush(Color.FromRgb(250, 245, 255))
            };
            Grid.SetColumn(pathBox, 0);
            pathRow.Children.Add(pathBox);

            browseBtn = new Button
            {
                Content = "Обзор...",
                Height = 32,
                Padding = new Thickness(14, 0, 14, 0),
                Margin = new Thickness(8, 0, 0, 0),
                Background = new SolidColorBrush(Color.FromRgb(240, 235, 245)),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4A235A")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D2B4DE")),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand
            };
            browseBtn.Click += (s, e) => BrowseFolder();
            Grid.SetColumn(browseBtn, 1);
            pathRow.Children.Add(browseBtn);
            optionsPanel.Children.Add(pathRow);

            desktopShortcutCheck = new CheckBox
            {
                Content = "Создать ярлык на Рабочем столе",
                IsChecked = true,
                FontSize = 12,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2C3E50")),
                Margin = new Thickness(0, 0, 0, 8),
                Cursor = Cursors.Hand
            };
            optionsPanel.Children.Add(desktopShortcutCheck);

            startMenuCheck = new CheckBox
            {
                Content = "Добавить в меню «Пуск»",
                IsChecked = true,
                FontSize = 12,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2C3E50")),
                Margin = new Thickness(0, 0, 0, 8),
                Cursor = Cursors.Hand
            };
            optionsPanel.Children.Add(startMenuCheck);

            launchAfterCheck = new CheckBox
            {
                Content = "Запустить питомца после завершения",
                IsChecked = true,
                FontSize = 12,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2C3E50")),
                Margin = new Thickness(0, 0, 0, 8),
                Cursor = Cursors.Hand
            };
            optionsPanel.Children.Add(launchAfterCheck);

            contentGrid.Children.Add(optionsPanel);

            // Panel B: Progress View
            progressPanel = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                Visibility = Visibility.Collapsed
            };

            statusText = new TextBlock
            {
                Text = "Подготовка к установке...",
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6C3483")),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 14)
            };
            progressPanel.Children.Add(statusText);

            progressBar = new ProgressBar
            {
                Height = 12,
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9B59B6")),
                Background = new SolidColorBrush(Color.FromRgb(235, 225, 245)),
                Margin = new Thickness(10, 0, 10, 0)
            };
            progressPanel.Children.Add(progressBar);
            contentGrid.Children.Add(progressPanel);

            // Panel C: Finished View
            finishedPanel = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                Visibility = Visibility.Collapsed
            };

            var successIcon = new TextBlock
            {
                Text = "🎉",
                FontSize = 42,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8)
            };
            var successTitle = new TextBlock
            {
                Text = "Установка успешно завершена!",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E8449")),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 6)
            };
            var successDesc = new TextBlock
            {
                Text = "Chibi Desktop Pet установлен и готов радовать вас!\nВы можете запустить его в любое время с Рабочего стола.",
                FontSize = 12,
                TextAlignment = TextAlignment.Center,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#555555"))
            };
            finishedPanel.Children.Add(successIcon);
            finishedPanel.Children.Add(successTitle);
            finishedPanel.Children.Add(successDesc);
            contentGrid.Children.Add(finishedPanel);

            Grid.SetRow(contentGrid, 1);
            rootGrid.Children.Add(contentGrid);

            // 3. Footer / Button Row
            var footerRow = new Grid { Margin = new Thickness(0, 16, 0, 0) };
            footerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            footerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            installBtn = new Button
            {
                Content = "✨ Установить Chibi Pet",
                Height = 38,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9B59B6")),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 13,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                Padding = new Thickness(24, 0, 24, 0)
            };
            installBtn.Click += (s, e) => StartInstall();
            Grid.SetColumn(installBtn, 1);
            footerRow.Children.Add(installBtn);

            Grid.SetRow(footerRow, 2);
            rootGrid.Children.Add(footerRow);

            card.Child = rootGrid;
            this.Content = card;
        }

        private void BrowseFolder()
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.Description = "Выберите папку для установки Chibi Desktop Pet:";
            dialog.SelectedPath = pathBox.Text;
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                pathBox.Text = dialog.SelectedPath;
            }
        }

        private void StartInstall()
        {
            string targetDir = pathBox.Text.Trim();
            if (string.IsNullOrEmpty(targetDir))
            {
                MessageBox.Show("Пожалуйста, укажите папку установки.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            optionsPanel.Visibility = Visibility.Collapsed;
            progressPanel.Visibility = Visibility.Visible;
            installBtn.IsEnabled = false;

            bool createDesktop = desktopShortcutCheck.IsChecked == true;
            bool createStartMenu = startMenuCheck.IsChecked == true;
            bool launchAfter = launchAfterCheck.IsChecked == true;

            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    UpdateStatus("Создание каталога...", 15);
                    if (!Directory.Exists(targetDir))
                    {
                        Directory.CreateDirectory(targetDir);
                    }

                    UpdateStatus("Распаковка файлов приложения...", 30);
                    ExtractPayload(targetDir);

                    UpdateStatus("Создание ярлыков...", 70);
                    string exePath = Path.Combine(targetDir, "DesktopPet.exe");

                    if (createDesktop)
                    {
                        string desktopLnk = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Chibi Desktop Pet.lnk");
                        CreateShortcut(desktopLnk, exePath, targetDir, exePath);
                    }

                    if (createStartMenu)
                    {
                        string startMenuDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs");
                        if (!Directory.Exists(startMenuDir)) Directory.CreateDirectory(startMenuDir);
                        string startMenuLnk = Path.Combine(startMenuDir, "Chibi Desktop Pet.lnk");
                        CreateShortcut(startMenuLnk, exePath, targetDir, exePath);
                    }

                    UpdateStatus("Регистрация в системе Windows...", 85);
                    RegisterUninstaller(targetDir, exePath);
                    CreateUninstallScript(targetDir);

                    UpdateStatus("Завершение...", 100);
                    Thread.Sleep(500);

                    Dispatcher.BeginInvoke(new Action(delegate
                    {
                        progressPanel.Visibility = Visibility.Collapsed;
                        finishedPanel.Visibility = Visibility.Visible;

                        installBtn.Content = "Готово ✨";
                        installBtn.IsEnabled = true;
                        installBtn.Click -= (s, e) => StartInstall();
                        installBtn.Click += (s, e) =>
                        {
                            if (launchAfter)
                            {
                                try { Process.Start(exePath); } catch { }
                            }
                            this.Close();
                        };
                    }));
                }
                catch (Exception ex)
                {
                    Dispatcher.BeginInvoke(new Action(delegate
                    {
                        MessageBox.Show("Ошибка при установке: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        this.Close();
                    }));
                }
            });
        }

        private void UpdateStatus(string text, int progress)
        {
            Dispatcher.BeginInvoke(new Action(delegate
            {
                statusText.Text = text;
                progressBar.Value = progress;
            }));
        }

        private void ExtractPayload(string targetDir)
        {
            using (Stream resStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ChibiPayload.zip"))
            {
                if (resStream == null)
                {
                    throw new InvalidOperationException("Встроенный архив ChibiPayload.zip не найден в инсталлере!");
                }

                using (ZipArchive archive = new ZipArchive(resStream, ZipArchiveMode.Read))
                {
                    int total = archive.Entries.Count;
                    int cur = 0;
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        cur++;
                        string outPath = Path.Combine(targetDir, entry.FullName);
                        string outDir = Path.GetDirectoryName(outPath);
                        if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

                        if (!string.IsNullOrEmpty(entry.Name))
                        {
                            entry.ExtractToFile(outPath, true);
                        }

                        int pct = 30 + (int)((cur / (double)total) * 35.0);
                        UpdateStatus(string.Format("Распаковка: {0}", entry.Name), pct);
                    }
                }
            }
        }

        private void RegisterUninstaller(string targetDir, string exePath)
        {
            try
            {
                string keyPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\ChibiDesktopPet";
                using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(keyPath))
                {
                    key.SetValue("DisplayName", "Chibi Desktop Pet");
                    key.SetValue("DisplayVersion", "0.8.0-beta");
                    key.SetValue("Publisher", "xzen");
                    key.SetValue("DisplayIcon", exePath);
                    key.SetValue("InstallLocation", targetDir);
                    key.SetValue("UninstallString", "\"" + Path.Combine(targetDir, "uninstall.bat") + "\"");
                    key.SetValue("EstimatedSize", 20000, Microsoft.Win32.RegistryValueKind.DWord);
                }
            }
            catch { }
        }

        private void CreateUninstallScript(string targetDir)
        {
            try
            {
                string uninstBat = Path.Combine(targetDir, "uninstall.bat");
                var sb = new StringBuilder();
                sb.AppendLine("@echo off");
                sb.AppendLine("chcp 65001 > nul");
                sb.AppendLine("echo ========================================");
                sb.AppendLine("echo   Удаление Chibi Desktop Pet...        ");
                sb.AppendLine("echo ========================================");
                sb.AppendLine("taskkill /F /IM DesktopPet.exe > nul 2>&1");
                sb.AppendLine("timeout /t 1 /nobreak > nul");
                sb.AppendLine("reg delete \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\ChibiDesktopPet\" /f > nul 2>&1");
                sb.AppendLine("del /f \"%USERPROFILE%\\Desktop\\Chibi Desktop Pet.lnk\" > nul 2>&1");
                sb.AppendLine("del /f \"%APPDATA%\\Microsoft\\Windows\\Start Menu\\Programs\\Chibi Desktop Pet.lnk\" > nul 2>&1");
                sb.AppendLine("cd /d \"%TEMP%\"");
                sb.AppendLine("powershell -NoProfile -ExecutionPolicy Bypass -Command \"Start-Sleep -Seconds 1; Remove-Item -Path '" + targetDir + "' -Recurse -Force\" > nul 2>&1");
                sb.AppendLine("echo.");
                sb.AppendLine("echo Chibi Desktop Pet успешно удален с вашего компьютера.");
                sb.AppendLine("timeout /t 2 > nul");
                File.WriteAllText(uninstBat, sb.ToString(), Encoding.GetEncoding(866));
            }
            catch { }
        }

        private void CreateShortcut(string shortcutPath, string targetPath, string workDir, string iconPath)
        {
            try
            {
                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                dynamic shell = Activator.CreateInstance(shellType);
                dynamic shortcut = shell.CreateShortcut(shortcutPath);
                shortcut.TargetPath = targetPath;
                shortcut.WorkingDirectory = workDir;
                if (!string.IsNullOrEmpty(iconPath) && File.Exists(iconPath))
                {
                    shortcut.IconLocation = iconPath + ",0";
                }
                shortcut.Save();
            }
            catch
            {
                try
                {
                    string ps = string.Format(
                        "$ws = New-Object -ComObject WScript.Shell; $s = $ws.CreateShortcut('{0}'); $s.TargetPath = '{1}'; $s.WorkingDirectory = '{2}'; $s.Save()",
                        shortcutPath.Replace("'", "''"), targetPath.Replace("'", "''"), workDir.Replace("'", "''")
                    );
                    var psi = new ProcessStartInfo("powershell", "-NoProfile -ExecutionPolicy Bypass -Command \"" + ps + "\"")
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    Process.Start(psi).WaitForExit(3000);
                }
                catch { }
            }
        }
    }

    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            var app = new Application();
            var win = new InstallerWindow();
            app.Run(win);
        }
    }
}
