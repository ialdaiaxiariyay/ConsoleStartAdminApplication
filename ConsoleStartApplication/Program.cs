using System.Diagnostics;
using Newtonsoft.Json.Linq;

namespace ConsoleStartApplication;

public static class Program
{
    private static readonly Dictionary<string, Process> RunningProcesses = new ();
    
    [STAThread]
    private static void Main()
    {
        const string jsonFilePath = "config.json";
        
        // 如果配置文件不存在，创建默认配置
        if (!File.Exists(jsonFilePath))
        {
            CreateDefaultConfig(jsonFilePath);
        }
        var applications = ReadConfig(jsonFilePath);
        switch (applications)
        {
            case { Count: 0 }:
            case null:
                return;
            default:
                CommandLoop(applications);
                break;
        }
    }
    
   private static void CreateDefaultConfig(string jsonFilePath)
    {
        // 创建默认配置，包含多个应用程序
        JArray defaultConfig =
        [
            new JObject
            {
                ["name"] = "PowerShell",
                ["path"] = "C:/Windows/system32/WindowsPowerShell/v1.0/powershell.exe",
                ["arguments"] = ""
            },

            new JObject
            {
                ["name"] = "Notepad",
                ["path"] = "notepad.exe",
                ["arguments"] = ""
            }
        ];
        
        File.WriteAllText(jsonFilePath, defaultConfig.ToString());
        Console.WriteLine("未找到config.json，已创建包含默认程序的新文件。");
    }
    
    private static List<ApplicationConfig>? ReadConfig(string jsonFilePath)
    {
        try
        {
            var jsonString = File.ReadAllText(jsonFilePath);
            var configArray = JArray.Parse(jsonString);

            return configArray.Select(item => new ApplicationConfig { Name = item["name"]?.ToString(), Path = item["path"]?.ToString(), Arguments = item["arguments"]?.ToString() }).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"读取配置文件时出错: {ex.Message}");
            return null;
        }
    }
    
    private static void StartApplication(ApplicationConfig appConfig)
    {
        try
        {
            var process = new Process();
            process.StartInfo.FileName = appConfig.Path;
            process.StartInfo.Arguments = appConfig.Arguments ?? "";
            process.Start();
            
            // 保存进程信息以便后续管理
            if (appConfig.Name == null) return;
            RunningProcesses[appConfig.Name] = process;
            Console.WriteLine($"已启动: {appConfig.Name} (PID: {process.Id})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"无法启动应用程序 '{appConfig.Name}': {ex.Message}");
        }
    }
    
    private static void StopApplication(string appName)
    {
        if (RunningProcesses.TryGetValue(appName, out var process))
        {
            if (!process.HasExited)
            {
                process.CloseMainWindow();
                if (!process.WaitForExit(5000)) // 等待5秒正常退出
                {
                    process.Kill();
                    Console.WriteLine($"已强制停止: {appName}");
                }
                else
                {
                    Console.WriteLine($"已停止: {appName}");
                }
            }
            RunningProcesses.Remove(appName);
        }
        else
        {
            Console.WriteLine($"未找到运行中的程序: {appName}");
        }
    }
    
    static void RestartApplication(ApplicationConfig appConfig)
    {
        if (appConfig.Name != null && RunningProcesses.ContainsKey(appConfig.Name))
        {
            StopApplication(appConfig.Name);
        }
        StartApplication(appConfig);
    }
    
    private static void CommandLoop(List<ApplicationConfig> applications)
    {
        Console.WriteLine("\n命令说明:");
        Console.WriteLine("  list - 显示所有配置的程序");
        Console.WriteLine("  running - 显示正在运行的程序");
        Console.WriteLine("  start <程序名> - 启动指定程序");
        Console.WriteLine("  stop <程序名> - 停止指定程序");
        Console.WriteLine("  restart <程序名> - 重启指定程序");
        Console.WriteLine("  stopall - 停止所有程序");
        Console.WriteLine("  exit - 退出管理器");
        Console.WriteLine("  help - 重新显示说明");
        
        while (true)
        {
            Console.Write("\n请输入命令: ");
            var input = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(input)) continue;
    
            // 分割输入，获取命令和参数
            var parts = input.Split(' ', 2);
            var command = parts[0].ToLower();
            var argument = parts.Length > 1 ? parts[1].Trim() : "";
            if (command == "exit")
            {
                StopAllApplications();
                break;
            }
            switch (command)
            {
                case "list":
                    ListApplications(applications);
                    break;
                case "running":
                    ListRunningApplications();
                    break;
                case "stopall":
                    StopAllApplications();
                    break;
                case "start":
                {
                    if (string.IsNullOrEmpty(argument))
                    {
                        Console.WriteLine("请指定要启动的程序名称，例如: start Notepad");
                        break;
                    }
                    var app = applications.Find(a => a.Name != null && a.Name.Equals(argument, StringComparison.OrdinalIgnoreCase));
                    if (app != null)
                    {
                        StartApplication(app);
                    }
                    else
                    {
                        Console.WriteLine($"未找到程序: {argument}");
                    }
                    break;
                }
                case "stop":
                {
                    if (string.IsNullOrEmpty(argument))
                    {
                        Console.WriteLine("请指定要停止的程序名称，例如: stop Notepad");
                        break;
                    }
                    StopApplication(argument);
                    break;
                }
                case "restart":
                {
                    if (string.IsNullOrEmpty(argument))
                    {
                        Console.WriteLine("请指定要重新启动的程序名称，例如: restart Notepad");
                        break;
                    }
                    var app = applications.Find(a => a.Name != null && a.Name.Equals(argument, StringComparison.OrdinalIgnoreCase));
                    if (app != null)
                    {
                        RestartApplication(app);
                    }
                    else
                    {
                        Console.WriteLine($"未找到程序: {argument}");
                    }
                    break;
                }
                case "help":
                {
                    Console.WriteLine("\n命令说明:");
                    Console.WriteLine("  list - 显示所有配置的程序");
                    Console.WriteLine("  running - 显示正在运行的程序");
                    Console.WriteLine("  start <程序名> - 启动指定程序");
                    Console.WriteLine("  stop <程序名> - 停止指定程序");
                    Console.WriteLine("  restart <程序名> - 重启指定程序");
                    Console.WriteLine("  stopall - 停止所有程序");
                    Console.WriteLine("  exit - 退出管理器");
                    Console.WriteLine("  help - 重新显示说明");
                    break;
                }
                default:
                {
                    Console.WriteLine("未知命令，请重新输入");
                    break;
                }
            }
        }
    }
    
    private static void ListApplications(List<ApplicationConfig> applications)
    {
        Console.WriteLine("\n配置的程序列表:");
        foreach (var app in applications)
        {
            var status = app.Name != null && RunningProcesses.TryGetValue(app.Name, out var value) ? 
                $"运行中 (PID: {value.Id})" : "未运行";
            Console.WriteLine($"  {app.Name}: {app.Path} [{status}]");
        }
    }
    
    private static void ListRunningApplications()
    {
        Console.WriteLine("\n正在运行的程序:");
        if (RunningProcesses.Count == 0)
        {
            Console.WriteLine("  没有程序正在运行");
            return;
        }
        
        foreach (var kvp in RunningProcesses)
        {
            Console.WriteLine($"  {kvp.Key}: PID {kvp.Value.Id} {(kvp.Value.HasExited ? "(已退出)" : "")}");
        }
    }
    
    private static void StopAllApplications()
    {
        Console.WriteLine("\n正在停止所有程序...");
        // 创建副本以避免修改集合时迭代
        var keys = new List<string>(RunningProcesses.Keys);
        foreach (var appName in keys)
        {
            StopApplication(appName);
        }
    }
}

internal class ApplicationConfig
{
    public string? Name { get; set; }
    public string? Path { get; set; }
    public string? Arguments { get; set; }
}