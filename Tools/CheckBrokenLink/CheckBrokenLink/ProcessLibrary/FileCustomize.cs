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
        aks,
        analysis_services,
        azure_resource_manager,
        container_registry,
        cosmos_db,
        resiliency,
        service_fabric,
        site_recovery,
        sql_server_stretch_database,
        traffic_manager,
        virtual_machines,
        virtual_network,
        includes,
    }

    //public enum InvolvedService
    //{
    //    aks,
    //    includes,
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
                if (curtService.ToString().ToLower() == InvolvedService.includes.ToString().ToLower())
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

                diskpath = CommonFun.GetConfigurationValue("RepositoryENUSArticleDir", ref message);
                relativefile = GetRightFileName(para);
                this.FullPath = string.Format(@"{0}\{1}\{2}", diskpath, relativefile, this.File);
                this.ArticleCategory = FileCategory.Article;

            }

            if (para[0].ToLower() == "includes")
            {

                diskpath = CommonFun.GetConfigurationValue("RepositoryENUSIncludeDir", ref message);
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

        public void CheckMatches(MatchCollection matches, ref List<string> lstURL, ref string articleContent)
        {
            string originalFileName = string.Empty;

            string filename = string.Empty;
            int idxSeq = 0;
            string lablename = string.Empty;
            string checkdirectory = string.Empty;
            string checkfile = string.Empty;
            string error = string.Empty;

            //Match existMath = null;

            string localpath = CommonFun.GetConfigurationValue("RepositoryENUSArticleDir", ref error);

            for (int i = 0; i < matches.Count; i++)
            {

                //First remove the current directory ./ when exists.
                lablename = matches[i].Groups["labelname"].ToString().ToLower().Trim();
                switch(lablename)
                {
                    case "401":
                    case "binary":
                    case "nvarchar":
                        continue;
                }

                originalFileName = matches[i].Groups["mdfilename"].ToString().Trim();
                filename = originalFileName.ToLower();

                // the lenght of --> is 3 and exclude at first.
                if (filename.Length >= 3 && filename.Substring(filename.Length - 3) == "-->")
                {
                    continue;
                }

                //Exclude the following sample
                //[Azure 门户](http://portal.azure.cn "Azure 门户")

                if(filename.IndexOf(' ')>0)
                {
                    //idxSeq = filename.IndexOf(' ') - 1;   Correct the Sequence of Sample.
                    idxSeq = filename.IndexOf(' ');    
                    filename = filename.Substring(0, idxSeq);
                    originalFileName= originalFileName.Substring(0, idxSeq);
                }

                // When we find that the filename is string.empty, we will discard it and go the check next round. 
                switch (filename.ToLower())
                {
                    case "":
                    case "16686":
                    case "20001":
                    case "3000":
                    case "401":
                    case "4222":
                    case "4222)`":
                    case "4222,":
                    case "4222`":
                    case "9090":
                    case "deviceFile.write(\",":
                    case "eventid":
                    case "eventid=evt":
                    case "https://code.visualstudio.com/":
                    case "https://configuration-server-name/ip:44315":
                    case "http://linux.yyz.us/nsupdate/":
                    case "http://linux.yyz.us/dns/ddns-server.html":
                    case "http://mysftestcluster.chinaeast.cloudapp.chinacloudapi.cn:19080/explorer/":
                    case "http://mycluster.region.cloudapp.chinacloudapi.cn:19080/explorer":
                    case "https://www.eembc.org/coremark/faq.php":
                    case "https://www.mysql.com/":
                    case "index.yml":
                    case "mailto:cosmosdbtooling@microsoft.com":
                    case "print":
                    case "PS":
                    case "response.data":
                    case "ssdt":
                    case "select":
                    case "SLF4J":
                    case "SparkPi":
                    case "#":
                    case ">>":
                    case "==":
                    case "/home":
                    case "</span></span>":
                        continue;
                        
                }
               

                

                // When the filename is normal item, we will continue to check the following decode. 
                if (filename.Substring(0, 2) == "./")
                {
                    filename = filename.Substring(2);
                    originalFileName = originalFileName.Substring(2);
                }


                //Get the current parent directory. 
                if (this.FullPath.LastIndexOf("\\") > -1)
                {
                    checkdirectory = this.FullPath.Substring(0, this.FullPath.LastIndexOf("\\"));
                }


                //Step 1:  Select Http(s):// link 
                if (filename.StartsWith("http://") || filename.StartsWith("https://"))
                {
                    //lstURL.Add(filename);
                    lstURL.Add(originalFileName);
                    continue;
                }

                //Step 2: Select inner Archor tag
                if (filename.StartsWith("#") == true)
                {
                    this.CheckArchorInFile(ref articleContent, originalFileName);
                    continue;
                }

                //Step 3: Select the outside Archor tag, omit due to there are exist some archor.
                if (filename.Contains("#") == true)
                {

                }

                string sPostfix = string.Empty;

                //Step 3: Sample of ../virtual-machines/windows/sizes.md?toc=%2fvirtual-machines%2fwindows%2ftoc.json
                if (filename.Contains(".md") == true)
                {
                    sPostfix = originalFileName.Substring(filename.IndexOf(".md") + 3);
                    if (sPostfix.Trim().Length>0 && sPostfix.Contains("#")==true)
                    {
                        string[] menuAndAnchor = sPostfix.Split('#');
                        sPostfix = "#" + menuAndAnchor[menuAndAnchor.Length - 1];   // Remove the ?toc=XXXX#AnchorName
                    }else
                    {
                        sPostfix = "";
                    }

                    idxSeq = filename.IndexOf(".md") + 3;
                    filename = filename.Substring(0, idxSeq) + sPostfix;
                    originalFileName = originalFileName.Substring(0, idxSeq) + sPostfix;
                }


                //Step 4: Check the md and image file.
                //The filename is larger than 4 charactors
                if (filename.Length >= 4 &&
                    (filename.Substring(filename.Length - 4).ToLower().ToString() == ".jpg" ||
                   filename.Substring(filename.Length - 4).ToLower().ToString() == ".svg" ||
                   filename.Substring(filename.Length - 4).ToLower().ToString() == ".png" ||
                   filename.Substring(filename.Length - 4).ToLower().ToString() == ".gif"))

                {

                    //reward to parent directory when exists the ../
                    while (filename.IndexOf("../") > -1)
                    {
                        idxSeq = filename.IndexOf("../") + 3;
                        filename = filename.Substring(idxSeq);
                        originalFileName = originalFileName.Substring(idxSeq);
                        checkdirectory = checkdirectory.Substring(0, checkdirectory.LastIndexOf("\\"));
                    }

                    // images file should add \\ in the following code
                    if (filename.StartsWith("/") == true)
                    {
                        checkfile = string.Format("{0}\\{1}", localpath, originalFileName.Replace("/", "\\"));
                    }
                    else
                    {
                        checkfile = string.Format("{0}\\{1}", checkdirectory, originalFileName.Replace("/", "\\"));
                    }

                        

                    // Replace some case which the file path contains \\\\.
                    checkfile = checkfile.Replace("\\\\", "\\");
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

                //The filename should be 3 characters at least.
                if (filename.Length >= 3 && filename.Substring(filename.Length - 3).ToLower().ToString() == ".md")
                {

                    //reward to parent directory when exists the ../
                    while (filename.IndexOf("../") > -1)
                    {
                        idxSeq = filename.IndexOf("../") + 3;
                        filename = filename.Substring(idxSeq);
                        originalFileName = originalFileName.Substring(idxSeq);
                        checkdirectory = checkdirectory.Substring(0, checkdirectory.LastIndexOf("\\"));
                    }

                    if (filename.StartsWith("/") == true)
                    {
                        // forexample /azure-resource-manager/XXXX.md
                        checkfile = string.Format("{0}{1}", localpath, originalFileName.Replace("/", "\\"));
                    }
                    else
                    {
                        // for example include/XXX.md, we should add the \\ to connect correct path.
                        checkfile = string.Format("{0}\\{1}", checkdirectory, originalFileName.Replace("/", "\\"));
                    }

                    if (System.IO.File.Exists(checkfile) == false)
                    {
                        //We will check whether contains redirection URL
                        checkdirectory = checkdirectory.Replace(localpath, "").Replace("\\", "/").TrimStart('/');

                        if (filename.StartsWith("articles/") == true)
                        {
                            // filename = "articles/XXXX.md"
                            // this choise match the sample of /article/XXXX.md
                            filename = filename.Substring("articles/".Length);
                            originalFileName = originalFileName.Substring("articles/".Length);
                            idxSeq = filename.IndexOf(".md");
                            filename = string.Format("https://docs.azure.cn/zh-cn/{0}", filename.Substring(0,idxSeq ));
                            originalFileName = string.Format("https://docs.azure.cn/zh-cn/{0}", originalFileName.Substring(0, idxSeq));
                        }
                        else
                        {

                            if (filename.StartsWith("/") == true)
                            {
                                // filename = "/XXXX.md"
                                // this choise match the sample of /article/event-hubs/XXXX.md 
                                filename = filename.Substring(1);
                                originalFileName = originalFileName.Substring(1);
                                idxSeq = filename.IndexOf(".md");
                                filename = string.Format("https://docs.azure.cn/zh-cn/{0}/{1}", checkdirectory, filename.Substring(0, idxSeq));
                                originalFileName = string.Format("https://docs.azure.cn/zh-cn/{0}/{1}", checkdirectory, originalFileName.Substring(0, idxSeq));

                            }
                            else
                            {
                                // filename = "XXXX.md"
                                // this choise match the sample of /article/event-hubs/XXXX.md 
                                idxSeq = filename.IndexOf(".md");
                                filename = string.Format("https://docs.azure.cn/zh-cn/{0}/{1}", checkdirectory, filename.Substring(0,idxSeq ));
                                originalFileName = string.Format("https://docs.azure.cn/zh-cn/{0}/{1}", checkdirectory, originalFileName.Substring(0, idxSeq));
                            }

                        }



                        lstURL.Add(originalFileName);
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
                    idxSeq = filename.IndexOf("../") + 3;
                    filename = filename.Substring(idxSeq);
                    originalFileName = originalFileName.Substring(idxSeq);
                    checkdirectory = checkdirectory.Substring(0, checkdirectory.LastIndexOf("\\"));
                }


                if (filename.StartsWith("/") == true)
                {
                    // forexample /azure-resource-manager/XXXX.md
                    checkfile = string.Format("{0}{1}", localpath, filename.Replace("/", "\\"));
                }
                else
                {
                    checkfile = string.Format("{0}\\{1}", checkdirectory, filename.Replace("/", "\\"));
                }

                bool isRedirect = false;
                if (checkfile.Contains("#"))    //we have check the .md file in preceding logical .
                {

                    checkfile = checkfile.ToLower().Replace(".md", "");
                    idxSeq = checkfile.IndexOf("#");
                    string localfile = checkfile.Substring(0, idxSeq) + ".md";
                    string localarchor = checkfile.Substring(idxSeq);

                    // isRedirect return flag to confirem the file is redirection or not.
                    bool isNeedRedirect = false;
                    this.CheckArchorInFile(isRedirect, localfile, localarchor, ref isNeedRedirect);
                    if (isNeedRedirect == false)
                    {
                        continue;
                    }
                }
                else
                {


                }

                // The Link style like [](../service-name/XXXX.md) 
                if (filename.ToLower().Contains(".md") == true)
                {
                    filename = filename.Replace(".md", "").Replace(".MD", "");
                    originalFileName = originalFileName.Replace(".md", "").Replace(".MD", "");
                }

                if (filename.Substring(0, 1) == "/")
                {
                    filename = filename.Substring(1);
                    originalFileName = originalFileName.Substring(1);
                    filename = string.Format("https://docs.azure.cn/zh-cn/{0}", filename);
                    originalFileName = string.Format("https://docs.azure.cn/zh-cn/{0}", originalFileName);
                }
                else
                {
                    checkdirectory = checkdirectory.Replace(localpath, "").Replace("\\", "/").TrimStart('/');
                    filename = string.Format("https://docs.azure.cn/zh-cn/{0}/{1}", checkdirectory, filename);
                    originalFileName = string.Format("https://docs.azure.cn/zh-cn/{0}/{1}", checkdirectory, originalFileName);
                }

                lstURL.Add(originalFileName);


            }
        }

        public void CheckArchorInFile(bool isRedirect, string filename, string archor, ref bool isNeedRedirect)
        {



            try
            {

                filename = RemoveJsonPostfixinFileName(filename);

                bool needLog = isRedirect; // Notice: isRedirect is 

                string articleContent = this.ReadArticleContent(filename, needLog);

                this.ReplaceIncludeLinkWithContent(filename, ref articleContent);

                bool matchOK = false;

                //string archPat = string.Format("<a[\\s]*(id|name)=(\'|\"){0}(\'|\")[\\s]*></a>", archor.TrimStart('#'));
                string archPat = string.Format("<a[\\s]*(id|name)[\\s]*=[\\s]*(\'|\")?{0}(\'|\")?[\\s]*>[^<]*</a>", archor.TrimStart('#')); //Middle will be exist characters [^<]*
                Match existMath = Regex.Match(articleContent, archPat, RegexOptions.IgnoreCase);

                if (existMath.Length > 0)
                {
                    matchOK = true;
                }

                if (matchOK == false)
                {
                    //archPat = string.Format("<a[\\s]*(id|name)=(\'|\"){0}(\'|\")[\\s]*/>", archor.TrimStart('#'));
                    archPat = string.Format("<a[\\s]*(id|name)[\\s]*=[\\s]*(\'|\")?{0}(\'|\")?[\\s]*/>", archor.TrimStart('#'));
                    existMath = Regex.Match(articleContent, archPat, RegexOptions.IgnoreCase);
                    if (existMath.Length > 0)
                    {
                        matchOK = true;
                    }
                }

                if (matchOK == true)
                {
                    if (this.ShowHistory == ConvertProcess.ShowHistory)
                    {
                        this.BrokenLink += string.Format("{0}{1} : exist.\n", filename, archor);
                    }
                }
                else
                {
                    if(isRedirect ==false )
                    {
                        isNeedRedirect = true;
                    }else
                    {
                        this.BrokenLink += string.Format("{0}{1} : missing.\n", filename, archor);
                    }
                    //
                }

            }

            catch (Exception ex)
            {
                //if (isRedirect == false)
                //{
                //    string sParam = ex.Message.ToString();
                //    if (sParam.Contains("Could not find file") == true)
                //    {
                //        isNeedRedirect = true;
                //    }
                //}
                //else
                //{
                //    this.BrokenLink += string.Format("{0}{1} : missing.\n", filename, archor);
                //}

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

                string archPat = string.Format("<a[\\s]*(id|name)[\\s]*=[\\s]*(\'|\"){0}(\'|\")[\\s]*>[\\s]*</a>", archor.TrimStart('#'));
                Match existMath = Regex.Match(articleContent, archPat, RegexOptions.IgnoreCase);

                if (existMath.Length > 0)
                {
                    matchOK = true;
                }

                if (matchOK == false)
                {
                    archPat = string.Format("<a[\\s]*(id|name)[\\s]*=[\\s]*(\'|\"){0}(\'|\")[\\s]*/>", archor.TrimStart('#'));
                    existMath = Regex.Match(articleContent, archPat, RegexOptions.IgnoreCase);
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
                this.BrokenLink += string.Format("Error : {0} ->  {1}\n", archor, ex.Message.ToString());
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
            string filePostfix = string.Empty;
            string orignalFilePostfix = string.Empty;

            if (category == ConvertCategory.CheckBrokenLinkByFile || category == ConvertCategory.CheckBrokenLinkByService)
            {


                List<string> lstURL = new List<string>();

                MatchCollection matches;

                orignalFilePostfix = this.FullPath.Trim().Substring(this.FullPath.Length - 3);
                filePostfix = this.FullPath.Trim().Substring(this.FullPath.Length - 3).ToLower();

                if(filePostfix ==".md")
                {
                    // Part I for Links of .md file.
                    //string mdfilePatFirst = "[^(<!--)]\\[([^\\[\\]])*\\]([\\s]*)\\((?<mdfilename>[^\\(\\)]*)\\)";
                    //string mdfilePatFirst = "(?!(<!--[\\s\\S]*))\\[([^\\[\\]])*\\]([\\s]*)\\((?<mdfilename>[^\\(\\)\\[\\]]*)\\)(?!(\\s*-->))";
                    string mdfilePatFirst = "(?!(<!--[\\s\\S]*))\\[(?<labelname>[^\\[\\]]*)\\]([\\s]*)\\((?<mdfilename>[^\\(\\)\\[\\]]*)\\)(?!(\\s*-->))";
                    matches = Regex.Matches(articleContent, mdfilePatFirst);

                    this.CheckMatches(matches, ref lstURL, ref articleContent);

                    // Exception the C++ method style -->  [XXX]::MethodName
                    // Invloved Sample 
                    // 1.[XXX]: XXXXX 
                    // 2.[XXX]: http(s)://XXXX
                    // 3.[XXX]: XXX
                    // 4.[XXX]: XXXXXX -->   '--> will show us it is link no need to verify in later process. 

                    //string mdfilePatSecond = "(?!(<!--[\\s\\S]*))\\[([^\\[\\]]*)\\]([\\s]*)\\:([\\s]*)(?<mdfilename>(https?:)?[^:\\s]*)(?!(\\s*-->))";
                    //string mdfilePatSecond = "(?!(<!--[\\s\\S]*))\\[([^\\[\\]])*\\]([\\s]*)\\:([\\s]*)(?<mdfilename>(http(s)?:)?[^:\\s\\[\\]]+(\\s*-->)?)";
                    string mdfilePatSecond = "(?!(<!--[\\s\\S]*))\\[(?<labelname>[^\\[\\]]*)\\]([\\s]*)\\:([\\s]*)(?<mdfilename>(http(s)?:)?[^:\\s\\[\\]\\<]+(\\s*-->)?)";

                    matches = Regex.Matches(articleContent, mdfilePatSecond);

                    this.CheckMatches(matches, ref lstURL, ref articleContent);

                }

                filePostfix = this.FullPath.Trim().Substring(this.FullPath.Length - 4).ToLower();
                if(filePostfix==".yml")
                {
                    // Part II for Links of .yml file.
                    string ymlfilePatFirst = "src:([\\s| ]*)([\\'|\\\"| ]{1})(?<mdfilename>[^ \\'\\\"\\r\\n]*)(\\2)";

                    matches = Regex.Matches(articleContent, ymlfilePatFirst);

                    this.CheckMatches(matches, ref lstURL, ref articleContent);

                    string ymlfilePatSecond = "href(\\:|\\=)([\\s| ]*)([\\'|\\\"| ]?)(?<mdfilename>[^ \\'\\\"\\r\\n]*)(\\3)[\\s]*(-->)*";     // (\\3) implement of ([\\'|\\\"| ]?) equal the 3rd element of groups 
                    // mdfilename should also not be equal to \r\n, or will be append \r\n and next row's character.
                    matches = Regex.Matches(articleContent, ymlfilePatSecond);

                    this.CheckMatches(matches, ref lstURL, ref articleContent);
                }



                this.CheckAllLinks(lstURL);



            }


        }

        public void CheckArchorInRedirectionFile(string sURL, ref HttpWebResponse resp)
        {

            string[] Param = sURL.Split('#');

            string fileName = resp.ResponseUri.OriginalString;
            string errProcess = "";
            string sRepalce = CommonFun.GetConfigurationValue("RepositoryENUSArticleDir", ref errProcess);
            fileName = fileName.Replace("https://docs.azure.cn/zh-cn", sRepalce).Replace("/", "\\").Trim();
            if (fileName.Substring(fileName.Length - 3).ToLower() != ".md")
            {
                fileName = string.Format("{0}.md", fileName);
            }

            string sArchor = string.Format("#{0}", Param[1]);

            bool isRedirection = true;
            bool isNeedRedirect = false;
            CheckArchorInFile(isRedirection, fileName, sArchor, ref isNeedRedirect);

        }

        public bool NeedToCheckArchorInRespostory(string sURL)
        {
            bool needCheckArchor = false;

            //Except the webpage which not belong to https://docs.azure.cn/zh-cn/
            if (sURL.StartsWith("https://docs.azure.cn/zh-cn/cli/") == true ||
                sURL.StartsWith("https://docs.azure.cn/zh-cn/dotnet/") == true ||
                sURL.StartsWith("https://docs.azure.cn/zh-cn/java/") == true ||
                sURL.StartsWith("https://docs.azure.cn/zh-cn/") == false) // the last check is "https://docs.azure.cn/zh-cn/"
            {
                return needCheckArchor;
            }

            string errProcess = string.Empty;
            string sRepositoryRoot = CommonFun.GetConfigurationValue("RepositoryENUSArticleDir", ref errProcess);
            string CurtDirectory = string.Empty;

            if (errProcess.Length == 0)
            {
                CurtDirectory = sURL.Replace("https://docs.azure.cn/zh-cn/", "");
            }

            string[] param = CurtDirectory.Split('/');
            string nextDirectory = string.Empty;
            string checkDirectory = string.Empty;
            string nextItem = string.Empty;

            checkDirectory = sRepositoryRoot;
            for (int i =0; i<param.Length;i++)
            {
                nextItem = param[i];
                if (i!=param.Length-1)
                {
                    checkDirectory = string.Format("{0}\\{1}", checkDirectory, nextItem);
                    if (System.IO.Directory.Exists(checkDirectory) == false)
                    {
                        needCheckArchor = true;
                        return needCheckArchor;
                    }
                }
                else
                {
                    nextItem = nextItem.Split('#')[0];
                    if (System.IO.File.Exists(string.Format("{0}\\{1}.md", checkDirectory, nextItem)) == false)
                    {
                        needCheckArchor = true;
                        return needCheckArchor;
                    }
                }
            }

            return needCheckArchor;
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
            bool converFlag = int.TryParse(sValue, out iConCount);

            if (converFlag == false)
            {
                Console.WriteLine(string.Format("MaxHttpConnectionCount {0} is not a valid interge.", sValue));
                return;
            }

            System.Net.ServicePointManager.DefaultConnectionLimit = iConCount;
            //ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3;

            string sTimeOut = CommonFun.GetConfigurationValue("HTTPTIMEOUT", ref error);
            if (error.Length > 0)
            {
                Console.WriteLine(error);
                return;

            }
            int iTimeOut = 0;
            converFlag = int.TryParse(sTimeOut, out iTimeOut);

            if (converFlag == false)
            {
                Console.WriteLine(string.Format("HTTPTIMEOUT {0} is not a valid interge.", sTimeOut));
                return;
            }



            HttpWebRequest req = null;
            HttpWebResponse resp = null;
            string sURL = string.Empty;
            string sLowCaseURL = string.Empty;

            for (int i = 0; i < urlList.Count; i++)
            {
                try
                {
                    sURL = urlList[i].ToString().Trim();

                    //Check the localhost URL in source code.
                    sLowCaseURL = sURL.ToLower();
                    if (sLowCaseURL.StartsWith("http://127.0.0.1")  || sLowCaseURL.StartsWith("https://127.0.0.1") ||
                        sLowCaseURL.StartsWith("http://localhost") || sLowCaseURL.StartsWith("https://localhost") ||
                        sLowCaseURL.StartsWith("http://mysftestcluster.chinaeast.cloudapp.chinacloudapi.cn:19080/explorer") || 
                        sLowCaseURL.StartsWith("https://mysftestcluster.chinaeast.cloudapp.chinacloudapi.cn:19080/explorer") ||
                        sLowCaseURL.StartsWith("https://github.com/Azure/azure-quickstart-templates/") ||
                        sLowCaseURL.StartsWith("https://vortex.data.microsoft.com/collect/v1"))
                    {
                        if (this.ShowHistory == ConvertProcess.ShowHistory)
                        {
                            this.BrokenLink += string.Format("{0} : {1}.\n", sURL, "OK, Discard to check link of source code.");
                        }
                        continue;
                    }

                    //Check the hide URL in source code. Sample : XXXX --> , We will discard the URL which no need to verify. 
                    if (sLowCaseURL.EndsWith("-->"))
                    {
                        if (this.ShowHistory == ConvertProcess.ShowHistory)
                        {
                            this.BrokenLink += string.Format("{0} : {1}.\n", sURL.Substring(0, sURL.IndexOf("-->")), "OK, Discard to check link of HIDE in document");
                        }
                        continue;
                    }


                    req = (HttpWebRequest)WebRequest.Create(sURL);
                    req.Method = "GET";
                    //req.KeepAlive = true;
                    req.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
                    req.UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/45.0.2454.101 Safari/537.36";
                    req.Timeout = iTimeOut;
                    req.AllowAutoRedirect = true;


                    resp = (HttpWebResponse)req.GetResponse();
                    //Console.WriteLine("checking " + urlList[i].ToString());
                    switch (resp.StatusCode)
                    {
                        case HttpStatusCode.OK:             //200
                        case HttpStatusCode.Accepted:       //202
                        case HttpStatusCode.Forbidden:      //403
                        case HttpStatusCode.RequestTimeout: //408
                        case HttpStatusCode.NonAuthoritativeInformation: //203

                            if (sURL.Contains("#") == true && NeedToCheckArchorInRespostory(sURL) == true)
                            {
                                CheckArchorInRedirectionFile(sURL, ref resp);

                            }
                            else
                            {
                                if (this.ShowHistory == ConvertProcess.ShowHistory)
                                {
                                    this.BrokenLink += string.Format("{0} : {1}\n", urlList[i].ToString(), resp.StatusCode.ToString());
                                }
                            }




                            break;
                        default:
                            this.BrokenLink += string.Format("{0} : {1}\n", urlList[i].ToString(), resp.StatusCode.ToString());
                            break;
                    }
                }
                catch (Exception ex)
                {
                    //Console.WriteLine(string.Format("{0}:{1}\n", urlList[i].ToString(), ex.Message.ToString()));
                    string exMsg = ex.Message.ToString();
                    if (exMsg.Contains("(403) Forbidden") == false &&
                        exMsg.Contains("The request was aborted: Could not create SSL/ TLS secure channel") == false &&
                        exMsg.Contains("The request was aborted") == false &&
                        exMsg.Contains("The operation has timed out") == false &&
                        exMsg.Contains("Too many automatic redirections were attempted") == false &&
                        exMsg.Contains("The underlying connection was closed") == false &&
                        exMsg.Contains("NonAuthoritativeInformation") == false &&
                        exMsg.Contains("Internal Server Error") == false &&
                        exMsg.Contains("The Authority/Host could not be parsed") == false &&
                        exMsg.Contains("Unable to connect to the remote server") == false &&
                        exMsg.Contains("The remote server returned an error: (408) Request Timeout") == false
                        )
                    {
                        this.BrokenLink += string.Format("Error : {0} -> {1}\n", urlList[i].ToString(), ex.Message.ToString());
                    }

                }
                finally
                {
                    if (req != null)
                    {
                        req.Abort();
                    }
                    if (resp != null)
                    {
                        resp.Close();
                    }
                }
            }

            if (req != null)
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
            string[] curtymlFiles = System.IO.Directory.GetFiles(parentPath, "*.yml");
            this.CheckFileList.AddRange(curtymlFiles);
            string[] curtDirList = System.IO.Directory.GetDirectories(parentPath);

            for (int i = 0; i < curtDirList.Length; i++)
            {
                this.GetAllFilesInDirectory(curtDirList[i]);
            }

        }

        public string GetMetuxFileName(string filepath)
        {
            string fileName = string.Empty;
            fileName = filepath.Replace(":", "_").Replace(@"\", "_").Replace("/", "_").ToUpper();
            return fileName;
        }


        public bool GetProcessConvertRule(ref JObject JConvert, ConvertCategory category, int iIndex, ConvertItem key, ref string valReturn)
        {
            bool bProcess = false;

            try
            {
                valReturn = JConvert[category.ToString()][iIndex][key.ToString()].ToString();
                bProcess = true;
            }
            catch (Exception ex)
            {
                string error = ex.Message.ToString();
            }

            return bProcess;

        }

        public void ReplaceIncludeLinkWithContent(string articlePath, ref string articleContent)
        {
            MatchCollection matches;
            string filename = string.Empty;
            string error = string.Empty;

            string mdfilePatFirst = "\\[\\!(INCLUDE|include)\\s*\\[[^\\[\\]]*\\]\\s*\\((?<mdfilename>[^\\[\\]\\(\\)]*)\\)\\]";
            matches = Regex.Matches(articleContent, mdfilePatFirst);
            string checkDirectory = string.Empty;

            string localpath = CommonFun.GetConfigurationValue("RepositoryENUSArticleDir", ref error);
            //string curtDirectory = string.Empty;
            string curtIncludePath = string.Empty;
            string includeContent = string.Empty;
            string mdfilePatSecond = string.Empty;

            Regex reg = null;
            for (int i = 0; i < matches.Count; i++)
            {
                //First remove the current directory ./ when exists.
                filename = matches[i].Groups["mdfilename"].ToString().Trim();
                mdfilePatSecond = "\\[!(INCLUDE|include)\\s*\\[[^\\[\\]]*\\]\\s*\\(" + filename + "[^\\[\\]\\(\\)]*\\)\\]";

                if (filename.Length > 0)
                {
                    checkDirectory = articlePath.Substring(0, articlePath.LastIndexOf("\\"));
                }

                while (filename.IndexOf("../") > -1)
                {
                    filename = filename.Substring(filename.IndexOf("../") + 3);
                    checkDirectory = checkDirectory.Substring(0, checkDirectory.LastIndexOf("\\"));
                }

                curtIncludePath = string.Format("{0}\\{1}", checkDirectory, filename);
                curtIncludePath = curtIncludePath.Replace("/", "\\");

                bool needLog = true;
                includeContent = this.ReadArticleContent(curtIncludePath, needLog);

                //Replace the INCLUDE link 
                reg = new Regex(mdfilePatSecond, RegexOptions.Multiline | RegexOptions.IgnoreCase);
                articleContent = reg.Replace(articleContent, includeContent);

            }
        }

        public string ReadArticleContent(string filePath, bool needLog)
        {
            string fullContent = string.Empty;
            FileStream fs = null;
            StreamReader sr = null;
            //string error = "";
            

            try
            {
                this.FileMutex = new Mutex(false, GetMetuxFileName(filePath));
                this.FileMutex.WaitOne();

                fs = new FileStream(filePath, FileMode.Open);
                sr = new StreamReader(fs);
                fullContent = sr.ReadToEnd();

                sr.Close();
            }
            catch (Exception ex)
            {
                if(needLog == true)
                {
                    this.BrokenLink += string.Format("Error : {0}\n", ex.Message.ToString());
                }
                
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

            return fullContent;
        }

        public void ProcessFileCustomize()
        {
            this.Status = ProcessStatus.Start;

            string fullContent = string.Empty;

            bool needLog = true;
            fullContent = this.ReadArticleContent(this.FullPath, needLog);

            // If we involve the ReplaceLink function. It will cause the images file missing. 
            // images function will reference the include file check itself. 
            //this.ReplaceIncludeLinkWithContent(this.FullPath, ref fullContent);

            try
            {
                
                ConvertCategory category = this.ProcessCategory;
                Console.WriteLine(string.Format("Processing Thread[{0}] : {1}", this.Id, this.FullPath));

                this.ProcessConvertJson(ref fullContent);

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
