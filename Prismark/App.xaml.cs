using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.IO;

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

            string projectPath = null;

            // コマンドライン引数をチェック
            var args = Environment.GetCommandLineArgs();
            int projectIndex = Array.IndexOf(args, "--project");
            if (projectIndex != -1 && args.Length > projectIndex + 1)
            {
                projectPath = args[projectIndex + 1];
            }

            if (projectPath != null && Directory.Exists(projectPath))
            {
                // ショートカットから起動された場合
                WorkingFolder = projectPath;
                ProjectName = Path.GetFileName(projectPath);
                IsLaunchedFromShortcut = true;

            }
            else
            {
                IsLaunchedFromShortcut = false;
            }
        }
        /// <summary>
        /// アプリケーション終了処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void App_Exit(object sender, ExitEventArgs e)
        {
            // 終了時に作業フォルダを記憶
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            config.AppSettings.Settings["LastWorkingDirectory"].Value = WorkingFolder;
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }
    }
}