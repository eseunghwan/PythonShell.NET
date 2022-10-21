
using System;
using System.Threading.Tasks;
using System.IO;
using System.Net.Http;
using System.Diagnostics;


namespace PythonShell {
    internal class Util {
        public static async Task DownloadFile(String url, String destFile, String fileType = "text") {
            var client = new HttpClient();
            var resp = await client.GetAsync(url);

            if (fileType.ToLower() == "binary") {
                File.WriteAllBytes(destFile, await resp.Content.ReadAsByteArrayAsync());
            }
            else if (fileType.ToLower() == "text") {
                File.WriteAllText(destFile, await resp.Content.ReadAsStringAsync());
            }
        }

        public static Process RunSettingCmd(String pythonToRun, String[] arguments, DataReceivedEventHandler? onError = null) {
            var process = new Process {
                StartInfo = new ProcessStartInfo {
                    FileName = pythonToRun,
                    Arguments = String.Join(" ", arguments),
                    UseShellExecute = false,
                    RedirectStandardOutput = true, RedirectStandardError = true
                }
            }!;

            process.ErrorDataReceived += onError;
            process.Start();
            process.WaitForExit();

            return process;
        }
    }
}
