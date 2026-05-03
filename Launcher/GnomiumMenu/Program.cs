using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;

namespace GnomiumMenu
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "Gnomium Internal Menu - Injector";
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(@"
  ____                       _                  __  __                 
 / ___| _ __   ___  _ __ ___(_)_   _ _ __ ___  |  \/  | ___ _ __  _   _ 
| |  _ | '_ \ / _ \| '_  _ \ | | | | '_  _ \ | |\/| |/ _ \ '_ \| | | |
| |_| || | | | (_) | | | | | | | |_| | | | | | || |  | |  __/ | | | |_| |
 \____||_| |_|\___/|_| |_| |_|_|\__,_|_| |_| |_||_|  |_|\___|_| |_|\__, |
                                                                   |___/ 
            ");
            Console.ResetColor();
            Console.WriteLine("欢迎使用 Burglin' Gnomes 内部注入器 v2.0");
            Console.WriteLine("------------------------------------------");

            string processName = "Gnomium";
            
            Console.WriteLine($"[1] 正在等待游戏进程 '{processName}'...");
            while (Process.GetProcessesByName(processName).Length == 0)
            {
                Thread.Sleep(1000);
            }
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[+] 找到游戏进程 '{processName}'！");
            Console.ResetColor();

            Console.WriteLine("[2] 准备释放负载...");
            string tempDir = Path.Combine(Path.GetTempPath(), "GnomiumLauncher_" + Guid.NewGuid().ToString().Substring(0, 8));
            Directory.CreateDirectory(tempDir);

            try
            {
                string payloadPath = Path.Combine(tempDir, "CheatPayload.dll");
                string smiPath = Path.Combine(tempDir, "smi.exe");
                string smiLibPath = Path.Combine(tempDir, "SharpMonoInjector.dll");

                ExtractResource("CheatPayload.dll", payloadPath);
                ExtractResource("smi.exe", smiPath);
                ExtractResource("SharpMonoInjector.dll", smiLibPath);

                Console.WriteLine("[3] 执行注入...");
                var processInfo = new ProcessStartInfo
                {
                    FileName = smiPath,
                    Arguments = $"inject -p \"{processName}\" -a \"{payloadPath}\" -n BurglinCheat -c Loader -m Init",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(processInfo))
                {
                    if (process != null)
                    {
                        process.WaitForExit();
                        string output = process.StandardOutput.ReadToEnd();
                        string error = process.StandardError.ReadToEnd();

                        if (process.ExitCode == 0 || output.Contains("Successfully injected"))
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("[+] 注入成功！请返回游戏按下 'Insert' 键开启菜单。");
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("[-] 注入可能失败了。");
                            Console.WriteLine("输出:");
                            Console.WriteLine(output);
                            Console.WriteLine("错误:");
                            Console.WriteLine(error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[-] 发生错误: " + ex.Message);
            }
            finally
            {
                Console.ResetColor();
                Console.WriteLine("------------------------------------------");
                Console.WriteLine("按任意键退出，并将清理临时释放的文件...");
                Console.ReadKey();

                try { Directory.Delete(tempDir, true); } catch { }
            }
        }

        static void ExtractResource(string resourceName, string outPath)
        {
            using (Stream? s = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                if (s == null) throw new Exception("无法找到嵌入资源: " + resourceName);
                using (FileStream fs = new FileStream(outPath, FileMode.Create))
                {
                    s.CopyTo(fs);
                }
            }
        }
    }
}
