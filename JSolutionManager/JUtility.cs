using System;
using System.IO;
using System.Xml;
using System.Text.RegularExpressions;
using System.Collections.Generic;

using EnvDTE80;
using EnvDTE;

using Microsoft.Win32;
using Microsoft.Build.Construction;

namespace JSolutionManager
{
    namespace Utility
    {
        public class JModProjConfigInfo
        {
            public string projectPath;
            public string name;
            public string value;
            public bool canFindAll;
            public bool allowAddIfFail;
            public bool allowAddexistValue;
            public JModProjConfigInfo(in string projectPath,
                in string name,
                in string value,
                bool canFindAll = true,
                bool allowAddIfFail = true,
                bool allowAddexistValue = false)
            {
                this.projectPath = projectPath;
                this.name = name;
                this.value = value;

                this.canFindAll = canFindAll;
                this.allowAddIfFail = allowAddIfFail;
                this.allowAddexistValue = allowAddexistValue;
            }

        }
        public class JSolutionDataSet
        {
            public DTE2 dte;
            public Solution2 solution;
            public string solutionPath;
            public void Intialize()
            {
                dte = (DTE2)Activator.CreateInstance(Type.GetTypeFromProgID("VisualStudio.DTE." + JConstants.GetVsVersion(2)));
                solution = (Solution2)dte.Solution;
            }
            public void Create(string folderPath, string name)
            {
                solution.Create(folderPath, name);
                if (IsOpen())
                {
                    solutionPath = folderPath + "\\" + name + ".sln";
                    JLog.PrintOut("Create Solution: " + solutionPath);
                }
            }
            public void Open(string _solutionPath)
            {
                solution.Open(_solutionPath);
                if (IsOpen())
                {
                    solutionPath = _solutionPath;
                    JLog.PrintOut("Open Solution: " + solutionPath);
                }
            }
            public void Close(bool isSave = true)
            {
                solution.Close(isSave); 
                dte.Quit();
                JLog.PrintOut("Close Solution");
            }
            public void AddProjectFromTemplate(in string templatePath, in string projFolderPath, in string name)
            {
                solution.AddFromTemplate(templatePath, projFolderPath, name + ".vcxproj");
            }
            public bool IsOpen()
            {
                return solution.IsOpen;
            }
        }
        public class JLog
        {
            public static bool isRecordLog = false;
            public static StreamWriter writer;
            public static void Initialize(in List<string> command)
            {
                isRecordLog = command[command.Count - 1].Contains("Debug");
                if (isRecordLog)
                {
                    string exePath = System.Reflection.Assembly.GetEntryAssembly().Location;
                    string logFilePath = Path.GetDirectoryName(exePath) + "\\Log.txt";
                    writer = new StreamWriter(logFilePath);
                }
            }
            public static void PrintOut(string msg)
            {
                Console.WriteLine(msg);
                if (isRecordLog)
                    writer.WriteLine(msg);
            }
        }
        class JConstants
        {
            public static bool CanRestoreProject()
            {
                return false;
            }
            public static string ItemGroup()
            {
                return "ItemGroup";
            }
            public static string Label()
            {
                return "Label";
            }
            public static string FilterGroupName()
            {
                return "FilterGroup";
            }
            public static string HeaderTagName()
            {
                return "Header";
            }
            public static string CppTagName()
            {
                return "Cpp";
            }
            public static string HeaderItemType()
            {
                return "ClInclude";
            }
            public static string CppItemType()
            {
                return "ClCompile";
            }
            public static void CreateFile(in string path, in string contetns)
            {
                File.WriteAllText(path, contetns);
            }
            public static ProjectItemGroupElement FindItemGroupByLabel(ProjectRootElement projectRootElement, string label)
            {
                foreach (ProjectItemGroupElement itemGroup in projectRootElement.ItemGroups)
                {
                    string itemGroupLabel = itemGroup.Label;
                    if (itemGroupLabel != null && itemGroupLabel.Equals(label, StringComparison.OrdinalIgnoreCase))
                    {
                        return itemGroup;
                    }
                }
                return null;
            }
            public static XmlNode FindAttributeByName(XmlNodeList nodeList, string attributeName, string attributeValue)
            {
                foreach (XmlNode node in nodeList)
                {
                    foreach (XmlAttribute attribute in node.Attributes)
                    {
                        if (attribute.Name.Equals(attributeName) && attribute.Value.Equals(attributeValue))
                            return node;
                    }
                }
                return null;
            }
            public static EnvDTE.Project FindProject(Solution2 sol, string name)
            {
                foreach (EnvDTE.Project project in sol.Projects)
                {
                    if (name.Equals(project.Name))
                        return project;
                }
                return null;
            }
            public static EnvDTE.ProjectItem FindProjectItem(Solution2 sol, string name, in string includePath)
            {
                if (includePath == null || includePath == string.Empty || includePath.Equals(" "))
                    return null;

                EnvDTE.Project proj = FindProject(sol, name);
                if (proj == null)
                    return null;

                EnvDTE.ProjectItem result = null;
                foreach (EnvDTE.ProjectItem item in proj.ProjectItems)
                { 
                    result = FindProjectItem(item, includePath, item.Name);
                    if (result != null)
                        break;
                }
                return result;
            }
            public static EnvDTE.ProjectItem FindProjectItem(EnvDTE.ProjectItem item, in string includePath, in string folderPath)
            { 
                if (includePath.Equals(folderPath))
                    return item;

                EnvDTE.ProjectItem result = null;
                foreach (EnvDTE.ProjectItem project in item.ProjectItems)
                {
                    //if(project.Kind == Constants.vsProjectItemKindPhysicalFolder)
                    result = FindProjectItem(project, includePath, folderPath + "\\" + project.Name);
                    if (result != null)
                        break;
                }
                return result;
            }
            public static void PrintProject(ref Solution2 sol)
            {
                foreach (EnvDTE.Project project in sol.Projects)
                {
                    JLog.PrintOut(project.Name);
                    JLog.PrintOut(project.FullName);
                    PrintInner(project.ProjectItems, null);
                }
            }
            public static void PrintInner(EnvDTE.ProjectItems p, string folderPath)
            {
                foreach (EnvDTE.ProjectItem project in p)
                {
                    if (folderPath == null)
                        PrintInner(project.ProjectItems, project.Name);
                    else
                        PrintInner(project.ProjectItems, folderPath + "\\" + project.Name);
                }
            }
            public static string GetVsVersion(int versionRange)
            {
                if (versionRange == 0)
                    versionRange = 1;
                if (versionRange > 4)
                    versionRange = 4;

                string mask = @"VisualStudio\.edmx";
                if (versionRange > 0)
                    mask += @"\.(";
                for (int i = 0; i < versionRange; ++i)
                {
                    if (i > 0)
                        mask += @"\.";
                    mask += @"\d+";
                }
                if (versionRange > 0)
                    mask += ")";

                var registry = Registry.ClassesRoot;
                var subKeyNames = registry.GetSubKeyNames();
                //var regex = new Regex(@"VisualStudio\.edmx\.(\d+\.\d+\.\d+\.\d+)");
                var regex = new Regex(mask);

                foreach (var subKeyName in subKeyNames)
                {
                    var match = regex.Match(subKeyName);
                    if (match.Success)
                        return match.Groups[1].Value;
                }
                return null;
            }
            public static string GetVsProductVersion()
            {
                string version = GetVsVersion(1);
                if (version == null)
                    return null;

                if (version[1] == '7')
                    return "2022";
                else if (version[1] == '6')
                    return "2019";
                else if (version[1] == '5')
                    return "2017";
                else if (version[1] == '4')
                    return "2015";
                else if (version[1] == '3')
                    return "2012";
                else
                    return null;
            }
            /*
             * Configuration = Release, Debug
             * PlatformName = x64, x86
             */
            public static void ActivateSolutionConfiguration(Solution2 sol, string configurationName, string platformName)
            {
                bool found = false;
                foreach (SolutionConfiguration2 configuration in sol.SolutionBuild.SolutionConfigurations)
                {
                    if (configuration.Name == configurationName && configuration.PlatformName == platformName)
                    { 
                        configuration.Activate();
                        found = true;
                        break;
                    }
                }
                if (found)
                    JLog.PrintOut($"Configuration: {configurationName}/{platformName}");
                else
                    JLog.PrintOut($"Configuration: {configurationName}/{platformName} not found");
            }
            public static bool BuildSolution(in string solutionPath, in string config, in string platform)
            {
                if (!File.Exists(solutionPath))
                {
                    JLog.PrintOut("Fail build sln can't find sln");
                    return false;
                }

                JSolutionDataSet set = new JSolutionDataSet();
                set.Intialize();
                set.Open(solutionPath);

                ActivateSolutionConfiguration(set.solution, config, platform);
                set.solution.SolutionBuild.Build(true);
                JLog.PrintOut("build state: " + set.solution.SolutionBuild.BuildState.ToString());
                JLog.PrintOut("last build Info: " + (set.solution.SolutionBuild.LastBuildInfo == 1 ? "Fail" : "Success"));

                set.Close();
                return true;
            }
            public static OutputWindowPane GetBuildPane(OutputWindow outputWindow)
            {
                // 모든 OutputWindowPane을 반복하여 "Build" 패널 찾기
                foreach (OutputWindowPane pane in outputWindow.OutputWindowPanes)
                {
                    if (pane.Name == "Build")
                        return pane;
                }
                return null; // "Build" 패널을 찾지 못한 경우
            }
            public static bool AddProjectFolder(string projectFilterPath, string folder)
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(projectFilterPath);

                XmlNodeList itemGroupNodes = doc.DocumentElement.ChildNodes;
                XmlNode filterNode = null;
                foreach (XmlNode node in itemGroupNodes)
                {
                    if (node.ChildNodes.Count > 0 && node.ChildNodes[0].Name == "Filter")
                    {
                        filterNode = node;
                        break;
                    }
                }
                if (filterNode == null && itemGroupNodes.Count > 0)
                    filterNode = itemGroupNodes[0];

                if (filterNode == null)
                    return false;

                XmlElement srcFilter = doc.CreateElement("Filter");
                filterNode.AppendChild(srcFilter);
                srcFilter.SetAttribute("Include", folder);

                XmlElement srcGuid = doc.CreateElement("UniqueIdentifier");
                srcFilter.AppendChild(srcGuid);
                srcGuid.InnerText = "{" + Guid.NewGuid().ToString() + "}";

                foreach (XmlElement element in doc.SelectNodes("//*[namespace-uri()='']"))
                {
                    if (element.HasAttribute("xmlns"))
                    {
                        element.RemoveAttribute("xmlns");
                    }
                }

                doc.Save(projectFilterPath);
                return true;
            }
            public static bool ModifyMetaData(in JModProjConfigInfo info)
            {
                ProjectRootElement root = ProjectRootElement.Open(info.projectPath);
                foreach (ProjectItemDefinitionGroupElement groups in root.ItemDefinitionGroups)
                {
                    foreach (ProjectItemDefinitionElement element in groups.ItemDefinitions)
                    {
                        bool isHit = false;
                        foreach (ProjectMetadataElement meta in element.Metadata)
                        {
                            if (meta.Name.Equals(info.name))
                            { 
                                if (info.allowAddexistValue)
                                    meta.Value += info.value;
                                else
                                    meta.Value = info.value;

                                isHit = true;
                                if (!info.canFindAll)
                                    return true;
                            }
                        }
                        if (!isHit && info.allowAddIfFail)
                            element.AddMetadata(info.name, info.value);
                    }
                }
                root.Save(); 
                return info.allowAddIfFail;
            }
            public static bool ModifyProperty(in JModProjConfigInfo info)
            {
                ProjectRootElement root = ProjectRootElement.Open(info.projectPath);
                foreach (ProjectPropertyGroupElement groups in root.PropertyGroups)
                {
                    bool isHit = false;
                    foreach (ProjectPropertyElement property in groups.Properties)
                    {
                        if (property.Name.Equals(info.name))
                        {
                            if (info.allowAddexistValue)
                                property.Value += info.value;
                            else
                                property.Value = info.value;
                            isHit = true;
                            if (!info.canFindAll)
                                return true;
                        }
                    }
                    if (!isHit && info.allowAddIfFail)
                        groups.AddProperty(info.name, info.value);
                }
                root.Save();
                return info.allowAddIfFail;
            }
        }
    }
}
