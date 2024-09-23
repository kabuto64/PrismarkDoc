using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using Prismark.Utils;
using Prismark.Resources.StartUp;

namespace Prismark
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {
        // アプリケーション共有変数
        public bool IsLaunchedFromShortcut { get; set; }
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
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                string projectPath = GetProjectPathFromCommandLine();

                if (!string.IsNullOrEmpty(projectPath) && Directory.Exists(projectPath))
                {
                    await InitializeWithProjectPath(projectPath);
                }
                else
                {
                    await InitializeWithoutProjectPath();
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
            int projectIndex = Array.IndexOf(args, "--project");
            return (projectIndex != -1 && args.Length > projectIndex + 1) ? args[projectIndex + 1] : null;
        }

        private async Task InitializeWithProjectPath(string projectPath)
        {
            WorkingFolder = projectPath;
            ProjectName = Path.GetFileName(projectPath);
            await SaveLastWorkingDirectory(WorkingFolder);
            ShowMainWindow();
        }

        private async Task InitializeWithoutProjectPath()
        {
            string lastWorkingDir = ConfigurationManager.AppSettings["LastWorkingDirectory"];
            if (string.IsNullOrEmpty(lastWorkingDir))
            {
                var startUpWindow = new StartUpWindow();
                startUpWindow.Show();
            }
            else
            {
                WorkingFolder = lastWorkingDir;
                ProjectName = Path.GetFileName(lastWorkingDir);
                await SaveLastWorkingDirectory(WorkingFolder);
                ShowMainWindow();
            }
        }

        private async Task SaveLastWorkingDirectory(string directory)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            config.AppSettings.Settings["LastWorkingDirectory"].Value = directory;
            await Task.Run(() => config.Save(ConfigurationSaveMode.Modified));
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
        private async void App_Exit(object sender, ExitEventArgs e)
        {
            // 終了時に作業フォルダを記憶
            if (LocalHttpServer != null)
            {
                LocalHttpServer.Stop();
            }
            await SaveLastWorkingDirectory(WorkingFolder);
        }
    }
}