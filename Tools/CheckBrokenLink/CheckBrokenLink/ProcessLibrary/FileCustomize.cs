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
using System.Net;

namespace CheckBrokenLink.ProcessLibrary
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
        CheckBrokenLinkByService,
        CheckBrokenLinkByFile,
    }

    public enum ConvertProcess
    {
        ShowResult,
        ShowHistory,
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

    public enum InvolvedService
    {
        analysis_services,
        azure_resource_manager,
        cosmos_db,
        event_hubs,
        load_balancer,
        resiliency,
        service_fabric,
        site_recovery,
        sql_data_warehouse,
        sql_server_stretch_database,
        stream_analytics,
        traffic_manager,
        virtual_machines,
        virtual_network,
    }

    //public enum InvolvedService
    //{
    //    virtual_machines,
    //}

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

        string brokenLink = "";
        public string BrokenLink { get; set; }

        ConvertCategory processCategory = ConvertCategory.AuthorReplacement;
        public ConvertCategory ProcessCategory { get; set; }

        ConvertProcess showHistory = ConvertProcess.ShowResult;
        public ConvertProcess ShowHistory { get; set; }

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

                diskpath = CommonFun.GetConfigurationValue("RepositoryENUSArticleDir", ref message);
                relativefile = GetRightFileName(para);
                this.Fullpath = string.Format(@"{0}\{1}\{2}", diskpath, relativefile, this.File);
                this.ArticleCategory = FileCategory.Article;

            }

            if (para[0].ToLower() == "includes")
            {

                diskpath = CommonFun.GetConfigurationValue("RepositoryENUSIncludeDir", ref message);
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

        public FileCustomize(int id, string filename, string directory, string customizedate, ConvertCategory category, ConvertProcess process)
        {
            this.Id = id;
            this.File = filename;
            string[] para = directory.Split('/');

            this.SetFullPathName(para);
            this.CustomizedDate = customizedate;
            this.ProcessCategory = category;
            this.WarningMessage = string.Empty;

            this.ShowHistory = process;

            this.CheckFileList = new ArrayList();


        }

        public void RepalceParameter(ref string paraValue)
        {

            if (paraValue.Contains(ReplaceParam.CustimzedDate.ToString()))
            {
                paraValue = paraValue.Replace("{CustimzedDate}", this.CustomizedDate);
            }

        }

        public void CheckMatches(MatchCollection matches, ref List<string> lstURL, ref string articleContent)
        {
            string filename = string.Empty;
            string checkdirectory = string.Empty;
            string checkfile = string.Empty;
            string error = string.Empty;

            Match existMath = null;

            string localpath = CommonFun.GetConfigurationValue("RepositoryENUSArticleDir", ref error);

            for (int i = 0; i < matches.Count; i++)
            {

                //First remove the current directory ./ when exists.
                filename = matches[i].Groups["mdfilename"].ToString().Trim();
                if (filename.Substring(0, 2) == "./")
                {
                    filename = filename.Substring(2);
                }
                //Get the current parent directory. 
                if (this.Fullpath.LastIndexOf("\\") > -1)
                {
                    checkdirectory = this.Fullpath.Substring(0, this.Fullpath.LastIndexOf("\\"));
                }


                //Step 1:  Select Http(s):// link 
                if (filename.StartsWith("http://") || filename.StartsWith("https://"))
                {
                    lstURL.Add(filename);
                    continue;
                }

                //Step 2: Select inner Archor tag
                if (filename.StartsWith("#") == true)
                {
                    this.CheckArchorInFile(ref articleContent, filename);
                    continue;
                }

                //Step 3: Select the outside Archor tag, omit due to there are exist some archor.
                if (filename.Contains("#") == true)
                {

                }


                //Step 4: Check the md and image file.
                if (filename.Substring(filename.Length - 3).ToLower().ToString() == ".md" ||
                   filename.Substring(filename.Length - 4).ToLower().ToString() == ".jpg" ||
                   filename.Substring(filename.Length - 4).ToLower().ToString() == ".svg" ||
                   filename.Substring(filename.Length - 4).ToLower().ToString() == ".png" ||
                   filename.Substring(filename.Length - 4).ToLower().ToString() == ".gif")

                {

                    //reward to parent directory when exists the ../
                    while (filename.IndexOf("../") > -1)
                    {
                        filename = filename.Substring(filename.IndexOf("../") + 3);
                        checkdirectory = checkdirectory.Substring(0, checkdirectory.LastIndexOf("\\"));
                    }

                    checkfile = string.Format("{0}\\{1}", checkdirectory, filename.Replace("/", "\\"));
                    if (System.IO.File.Exists(checkfile) == false)
                    {
                        this.BrokenLink += string.Format("{0} : missing.\n", checkfile);
                    }
                    else
                    {
                        if (this.ShowHistory == ConvertProcess.ShowHistory)
                        {
                            this.BrokenLink += string.Format("{0} : correct.\n", checkfile);
                        }
                    }

                    continue;
                }


                // Check the local path. 
                while (filename.IndexOf("../") > -1)
                {
                    filename = filename.Substring(filename.IndexOf("../") + 1);
                    checkdirectory = checkdirectory.Substring(0, checkdirectory.LastIndexOf("/"));
                }
                
                checkfile = string.Format("{0}\\{1}", checkdirectory, filename);
                if(checkfile.Contains("#"))    //we have check the .md file in preceding logical .
                {
                    string localfile = checkfile.Substring(0,checkfile.IndexOf("#")-1) + ".md";
                    string localarchor = checkfile.Substring(checkfile.IndexOf("#"));
                    this.CheckArchorInFile(localfile, localarchor);
                    continue;
                }

                //checkfile = checkfile.Replace(localpath, "https://docs.azure.cn/zh-cn/");
                lstURL.Add(filename);


            }
        }

        public void CheckArchorInFile(string filename, string archor)
        {
            
           
            
            try
            {
                Mutex fileMutex = null;
                string articleContent = string.Empty;

                fileMutex = new Mutex(true, GetMetuxFileName(filename));
                fileMutex.WaitOne();
                FileStream fs = new FileStream(filename, FileMode.Open);
                StreamReader sr = new StreamReader(fs);
                articleContent = sr.ReadToEnd();
                sr.Close();

                bool matchOK = false;

                string archPat = string.Format("<a[\\s]*(id|name)=(\'|\"){0}(\'|\")[\\s]*></a>", archor.TrimStart('#'));
                Match existMath = Regex.Match(articleContent, archPat);

                if (existMath.Length > 0)
                {
                    matchOK = true;
                }

                if(matchOK==false)
                {
                    archPat = string.Format("<a[\\s]*(id|name)=(\'|\"){0}(\'|\")[\\s]*/>", archor.TrimStart('#'));
                    existMath = Regex.Match(articleContent, archPat);
                    if (existMath.Length > 0)
                    {
                        matchOK = true;
                    }
                }

                if (matchOK == true )
                {
                    if (this.ShowHistory == ConvertProcess.ShowHistory)
                    {
                        this.BrokenLink += string.Format("{0}{1} : exist.\n", filename,archor);
                    }
                }
                else
                {
                    this.BrokenLink += string.Format("{0}{1} : missing.\n", filename,archor);
                }
            }

            catch (Exception ex)
            {
                this.BrokenLink += string.Format("Error : {0}{1} ->  {2}\n", filename,archor, ex.Message.ToString() );
            }
            finally
            {

            }

            
        }


        public void CheckArchorInFile(ref string articleContent, string archor)
        {
            
            try
            {

                bool matchOK = false;

                string archPat = string.Format("<a[\\s]*(id|name)=(\'|\"){0}(\'|\")[\\s]*></a>", archor.TrimStart('#'));
                Match existMath = Regex.Match(articleContent, archPat);

                if (existMath.Length > 0)
                {
                    matchOK = true;
                }

                if (matchOK == false)
                {
                    archPat = string.Format("<a[\\s]*(id|name)=(\'|\"){0}(\'|\")[\\s]*/>", archor.TrimStart('#'));
                    existMath = Regex.Match(articleContent, archPat);
                    if (existMath.Length > 0)
                    {
                        matchOK = true;
                    }
                }

                if (matchOK == true)
                {
                    if (this.ShowHistory == ConvertProcess.ShowHistory)
                    {
                        this.BrokenLink += string.Format("{0} : exist.\n", archor);
                    }
                }
                else
                {
                    this.BrokenLink += string.Format("{0} : missing.\n", archor);
                }
            }

            catch (Exception ex)
            {
                this.BrokenLink += string.Format("Error : {0} ->  {1}\n",  archor, ex.Message.ToString());
            }
            finally
            {

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

            ConvertCategory category = this.ProcessCategory;

            if (category == ConvertCategory.CheckBrokenLinkByFile || category== ConvertCategory.CheckBrokenLinkByService)
            {


                List<string> lstURL = new List<string>();

                MatchCollection matches;


                //string mdfilePatFirst = "[^(<!--)]\\[([^\\[\\]])*\\]([\\s]*)\\((?<mdfilename>[^\\(\\)]*)\\)";
                string mdfilePatFirst = "\\[([^\\[\\]])*\\]([\\s]*)\\((?<mdfilename>[^\\(\\)]*)\\)";
                matches = Regex.Matches(articleContent, mdfilePatFirst);

                this.CheckMatches(matches, ref lstURL, ref articleContent);

                string mdfilePatSecond = "\\[([^\\[\\]]*)\\]([\\s]*)\\:([\\s]*)(?<mdfilename>[^\\s]*)([\\s]*)";
                //string mdfilePatSecond = "[^(<!--)]\\[([^\\[\\]]*)\\]([\\s]*)\\:([\\s]*)(?<mdfilename>[^\\s]*)([\\s]*)";

                matches = Regex.Matches(articleContent, mdfilePatSecond);

                this.CheckMatches(matches, ref lstURL, ref articleContent);


                this.CheckAllLinks(lstURL);
               
                

            }


        }

        public void CheckAllLinks(List<string> urlList)
        {
            string error = string.Empty;
            string sValue = CommonFun.GetConfigurationValue("MaxHttpConnectionCount", ref error);
            if (error.Length > 0)
            {
                Console.WriteLine(error);
                return;

            }
            int iConCount = 0;
            bool converFlag = int.TryParse(sValue, out  iConCount);

            if(converFlag == false)
            {
                Console.WriteLine(string.Format("MaxHttpConnectionCount {0} is not a valid interge." , sValue));
                return;
            }

            System.Net.ServicePointManager.DefaultConnectionLimit = iConCount;
            

            HttpWebRequest req= null;
            HttpWebResponse resp = null;

            for (int i = 0; i < urlList.Count; i++)
            {
                try
                {
                    req = (HttpWebRequest)WebRequest.Create(urlList[i].ToString());
                    req.Method = "Get";
                    req.ContentType= "application/x-www-form-urlencoded; charset=UTF-8";
                    req.UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/45.0.2454.101 Safari/537.36";
                    req.Timeout = 2000;
                    req.AllowAutoRedirect = true;


                    resp = (HttpWebResponse)req.GetResponse();
                    //Console.WriteLine("checking " + urlList[i].ToString());
                    switch (resp.StatusCode)
                    {
                        case HttpStatusCode.OK:
                            if(this.ShowHistory== ConvertProcess.ShowHistory)
                            {
                                this.BrokenLink += string.Format("{0}:{1}\n", urlList[i].ToString(), resp.StatusCode.ToString());
                            }
                            break;
                        default:
                            this.BrokenLink += string.Format("{0}:{1}\n", urlList[i].ToString(), resp.StatusCode.ToString());
                            break;
                    }
                }
                catch (Exception ex)
                {
                    //Console.WriteLine(string.Format("{0}:{1}\n", urlList[i].ToString(), ex.Message.ToString()));
                    this.BrokenLink += string.Format("Error: {0} -> {1}\n", urlList[i].ToString(), ex.Message.ToString());
                }
                finally
                {
                    if(req!=null)
                    {
                        req.Abort();
                    }
                    if (resp != null)
                    {
                        resp.Close();
                    }
                }
            }

            if(req!=null)
            {
                req = null;
            }
            if (resp != null)
            {
                resp.Close();
                resp = null;
            }
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

        //public string GetParentIncludeFileOfIncludeFile()
        //{
        //    string parentInclude = string.Empty;
        //    string message = string.Empty;
        //    string parentpath = CommonFun.GetConfigurationValue("RepositoryZHCNIncludeDir", ref message);

        //    string filecontent = string.Empty;
        //    string regRule = "\\[\\!INCLUDE \\[([\\S|\\s]+)(\\.md)?\\]\\(((\\.\\.\\/)*)includes\\/{0}\\)\\]";
        //    string curtCheckFile = string.Empty;

        //    Regex reg;

        //    FileStream fs;
        //    StreamReader sr;
        //    StreamWriter sw;
        //    bool findFile = false;

        //    if (this.CheckFileList != null && this.CheckFileList.Count > 0)
        //    {
        //        this.CheckFileList = new ArrayList();
        //    }


        //    this.GetAllFilesInDirectory(parentpath);
        //    ArrayList fileList = this.CheckFileList;

        //    int idx = 1;
        //    parentInclude = this.Fullpath;

        //    while(parentInclude!=string.Empty )
        //    {
        //        foreach (string curtFile in fileList)
        //        {
        //            //Console.WriteLine(string.Format("Tread[{0}] checking {1}", this.Id, curtFile));
        //            try
        //            {
        //                curtCheckFile = curtFile;
                        
        //                Mutex fileMutex = new Mutex(true, GetMetuxFileName(curtCheckFile));
        //                fileMutex.WaitOne();
        //                fs = new FileStream(curtCheckFile, FileMode.Open);

        //                try
        //                {
        //                    sr = new StreamReader(fs);
        //                    filecontent = sr.ReadToEnd();
        //                    sr.Close();

        //                    reg = new Regex(string.Format(regRule, Path.GetFileName(parentInclude).Replace(".", "\\.")));

        //                    if (reg.IsMatch(filecontent))
        //                    {
        //                        sw = new StreamWriter(curtCheckFile, false);
        //                        filecontent += string.Format("\n<!--Not Available the {0} parent file {1} of includes file of {2}-->",idx, Path.GetFileName(curtCheckFile), Path.GetFileName(parentInclude));
        //                        filecontent += string.Format("\n<!--ms.date:{0}-->", this.CustomizedDate);
        //                        sw.Write(filecontent);
        //                        sw.Flush();
        //                        sw.Close();
        //                        parentInclude = curtCheckFile;
        //                        this.ParentIncludeFile = Path.GetFileName(parentInclude);

        //                        findFile = true;
        //                    }
        //                }
        //                catch (Exception ex)
        //                {
        //                    Console.WriteLine(ex.Message.ToString());
        //                }
        //                finally
        //                {
        //                    fileMutex.ReleaseMutex();
        //                    if (fs != null)
        //                    {
        //                        fs.Close();
        //                        fs = null;

        //                    }
        //                }
        //                if (findFile == true)
        //                {
        //                    break;
        //                }
        //            }
        //            catch (Exception ex)
        //            {

        //            }

        //        }

        //        if (findFile == false)
        //        {
        //            parentInclude = string.Empty;
        //        }

        //    }

        //    if (this.ParentIncludeFile == string.Empty)
        //    {
        //        parentInclude = this.File;
        //    }else
        //    {
        //        parentInclude = this.ParentIncludeFile;
        //    }

        //    return parentInclude;
        //}

        //public string FindParentOfIncludeFile() 
        //{

        //    string parentFile = string.Empty;
        //    //string parentFile = this.GetParentIncludeFileOfIncludeFile();

        //    string message = string.Empty;
        //    string diskpath = CommonFun.GetConfigurationValue("GlobalArticleDir", ref message);
        //    string parentpath = string.Empty;
        //    string filecontent = string.Empty;
        //    string regRule = "\\[\\!INCLUDE \\[([\\S|\\s]+)(\\.md)?\\]\\(((\\.\\.\\/)*)includes\\/{0}\\)\\]";
        //    //string regRule = "\\[\\!INCLUDE \\[(\\S+)(\\.md)?\\]\\(((\\.\\.\\/)*)includes\\/{0}\\)\\]";
        //    Regex reg;

        //    FileStream fs ;
        //    StreamReader sr;
        //    StreamWriter sw;
        //    bool findFile = false;


        //    foreach (InvolvedService curtService in Enum.GetValues(typeof(InvolvedService)))
        //    {
        //        if (this.CheckFileList != null && this.CheckFileList.Count > 0)
        //        {
        //            this.CheckFileList = new ArrayList();
        //        }

        //        findFile = false;
        //        parentpath = string.Format("{0}\\{1}", diskpath, curtService.ToString().Replace('_', '-'));

        //        this.GetAllFilesInDirectory(parentpath);
        //        ArrayList fileList = this.CheckFileList;

        //        foreach (string curtFile in fileList)
        //        {
        //            //Console.WriteLine(string.Format("Tread[{0}] checking {1}", this.Id, curtFile));
        //            try
        //            {
        //                Mutex fileMutex = new Mutex(true, GetMetuxFileName(curtFile));
        //                fileMutex.WaitOne();
        //                fs = new FileStream(curtFile, FileMode.Open);

        //                try
        //                {
        //                    sr = new StreamReader(fs);
        //                    filecontent = sr.ReadToEnd();
        //                    sr.Close();

        //                    reg = new Regex(string.Format(regRule, this.File.Replace(".", "\\.")));

        //                    if (reg.IsMatch(filecontent))
        //                    {
        //                        sw = new StreamWriter(curtFile, false);
        //                        filecontent += string.Format("\n<!--Not Available the parent file of includes file of {0}-->", this.File);
        //                        filecontent += string.Format("\n<!--ms.date:{0}-->", this.CustomizedDate);
        //                        sw.Write(filecontent);
        //                        sw.Flush();
        //                        sw.Close();
        //                        parentFile = curtFile;
        //                        findFile = true;
        //                    }
        //                }
        //                catch (Exception ex)
        //                {
        //                    Console.WriteLine(ex.Message.ToString());
        //                }
        //                finally
        //                {
        //                    fileMutex.ReleaseMutex();
        //                    if (fs != null)
        //                    {
        //                        fs.Close();
        //                        fs = null;

        //                    }
        //                }
        //                if (findFile == true)
        //                {
        //                    break;
        //                }
        //            }
        //            catch(Exception ex)
        //            {

        //            }
                   
        //        }
               
        //        if (findFile == true)
        //        {
        //            break;
        //        }
        //    }

        //    return parentFile;

        //}



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

            FileStream fs = null;
            StreamReader sr = null;
            string error = "";
            try
            {
                fs = new FileStream(this.Fullpath, FileMode.Open);

                sr = new StreamReader(fs);
                string fullcontent = sr.ReadToEnd();

                sr.Close();

                ConvertCategory category = this.ProcessCategory;
                Console.WriteLine(string.Format("Processing Thread[{0}] : {1}", this.Id, this.Fullpath));

                this.ProcessConvertJson(ref fullcontent);
                
                //Check Broken Link No need to modified the content.
                //sw = new StreamWriter(this.Fullpath,false);
                //sw.Write(fullcontent);
                //sw.Flush();

            }
            catch(Exception ex)
            {
                this.BrokenLink += string.Format("Error : {0}", ex.Message.ToString());
            }
            finally
            {
                if (sr != null)
                {
                    sr.Close();
                }

                //if (sw != null)
                //{
                //    sw.Close();
                //}

                if (fs != null)
                {
                    fs.Close();
                }
            }

            this.Status = ProcessStatus.Complete;
            Console.WriteLine("The Thread[{0}] run successfully : {1}", this.Id, this.Status.ToString());


        }

       

    }


}
