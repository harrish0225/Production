using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using System.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading;


namespace AuthorCustmization.ProcessLibrary
{
    public enum ConvertItem {
        global,
        mooncake,
        service,
    }

    public enum CommandPara
    {
        Null,
        Servcie,
        Customize,
        VerifyFail,
    }

    public enum ConvertCategory
    {
        ALL,
        AuthorReplacement,
        URLReplacement,
        URLCorrection,
        FindArticle,
        IncludeParentFile,
        CLIReplacement,
        ToolReplacement,
        MDFileCorrection,
        YMLFileCorrection
    }

    public enum CustomizedCategory
    {
        CustomizedByService,
        CustomizedByFile,
    }

    public enum ProcessStatus
    {
        UnDefine,
        Start,
        Process,
        Complete,
    }

    //public enum InvolvedService
    //{
    //    //analysis_services,
    //    articles,
    //    includes,

    //}

    public enum InvolvedService
    {
        analysis_services,
        aks,
        azure_resource_manager,
        cosmos_db,
        container_registry,
        firewall,
        service_fabric,
        site_recovery,
        sql_server_stretch_database,
        traffic_manager,
        virtual_machines,
        virtual_network,
        virtual_wan,
        includes,
        articles,
    }

    public enum ReplaceParam
    {
        CustimzedDate,
    }

    public enum FileCategory
    {
        Article,
        Includes,
    }

    public class CollectAllFileByService : FileCustomize
    {

        public ArrayList GetAllFileByService()
        {
            ArrayList fileList = new ArrayList();
            string message = string.Empty;
            string diskpath = string.Empty;
            string parentpath = string.Empty;

            if (this.CheckFileList != null && this.CheckFileList.Count > 0)
            {
                this.CheckFileList = new ArrayList();
            }

            foreach (InvolvedService curtService in Enum.GetValues(typeof(InvolvedService)))
            {
                if (curtService.ToString().ToLower() == "includes".ToLower())
                {
                    diskpath = CommonFun.GetConfigurationValue("RepositoryENUSIncludeDir", ref message);
                    parentpath = string.Format("{0}", diskpath);
                }
                else
                {
                    diskpath = CommonFun.GetConfigurationValue("RepositoryENUSArticleDir", ref message);
                    parentpath = string.Format("{0}\\{1}", diskpath, curtService.ToString().Replace('_', '-'));
                }

                this.GetAllFilesInDirectory(parentpath);

            }

            fileList = this.CheckFileList;
            return fileList;
        }

        public ArrayList GetAllFileByServiceWithCustomziedate(string customizedate)
        {
            ArrayList fileList = new ArrayList();
            string message = string.Empty;
            string diskpath = string.Empty;
            string parentpath = string.Empty;

            if (this.CheckFileList != null && this.CheckFileList.Count > 0)
            {
                this.CheckFileList = new ArrayList();
            }

            foreach (InvolvedService curtService in Enum.GetValues(typeof(InvolvedService)))
            {
                if (curtService.ToString().ToLower() == "articles".ToLower())
                {
                    //Did not collect the articles in the <repository-parth>/articles/ direction
                    break;
                }
                if (curtService.ToString().ToLower() == "includes".ToLower())
                {
                    diskpath = CommonFun.GetConfigurationValue("GlobalIncludeDir", ref message);
                    parentpath = string.Format("{0}", diskpath);
                }
                else
                {
                    diskpath = CommonFun.GetConfigurationValue("GlobalArticleDir", ref message);
                    parentpath = string.Format("{0}\\{1}", diskpath, curtService.ToString().Replace('_', '-'));
                }

                this.GetAllFilesInDirectoryWithCustomizedate(parentpath, customizedate);

            }

            fileList = this.CheckFileList;
            return fileList;
        }

    }

    public class FileCustomize
    {
        int id = 0;
        public int Id { get; set; }

        string file = "";
        public string File { get; set; }

        string directory = "";
        public string Directory { get; set; }

        string fullpath = "";
        public string Fullpath { get; set; }

        string customizedDate = "";
        public string CustomizedDate { get; set; }

        ProcessStatus status = ProcessStatus.UnDefine;
        public ProcessStatus Status { get; set; }

        string parentFile = "";
        public string ParentFile { get; set; }

        string parentIncludFile = "";
        public string ParentIncludeFile { get; set; }


        FileCategory articleCategory = FileCategory.Article;
        public FileCategory ArticleCategory { get; set; }

        ArrayList checkFileList = new ArrayList();
        public ArrayList CheckFileList { get; set; }


        ConvertCategory processCategory = ConvertCategory.AuthorReplacement;
        public ConvertCategory ProcessCategory { get; set; }

        public static Object ObjJason = new Object();

        public string GetRightFileName(string[] para)
        {
            string rightname = "";
            for (int i = 1; i <= para.Length - 1; i++)
            {
                rightname = string.Format(@"{0}\{1}", rightname, para[i]);
            }
            rightname = rightname.Trim(new Char[] { '\\' });
            return rightname;
        }

        public void SetFullPathName(string[] para)
        {

            string diskpath = "";
            string message = "";
            string relativefile = "";

            if (para[0].ToLower() == "articles")
            {

                diskpath = CommonFun.GetConfigurationValue("GlobalArticleDir", ref message);
                relativefile = GetRightFileName(para);
                this.Fullpath = string.Format(@"{0}\{1}\{2}", diskpath, relativefile, this.File);
                this.ArticleCategory = FileCategory.Article;

            }

            if (para[0].ToLower() == "includes")
            {

                diskpath = CommonFun.GetConfigurationValue("GlobalIncludeDir", ref message);
                relativefile = GetRightFileName(para);
                this.Fullpath = string.Format(@"{0}\{2}", diskpath, relativefile, this.File);
                this.ArticleCategory = FileCategory.Includes;

            }

        }

        public FileCustomize()
        {
            this.CheckFileList = new ArrayList();
        }

        public FileCustomize(int id, string filename, string directory, string customizedate, ConvertCategory category)
        {
            this.Id = id;
            this.File = filename;
            string[] para = directory.Split('/');

            this.SetFullPathName(para);
            this.CustomizedDate = customizedate;
            this.ProcessCategory = category;

            this.CheckFileList = new ArrayList();

        }

        public void RepalceParameter(ref string paraValue)
        {

            if (paraValue.Contains(ReplaceParam.CustimzedDate.ToString()))
            {
                paraValue = paraValue.Replace("{CustimzedDate}", this.CustomizedDate);
            }

        }

        public void PrintMatchCollection(ref string articleContent, ref string urlGlobal,ref string urlMooncake)
        {

            MatchCollection matches;
            matches = Regex.Matches(articleContent, urlGlobal);

            for(int i=0; i<matches.Count; i++)
            {
                Console.WriteLine(string.Format("Result: {0}  Text: ", i, matches[i].Groups.ToString()));
            }
        }

        public void ProcessConvertJson(ref string articleContent)
        {

            this.Status = ProcessStatus.Process;

            string ruleJson = string.Empty;

            lock (ObjJason)
            {
                string ruleDir = AppDomain.CurrentDomain.BaseDirectory;
                ruleDir = Path.GetFullPath("../../ConvertJson/ConvertRule.json");
                FileStream fsRule = new FileStream(ruleDir, FileMode.Open);
                StreamReader sr = new StreamReader(fsRule);
                ruleJson = sr.ReadToEnd();
                sr.Close();
            }



            JObject JUrl = (JObject)JsonConvert.DeserializeObject(ruleJson);

            string urlGlobal = string.Empty;
            string urlMooncake = string.Empty;
            bool bGlobal = false;
            bool bMooncake = false;
            bool bValidService = false;

            Regex reg = null;

            //AuthorReplacement Section
            int iCount = 0;

            ConvertCategory category = this.ProcessCategory;

            if (category == ConvertCategory.ALL || category == ConvertCategory.FindArticle)
            {
                //Empth code to find the articles in Source Tree.
                articleContent += "\n";
            }

#if DEBUG
            DateTime dStart = DateTime.Now;
            DateTime dEnd = DateTime.Now;
            string sTakeTime = string.Empty;
#endif

            string[] fileKey = this.Fullpath.Split('\\');
            int iCheckStart = 0;
            for (int i = 0; i <= fileKey.Length - 1; i++)
            {
                switch (fileKey[i].ToLower())
                {
                    case "articles":
                    case "includes":
                        iCheckStart = i;
                        break;

                }
            }


            if (category == ConvertCategory.ALL || category == ConvertCategory.AuthorReplacement)
            {
                iCount = JUrl[ConvertCategory.AuthorReplacement.ToString()].Count();

                for (int i = 0; i < iCount; i++)
                {
                    bValidService = this.GetProcessValidService(ref JUrl, ConvertCategory.AuthorReplacement, i, ConvertItem.service, fileKey, iCheckStart, this.File);
                    if (bValidService == false)
                    {
                        continue;
                    }

                    bGlobal = this.GetProcessConvertRule(ref JUrl, ConvertCategory.AuthorReplacement, i, ConvertItem.global, ref urlGlobal);
                    bMooncake = this.GetProcessConvertRule(ref JUrl, ConvertCategory.AuthorReplacement, i, ConvertItem.mooncake, ref urlMooncake);
                    if (bGlobal && bMooncake)
                    {
                        this.RepalceParameter(ref urlMooncake);
                        reg = new Regex(urlGlobal, RegexOptions.Multiline);
                        articleContent = reg.Replace(articleContent, urlMooncake);
                    }

#if DEBUG
                    dEnd = DateTime.Now;
                    sTakeTime = CommonFun.DateDiff(ref dEnd, ref dStart);
                    Console.WriteLine(string.Format("{0}{1} Regular Express {2} --> {3} takes\t{4}", ConvertCategory.AuthorReplacement.ToString(), i, urlGlobal, urlMooncake, sTakeTime));
#endif
                }
            }



            //URLReplacement Section
            if (category == ConvertCategory.ALL || category == ConvertCategory.URLReplacement)
            {
                iCount = JUrl[ConvertCategory.URLReplacement.ToString()].Count();

                for (int i = 0; i < iCount; i++)
                {
                    bValidService = this.GetProcessValidService(ref JUrl, ConvertCategory.URLReplacement, i, ConvertItem.service, fileKey, iCheckStart, this.File);
                    if (bValidService == false)
                    {
                        continue;
                    }

                    bGlobal = this.GetProcessConvertRule(ref JUrl, ConvertCategory.URLReplacement, i, ConvertItem.global, ref urlGlobal);
                    bMooncake = this.GetProcessConvertRule(ref JUrl, ConvertCategory.URLReplacement, i, ConvertItem.mooncake, ref urlMooncake);

                    if (bGlobal && bMooncake)
                    {

                        reg = new Regex(urlGlobal);
                        articleContent = reg.Replace(articleContent, urlMooncake);
                    }
#if DEBUG
                    dEnd = DateTime.Now;
                    sTakeTime = CommonFun.DateDiff(ref dEnd, ref dStart);
                    Console.WriteLine(string.Format("{0}{1} Regular Express {2} --> {3} takes\t{4}", ConvertCategory.URLReplacement.ToString(), i, urlGlobal, urlMooncake, sTakeTime));
#endif
                }
            }


            //URLCorrection Section
            if (category == ConvertCategory.ALL || category == ConvertCategory.URLCorrection)
            {
                iCount = JUrl[ConvertCategory.URLCorrection.ToString()].Count();

                for (int i = 0; i < iCount; i++)
                {
                    bValidService = this.GetProcessValidService(ref JUrl, ConvertCategory.URLCorrection, i, ConvertItem.service, fileKey, iCheckStart, this.File);
                    if (bValidService == false)
                    {
                        continue;
                    }

                    bGlobal = this.GetProcessConvertRule(ref JUrl, ConvertCategory.URLCorrection, i, ConvertItem.global, ref urlGlobal);
                    bMooncake = this.GetProcessConvertRule(ref JUrl, ConvertCategory.URLCorrection, i, ConvertItem.mooncake, ref urlMooncake);
                    //bValidService = this.GetProcessConvertRuleValidService(ref JUrl, ConvertCategory.URLReplacement, i, ConvertItem.validservice, this.Fullpath);
                    if (bGlobal && bMooncake)
                    {
                        reg = new Regex(urlGlobal);
                        articleContent = reg.Replace(articleContent, urlMooncake);
                    }
#if DEBUG
                    dEnd = DateTime.Now;
                    sTakeTime = CommonFun.DateDiff(ref dEnd, ref dStart);
                    Console.WriteLine(string.Format("{0}{1} Regular Express {2} --> {3} takes\t{4}", ConvertCategory.URLCorrection.ToString(), i, urlGlobal, urlMooncake, sTakeTime));
#endif
                }

                //For only md File

                string filePostfix = this.File.Trim().Substring(this.File.Trim().Length - 3).ToLower();

                if (filePostfix == ".md")
                {
                    iCount = JUrl[ConvertCategory.MDFileCorrection.ToString()].Count();

                    for (int i = 0; i < iCount; i++)
                    {
                        bValidService = this.GetProcessValidService(ref JUrl, ConvertCategory.MDFileCorrection, i, ConvertItem.service, fileKey, iCheckStart, this.File);
                        if (bValidService == false)
                        {
                            continue;
                        }

                        bGlobal = this.GetProcessConvertRule(ref JUrl, ConvertCategory.MDFileCorrection, i, ConvertItem.global, ref urlGlobal);
                        bMooncake = this.GetProcessConvertRule(ref JUrl, ConvertCategory.MDFileCorrection, i, ConvertItem.mooncake, ref urlMooncake);
                        //bValidService = this.GetProcessConvertRuleValidService(ref JUrl, ConvertCategory.URLReplacement, i, ConvertItem.validservice, this.Fullpath);
                        if (bGlobal && bMooncake)
                        {
                            reg = new Regex(urlGlobal);
                            articleContent = reg.Replace(articleContent, urlMooncake);
                        }
#if DEBUG
                        dEnd = DateTime.Now;
                        sTakeTime = CommonFun.DateDiff(ref dEnd, ref dStart);
                        Console.WriteLine(string.Format("{0}{1} Regular Express {2} --> {3} takes\t{4}", ConvertCategory.MDFileCorrection.ToString(), i, urlGlobal, urlMooncake, sTakeTime));
#endif
                    }

                }

                if (filePostfix == "yml")
                {
                    //For only yml File
                    iCount = JUrl[ConvertCategory.YMLFileCorrection.ToString()].Count();

                    for (int i = 0; i < iCount; i++)
                    {
                        bValidService = this.GetProcessValidService(ref JUrl, ConvertCategory.YMLFileCorrection, i, ConvertItem.service, fileKey, iCheckStart, this.File);
                        if (bValidService == false)
                        {
                            continue;
                        }

                        bGlobal = this.GetProcessConvertRule(ref JUrl, ConvertCategory.YMLFileCorrection, i, ConvertItem.global, ref urlGlobal);
                        bMooncake = this.GetProcessConvertRule(ref JUrl, ConvertCategory.YMLFileCorrection, i, ConvertItem.mooncake, ref urlMooncake);
                        //bValidService = this.GetProcessConvertRuleValidService(ref JUrl, ConvertCategory.URLReplacement, i, ConvertItem.validservice, this.Fullpath);
                        if (bGlobal && bMooncake)
                        {
                            reg = new Regex(urlGlobal);
                            articleContent = reg.Replace(articleContent, urlMooncake);
                        }
#if DEBUG
                        dEnd = DateTime.Now;
                        sTakeTime = CommonFun.DateDiff(ref dEnd, ref dStart);
                        Console.WriteLine(string.Format("{0}{1} Regular Express {2} --> {3} takes\t{4}", ConvertCategory.YMLFileCorrection.ToString(), i, urlGlobal, urlMooncake, sTakeTime));
#endif
                    }

                }



            }

            //ToolReplacement Section
            if (category == ConvertCategory.ALL || category == ConvertCategory.ToolReplacement)
            {
                iCount = JUrl[ConvertCategory.ToolReplacement.ToString()].Count();

                for (int i = 0; i < iCount; i++)
                {
                    bValidService = this.GetProcessValidService(ref JUrl, ConvertCategory.ToolReplacement, i, ConvertItem.service, fileKey, iCheckStart, this.File);
                    if (bValidService == false)
                    {
                        continue;
                    }

                    bGlobal = this.GetProcessConvertRule(ref JUrl, ConvertCategory.ToolReplacement, i, ConvertItem.global, ref urlGlobal);
                    bMooncake = this.GetProcessConvertRule(ref JUrl, ConvertCategory.ToolReplacement, i, ConvertItem.mooncake, ref urlMooncake);

                    if (bGlobal && bMooncake)
                    {
#if DEBUG
                        this.PrintMatchCollection(ref articleContent, ref urlGlobal, ref urlMooncake);
#endif
                        reg = new Regex(urlGlobal);
                        articleContent = reg.Replace(articleContent, urlMooncake);
                    }
#if DEBUG
                    dEnd = DateTime.Now;
                    sTakeTime = CommonFun.DateDiff(ref dEnd, ref dStart);
                    Console.WriteLine(string.Format("{0}{1} Regular Express {2} --> {3} takes\t{4}", ConvertCategory.ToolReplacement.ToString(), i, urlGlobal, urlMooncake, sTakeTime));
#endif
                }
            }

            //Find Reference file of Include File
            if (category == ConvertCategory.ALL || category == ConvertCategory.IncludeParentFile)
            {
                if (this.ArticleCategory == FileCategory.Includes)
                {
                    if (this.CheckFileList == null && this.CheckFileList.Count > 0)
                    {
                        this.CheckFileList.Clear();
                    }
                    this.ParentFile = this.FindParentOfIncludeFile();
                }
            }

            //articleContent += "\n";


        }

        public void GetAllFilesInDirectory(string parentPath)
        {
            if(System.IO.Directory.Exists(parentPath)==true)
            {
                string[] curtFiles = System.IO.Directory.GetFiles(parentPath, "*.md");
                this.CheckFileList.AddRange(curtFiles);
                string[] curtymlFiles = System.IO.Directory.GetFiles(parentPath, "*.yml");
                this.CheckFileList.AddRange(curtymlFiles);


                string[] directoryson = parentPath.Replace("\\", "/").Split('/');
                switch(directoryson[directoryson.Length-1])
                {
                    case "articles":
                        // We just collection the files in repostorypath/articles, the involved service did not collection.
                        break;
                    default:
                        string[] curtDirList = System.IO.Directory.GetDirectories(parentPath);

                        for (int i = 0; i < curtDirList.Length; i++)
                        {
                            this.GetAllFilesInDirectory(curtDirList[i]);
                        }
                        break;
                }

               

            }
            
        }

        public void GetAllFilesInDirectoryWithCustomizedate(string parentPath, string customizedate)
        {
            string[] curtFiles = System.IO.Directory.GetFiles(parentPath, "*.md");
            string filename = string.Empty;
            string directory = string.Empty;

            string curtValue = string.Empty;
            string[] curtKey = new string[] { };

            int iStartIdx = 0;
            
            if(curtFiles.Length>0)
            {
                curtValue = curtFiles[0].Replace("\\", "/");
                curtKey = curtValue.Split('/');

                for(int i=0; i<curtKey.Length;i++)
                {
                    if(curtKey[i].ToUpper()== "ARTICLES" || curtKey[i].ToUpper() == "INCLUDES")
                    {
                        iStartIdx = i;
                        break;
                    }
                }

                for (int i = 0; i < curtFiles.Length; i++)
                {
                    curtValue = curtFiles[i].Replace("\\", "/");
                    curtKey = curtValue.Split('/');
                    directory = string.Empty;
                    for (int j = iStartIdx; j < curtKey.Length - 1; j++)
                    {
                        directory += string.Format("/{0}", curtKey[j]);
                    }
                    directory = directory.Trim('/');
                    filename = curtKey[curtKey.Length - 1];

                    this.CheckFileList.Add(new string[] { filename, directory, customizedate });
                }
            }

            
            


            string[] curtymlFiles = System.IO.Directory.GetFiles(parentPath, "*.yml");

            if (curtFiles.Length > 0)
            {
                for (int i = 0; i < curtymlFiles.Length; i++)
                {
                    curtValue = curtymlFiles[i].Replace("\\", "/");
                    curtKey = curtValue.Split('/');
                    directory = string.Empty;
                    for (int j = iStartIdx; j < curtKey.Length - 1; j++)
                    {
                        directory += string.Format("/{0}", curtKey[j]);
                    }
                    directory = directory.Trim('/');
                    filename = curtKey[curtKey.Length - 1];

                    this.CheckFileList.Add(new string[] { filename, directory, customizedate });
                }
            }

            string[] curtDirList = System.IO.Directory.GetDirectories(parentPath);

            for (int i = 0; i < curtDirList.Length; i++)
            {
                this.GetAllFilesInDirectoryWithCustomizedate(curtDirList[i], customizedate);
            }

        }

        public string GetMetuxFileName(string filepath)
        {
            string fileName = string.Empty;
            fileName = filepath.Replace(":", "_").Replace(@"\", "_").Replace("/", "_");
            return fileName;
        }

        public string GetParentIncludeFileOfIncludeFile()
        {
            string parentInclude = string.Empty;
            string message = string.Empty;
            string parentpath = CommonFun.GetConfigurationValue("GlobalIncludeDir", ref message);

            string filecontent = string.Empty;
            string regRule = "\\[\\!INCLUDE \\[([\\S|\\s]+)(\\.md)?\\]\\(((\\.\\.\\/)*)includes\\/{0}\\)\\]";
            string curtCheckFile = string.Empty;

            Regex reg;

            FileStream fs;
            StreamReader sr;
            StreamWriter sw;
            bool findFile = false;

            if (this.CheckFileList != null && this.CheckFileList.Count > 0)
            {
                this.CheckFileList = new ArrayList();
            }


            this.GetAllFilesInDirectory(parentpath);
            ArrayList fileList = this.CheckFileList;

            int idx = 1;
            parentInclude = this.Fullpath;

            while (parentInclude != string.Empty)
            {
                foreach (string curtFile in fileList)
                {
                    //Console.WriteLine(string.Format("Tread[{0}] checking {1}", this.Id, curtFile));
                    try
                    {
                        curtCheckFile = curtFile;

                        Mutex fileMutex = new Mutex(false, GetMetuxFileName(curtCheckFile));
                        fileMutex.WaitOne();
                        fs = new FileStream(curtCheckFile, FileMode.OpenOrCreate);

                        try
                        {
                            sr = new StreamReader(fs);
                            filecontent = sr.ReadToEnd();
                            sr.Close();

                            reg = new Regex(string.Format(regRule, Path.GetFileName(parentInclude).Replace(".", "\\.")));

                            if (reg.IsMatch(filecontent))
                            {
                                sw = new StreamWriter(curtCheckFile, false);
                                filecontent += string.Format("\n<!--Not Available the {0} parent file {1} of includes file of {2}-->", idx, Path.GetFileName(curtCheckFile), Path.GetFileName(parentInclude));
                                filecontent += string.Format("\n<!--ms.date:{0}-->", this.CustomizedDate);
                                sw.Write(filecontent);
                                sw.Flush();
                                sw.Close();
                                parentInclude = curtCheckFile;
                                this.ParentIncludeFile = Path.GetFileName(parentInclude);

                                findFile = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message.ToString());
                        }
                        finally
                        {
                            fileMutex.ReleaseMutex();
                            if (fs != null)
                            {
                                fs.Close();
                                fs = null;

                            }
                        }
                        if (findFile == true)
                        {
                            break;
                        }
                    }
                    catch (Exception ex)
                    {

                    }

                }

                if (findFile == false)
                {
                    parentInclude = string.Empty;
                }

            }

            if (this.ParentIncludeFile == string.Empty)
            {
                parentInclude = this.File;
            }
            else
            {
                parentInclude = this.ParentIncludeFile;
            }

            return parentInclude;
        }

        public string FindParentOfIncludeFile()
        {

            string parentFile = string.Empty;
            //string parentFile = this.GetParentIncludeFileOfIncludeFile();

            string message = string.Empty;
            string diskpath = CommonFun.GetConfigurationValue("GlobalArticleDir", ref message);
            string parentpath = string.Empty;
            string filecontent = string.Empty;
            string regRule = "\\[\\!INCLUDE \\[([\\S|\\s]+)(\\.md)?\\]\\(((\\.\\.\\/)*)includes\\/{0}\\)\\]";
            //string regRule = "\\[\\!INCLUDE \\[(\\S+)(\\.md)?\\]\\(((\\.\\.\\/)*)includes\\/{0}\\)\\]";
            Regex reg;

            FileStream fs;
            StreamReader sr;
            StreamWriter sw;
            bool findFile = false;


            foreach (InvolvedService curtService in Enum.GetValues(typeof(InvolvedService)))
            {
                if (this.CheckFileList != null && this.CheckFileList.Count > 0)
                {
                    this.CheckFileList = new ArrayList();
                }

                findFile = false;
                switch(curtService)
                {
                    case InvolvedService.articles:
                    case InvolvedService.includes:
                        parentpath = string.Format("{0}", diskpath);
                        break;
                    default:
                        parentpath = string.Format("{0}\\{1}", diskpath, curtService.ToString().Replace('_', '-'));
                        break;
                }
                

                this.GetAllFilesInDirectory(parentpath);
                ArrayList fileList = this.CheckFileList;

                foreach (string curtFile in fileList)
                {
                    //Console.WriteLine(string.Format("Tread[{0}] checking {1}", this.Id, curtFile));
                    try
                    {
                        Mutex fileMutex = new Mutex(false, GetMetuxFileName(curtFile));
                        fileMutex.WaitOne();
                        fs = new FileStream(curtFile, FileMode.OpenOrCreate);

                        try
                        {
                            sr = new StreamReader(fs);
                            filecontent = sr.ReadToEnd();
                            sr.Close();

                            reg = new Regex(string.Format(regRule, this.File.Replace(".", "\\.")));

                            if (reg.IsMatch(filecontent))
                            {
                                sw = new StreamWriter(curtFile, false);
                                filecontent += string.Format("\n<!--The parent file of includes file of {0}-->", this.File);
                                filecontent += string.Format("\n<!--ms.date:{0}-->", this.CustomizedDate);
                                sw.Write(filecontent);
                                sw.Flush();
                                sw.Close();
                                parentFile = curtFile;
                                findFile = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message.ToString());
                        }
                        finally
                        {
                            fileMutex.ReleaseMutex();
                            if (fs != null)
                            {
                                fs.Close();
                                fs = null;

                            }
                        }
                        if (findFile == true)
                        {
                            break;
                        }
                    }
                    catch (Exception ex)
                    {

                    }

                }

                if (findFile == true)
                {
                    break;
                }
            }

            return parentFile;

        }

        public bool GetProcessConvertRule(ref JObject JConvert, ConvertCategory category, int iIndex, ConvertItem key, ref string valReturn)
        {
            bool bProcess = false;
            try
            {
                valReturn = JConvert[category.ToString()][iIndex][key.ToString()].ToString();
                if (valReturn == "mysetting")
                {
                    return bProcess;
                }
                bProcess = true;
            }
            catch (Exception ex)
            {

            }

            return bProcess;

        }

        public bool CheckIsPatternfile(string[] fileKey, string newPattern)
        {
            bool isPattern = false;
            string fileName = fileKey[fileKey.Length - 1].ToLower();
            if (newPattern.Substring(0,1).ToLower()=="*")
            {
                isPattern = fileName.EndsWith(newPattern.Substring(1));
            }

            if (newPattern.Substring(0, 1).ToLower() == "!")
            {
                isPattern = fileName.EndsWith(newPattern.Substring(1));
            }
            return isPattern;
        }
        public bool GetProcessValidService(ref JObject JConvert, ConvertCategory category, int iIndex, ConvertItem key, string[] fileKey, int iCheckStart, string fileName)
        {
            bool bProcess = false;
            string valReturn = string.Empty;

            try
            {
                // service="ALL"
                valReturn = JConvert[category.ToString()][iIndex][key.ToString()].ToString().ToLower();

                bool bCheckALLPattern = false;
                string sFirstChar = "";

                string[] invloveService = valReturn.Split('|');
                for (int j = 0; j <= invloveService.Length - 1; j++)
                {
                    if(invloveService[j].ToLower().Substring(0,1)=="!")
                    {
                        bCheckALLPattern = true;
                        break;
                    }
                }


                for (int i = iCheckStart + 1; i <= fileKey.Length - 1; i++)
                {
                    for (int j = 0; j <= invloveService.Length - 1; j++)
                    {
                        sFirstChar = invloveService[j].ToLower().Substring(0, 1);

                        // Take most priority 
                        if (sFirstChar == "!")
                        {
                            bProcess = CheckIsPatternfile(fileKey, invloveService[j].ToLower());
                            if (bProcess == true)
                            {
                                bProcess = false;
                                return bProcess;
                            }
                        }

                        if (sFirstChar == "*")
                        {
                            bProcess = CheckIsPatternfile(fileKey, invloveService[j].ToLower());
                            if (bCheckALLPattern == false)
                            {
                                return bProcess;
                            }
                        }


                        if (invloveService[j].ToLower() == "all")
                        {
                            bProcess = true;
                            if (bCheckALLPattern == false)
                            {
                                return bProcess;
                            }
                        }

                        if (fileKey[i].ToLower() == invloveService[j].ToLower())
                        {
                            bProcess = true;
                            if(bCheckALLPattern==false)
                            {
                                return bProcess;
                            }
                            
                        }

                        //for check the files in the include directory.
                        if (invloveService[j].Length < fileName.Length && invloveService[j].ToLower() == fileName.Substring(0, invloveService[j].Length))
                        {
                            bProcess = true;
                            if (bCheckALLPattern == false)
                            {
                                return bProcess;
                            }
                        }
                    }

                }

            }
            catch (Exception ex)
            {
            }

            return bProcess;

        }

        public void ProcessFileCustomize()
        {
            this.Status = ProcessStatus.Start;

            FileStream fs = null;

            StreamReader sr = null;
            StreamWriter sw = null;
            string error = "";
            try
            {
                fs = new FileStream(this.Fullpath, FileMode.OpenOrCreate);
                sr = new StreamReader(fs);
                string fullcontent = sr.ReadToEnd();

                sr.Close();

                ConvertCategory category = this.ProcessCategory;
                Console.WriteLine(string.Format("Processing Thread[{0}] : {1}", this.Id, this.Fullpath));

                this.ProcessConvertJson(ref fullcontent);

                sw = new StreamWriter(this.Fullpath, false);
                sw.Write(fullcontent);
                sw.Flush();

            }
            catch (Exception ex)
            {
                error = ex.Message.ToString();
                CommonFun.GenerateErrorDownloadFile(ref error);

            }
            finally
            {
                if (sr != null)
                {
                    sr.Close();
                }

                if (sw != null)
                {
                    sw.Close();
                }

                if (fs != null)
                {
                    fs.Close();
                }
            }

            this.Status = ProcessStatus.Complete;
            Console.WriteLine("Check Status of the Thread[{0}] : {1}", this.Id, this.Status.ToString());


        }



    }

}




