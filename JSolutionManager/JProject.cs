using System;
using SystemIO = System.IO;
using System.Collections.Generic; 
using System.Xml;

using EnvDTE80;
using EnvDTE;

using JSolutionManager.Utility;
using Microsoft.Build.Construction;

namespace JSolutionManager
{ 
    /*
     * Project config type
     * Meta
     * Property
     */
    enum PROJECT_CONFIG_TYPE
    {
        ADD_META,
        ADD_PROPERTY,   
        COPY_OTHER_PROJECT_CONFIG
    } 
    class JProjectConfig
    {
        public const int parameterCount = 3;
        public List<(string type, string name, string value)> value { get; set; }
        public JProjectConfig()
        {
            value = new List<(string type, string name, string value)>();
        }
        public void AddConfig(List<string> command, int index)
        { 
            if (index + 2 >= command.Count)
                return;

            value.Add((command[index], command[index + 1], command[index + 2]));
        }
    }
    class JProject
    { 
        private static void CreateProjConfig(ref ProjectItemGroupElement group, string config, string platform)
        {
            ProjectItemElement c = group.AddItem("ProjectConfiguration", config + @"|" + platform);
            c.AddMetadata("Configuration", config);
            c.AddMetadata("Platform", platform);
        }
        private static void CreateConfig(ref ProjectRootElement root, string config, string platform)
        {
            bool isRelease = config.Equals("Release");
            ProjectPropertyGroupElement c = root.CreatePropertyGroupElement();
            root.AppendChild(c);
            c.Condition = "'$(Configuration)|$(Platform)'==" + "'" + config + @"|" + platform + "'";
            c.Label = "Configuration";
            c.AddProperty("ConfigurationType", "DynamicLibrary");
            c.AddProperty("UseDebugLibraries", isRelease ? "false" : "true");
            c.AddProperty("PlatformToolset", "v142");
            if (isRelease)
                c.AddProperty("WholeProgramOptimization", "true");
            c.AddProperty("CharacterSet", "Unicode");
        }
        private static void CreatePropertySheet(ref ProjectRootElement root, string config, string platform)
        {
            ProjectImportGroupElement importGroupElement = root.CreateImportGroupElement();
            root.AppendChild(importGroupElement);
            importGroupElement.Label = "PropertySheets";
            importGroupElement.Condition = "'$(Configuration)|$(Platform)'==" + "'" + config + @"|" + platform + "'";

            ProjectImportElement importElement = importGroupElement.AddImport("$(UserRootDir)\\Microsoft.Cpp.$(Platform).user.props");
            importElement.Condition = "exists('$(UserRootDir)\\Microsoft.Cpp.$(Platform).user.props')";
            importElement.Label = "LocalAppDataPlatform";
        }
        private static void CreateLinkIncremental(ref ProjectRootElement root, string config, string platform)
        {
            ProjectPropertyGroupElement group = root.CreatePropertyGroupElement();
            root.AppendChild(group);
            group.Condition = "'$(Configuration)|$(Platform)'==" + "'" + config + @"|" + platform + "'";

            group.AddProperty("LinkIncremental", config.Equals("Release") ? "false" : "true");
        }
        private static void CreateCompileLinkOption(ref ProjectRootElement root, string config, string platform)
        {
            bool isRelease = config.Equals("Release");
            bool isX64 = platform.Equals("x64");
            ProjectItemDefinitionGroupElement itemGroup = root.CreateItemDefinitionGroupElement();
            root.AppendChild(itemGroup);
            itemGroup.Condition = "'$(Configuration)|$(Platform)'==" + "'" + config + @"|" + platform + "'";

            ProjectItemDefinitionElement compile = itemGroup.AddItemDefinition("ClCompile");
            compile.AddMetadata("WarningLevel", "Level3");
            if (isRelease)
            {
                compile.AddMetadata("FunctionLevelLinking", "true");
                compile.AddMetadata("IntrinsicFunctions", "true");
            }
            compile.AddMetadata("SDLCheck", "true");
            compile.AddMetadata("ConformanceMode", "true");
            compile.AddMetadata("LanguageStandard", "stdcpp17");

            ProjectItemDefinitionElement link = itemGroup.AddItemDefinition("Link");
            link.AddMetadata("SubSystem", "Windows");
            if (isRelease)
            {
                compile.AddMetadata("EnableCOMDATFolding", "true");
                compile.AddMetadata("OptimizeReferences", "true");
            }
            link.AddMetadata("GenerateDebugInformation", "true");
            link.AddMetadata("EnableUAC", "false");
        }
        private static void CreateProjectStructure(in string name, in string projPath, in string srcPath)
        {
            //CreateSubDirectory(projPath, srcPath);
            //수정필요
            JConstants.CreateFile(srcPath + name + ".h", "HeaderFile");
            JConstants.CreateFile(srcPath + name + ".cpp", " ");

            ProjectRootElement root = ProjectRootElement.Create(projPath + name + ".vcxproj");
            root.DefaultTargets = "Build";

            ProjectItemGroupElement projConfigGroup = root.CreateItemGroupElement();
            root.AppendChild(projConfigGroup);

            CreateProjConfig(ref projConfigGroup, "Debug", "Win32");
            CreateProjConfig(ref projConfigGroup, "Release", "Win32");
            CreateProjConfig(ref projConfigGroup, "Debug", "x64");
            CreateProjConfig(ref projConfigGroup, "Release", "x64");

            ProjectPropertyGroupElement globalGroup = root.CreatePropertyGroupElement();
            root.AppendChild(globalGroup);
            globalGroup.Label = "Globals";
            globalGroup.AddProperty("VCProjectVersion", JConstants.GetVsVersion(2));
            globalGroup.AddProperty("Keyword", "Win32Proj");
            globalGroup.AddProperty("ProjectGuid", "{" + Guid.NewGuid().ToString() + "}");
            globalGroup.AddProperty("RootNamespace", name);
            globalGroup.AddProperty("WindowsTargetPlatformVersion", "10.0");

            // Import 태그 추가
            ProjectImportElement importElement = root.CreateImportElement("$(VCTargetsPath)\\Microsoft.Cpp.Default.props");
            root.AppendChild(importElement);

            CreateConfig(ref root, "Debug", "Win32");
            CreateConfig(ref root, "Release", "Win32");
            CreateConfig(ref root, "Debug", "x64");
            CreateConfig(ref root, "Release", "x64");

            // Import 태그 추가 
            ProjectImportElement importElement2 = root.CreateImportElement("$(VCTargetsPath)\\Microsoft.Cpp.props");
            root.AppendChild(importElement2);

            ProjectImportGroupElement exSetting = root.CreateImportGroupElement();
            root.AppendChild(exSetting);
            exSetting.Label = "ExtensionSettings";

            ProjectImportGroupElement shared = root.CreateImportGroupElement();
            root.AppendChild(shared);
            shared.Label = "Shared";

            CreatePropertySheet(ref root, "Debug", "Win32");
            CreatePropertySheet(ref root, "Release", "Win32");
            CreatePropertySheet(ref root, "Debug", "x64");
            CreatePropertySheet(ref root, "Release", "x64");

            ProjectPropertyGroupElement macroGroup = root.CreatePropertyGroupElement();
            macroGroup.Label = "UserMacros";
            root.AppendChild(macroGroup);

            CreateLinkIncremental(ref root, "Debug", "Win32");
            CreateLinkIncremental(ref root, "Release", "Win32");
            CreateLinkIncremental(ref root, "Debug", "x64");
            CreateLinkIncremental(ref root, "Release", "x64");

            CreateCompileLinkOption(ref root, "Debug", "Win32");
            CreateCompileLinkOption(ref root, "Release", "Win32");
            CreateCompileLinkOption(ref root, "Debug", "x64");
            CreateCompileLinkOption(ref root, "Release", "x64");

            ProjectItemGroupElement headerGroup = root.CreateItemGroupElement();
            root.AppendChild(headerGroup);
            headerGroup.Label = JConstants.HeaderTagName();
            headerGroup.AddItem(JConstants.HeaderItemType(), "Src\\EngineDefined\\" + name + ".h");

            ProjectItemGroupElement cppGroup = root.CreateItemGroupElement();
            root.AppendChild(cppGroup);
            cppGroup.Label = JConstants.CppTagName();
            cppGroup.AddItem(JConstants.CppItemType(), "Src\\EngineDefined\\" + name + ".cpp");

            ProjectImportElement importElement3 = root.CreateImportElement("$(VCTargetsPath)\\Microsoft.Cpp.targets");
            root.AppendChild(importElement3);

            ProjectImportGroupElement extendTar = root.CreateImportGroupElement();
            root.AppendChild(extendTar);
            extendTar.Label = "ExtensionTargets";

            root.Save();
        }
        private static void CreateProjectFilter(in string name, in string projPath)
        {
            string filePath = projPath + name + ".vcxproj.filters";

            // XML 문서 로드
            XmlDocument doc = new XmlDocument();
            // 루트 요소 생성
            XmlElement rootElement = doc.CreateElement("Project");
            doc.AppendChild(rootElement);

            XmlElement filterGroup = doc.CreateElement(JConstants.ItemGroup());
            filterGroup.SetAttribute(JConstants.Label(), JConstants.FilterGroupName());
            rootElement.AppendChild(filterGroup);

            XmlElement srcFilter = doc.CreateElement("Filter");
            srcFilter.SetAttribute("Include", "Src");
            filterGroup.AppendChild(srcFilter);

            XmlElement srcGuid = doc.CreateElement("UniqueIdentifier");
            srcGuid.InnerText = Guid.NewGuid().ToString();
            srcFilter.AppendChild(srcGuid);

            XmlElement userFilter = doc.CreateElement("Filter");
            userFilter.SetAttribute("Include", "Src\\EngineDefined");
            filterGroup.AppendChild(userFilter);

            XmlElement userGuid = doc.CreateElement("UniqueIdentifier");
            userGuid.InnerText = Guid.NewGuid().ToString();
            userFilter.AppendChild(userGuid);

            XmlElement fileHeaderGroup = doc.CreateElement(JConstants.ItemGroup());
            fileHeaderGroup.SetAttribute(JConstants.Label(), JConstants.HeaderTagName());
            rootElement.AppendChild(fileHeaderGroup);

            XmlElement fileCppGroup = doc.CreateElement(JConstants.ItemGroup());
            fileCppGroup.SetAttribute(JConstants.Label(), JConstants.CppTagName());
            rootElement.AppendChild(fileCppGroup);

            string defaultHeader = "Src\\EngineDefined\\" + name + ".h";
            string defaultCpp = "Src\\EngineDefined\\" + name + ".cpp";

            XmlElement defaultHeaderInclude = doc.CreateElement(JConstants.HeaderItemType());
            defaultHeaderInclude.SetAttribute("Include", defaultHeader);
            fileHeaderGroup.AppendChild(defaultHeaderInclude);

            XmlElement defaultHeaderFilter = doc.CreateElement("Filter");
            defaultHeaderFilter.InnerText = "Src\\EngineDefined";
            defaultHeaderInclude.AppendChild(defaultHeaderFilter);

            XmlElement defaultCppInclude = doc.CreateElement(JConstants.CppItemType());
            defaultCppInclude.SetAttribute("Include", defaultCpp);
            fileCppGroup.AppendChild(defaultCppInclude);

            XmlElement defaultCppFilter = doc.CreateElement("Filter");
            defaultCppFilter.InnerText = "Src\\EngineDefined";
            defaultCppInclude.AppendChild(defaultCppFilter);

            doc.Save(filePath);
        }
        private static void SetProjectConfig(in string projectPath, JProjectConfig projConfig)
        { 
            foreach (var data in projConfig.value)
            {
                JModProjConfigInfo config = new JModProjConfigInfo(projectPath, data.name, data.value);
                bool res = false;
                if (data.type == "Meta")
                    res = JConstants.ModifyMetaData(config);
                else if (data.type == "Property")
                    res = JConstants.ModifyProperty(config);
                JLog.PrintOut((res ? "Success: " : "Fail: ") + data.type + " " + data.name + " " + data.value);
            }
        }
        private static void CreateSolution(in string name,
            in string folderPath,
            in string templatePath,
            in string buildConfig,       //Release or Debug
            in string platform,          //x86 or x64
            bool isSameLocation,
            JProjectConfig projConfig)
        {
            string projFolderPath = isSameLocation ? folderPath : folderPath + "\\" + name;
            string slnPath = folderPath + "\\" + name + ".sln";
            string projectPath = SystemIO.Path.Combine(projFolderPath, name + ".vcxproj");
            //string srcPath = projPath + "Src\\EngineDefined\\";
 
            if(SystemIO.File.Exists(slnPath))
            {
                JLog.PrintOut("Solution exist");
                return;
            }
            JSolutionDataSet set = new JSolutionDataSet();
            set.Intialize(); 
            set.Create(folderPath, name); 
            set.AddProjectFromTemplate(templatePath, projFolderPath, name); 

            //if (JConstants.ModifyMetaData(new JModProjConfigInfo(projectPath, "PrecompiledHeader", "NotUsing")))
          //      JLog.PrintOut("PrecompiledHeader NotUsing");
          //  if (JConstants.ModifyMetaData(new JModProjConfigInfo(projectPath, "PrecompiledHeaderFile", "")))
           //     JLog.PrintOut("PrecompiledHeaderFile Empty");
           // if (JConstants.ModifyMetaData(new JModProjConfigInfo(projectPath, "LanguageStandard", "stdcpp17")))
            //    JLog.PrintOut("LanguageStandard stdcpp17");
            //if (JConstants.ModifyMetaData(new JModProjConfigInfo(projectPath, "GenerateDebugInformation", "false")))
           //     JLog.PrintOut("GenerateDebugInformation false");
      
            SetProjectConfig(projectPath, projConfig); 
            JConstants.ActivateSolutionConfiguration(set.solution, buildConfig, platform);
            set.solution.SolutionBuild.Build(true);
            set.solution.SaveAs(slnPath);
            set.Close(false); 
        }
        public static bool CreateProject(in string name,
            in string folderPath,
            in string templatePath,
            in string buildConfig,
            in string platform,
            in string isSameLocation,
            JProjectConfig projConfig)
        { 
            if (templatePath == null)
            {
                JLog.PrintOut("Invalid template path ");
                return false;
            }
            CreateSolution(name, folderPath, templatePath, buildConfig, platform, isSameLocation.Equals("true"), projConfig);
            return true;
        }
        public static bool RemoveProjectItem(in string includePath,
            in string solutionPath,
            in string projectName)
        {
            JLog.PrintOut("Try remove project item");
            JLog.PrintOut("path " + includePath);

            JSolutionDataSet set = new JSolutionDataSet();
            set.Intialize();
            set.Open(solutionPath);

            EnvDTE.ProjectItem projItem = JConstants.FindProjectItem(set.solution, projectName, includePath);
            if (projItem == null)
                return false;
            projItem.Remove();

            set.Close();
            return true;
        }
        public static bool RemoveAllProjectItem(in string solutionPath, in string projectName)
        { 
            JSolutionDataSet set = new JSolutionDataSet();
            set.Intialize();
            set.Open(solutionPath);

            EnvDTE.Project proj = JConstants.FindProject(set.solution, projectName);
            if (proj == null)
                return false;

            int index = 1;
            while (proj.ProjectItems.Count >= index)
            {
                ProjectItem p = proj.ProjectItems.Item(index);
                if (SystemIO.Path.GetExtension(p.Name) == ".filters")
                    ++index;
                else
                    p.Remove();
            }

            set.Close();
            return true;
        }
        public static bool SetProjectConfig(in string solutionPath, string projectPath, JProjectConfig projConfig)
        { 
            DTE2 dte = (DTE2)Activator.CreateInstance(Type.GetTypeFromProgID("VisualStudio.DTE." + JConstants.GetVsVersion(2)));
            Solution2 solution = (Solution2)dte.Solution;
            solution.Open(solutionPath);

            SetProjectConfig(projectPath, projConfig);

            solution.Close(true);
            dte.Quit();
            return true;
        }

        public static void RestoreProjectUseFilter(in string name, in string folderPath)
        {
            string projFolderPath = folderPath + "\\" + name;
            string slnPath = folderPath + "\\" + name + ".sln";
            string projectPath = SystemIO.Path.Combine(projFolderPath, name + ".vcxproj");
            string projectFilterPath = SystemIO.Path.Combine(projFolderPath, name + ".vcxproj.filters");

            XmlDocument doc = new XmlDocument();
            doc.Load(projectFilterPath);
            XmlNode root = doc.DocumentElement;

            DTE2 dte = (DTE2)Activator.CreateInstance(Type.GetTypeFromProgID("VisualStudio.DTE." + JConstants.GetVsVersion(2)));
            Solution2 solution = (Solution2)dte.Solution;
            solution.Open(slnPath);

            AddDirectoryFilterToProj(solution, root, name);
            solution.Close(true);
            dte.Quit();
        }
        static void AddDirectoryFilterToProj(Solution2 sln, XmlNode node, string projName)
        {
            if (node.NodeType == XmlNodeType.Element)
            {
                if (node.Attributes.Count > 0)
                {
                    string fullPath = node.Attributes[0].Value;
                    JLog.PrintOut("Try Add: " + fullPath);
                    if (JConstants.FindProjectItem(sln, projName, fullPath) == null)
                        JLog.PrintOut("Exist");
                    else
                    {
                        if (node.Name == "Filter" && node.Attributes[0].Name == "Include")
                        {
                            string folderPath = SystemIO.Path.GetDirectoryName(fullPath);
                            string name = SystemIO.Path.GetFileName(fullPath);
                            EnvDTE.Project proj = JConstants.FindProject(sln, projName);
                            EnvDTE.ProjectItem projItem = JConstants.FindProjectItem(sln, projName, folderPath);

                            if (projItem != null)
                            {
                                ProjectItems pItem = projItem.ProjectItems;
                                pItem.AddFolder(fullPath, EnvDTE.Constants.vsProjectItemKindVirtualFolder);
                                JLog.PrintOut("Success");
                            }
                            else if (proj != null)
                            {
                                ProjectItems pItem = proj.ProjectItems;
                                pItem.AddFolder(fullPath, EnvDTE.Constants.vsProjectItemKindVirtualFolder);
                                JLog.PrintOut("Success");
                            }
                            else
                                JLog.PrintOut("Fail");
                        }
                        else if (node.Name == "ClInclude" && node.Attributes[0].Name == "Include")
                        {
                            var childNode = node.ChildNodes[0];
                            EnvDTE.Project proj = JConstants.FindProject(sln, projName);
                            EnvDTE.ProjectItem projItem = JConstants.FindProjectItem(sln, projName, childNode.InnerText);

                            if (projItem != null)
                            {
                                ProjectItems pItem = projItem.ProjectItems;
                                pItem.AddFromFile(fullPath);
                                JLog.PrintOut("Success");
                            }
                            else if (proj != null)
                            {
                                ProjectItems pItem = proj.ProjectItems;
                                pItem.AddFromFile(fullPath);
                                JLog.PrintOut("Success");
                            }
                            else
                                JLog.PrintOut("Fail");
                        }
                        else if (node.Name == "ClCompile" && node.Attributes[0].Name == "Include")
                        {
                            var childNode = node.ChildNodes[0];
                            EnvDTE.Project proj = JConstants.FindProject(sln, projName);
                            EnvDTE.ProjectItem projItem = JConstants.FindProjectItem(sln, projName, childNode.InnerText);

                            if (projItem != null)
                            {
                                ProjectItems pItem = projItem.ProjectItems;
                                pItem.AddFromFile(fullPath);
                                JLog.PrintOut("Success");
                            }
                            else if (proj != null)
                            {
                                ProjectItems pItem = proj.ProjectItems;
                                pItem.AddFromFile(fullPath);
                                JLog.PrintOut("Success");
                            }
                            else
                                JLog.PrintOut("Fail");
                        }
                    }
                }
            }
            foreach (XmlNode childNode in node.ChildNodes)
            {
                AddDirectoryFilterToProj(sln, childNode, projName);
            }
        }
    }
}
