using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace GnomiumMenu
{
    class Program
    {
        static readonly string LogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            "GnomiumMenu_log.txt");

        static void Log(string msg)
        {
            string line = $"[{DateTime.Now:HH:mm:ss}] {msg}";
            Console.WriteLine(line);
            try { File.AppendAllText(LogPath, line + "\n"); } catch { }
        }

        static void Main(string[] args)
        {
            try { File.WriteAllText(LogPath, $"=== GnomiumMenu Log {DateTime.Now} ===\n"); } catch { }
            Console.Title = "Gnomium Internal Menu - Injector";
            try   { Run(); }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Log("[FATAL] " + ex.ToString());
                Console.ResetColor();
            }
            finally
            {
                Console.ResetColor();
                Console.WriteLine("\n========== 按任意键关闭 ==========");
                try { Console.ReadKey(true); } catch { Thread.Sleep(5000); }
            }
        }

        static void Run()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("  +==========================================+");
            Console.WriteLine("  |  Burglin Gnomes  Internal Menu  v2.1    |");
            Console.WriteLine("  +==========================================+");
            Console.ResetColor();
            Console.WriteLine();

            var asm = typeof(Program).Assembly;
            Log("[INFO] BaseDirectory : " + AppDomain.CurrentDomain.BaseDirectory);
            Log("[INFO] 嵌入资源      : [" + string.Join(", ", asm.GetManifestResourceNames()) + "]");

            string processName = "Gnomium";
            Log("[1] 等待游戏进程 '" + processName + "' ...");
            Console.WriteLine("    (先启动游戏，本程序自动检测)");
            int waited = 0;
            while (Process.GetProcessesByName(processName).Length == 0)
            {
                Thread.Sleep(1000);
                waited++;
                if (waited % 5 == 0)
                    Console.Write("    已等待 " + waited + "s ...\r");
            }
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Log("[+] 找到游戏进程!");
            Console.ResetColor();

            string tempDir = Path.Combine(Path.GetTempPath(), "GnomiumInjectorCache");
            if (!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);
            Log("[2] 临时目录: " + tempDir);

            string payloadPath = Path.Combine(tempDir, "CheatPayload.dll");
            string smiPath     = Path.Combine(tempDir, "smi.exe");
            string smiLibPath  = Path.Combine(tempDir, "SharpMonoInjector.dll");

            ExtractResource("CheatPayload.dll",       payloadPath, asm);
            ExtractResource("smi.exe",                smiPath,     asm);
            ExtractResource("SharpMonoInjector.dll",  smiLibPath,  asm);
            Log("[+] 嵌入资源释放完成");

            string smiArgs = "inject -p \"" + processName + "\" -a \"" + payloadPath + "\" -n BurglinCheat -c Loader -m Init";
            Log("[3] 注入命令: " + smiArgs);

            var psi = new ProcessStartInfo
            {
                FileName               = smiPath,
                Arguments              = smiArgs,
                UseShellExecute        = false,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                CreateNoWindow         = true,
                WorkingDirectory       = tempDir
            };

            using var proc = Process.Start(psi)
                ?? throw new Exception("无法启动 smi.exe");

            string stdout = proc.StandardOutput.ReadToEnd();
            string stderr = proc.StandardError.ReadToEnd();
            proc.WaitForExit();

            Log("[SMI] 退出码: " + proc.ExitCode);
            if (!string.IsNullOrWhiteSpace(stdout)) Log("[SMI] stdout: " + stdout.Trim());
            if (!string.IsNullOrWhiteSpace(stderr)) Log("[SMI] stderr: " + stderr.Trim());

            bool ok = proc.ExitCode == 0 && !stdout.Contains("ERROR") && !stderr.Contains("ERROR");
            if (ok)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Log("[+] 注入成功！返回游戏按 Insert 键开启菜单。");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Log("[-] 注入失败，详见日志: GnomiumMenu_log.txt (桌面)");
                Console.ResetColor();
            }
        }

        static void ExtractResource(string name, string outPath, System.Reflection.Assembly asm)
        {
            if (File.Exists(outPath) && new FileInfo(outPath).Length > 0)
            {
                Log("[SKIP] " + name + " 已缓存");
                return;
            }
            using var s = asm.GetManifestResourceStream(name);
            if (s == null)
                throw new Exception("嵌入资源未找到: " + name + "\n可用: [" + string.Join(", ", asm.GetManifestResourceNames()) + "]");
            using var fs = new FileStream(outPath, FileMode.Create, FileAccess.Write);
            s.CopyTo(fs);
            Log("[+] 释放 " + name + " (" + new FileInfo(outPath).Length + " bytes)");
        }
    }
}