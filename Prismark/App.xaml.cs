using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using Prismark.Properties;
using Prismark.Utils;
using Prismark.UI.StartUp;

namespace Prismark
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {
        // アプリケーション共有変数
        public bool IsLaunchedFromShortcut { get; set; }
        public bool IsAbnormalClose { get; set; } = false;
        public string WorkingFolder { get; set; }
        public string ProjectName { get; set; }

        public LocalHttpServer LocalHttpServer{ get; set; }

        public App()
        {
            this.Exit += App_Exit;
        }

        /// <summary>
        /// アプリケーションスタートアップ処理
        /// </summary>
        /// <param name="e"></param>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                if (Settings.Default.IsRunning)
                {
                    IsAbnormalClose = true;
                }
                Settings.Default.IsRunning = true;
                Settings.Default.Save();

                string projectPath = GetProjectPathFromCommandLine();
                if (!string.IsNullOrEmpty(projectPath) && Directory.Exists(projectPath))
                {
                    InitializeWithProjectPath(projectPath);
                }
                else
                {
                    InitializeWithoutProjectPath();
                }
            }
            catch (Exception ex)
            {
                // エラー処理（ログ出力やエラーダイアログの表示など）
                MessageBox.Show($"アプリケーションの起動中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        private string GetProjectPathFromCommandLine()
        {
            var args = Environment.GetCommandLineArgs();
            int projectIndex = Array.IndexOf(args, "--from-launcher");
            return (projectIndex != -1 && args.Length > projectIndex + 1) ? args[projectIndex + 1] : null;
        }

        private void InitializeWithProjectPath(string projectPath)
        {
            WorkingFolder = projectPath;
            ProjectName = Path.GetFileName(projectPath);
            Settings.Default.LastWorkingDirectory = WorkingFolder;
            Settings.Default.Save();
            ShowMainWindow();
        }

        private void InitializeWithoutProjectPath()
        {
            string lastWorkingDir = Settings.Default.LastWorkingDirectory;
            if (string.IsNullOrEmpty(lastWorkingDir))
            {
                var startUpWindow = new StartUpWindow();
                startUpWindow.Show();
            }
            else
            {
                WorkingFolder = lastWorkingDir;
                ProjectName = Path.GetFileName(lastWorkingDir);
                Settings.Default.LastWorkingDirectory = WorkingFolder;
                Settings.Default.Save();
                ShowMainWindow();
            }
        }

        private void ShowMainWindow()
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
        }
        /// <summary>
        /// アプリケーション終了処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void App_Exit(object sender, ExitEventArgs e)
        {
            // 終了時に作業フォルダを記憶
            if (LocalHttpServer != null)
            {
                LocalHttpServer.Stop();
            }
            Settings.Default.IsRunning = false;
            Settings.Default.LastWorkingDirectory = WorkingFolder;
            Settings.Default.Save();
        }
    }
}