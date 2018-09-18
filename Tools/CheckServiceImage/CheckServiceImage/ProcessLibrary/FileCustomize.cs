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

namespace CheckImageService.ProcessLibrary
{
    public enum ConvertItem
    {
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
        CheckImageByService,
        CheckImageByFile,
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
    //    includes,
    //}





    public enum InvolvedService
    {
        includes,
        analysis_services,
        azure_resource_manager,
        container_registry,
        cosmos_db,
        event_hubs,
        load_balancer,
        resiliency,
        network_watcher,
        service_fabric,
        site_recovery,
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

    public class CollectAllImageByService : FileCustomize
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

            string medianame= CommonFun.GetConfigurationValue("MediaDirName",ref message);
            string servicename = string.Empty;


            foreach (InvolvedService curtService in Enum.GetValues(typeof(InvolvedService)))
            {
                servicename = this.GetInitFormat(curtService.ToString());
                if (curtService.ToString().ToLower() == InvolvedService.includes.ToString().ToLower())
                {
                    diskpath = CommonFun.GetConfigurationValue("RepositoryGlobalIncludeDir", ref message);
                    parentpath = string.Format("{0}", diskpath);
                }
                else
                {
                    diskpath = CommonFun.GetConfigurationValue("RepositoryGlobalArticleDir", ref message);
                    parentpath = string.Format("{0}\\{1}", diskpath, curtService.ToString().Replace('_', '-'));
                }

                this.GetAllFilesInDirectory( curtService,parentpath, medianame);

            }

            fileList = this.CheckFileList;
            return fileList;
        }


        public string GetInitFormat(string curtService)
        {
            string sInitService = string.Empty;
            string[] names = curtService.Split('-');
            foreach (string vCurtName in names)
            {
                sInitService += string.Format("{0} {1}{2}", sInitService, vCurtName.Substring(0, 1).ToUpper(), vCurtName.Substring(1).ToLower());
            }
            return sInitService;
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
        public string FullPath { get; set; }

        string customizedDate = "";
        public string CustomizedDate { get; set; }

        ProcessStatus status = ProcessStatus.UnDefine;
        public ProcessStatus Status { get; set; }

        string servicename = "";
        public string ServiceName { get; set; }

        int totalimagecount = 0;
        public int TotalImageCount { get; set; }

        int modifyimagecount = 0;
        public int ModifyImageCount { get; set; }

        string modifyimagename = "";
        public string ModifyImageName { get; set; }

        string imagepath = "";
        public string ImagePath { get; set; }

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

        //bool forceTerminate = false;
        //public bool ForceTerminate { get; set; }

        int checkRound = 0;
        public int CheckRound { get; set; }

        private Mutex fileMutex = null;
        public Mutex FileMutex { get; set; }


        ConvertCategory processCategory = ConvertCategory.AuthorReplacement;
        public ConvertCategory ProcessCategory { get; set; }

        ConvertProcess showHistory = ConvertProcess.ShowResult;
        public ConvertProcess ShowHistory { get; set; }

        //CancellationTokenSource cancelToken;
        //public CancellationTokenSource CancelToken { get; set; }

        public static Object ObjJason = new Object();

        private Hashtable htbSevice = new Hashtable();
        public Hashtable HTBService
        {
            get { return this.htbSevice; }
            set { this.htbSevice = value; }
        }


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

                diskpath = CommonFun.GetConfigurationValue("RepositoryGlobalArticleDir", ref message);
                relativefile = GetRightFileName(para);
                this.FullPath = string.Format(@"{0}\{1}\{2}", diskpath, relativefile, this.File);
                this.ArticleCategory = FileCategory.Article;

            }

            if (para[0].ToLower() == "includes")
            {

                diskpath = CommonFun.GetConfigurationValue("RepositoryGlobalIncludeDir", ref message);
                relativefile = GetRightFileName(para);
                this.FullPath = string.Format(@"{0}\{2}", diskpath, relativefile, this.File);
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
            this.WarningMessage = string.Empty;

            this.CheckFileList = new ArrayList();

        }

        public FileCustomize(int id, string filename, string directory, string customizedate, ConvertCategory category, ConvertProcess process, CancellationTokenSource cancelToken)
        {
            this.Id = id;
            this.File = filename;
            string[] para = directory.Split('/');

            this.SetFullPathName(para);
            this.CustomizedDate = customizedate;
            this.ProcessCategory = category;
            this.WarningMessage = string.Empty;

            this.ShowHistory = process;
            //this.ForceTerminate = false;
            this.CheckRound = 0;
            this.CheckFileList = new ArrayList();
            //this.CancelToken = cancelToken;

        }

        public FileCustomize(int id,string imagefilepath, string filename, string directory, string customizedate, ConvertCategory category, ConvertProcess process)
        {
            this.Id = id;
            this.ImagePath = imagefilepath;

            string fullFileName = "";
            string[] para = directory.Split('/');

            
            foreach(string vpara in para)
            {
                if(vpara!="media")
                {
                    fullFileName += string.Format("/{0}", vpara);
                }
            }
            fullFileName = fullFileName.TrimStart('/') + "/" + string.Format("{0}.md", filename);
            this.File = fullFileName;

            if (para[0]=="includes")
            {
                this.ServiceName = para[0];
            }
            else
            {
                this.ServiceName = para[1];
            }
            

            this.SetFullPathName(para);
            this.CustomizedDate = customizedate;
            this.ProcessCategory = category;
            this.WarningMessage = string.Empty;

            this.ShowHistory = process;
            //this.ForceTerminate = false;
            this.CheckRound = 0;
            this.CheckFileList = new ArrayList();
            //this.CancelToke = cancelToke;

        }


        public FileCustomize(int id, string filename, string customizedate, ConvertCategory category, ConvertProcess process)
        {
            this.Id = id;
            this.File = filename;
            string[] para = directory.Split('/');

            this.SetFullPathName(para);
            this.CustomizedDate = customizedate;
            this.ProcessCategory = category;
            this.WarningMessage = string.Empty;

            this.ShowHistory = process;
            //this.ForceTerminate = false;
            this.CheckRound = 0;
            this.CheckFileList = new ArrayList();
            //this.CancelToke = cancelToke;

        }

        public void RepalceParameter(ref string paraValue)
        {

            if (paraValue.Contains(ReplaceParam.CustimzedDate.ToString()))
            {
                paraValue = paraValue.Replace("{CustimzedDate}", this.CustomizedDate);
            }

        }

        public string RemoveJsonPostfixinFileName(string fileName)
        {
            string correctName = string.Empty;
            string josnPat = "\\?toc=[\\S*]*\\.json";
            //Replace the INCLUDE link 
            Regex reg = new Regex(josnPat, RegexOptions.IgnoreCase);
            correctName = reg.Replace(fileName, "");

            return correctName;
        }



        public void GetAllFilesInDirectory(InvolvedService curtService,string parentPath, string medianame)
        {
            

            string[] curtDirList = System.IO.Directory.GetDirectories(parentPath);

            string curtDirName = string.Empty;

            foreach (string vDir in curtDirList)
            {
                curtDirName = vDir.Substring(vDir.Length - medianame.Length, medianame.Length);
                if(curtDirName==medianame)
                {
                    this.GetAllDirinMediaDirectory(curtService,vDir);
                }
                else
                {
                    this.GetAllFilesInDirectory(curtService,vDir, medianame);
                }
            }


        }

        public bool isCheckInvolveSpecService(string checkfile)
        {
            bool bExist = false;
            Console.WriteLine(checkfile);
          
            checkfile =checkfile.Substring(checkfile.LastIndexOf(@"\")+1);
            
            foreach (string vKey in this.HTBService.Keys)
            {
                if(checkfile.Length>vKey.Length && vKey.Trim().ToLower()==checkfile.Substring(0,vKey.Length))
                {
                    bExist = true;
                    break;
                }
            }
            Console.WriteLine(bExist.ToString());
            return bExist;
        }


        public void GetAllDirinMediaDirectory(InvolvedService curtService,string parentPath)
        {
            string[] curtFiles = System.IO.Directory.GetDirectories(parentPath);

            switch (curtService)
            {
                case InvolvedService.includes:
                    foreach (string vFile in curtFiles)
                    {
                        if (isCheckInvolveSpecService(vFile))
                        {
                            this.CheckFileList.Add(vFile.ToString());
                        }
                    }
                    break;

                default:
                    foreach (string vFile in curtFiles)
                    {
                        this.CheckFileList.Add(vFile.ToString());
                    }
                    break;
            }
            
        }

        public string GetMetuxFileName(string filepath)
        {
            string fileName = string.Empty;
            fileName = filepath.Replace(":", "_").Replace(@"\", "_").Replace("/", "_").ToUpper();
            return fileName;
        }




        public void CountOriginAndModifyImageCount(string imagefilepath, string globalpath, string mooncakepath)
        {

            FileStream fs = null;
            StreamReader sr = null;
            //string error = "";

            try
            {
                this.FileMutex = new Mutex(false, GetMetuxFileName(imagefilepath));
                this.FileMutex.WaitOne();

                DirectoryInfo globalFolder = new DirectoryInfo(imagefilepath);
                this.TotalImageCount = globalFolder.GetFiles().Length;

                string mooncakeFolderPath = imagefilepath.Replace(globalpath, mooncakepath);

                if (System.IO.Directory.Exists(mooncakeFolderPath))
                {
                    DirectoryInfo mooncakeFolder = new DirectoryInfo(mooncakeFolderPath);
                    this.ModifyImageCount = mooncakeFolder.GetFiles().Length;
                    FileInfo[] files = mooncakeFolder.GetFiles();
                    foreach(FileInfo vfile in files)
                    {
                        this.ModifyImageName += string.Format(";{0}", vfile.Name);
                    };
                    this.ModifyImageName= this.ModifyImageName.Trim(';');
                }
                else
                {
                    this.ModifyImageCount = 0;
                    this.ModifyImageName = string.Empty;
                }
                
            }
            catch (Exception ex)
            {

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

                this.FileMutex.ReleaseMutex();

                //if (fileMutex != null)
                //{
                //    fileMutex = null;
                //}

            }

        }


        public void ProcessFileCustomize()
        {
            this.Status = ProcessStatus.Start;

            string fullContent = string.Empty;

            bool needLog = true;

            string error = "";

            // If we involve the ReplaceLink function. It will cause the images file missing. 
            // images function will reference the include file check itself. 
            //this.ReplaceIncludeLinkWithContent(this.FullPath, ref fullContent);
            string globalprefix = CommonFun.GetConfigurationValue("GlobalRepository", ref error);

                
           string mooncakeprefix = CommonFun.GetConfigurationValue("MooncakeImageRepository", ref error);


            try
            {
                
                ConvertCategory category = this.ProcessCategory;
                Console.WriteLine(string.Format("Processing Thread[{0}] : {1}", this.Id, this.FullPath));

                this.CountOriginAndModifyImageCount(this.ImagePath, globalprefix, mooncakeprefix);


                //Check Broken Link No need to modified the content.
                //sw = new StreamWriter(this.Fullpath,false);
                //sw.Write(fullcontent);
                //sw.Flush();

            }
            catch (Exception ex)
            {
                this.BrokenLink += string.Format("Error : {0}\n", ex.Message.ToString());
                Console.WriteLine(string.Format("Thread[{0}]({1}) occure error with {2}. ", this.Id, this.FullPath, ex.Message.ToString()));
            }
            finally
            {
               
            }

            this.Status = ProcessStatus.Complete;
            Console.WriteLine("The Thread[{0}] run successfully : {1}", this.Id, this.Status.ToString());


        }



    }


}
