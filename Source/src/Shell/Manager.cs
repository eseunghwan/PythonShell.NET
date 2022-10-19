
using System;
using System.IO;
using System.Runtime.InteropServices;


namespace PythonShell.Shell {
    public static class ShellManager {
        public static ShellInstance CreateInstance(String? instanceName = null, String[]? pythonRequires = null, Boolean echo = true) {
            instanceName = instanceName ?? DateTime.Now.ToString("yyyy.MM.dd.HH.mm.ss");
            if (echo) Console.WriteLine($"Creating shell instance [{instanceName}]...");
            String instanceDir = Path.Join(Config.InstanceDir, instanceName);

            if (Directory.Exists(instanceDir)) {
                return ShellManager.GetInstance(instanceName);
            }
            else {
                Directory.CreateDirectory(instanceDir);
                String tempDir = Path.Join(instanceDir, "temp");
                Directory.CreateDirectory(tempDir);

                if (echo) Console.WriteLine("Creating virtualenv...");
                String envDir = Path.Join(instanceDir, "env");
                String envPython = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? Path.Join(envDir, "Scripts", "python.exe") : Path.Join(envDir, "bin", "python");
                var instance = new ShellInstance {
                    Name = instanceName, PythonPath = envPython, TempDir = tempDir
                };

                Util.RunSettingCmd(Config.DefaultPythonPath, new String[] { "-m", "virtualenv", envDir });
                if (pythonRequires != null) {
                    instance.InstallRequires(pythonRequires, echo);
                }
                if (echo) Console.WriteLine("Virtualenv created.");
                if (echo) Console.WriteLine("Shell instance created.");

                return instance;
            }
        }

        public static ShellInstance GetInstance(String instanceName) {
            String instanceDir = Path.Join(Config.InstanceDir, instanceName);
            if (Directory.Exists(instanceDir)) {
                return new ShellInstance {
                    Name = instanceName,
                    PythonPath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? Path.Join(instanceDir, "env", "Scripts", "python.exe") : Path.Join(instanceDir, "env", "bin", "python"),
                    TempDir = Path.Join(instanceDir, "temp")
                };
            }
            else {
                return ShellManager.CreateInstance(instanceName, echo: false);
            }
        }

        public static void RemoveInstance(ShellInstance instance) {
            instance.Remove();
        }

        public static void Clear() {
            foreach (var path in Directory.GetDirectories(Config.InstanceDir)) {
                if (Path.GetFileName(path).ToLower() != "default") {
                    Directory.Delete(path, true);
                }
            }
        }
    }
}
