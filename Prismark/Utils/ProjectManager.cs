using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;

namespace Prismark.Utils
{
    public class ProjectManager
    {
        public static void CreateProjectLauncher(string projectPath, string projectName)
        {
            // プロジェクト名に基づいて実行ファイル名を生成
            string launcherFileName = $"{projectName}.exe";
            string launcherPath = Path.Combine(projectPath, launcherFileName);

            // 埋め込みリソースから子実行ファイルのバイナリを取得
            byte[] launcherBytes;
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Prismark.Resources.LaunchProject.exe"))
            {
                if (stream == null)
                {
                    throw new Exception("LaunchProject.exe resource not found.");
                }
                launcherBytes = new byte[stream.Length];
                stream.Read(launcherBytes, 0, launcherBytes.Length);
            }

            // 子実行ファイルを作業フォルダに書き込む
            File.WriteAllBytes(launcherPath, launcherBytes);

            // プロジェクト情報ファイルを作成
            var projectInfo = new ProjectInfo
            {
                MainAppPath = Assembly.GetExecutingAssembly().Location,
                ProjectPath = projectPath,
                ProjectName = projectName
            };
            string json = JsonConvert.SerializeObject(projectInfo, Formatting.Indented);
            File.WriteAllText(Path.Combine(projectPath, "ProjectInfo.json"), json);
        }
    }
    public class ProjectInfo
    {
        public string MainAppPath { get; set; }
        public string ProjectPath { get; set; }
        public string ProjectName { get; set; }
    }
}
