using System;
using SystemIO = System.IO;
using System.Collections.Generic;
using EnvDTE80;
using EnvDTE;

using JSolutionManager.Utility;

namespace JSolutionManager
{
    class JFileConfig
    {
        public const int parameterCount = 2;
        public List<(string fullPath, string includePath)> value { get; set; }
        public JFileConfig()
        {
            value = new List<(string fullPath, string includePath)>();
        }
        public void AddConfig(List<string> command, int index)
        {
            if (index + 1 >= command.Count)
                return;

            value.Add((command[index], command[index + 1]));
        }
    }
    class JFile
    {
        public static bool IsValidFormat(string format)
        {
            return format.Contains(".h") || format.Contains(".cpp") || format.Contains(".hpp");
        }
        public static void AddFile(JSolutionDataSet set,
            in string fullPath,
            in string includePath,
            in string projName)
        {
            //if (format)
            JLog.PrintOut("Add file");
            JLog.PrintOut("file path: " + fullPath);
            JLog.PrintOut("include path: " + includePath);
             
            if (!IsValidFormat(SystemIO.Path.GetExtension(fullPath)))
            {
                JLog.PrintOut("Invalid format");
                return;
            } 
            EnvDTE.Project proj = JConstants.FindProject(set.solution, projName);
            EnvDTE.ProjectItem projItem = JConstants.FindProjectItem(set.solution, projName, includePath);
             
            if (projItem != null)
            {
                JLog.PrintOut("ProjectItems AddFromFile");
                projItem.ProjectItems.AddFromFile(fullPath);
            }
            else if (proj != null)
            {
                JLog.PrintOut("Project AddFromFile");
                proj.ProjectItems.AddFromFile(fullPath);
            }
        }
        public static bool AddFile(in string fullPath,
            in string includePath,
            in string solutionPath,
            in string projName,
            in string config,
            in string platform,
            bool allowBuild)
        { 
            if (!SystemIO.File.Exists(solutionPath))
            {
                JLog.PrintOut("Fail add file can't find sln");
                return false;
            }
            if (!SystemIO.File.Exists(fullPath))
            {
                JLog.PrintOut("Fail add file can't find file .." + fullPath);
                return false;
            }
            JSolutionDataSet set = new JSolutionDataSet();
            set.Intialize();
            set.Open(solutionPath);

            AddFile(set, fullPath, includePath, projName);
            if (allowBuild)
            {
                JConstants.ActivateSolutionConfiguration(set.solution, config, platform);
                set.solution.SolutionBuild.Build(true);
            }

            set.Close();
            return true;
        }
        public static bool AddMultiFile(in string solutionPath,
            in string projName,
            in string config,
            in string platform,
            bool allowBuild,
            JFileConfig fileConfig)
        { 
            if (fileConfig.value.Count == 0)
                return false;

            if (!SystemIO.File.Exists(solutionPath))
            {
                JLog.PrintOut("Fail add file can't find sln");
                return false;
            }

            JSolutionDataSet set = new JSolutionDataSet();
            set.Intialize();
            set.Open(solutionPath);

            foreach (var data in fileConfig.value)
                AddFile(set, data.fullPath, data.includePath, projName);
             
            if (allowBuild)
            { 
                JConstants.ActivateSolutionConfiguration(set.solution, config, platform);

                JLog.PrintOut("AddMultiFile  try build");
                set.solution.SolutionBuild.Build(true);
            }
            set.Close();
            return true;
        }
    }
}
