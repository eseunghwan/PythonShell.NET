
using System;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Diagnostics;
using System.Runtime.InteropServices;


namespace PythonShell.Utils {
    internal class InstanceMap {
        public String Dir { get; set; } = String.Empty;
        public String Python { get; set; } = String.Empty;
    }

    internal class Util {
        private static String GetShellInstanceDir(ShellConfig config, String? instanceName = null) {
            instanceName = instanceName ?? DateTime.Now.ToString("yyyy.MM.dd.HH.mm.ss");
            if (instanceName.ToLower() == "default") {
                return Path.Join(config.InstanceDir, "default");
            }
            else {
                return Path.Join(config.InstanceDir, instanceName);
            }
        }

        private static async Task DownloadFile(String url, String destFile) {
            var client = new HttpClient();
            var resp = await client.GetAsync(url);
            using (var fs = new FileStream(destFile, FileMode.CreateNew)) {
                await resp.Content.CopyToAsync(fs);
            }
        }

        public static async Task InitializeApp(ShellConfig config, Boolean createDefaultEnv) {
            config.DefaultPythonVersion = Util.CheckPythonVersion(config.DefaultPythonVersion);

            String userHomeDir = System.Environment.GetEnvironmentVariable("HOME")!;
            config.AppDir = Path.Join(userHomeDir, ".python_shell.net");
            if (!Directory.Exists(config.AppDir)) {
                Directory.CreateDirectory(config.AppDir);
            }

            if (!Directory.Exists(config.TempDir)) {
                Directory.CreateDirectory(config.TempDir);
            }

            if (!Directory.Exists(config.InstanceDir)) {
                Directory.CreateDirectory(config.InstanceDir);
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                String pythonDir = Path.Join(config.AppDir, "python");
                String pythonZipFile = Path.Join(config.TempDir, "python.zip");
                await Util.DownloadFile($"https://www.python.org/ftp/python/${config.DefaultPythonVersion}/python-{config.DefaultPythonVersion}-embed-amd64.zip", pythonZipFile);
                ZipFile.ExtractToDirectory(pythonZipFile, pythonDir);
                File.Delete(pythonZipFile);

                var pthFile = Util.GetPythonPthFile(config, pythonDir);
                File.WriteAllText(
                    pthFile,
                    File.ReadAllText(pthFile).Replace("#import site", "import site"),
                    System.Text.Encoding.UTF8
                );

                config.DefaultPythonPath = Path.Join(pythonDir, "python");
            }

            if (File.Exists(config.DefaultPythonPath)) {
                var p = Process.Start(config.DefaultPythonPath, new String[] { "-m", "pip", "install", "pip", "--upgrade" })!;
                p.WaitForExit();
                if (p.ExitCode != 0) {
                    String pipInstallFile = Path.Join(config.TempDir, "get-pip.py");
                    await Util.DownloadFile("https://bootstrap.pypa.io/pip/get-pip.py", pipInstallFile);
                    Process.Start(config.DefaultPythonPath, new String[] { pipInstallFile }).WaitForExit();
                    File.Delete(pipInstallFile);
                }

                Process.Start(config.DefaultPythonPath, new String[] { "-m", "pip", "install", "virtualenv", "--upgrade" }).WaitForExit();
            }

            String defaultEnvdir = Util.GetShellInstanceDir(config, "default");
            if (createDefaultEnv) {
                if (Directory.Exists(defaultEnvdir)) {
                    config.DefaultPythonEnvPath = Util.GetVirtualEnv(config, "default");
                }
                else {
                    config.DefaultPythonEnvPath = Util.CreateShellInstance(config, "default").Python;
                }

                Util.InstallRequiresToEnv(config, config.DefaultPythonEnvPath);
            }
            // else if (File.Exists(config.DefaultPythonPath)) {
            //     Util.InstallRequiresToEnv(config, config.DefaultPythonPath);
            // }
        }

        public static String CheckPythonVersion(String rawPythonVersion) {
            String realPythonVersion = "3.9.13";

            var versions = rawPythonVersion.Split(".");
            if (versions.Length == 3) {
                if (rawPythonVersion.EndsWith(".")) {
                    if (versions.Last() != "") {
                        realPythonVersion = String.Join(".", versions.SkipLast(1));
                    }
                }
                else {
                    realPythonVersion = rawPythonVersion;
                }
            }
            else if (versions.Length == 2) {
                if (rawPythonVersion.EndsWith(".")) {
                    if (versions.Last() != "") {
                        realPythonVersion = String.Join(".", versions) + ".0";
                    }
                }
                else {
                    realPythonVersion = $"{rawPythonVersion}.0";
                }
            }

            return realPythonVersion;
        }

        public static String GetPythonPthFile(ShellConfig config, String pythonDiir) {
            return Path.Join(pythonDiir, $"python{config.DefaultPythonVersion.Replace($".{config.DefaultPythonVersion.Split(".").Last()}", "")}._pth");
        }

        public static void ClearShellInstance(ShellConfig config, String instanceName) {
            var instanceDir = Util.GetShellInstanceDir(config, instanceName);
            foreach (var path in Directory.GetDirectories(Path.Join(instanceDir, "temp"))) {
                Directory.Delete(path, true);
            }

            foreach (var path in Directory.GetFiles(Path.Join(instanceDir, "temp"))) {
                File.Delete(path);
            }
        }

        public static InstanceMap CreateShellInstance(ShellConfig config, String? instanceName = null) {
            instanceName = instanceName ?? DateTime.Now.ToString("yyyy.MM.dd.HH.mm.ss");
            String instanceDir = Util.GetShellInstanceDir(config, instanceName);
            String envPython;

            if (!Directory.Exists(instanceDir)) {
                Directory.CreateDirectory(instanceDir);
                Directory.CreateDirectory(Path.Join(instanceDir, "temp"));
                envPython = Util.CreateVirtualEnv(config, instanceName);
            }
            else {
                Util.ClearShellInstance(config, instanceName);
                envPython = Util.GetVirtualEnv(config, instanceName);
            }

            return new InstanceMap {
                Dir = instanceDir, Python = envPython
            };
        }

        public static String CreateVirtualEnv(ShellConfig config, String instanceName, String[]? pythonRequires = null) {
            pythonRequires = pythonRequires ?? config.PythonRequires;

            String envDir = Path.Join(Util.GetShellInstanceDir(config, instanceName), "env");
            if (instanceName.ToLower() == "default") {
                config.DefaultPythonEnvPath = Util.GetVirtualEnv(config, "default");
            }

            Process.Start(config.DefaultPythonPath, new String[] { "-m", "virtualenv", envDir }).WaitForExit();
            String envPython = Util.GetVirtualEnv(config, instanceName);
            Process.Start(envPython, new String[] { "-m", "pip", "install", "pip", "--upgrade" }).WaitForExit();
            Util.InstallRequiresToEnv(config, envPython, pythonRequires: pythonRequires);

            return envPython;
        }

        public static void DeleteShellInstance(ShellConfig config, String instanceName) {
            Directory.Delete(Util.GetShellInstanceDir(config, instanceName), true);
        }

        public static InstanceMap GetShellInstance(ShellConfig config, String instanceName) {
            String instanceDir = Util.GetShellInstanceDir(config, instanceName);
            if (Directory.Exists(instanceDir)) {
                return new InstanceMap {
                    Dir = instanceDir,
                    Python = Util.GetVirtualEnv(config, instanceName)
                };
            }
            else {
                return Util.CreateShellInstance(config, instanceName);
            }
        }

        public static String GetVirtualEnv(ShellConfig config, String instanceName) {
            String envDir = Util.GetShellInstanceDir(config, instanceName);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                return Path.Join(envDir, "env", "Scripts", "python");
            }
            else {
                return Path.Join(envDir, "env", "bin", "python");
            }
        }

        public static void InstallRequiresToEnv(ShellConfig config, String envPython, String[]? pythonRequires = null) {
            pythonRequires = pythonRequires ?? config.PythonRequires;

            if (pythonRequires == null && config.PythonRequireFile != null) {
                Process.Start(envPython, new String[] { "-m", "pip", "install", "-r", config.PythonRequireFile }).WaitForExit();
            }
            else if (pythonRequires != null) {
                String tempPythonRequireFile = Path.Join(config.TempDir, DateTime.Now.ToString("yyyy.MM.dd.HH.mm.ss"));
                File.WriteAllText(tempPythonRequireFile, String.Join("\n", pythonRequires), System.Text.Encoding.UTF8);
                Process.Start(envPython, new String[] { "-m", "pip", "install", "-r", tempPythonRequireFile }).WaitForExit();
                File.Delete(tempPythonRequireFile);
            }
        }

        public static void RemoveShellInstance(ShellConfig config, String instanceName) {
            String instanceDir = Util.GetShellInstanceDir(config, instanceName);
            if (Directory.Exists(instanceDir)) {
                Directory.Delete(instanceDir, true);
            }
        }
    }
}
