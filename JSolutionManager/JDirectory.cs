using System; 
using SystemIO = System.IO;
using System.Collections.Generic;
using EnvDTE80;
using EnvDTE;

using JSolutionManager.Utility;

namespace JSolutionManager
{
    class JDirectory
    {
        private static void AddVirtualFolder(JSolutionDataSet set, in string fullPath, in string includePath, in string projName)
        {
            EnvDTE.Project proj = JConstants.FindProject(set.solution, projName);
            EnvDTE.ProjectItem projItem = JConstants.FindProjectItem(set.solution, projName, includePath);

            if (projItem != null)
            {
                ProjectItems pItem = projItem.ProjectItems;
                pItem.AddFolder(fullPath, EnvDTE.Constants.vsProjectItemKindVirtualFolder);
            }
            else if (proj != null)
            {
                ProjectItems pItem = proj.ProjectItems;
                pItem.AddFolder(fullPath, EnvDTE.Constants.vsProjectItemKindVirtualFolder);
            }
        }
        private static void AddExistDirectory(JSolutionDataSet set, in string dirPath, in string includePath, in string projName)
        {
            JLog.PrintOut("AddExistDirectory: ");
            JLog.PrintOut("dirPath: " + dirPath);
            JLog.PrintOut("includePath: " + includePath);

            //can't allow overlap 
            if (JConstants.FindProjectItem(set.solution, projName, dirPath) == null)
                AddVirtualFolder(set, dirPath, includePath, projName);

            string[] directories = SystemIO.Directory.GetDirectories(dirPath);
            foreach (var data in directories)
                AddExistDirectory(set, dirPath + "\\" + data, includePath +"\\" + data, projName);
             
            string[] files = SystemIO.Directory.GetFiles(dirPath);
            foreach (var data in files)
                JFile.AddFile(set, dirPath + "\\" + data, includePath, projName);
        }
        public static bool CreateVirtualDirectory(in string fullPath, in string solutionPath, in string projName)
        {
            string includePath = SystemIO.Path.GetDirectoryName(fullPath); 
            JLog.PrintOut("virtual directory path: " + fullPath);
            JLog.PrintOut("include path: " + includePath);

            if (!SystemIO.File.Exists(solutionPath))
            {
                JLog.PrintOut("Fail add dir can't find sln");
                return false;
            }

            JSolutionDataSet set = new JSolutionDataSet();
            set.Intialize();
            set.Open(solutionPath);

            AddVirtualFolder(set, fullPath, includePath, projName);

            set.Close();
            return true;
        }
        public static bool AddDirectory(in string dirPath, in string virtualDirPath, in string solutionPath, in string projName)
        { 
            if (!SystemIO.File.Exists(solutionPath))
            {
                JLog.PrintOut("Fail add dir can't find sln");
                return false;
            }

            JSolutionDataSet set = new JSolutionDataSet();
            set.Intialize();
            set.Open(solutionPath);

            AddExistDirectory(set, dirPath, virtualDirPath, projName);

            set.Close();
            return true;
        }
    }
}
