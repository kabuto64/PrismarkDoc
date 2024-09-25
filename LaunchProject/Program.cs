using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace LaunchProject
{
    internal class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string projectPath = AppDomain.CurrentDomain.BaseDirectory;
            string projectInfoPath = Path.Combine(projectPath, "ProjectInfo.json");

            if (File.Exists(projectInfoPath))
            {
                try
                {
                    string jsonString = File.ReadAllText(projectInfoPath);
                    ProjectInfo projectInfo = JsonConvert.DeserializeObject<ProjectInfo>(jsonString);

                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = projectInfo.MainAppPath,
                        Arguments = $"--from-launcher \"{projectInfo.ProjectPath}\" \"{projectInfo.ProjectName}\"",
                        UseShellExecute = false
                    };

                    Process.Start(startInfo);
                }
                catch (JsonException ex)
                {
                    MessageBox.Show($"Error parsing ProjectInfo.json: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Error: ProjectInfo.json not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        class ProjectInfo
        {
            public string MainAppPath { get; set; }
            public string ProjectPath { get; set; }
            public string ProjectName { get; set; }
        }
    }
}
