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
        private App _app = System.Windows.Application.Current as App;
        public static void CreateProjectLauncher(string projectPath, string projectName)
        {
            if (string.IsNullOrEmpty(projectPath) || string.IsNullOrEmpty(projectName)) return;
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
            ProjectInfo info = new ProjectInfo();
            info.MainAppPath = Assembly.GetExecutingAssembly().Location;
            info.ProjectPath = projectPath;
            info.ProjectName = projectName;
            WriteProjectInfo(info);
        }
        public static ProjectInfo ReadProjectInfo(string warkingDir)
        {
            string jsonString = File.ReadAllText(Path.Combine(warkingDir, "ProjectInfo.json"));
            ProjectInfo projectInfo = JsonConvert.DeserializeObject<ProjectInfo>(jsonString);
            return projectInfo;
        }
        public static void WriteProjectInfo(ProjectInfo info)
        {
            string json = JsonConvert.SerializeObject(info, Formatting.Indented);
            File.WriteAllText(Path.Combine(info.ProjectPath, "ProjectInfo.json"), json, Encoding.UTF8);
        }
    }
    public class ProjectInfo
    {
        public string MainAppPath { get; set; }
        public string ProjectPath { get; set; }
        public string ProjectName { get; set; }
        public List<ProjectFile> ProjectFiles { get; set; }
    }
    public class ProjectFile
    {
        public string FileName { get; set; }
        public string Section { get; set; }
        public string Content { get; set; }
        public bool IsSaved { get; set; } = true;
        public bool IsInit { get; set; } = true;
    }
}
