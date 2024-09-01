using System.Collections.Generic;
using System.IO; 
 
using JSolutionManager.Utility;
using JSolutionManager;
 
//updated 2023-06-28
//updated 2023-07-08   
//---- 프로그램 안정화 ----

/*
 * updated 2024-05-02  
 * JinEngine에 종속성 directory path를 직접 입력하는게 아닌 project에 .vcxproj파일에서 읽어들이는 기능추가
 * 기능 분리
*/

namespace SolutionManager
{
    enum COMMAND_TYPE
    {
        INVALID,
        HELP,               // -h
        CREATE_PROJECT,     // -p
        CREATE_VIRTUAL_DIRECTORY,      // -vd
        ADD_DIRECTORY,      // -d
        ADD_FILE,           // -f
        BUILD,              // -b 
        ADD_MULTI_FILE,     // -mf
        REMOVE,             // -r
        ALL_REMOVE,         // -ar
        PROJECT_CONFIG,     //-pc
        RESTORE_PROJECT,    //rp
        Count
    }
    class Program
    {
        class MinCommandCount
        {
            public static Dictionary<COMMAND_TYPE, int> value = new Dictionary<COMMAND_TYPE, int>
            {
                { COMMAND_TYPE.INVALID, 0},
                { COMMAND_TYPE.HELP, 0},
                { COMMAND_TYPE.CREATE_PROJECT, 6},
                { COMMAND_TYPE.CREATE_VIRTUAL_DIRECTORY, 4},
                { COMMAND_TYPE.ADD_DIRECTORY, 4},
                { COMMAND_TYPE.ADD_FILE, 7},
                { COMMAND_TYPE.BUILD, 4},
                { COMMAND_TYPE.ADD_MULTI_FILE, 5},
                { COMMAND_TYPE.REMOVE, 4},
                { COMMAND_TYPE.ALL_REMOVE, 3},
                { COMMAND_TYPE.PROJECT_CONFIG, 3},
                { COMMAND_TYPE.RESTORE_PROJECT, 3}
            };
        }
        class CommandLog
        {
            public static Dictionary<COMMAND_TYPE, string> entry = new Dictionary<COMMAND_TYPE, string>
            {
                { COMMAND_TYPE.INVALID, "Invalid"},
                { COMMAND_TYPE.HELP, "Help"},
                { COMMAND_TYPE.CREATE_PROJECT, "Create project"},
                { COMMAND_TYPE.CREATE_VIRTUAL_DIRECTORY, "Create virtual directory"},
                { COMMAND_TYPE.ADD_DIRECTORY, "Add directory"},
                { COMMAND_TYPE.ADD_FILE, "Add file"},
                { COMMAND_TYPE.BUILD, "Build"},
                { COMMAND_TYPE.ADD_MULTI_FILE, "Add multi file"},
                { COMMAND_TYPE.REMOVE, "Remove"},
                { COMMAND_TYPE.ALL_REMOVE, "All remove"},
                { COMMAND_TYPE.PROJECT_CONFIG, "Set project config"},
                { COMMAND_TYPE.RESTORE_PROJECT, "Restore project"}
            };
        }
        private struct CmdInfo
        {
            public COMMAND_TYPE type;
            public int configIndexOffset;
            public bool isValid; 
        }
        private static void PrintHelp()
        {
            JLog.PrintOut("Keyword");
            JLog.PrintOut("-h" + " Print help message");
            JLog.PrintOut("-p<template prefix>" + "... Create project ex) -pa <project name> <folder path> <template path> <config> <platform> " +
                "<is same location sln and .vcxproj ... true or false>" + "<... .vcxproj config(type(Meta, Property), name, value)>");
            JLog.PrintOut("-pc<template prefix>" + "... Set project config ex) -pc <solution path> <project path> " +
                "<config(type(Meta, Property), name, value)>");
            JLog.PrintOut("-vd" + " Create project virtual directory ex) -d <virtual dir path> <solution path> <proj name>");
            JLog.PrintOut("-d" + " Add directory ex) -d <dir path> <virtual dir path> <solution path> <proj name>");
            JLog.PrintOut("-f<can combine build prefix>" + "  Add .h .cpp file ex) -f <file path> <file include path> <solution path> <proj name> <config> <platform>");
            JLog.PrintOut("-mf<can combine build prefix>" + " (Add .h .cpp file)... ex) -mf <solution path> <proj name> <config> <platform> (<file path> <file include path>)...");
            JLog.PrintOut("-b " + "build sln file ex) -b <solution path> <config> <platform>");
            JLog.PrintOut("-r" + "remove project item ex) -r <include path> <solution path> <project name> ");
            JLog.PrintOut("-ar" + "remove all project item ex) -ar <solution path> <project name> ");
            JLog.PrintOut("-rp" + "restoreProject ex) -ar <name> <folder name> ");
            JLog.PrintOut("Last updated 2023-07-08");
            JLog.PrintOut("developed by jung jin woo");
        }
        private static List<string> CombineCommand(string[] args)
        {
            List<string> command = new List<string>();
            string stack = "";
            foreach (string item in args)
            {
                if (item == ",")
                {
                    command.Add(" ");
                    stack = "";
                }
                else if (item[item.Length - 1] == ',')
                {
                    command.Add(stack + item.Substring(0, item.Length - 1));
                    stack = "";
                }
                else
                    stack += item;
            }
            if (stack != "")
                command.Add(stack);

            return command;
        }
        private static CmdInfo ClassifyCommandType(string args)
        {
            CmdInfo info = new CmdInfo();
            info.type = COMMAND_TYPE.INVALID;
            if (args.Length < 2 || args.Length > 4 || args[0] != '-')
                return info;

            if (args[1] == 'h')
                info.type = COMMAND_TYPE.HELP;
            else if (args[1] == 'p')
            {
                if (args.Length > 2 && args[2] == 'c')
                    info.type = COMMAND_TYPE.PROJECT_CONFIG;
                else
                    info.type = COMMAND_TYPE.CREATE_PROJECT;
            }
            else if (args[1] == 'v' && args[2] == 'd')
                info.type = COMMAND_TYPE.CREATE_VIRTUAL_DIRECTORY;
            else if (args[1] == 'd')
                info.type = COMMAND_TYPE.ADD_DIRECTORY;
            else if (args[1] == 'f')
                info.type = COMMAND_TYPE.ADD_FILE;
            else if (args[1] == 'b')
                info.type = COMMAND_TYPE.BUILD;
            else if (args[1] == 'm' && args.Length > 2 && args[2] == 'f')
                info.type = COMMAND_TYPE.ADD_MULTI_FILE;
            else if (args[1] == 'a' && args.Length > 2 && args[2] == 'r')
                info.type = COMMAND_TYPE.ALL_REMOVE;
            else if (args[1] == 'r' && args.Length > 2 && args[2] == 'p')
                info.type = COMMAND_TYPE.RESTORE_PROJECT;
            else if (args[1] == 'r')
                info.type = COMMAND_TYPE.REMOVE;
            else
                info.type = COMMAND_TYPE.INVALID;

            int minCommandCount = MinCommandCount.value[info.type];
            info.isValid = minCommandCount >= args.Length;
            info.configIndexOffset = minCommandCount;
            return info;
        }
        private static TEMPLATE_TYPE ClassifyTemplateType(char ch)
        {
            if (ch == 'd')
                return TEMPLATE_TYPE.DLL;
            else if (ch == 'a')
                return TEMPLATE_TYPE.APPLICATION;
            else
                return TEMPLATE_TYPE.INVALID;
        }
        private static string GetTemplatePath(TEMPLATE_TYPE type)
        {
            // Assembly assembly = Assembly.GetExecutingAssembly();
            //FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            //string version = fileVersionInfo.ProductVersion;
            //version = "2019";
            //string userProfileDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            //string templatesDir = Path.Combine("C:\Program Files (x86)", "Documents", "Visual Studio " + version, "Templates", "ProjectTemplates", "VC");
            //string path = FindFileInSubdirectories(templatesDir, GetTemplateName(type));
            return "C:\\Program Files (x86)\\Microsoft Visual Studio\\2019\\Community\\Common7\\IDE\\ProjectTemplates\\VC\\WindowsDesktop\\WindowsDLL\\WindowsDLL.vstemplate";
        }
        private static string GetTemplateName(TEMPLATE_TYPE type)
        {
            if (type == TEMPLATE_TYPE.DLL)
                return "WindowsDLL.vstemplate";
            else if (type == TEMPLATE_TYPE.APPLICATION)
                return "DesktopApplication.vstemplate";
            else if (type == TEMPLATE_TYPE.EMPTY)
                return "Empty.vstemplate";
            else
                return null;
        }
        private static string FindFileInSubdirectories(string directory, string fileName)
        {
            if (fileName == null)
                return null;

            string[] files = Directory.GetFiles(directory, fileName);
            if (files.Length > 0)
            {
                return files[0]; // 파일을 찾았을 때 경로 반환
            }

            string[] subdirectories = Directory.GetDirectories(directory);
            foreach (string subdirectory in subdirectories)
            {
                string filePath = FindFileInSubdirectories(subdirectory, fileName);
                if (!string.IsNullOrEmpty(filePath))
                    return filePath; // 하위 폴더에서 파일을 찾았을 때 경로 반환
            }

            return null; // 파일을 찾지 못한 경우
        }
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                JLog.PrintOut("Invalid input empty args");
                return;
            }

            List<string> command = CombineCommand(args);
            JLog.Initialize(command);
 
            CmdInfo cmdInfo = ClassifyCommandType(command[0]);
            if (cmdInfo.type == COMMAND_TYPE.INVALID || !cmdInfo.isValid)
            {
                JLog.PrintOut("Invalid input cmd type"); 
                return;
            }

            JLog.PrintOut(CommandLog.entry[cmdInfo.type]);
            switch (cmdInfo.type)
            {
                case COMMAND_TYPE.HELP:
                    {
                        PrintHelp();
                        break;
                    }
                case COMMAND_TYPE.CREATE_PROJECT:
                    {
                        var configInfo = new JProjectConfig();
                        for (int i = cmdInfo.configIndexOffset; i < command.Count; i += JProjectConfig.parameterCount)
                            configInfo.AddConfig(command, i);

                        TEMPLATE_TYPE templateType = command[0].Length == 3 ? ClassifyTemplateType(command[0][2]) : TEMPLATE_TYPE.INVALID;
                        if (JProject.CreateProject(command[1], command[2], GetTemplatePath(templateType), command[3], command[4], command[5], configInfo))
                            JLog.PrintOut("Success");
                        else
                            JLog.PrintOut("Fail");
                        break;
                    }
                case COMMAND_TYPE.CREATE_VIRTUAL_DIRECTORY:
                    {
                        if (JDirectory.CreateVirtualDirectory(command[1], command[2], command[3]))
                            JLog.PrintOut("Success");
                        else
                            JLog.PrintOut("Fail");
                        break;
                    }
                case COMMAND_TYPE.ADD_DIRECTORY:
                    {
                        if (JDirectory.AddDirectory(command[1], command[2], command[3], command[4]))
                            JLog.PrintOut("Success");
                        else
                            JLog.PrintOut("Fail");
                        break;
                    }
                case COMMAND_TYPE.ADD_FILE:
                    {
                        bool allowBuild = command[0].Length == 3 && command[0][2] == 'b';
                        if (JFile.AddFile(command[1], command[2], command[3], command[4], command[5], command[6], allowBuild))
                            JLog.PrintOut("Success");
                        else
                            JLog.PrintOut("Fail");
                        break;
                    }
                case COMMAND_TYPE.BUILD:
                    {
                        if (JConstants.BuildSolution(command[1], command[2], command[3]))
                            JLog.PrintOut("Success");
                        else
                            JLog.PrintOut("Fail");
                        break;
                    }
                case COMMAND_TYPE.ADD_MULTI_FILE:
                    {
                        var fileConfig = new JFileConfig();
                        for (int i = cmdInfo.configIndexOffset; i < command.Count; i += JFileConfig.parameterCount)
                            fileConfig.AddConfig(command, i);

                        bool allowBuild = command[0].Length == 4 && command[0][3] == 'b';
                        if (JFile.AddMultiFile(command[1], command[2], command[3], command[4], allowBuild, fileConfig))
                            JLog.PrintOut("Success");
                        else
                            JLog.PrintOut("Fail");
                        break;
                    }
                case COMMAND_TYPE.REMOVE:
                    {
                        if (JProject.RemoveProjectItem(command[1], command[2], command[3]))
                            JLog.PrintOut("Success");
                        else
                            JLog.PrintOut("Fail");
                        break;
                    }
                case COMMAND_TYPE.ALL_REMOVE:
                    {
                        if (JProject.RemoveAllProjectItem(command[1], command[2]))
                            JLog.PrintOut("Success");
                        else
                            JLog.PrintOut("Fail");
                        break;
                    }
                case COMMAND_TYPE.PROJECT_CONFIG:
                    {
                        var configInfo = new JProjectConfig();
                        for (int i = cmdInfo.configIndexOffset; i < command.Count; i += JProjectConfig.parameterCount)
                            configInfo.AddConfig(command, i);

                        if (JProject.SetProjectConfig(command[1], command[2], configInfo))
                            JLog.PrintOut("Success");
                        else
                            JLog.PrintOut("Fail");
                        break;
                    }
                case COMMAND_TYPE.RESTORE_PROJECT:
                    {
                        if (JConstants.CanRestoreProject())
                        {
                            JProject.RestoreProjectUseFilter(command[1], command[2]);
                            JLog.PrintOut("Success");
                        }
                        else
                            JLog.PrintOut("Fail");
                        break;
                    }
                default:
                    {
                        JLog.PrintOut("Invalid input parameter");
                        break;
                    }
            }
            System.Threading.Thread.Sleep(2000);
        }
    }
}


/*unuse
 *         public static bool BuildProject(string path)
{
    var dic = new Dictionary<string, string>
    {
        // 구성 및 플랫폼 설정 추가
        { "Configuration", "Release" },
        { "Platform", "x64" }
    };
    // 빌드 수행 
    BuildParameters buildParameters = new BuildParameters();
    buildParameters.Loggers = new[] { new ConsoleLogger() }; //
    BuildManager buildManager = BuildManager.DefaultBuildManager;
    BuildRequestData requestData = new BuildRequestData(path, new Dictionary<string, string> { }, "4.0", new string[] { "Build" }, null);
    BuildResult buildResult = buildManager.Build(buildParameters, requestData);

    JLog.PrintOut("BuildResult");
    // 빌드 결과 출력 
    //foreach (TargetResult targetResult in buildResult.ResultsByTarget.Values)
    //{
    //    foreach (ITaskItem item in targetResult.Items)
    //        BuildLogErrorAndWarning(item);
    //}

    // 실행 중인 콘솔 창에 메시지 출력
    JLog.PrintOut("Press any key to continue...");

    // 사용자가 키를 입력할 때까지 대기
    Console.ReadKey();
    return true;
}
private static void BuildLogErrorAndWarning(ITaskItem item)
{
    string itemPath = item.ItemSpec;

    if (!string.IsNullOrEmpty(itemPath))
    {
        JLog.PrintOut($"Item Path: {itemPath}");
        foreach (string error in item.GetMetadata("Error").Split(';'))
            JLog.PrintOut($"Error: {error}");
        foreach (string warning in item.GetMetadata("Warning").Split(';'))
            JLog.PrintOut($"Warning: {warning}");
    }
}
public static bool RebuildProject(string projPath)
{
    // Visual Studio의 MSBuild.exe 경로
    string msbuildPath = GetMSBuildPath();
    if (msbuildPath == null)
    {
        JLog.PrintOut("Rebuild fail.");
        return false;
    }
    // 프로젝트 파일 경로를 인용 부호로 감싸고, 리빌드 명령을 생성
    string rebuildCommand = $"\"{projPath}\" /t:Rebuild";

    // 프로세스 실행을 위한 설정
    ProcessStartInfo startInfo = new ProcessStartInfo
    {
        FileName = msbuildPath,
        Arguments = rebuildCommand,
        RedirectStandardOutput = true,
        UseShellExecute = false,
        CreateNoWindow = true
    };

    // MSBuild 프로세스 실행
    Process process = new Process();
    process.StartInfo = startInfo;
    process.Start();

    // 프로세스의 출력을 읽어옴 (필요에 따라 사용)
    //string output = process.StandardOutput.ReadToEnd();

    // 프로세스가 완료될 때까지 대기
    process.WaitForExit();

    // 리빌드 완료
    JLog.PrintOut("Rebuild success.");
    return true;
}
public static string GetMSBuildPath()
{
    // Visual Studio 인스턴스 확인
    var instances = MSBuildLocator.QueryVisualStudioInstances();

    if (instances.Count() == 0)
        return null;

    // 가장 최신 버전의 Visual Studio 인스턴스 선택
    var instance = instances.OrderByDescending(i => i.Version).First();

    // MSBuild 위치 확인
    if (!string.IsNullOrEmpty(instance.MSBuildPath))
        return Path.Combine(instance.MSBuildPath, "MSBuild.exe");

    return null;
}
 */