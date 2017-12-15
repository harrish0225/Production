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

namespace H1ToTitle.ProcessLibrary
{
    public enum ConvertItem {
        global,
        mooncake,
        H1,
        Title,
        TitleReplace,
        TitleReplaceWithQuotation,
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
        H1ToTitle,
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
    //    virtual_machines,
    //    virtual_network,
    //}

    //public enum InvolvedService
    //{
    //    analysis_services,
    //    azure_resource_manager,
    //    cosmos_db,
    //    event_hubs,
    //    load_balancer,
    //    resiliency,
    //    service_fabric,
    //    site_recovery,
    //    sql_data_warehouse,
    //    sql_server_stretch_database,
    //    stream_analytics,
    //    traffic_manager,
    //    virtual_machines,
    //    virtual_network,
    //}

    public enum InvolvedService
    {
        virtual_machines,
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
                diskpath = CommonFun.GetConfigurationValue("RepositoryZHCNArticleDir", ref message);

                parentpath = string.Format("{0}\\{1}", diskpath, curtService.ToString().Replace('_', '-'));

                this.GetAllFilesInDirectory(parentpath);
                
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

        string warningMessage = "";
        public string WarningMessage { get; set; }


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
            for (int i =1; i <= para.Length - 1; i++)
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

                diskpath = CommonFun.GetConfigurationValue("RepositoryZHCNArticleDir", ref message);
                relativefile = GetRightFileName(para);
                this.Fullpath = string.Format(@"{0}\{1}\{2}", diskpath, relativefile, this.File);
                this.ArticleCategory = FileCategory.Article;

            }

            if (para[0].ToLower() == "includes")
            {

                diskpath = CommonFun.GetConfigurationValue("RepositoryZHCNIncludeDir", ref message);
                relativefile = GetRightFileName(para);
                this.Fullpath = string.Format(@"{0}\{2}", diskpath, relativefile, this.File);
                this.ArticleCategory = FileCategory.Includes;

            }

        }
       
        public FileCustomize()
        {
            this.CheckFileList = new ArrayList();
        }

        public FileCustomize(int id,string filename,string directory,string customizedate, ConvertCategory category)
        {
            this.Id = id;
            this.File = filename;
            string[] para = directory.Split('/');

            this.SetFullPathName(para);
            this.CustomizedDate = customizedate;
            this.ProcessCategory = category;
            this.WarningMessage = string.Empty;

            this.CheckFileList = new ArrayList();

        }

        public void RepalceParameter(ref string paraValue)
        {

            if (paraValue.Contains(ReplaceParam.CustimzedDate.ToString()))
            {
                paraValue = paraValue.Replace("{CustimzedDate}", this.CustomizedDate);
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
            Regex reg = null;

            //AuthorReplacement Section
            int iCount = 0;

            ConvertCategory category = this.ProcessCategory;

           


            

            //CLIReplacment Section
            if (category == ConvertCategory.CLIReplacement)
            {
                iCount = JUrl[ConvertCategory.CLIReplacement.ToString()].Count();

                for (int i = 0; i < iCount; i++)
                {
                    bGlobal = this.GetProcessConvertRule(ref JUrl, ConvertCategory.CLIReplacement, i, ConvertItem.global, ref urlGlobal);
                    bMooncake = this.GetProcessConvertRule(ref JUrl, ConvertCategory.CLIReplacement, i, ConvertItem.mooncake, ref urlMooncake);
                    if (bGlobal && bMooncake)
                    {
                        reg = new Regex(urlGlobal);
                        articleContent = reg.Replace(articleContent, urlMooncake);
                    }
                }
            }

            if (category == ConvertCategory.H1ToTitle)
            {
                string ruleH1 = string.Empty;
                string ruleTitle = string.Empty;
                string rultTitleReplace = string.Empty;

                string sH1 = string.Empty;
                string sTitle = string.Empty;
                string sTitleReplace = string.Empty;

                int contentOrigal = articleContent.Length;
                int contentConvert = 0;

                iCount = JUrl[ConvertCategory.H1ToTitle.ToString()].Count();

                Match match;
                for (int i = 0; i < iCount; i++)
                {
                    bool containQuotation = false;
                    bGlobal = this.GetProcessConvertRule(ref JUrl, ConvertCategory.H1ToTitle, i, ConvertItem.H1, ref ruleH1);
                    if (bGlobal==true)
                    {
                        match = Regex.Match(articleContent, ruleH1);
                        if(match.Groups.Count>0)
                        {
                           sH1 = match.Groups["h1"].ToString();
                            if(sH1.IndexOf("</a>")>=0)
                            {
                                sH1 = sH1.Substring(sH1.IndexOf("</a>") + 4);
                            }
                            sH1 = sH1.Trim('\r');
                        }
                    }

                    bMooncake = this.GetProcessConvertRule(ref JUrl, ConvertCategory.H1ToTitle, i, ConvertItem.Title, ref ruleTitle);
                    if (bMooncake == true)
                    {
                        match = Regex.Match(articleContent, ruleTitle);
                        if (match.Groups.Count > 0)
                        {
                            sTitle = match.Groups["title"].ToString();
                            if (sTitle.IndexOf('"') >= 0)
                            {
                                containQuotation = true;
                                
                            }
                            sTitle = sTitle.Trim('"');
                        }
                        
                    }


                    if (bGlobal && bMooncake)
                    {
                        if (sH1.Trim() != sTitle.Trim())
                        {
                            if (containQuotation == true)
                            {
                                bMooncake = this.GetProcessConvertRule(ref JUrl, ConvertCategory.H1ToTitle, i, ConvertItem.TitleReplaceWithQuotation, ref rultTitleReplace);
                            }
                            else
                            {
                                bMooncake = this.GetProcessConvertRule(ref JUrl, ConvertCategory.H1ToTitle, i, ConvertItem.TitleReplace, ref rultTitleReplace);
                            }
                               
                            if (bMooncake == true)
                            {
                                switch (sH1.Trim())
                                {
                                    case "":
                                    case "后续步骤":
                                        this.WarningMessage = String.Format("Convert H1 Description: {0} ", sH1);
                                        break;
                                    default:

                                        sTitleReplace = string.Format(rultTitleReplace, sH1);

                                        reg = new Regex(ruleTitle);
                                        articleContent = reg.Replace(articleContent, sTitleReplace);

                                        contentConvert = articleContent.Length;

                                        int contentDiff = Math.Abs(contentOrigal - contentConvert);
                                        if (contentDiff > 50)
                                        {
                                            this.WarningMessage = string.Format("Content Difference Count: {0}", contentDiff);
                                        }

                                        break;
                                    
                                }

                               


                            }
                            
                        }
                    }

                }
            }


            //articleContent += "\n";


        }

        public void GetAllFilesInDirectory(string parentPath)
        {
            string[] curtFiles = System.IO.Directory.GetFiles(parentPath, "*.md");
            this.CheckFileList.AddRange(curtFiles);
            string[] curtDirList = System.IO.Directory.GetDirectories(parentPath);

            for(int i=0; i<curtDirList.Length;i++)
            {
                this.GetAllFilesInDirectory(curtDirList[i]);
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
            string parentpath = CommonFun.GetConfigurationValue("RepositoryZHCNIncludeDir", ref message);

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

            while(parentInclude!=string.Empty )
            {
                foreach (string curtFile in fileList)
                {
                    //Console.WriteLine(string.Format("Tread[{0}] checking {1}", this.Id, curtFile));
                    try
                    {
                        curtCheckFile = curtFile;
                        
                        Mutex fileMutex = new Mutex(true, GetMetuxFileName(curtCheckFile));
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
                                filecontent += string.Format("\n<!--Not Available the {0} parent file {1} of includes file of {2}-->",idx, Path.GetFileName(curtCheckFile), Path.GetFileName(parentInclude));
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
            }else
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

            FileStream fs ;
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
                parentpath = string.Format("{0}\\{1}", diskpath, curtService.ToString().Replace('_', '-'));

                this.GetAllFilesInDirectory(parentpath);
                ArrayList fileList = this.CheckFileList;

                foreach (string curtFile in fileList)
                {
                    //Console.WriteLine(string.Format("Tread[{0}] checking {1}", this.Id, curtFile));
                    try
                    {
                        Mutex fileMutex = new Mutex(true, GetMetuxFileName(curtFile));
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
                                filecontent += string.Format("\n<!--Not Available the parent file of includes file of {0}-->", this.File);
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
                    catch(Exception ex)
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



        public bool GetProcessConvertRule(ref JObject JConvert, ConvertCategory category, int iIndex, ConvertItem key,ref string valReturn)
        {
            bool bProcess = false;

            try
            {
                valReturn = JConvert[category.ToString()][iIndex][key.ToString()].ToString();
                bProcess = true;
            }
            catch(Exception ex)
            {

            }

            return bProcess;

        }
        public void ProcessFileCustomize()
        {
            this.Status = ProcessStatus.Start;

            FileStream fs = new FileStream(this.Fullpath, FileMode.OpenOrCreate);

            StreamReader sr = null;
            StreamWriter sw = null;
            string error = "";
            try
            {
                sr = new StreamReader(fs);
                string fullcontent = sr.ReadToEnd();

                sr.Close();

                ConvertCategory category = this.ProcessCategory;
                Console.WriteLine(string.Format("Processing Thread[{0}] : {1}", this.Id, this.Fullpath));

                this.ProcessConvertJson(ref fullcontent);
                
                sw = new StreamWriter(this.Fullpath,false);
                sw.Write(fullcontent);
                sw.Flush();

            }
            catch(Exception ex)
            {
                error = ex.Message.ToString();
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
