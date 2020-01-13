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

namespace LiveSXSUpdate.ProcessLibrary
{
    public enum ConvertItem {
        global,
        mooncake,
        //H1,
        //Title,
        //TitleReplace,
        //TitleReplaceWithQuotation,
        Source,
        Target,
        SourceRegex,
        TargetRegex,
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
        SXSUpdate,
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
        aks,
        azure_resource_manager,
        container_registry,
        cosmos_db,
        firewall,
        //private_link,
        service_fabric,
        site_recovery,
        traffic_manager,
        virtual_machines,
        virtual_network,
        virtual_wan,
        includes,
    }


    public enum ReplaceParam
    {
        CustimzedDate,
    }

    public enum CustomizedCategory
    {
        CustomizedByService,
        CustomizedByFile,
    }

    public enum CommandPara
    {
        Null,
        Servcie,
        Customize,
        VerifyFail,
    }

    public enum FileCategory
    {
        Articles,
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
                diskpath = CommonFun.GetConfigurationValue("GlobalTaregetArticleDir", ref message);

                parentpath = string.Format("{0}\\{1}", diskpath, curtService.ToString().Replace('_', '-'));

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
                if (curtService.ToString().ToLower() == "includes".ToLower())
                {
                    diskpath = CommonFun.GetConfigurationValue("GlobalTaregetIncludeDir", ref message);
                    parentpath = string.Format("{0}", diskpath);
                }
                else
                {
                    diskpath = CommonFun.GetConfigurationValue("GlobalTaregetArticleDir", ref message);
                    parentpath = string.Format("{0}\\{1}", diskpath, curtService.ToString().Replace('_', '-'));
                }

                this.GetAllFilesInDirectoryWithCustomizedate(parentpath, customizedate);

            }

            fileList = this.CheckFileList;
            return fileList;
        }

        public void GetAllFilesInDirectoryWithCustomizedate(string parentPath, string customizedate)
        {
            string[] curtFiles = System.IO.Directory.GetFiles(parentPath, "*.md", SearchOption.AllDirectories);
            string filename = string.Empty;
            string directory = string.Empty;

            string curtValue = string.Empty;
            string[] curtKey = new string[] { };

            int iStartIdx = 0;

            if (curtFiles.Length > 0)
            {
                curtValue = curtFiles[0].Replace("\\", "/");
                curtKey = curtValue.Split('/');

                for (int i = 0; i < curtKey.Length; i++)
                {
                    if (curtKey[i].ToUpper() == "ARTICLES" || curtKey[i].ToUpper() == "INCLUDES")
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





            string[] curtymlFiles = System.IO.Directory.GetFiles(parentPath, "*.yml",SearchOption.AllDirectories);

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

            //string[] curtDirList = System.IO.Directory.GetDirectories(parentPath);

            //for (int i = 0; i < curtDirList.Length; i++)
            //{
            //    this.GetAllFilesInDirectoryWithCustomizedate(curtDirList[i], customizedate);
            //}

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

        string fullmasterpath = "";
        public string FullMasterPath { get; set; }

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


        FileCategory articleCategory = FileCategory.Articles;
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
            string diskmasterpath = "";
            string message = "";
            string relativefile = "";

            if (para[0].ToLower() == "articles")
            {

                diskpath = CommonFun.GetConfigurationValue("GlobalTaregetArticleDir", ref message);
                diskmasterpath = CommonFun.GetConfigurationValue("GlobalSourceArticleDir", ref message);
                relativefile = GetRightFileName(para);
                this.Fullpath = string.Format(@"{0}\{1}\{2}", diskpath, relativefile, this.File);
                this.FullMasterPath = string.Format(@"{0}\{1}\{2}", diskmasterpath, relativefile, this.File);
                this.ArticleCategory = FileCategory.Articles;

            }

            if (para[0].ToLower() == "includes")
            {

                diskpath = CommonFun.GetConfigurationValue("GlobalTaregetIncludeDir", ref message);
                diskmasterpath = CommonFun.GetConfigurationValue("GlobalSourceIncludeDir", ref message);
                relativefile = GetRightFileName(para);
                this.Fullpath = string.Format(@"{0}\{2}", diskpath, relativefile, this.File);
                this.FullMasterPath = string.Format(@"{0}\{1}\{2}", diskmasterpath, relativefile, this.File);
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

        public bool isNewReleaseFile(ref string articleSourceContent, ref string articleTargetContent )
        {
            bool isMyFile = false;
            string ruleCheck = string.Empty;

            switch (this.ArticleCategory)
            {
                case FileCategory.Articles:
                    isMyFile = true;
                    break;
                case FileCategory.Includes:

                    ruleCheck = "ms\\.author:\\s*v-yeche";

                    Match match;

                    match = Regex.Match(articleSourceContent, ruleCheck);

                    if (match.Captures.Count > 0)
                    {
                        isMyFile = true;
                    }

                    break;
            }
            
            if(isMyFile == true)
            {
                Match matchSource;
                Match matchTarget;
                string sourceHandOff = string.Empty;
                string targetHandOff = string.Empty;

                ruleCheck = "ms.lasthandoff:\\s*(?<HandOffDate>\\S*)";

                matchSource = Regex.Match(articleSourceContent, ruleCheck);
                if (matchSource.Groups.Count > 0)
                {
                    sourceHandOff = matchSource.Groups["HandOffDate"].ToString();
                }

                matchTarget = Regex.Match(articleTargetContent, ruleCheck);
                if (matchTarget.Groups.Count > 0)
                {
                    targetHandOff = matchTarget.Groups["HandOffDate"].ToString();
                }

                if (sourceHandOff!=targetHandOff)
                {
                    articleTargetContent += "\n\r";
                }

            }


            return isMyFile;
        }

        public void ProcessConvertJson(ref string articleSourceContent,ref string articleTargetContent )
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
            bool bSource= false;
            bool bTarget = false;
            bool bSourceReg = false;
            bool bTargetReg = false;
            bool bConvert = false;
            Regex reg = null;

            //AuthorReplacement Section
            int iCount = 0;

            ConvertCategory category = this.ProcessCategory;

            //CLIReplacment Section

            if (category == ConvertCategory.SXSUpdate && isNewReleaseFile(ref articleSourceContent,ref articleTargetContent) == true)
            {


                string ruleSource = string.Empty;
                string ruleTarget = string.Empty;
                string ruleSourceReg = string.Empty;
                string ruleTargetReg = string.Empty;

                string sMasterSourceHash = string.Empty;
                string sLiveTargetHash = string.Empty;
                string sBeforeHashReplace = string.Empty;
                string sUpdatetHashReplace = string.Empty;

                int contentOrigal = articleSourceContent.Length;

                iCount = JUrl[ConvertCategory.SXSUpdate.ToString()].Count();

                Match match;
                for (int i = 0; i < iCount; i++)
                {
                    bSource = this.GetProcessConvertRule(ref JUrl, ConvertCategory.SXSUpdate, i, ConvertItem.Source, ref ruleSource);
                    if (bSource==true)
                    {
                        match = Regex.Match(articleSourceContent, ruleSource);
                        if(match.Groups.Count>0)
                        {
                           sMasterSourceHash = match.Groups["HashCode"].ToString();
                        }
                        else
                        {
                            bSource = false;
                        }
                    }

                    bTarget = this.GetProcessConvertRule(ref JUrl, ConvertCategory.SXSUpdate, i, ConvertItem.Target, ref ruleTarget);
                    if (bTarget == true)
                    {
                        match = Regex.Match(articleTargetContent, ruleTarget);
                        if (match.Groups.Count > 0)
                        {
                            sLiveTargetHash = match.Groups["HashCode"].ToString();
                        }
                        else
                        {
                            bTarget = false;
                        }
                        
                    }


                    if (bSource && bTarget && sMasterSourceHash!=sLiveTargetHash)
                    {

                        bSourceReg = this.GetProcessConvertRule(ref JUrl, ConvertCategory.SXSUpdate, i, ConvertItem.SourceRegex, ref ruleSourceReg);

                        if (bSourceReg == true)
                        {
                            sBeforeHashReplace = ruleSourceReg.Replace("{0}", sLiveTargetHash);
                        }

                        bTargetReg = this.GetProcessConvertRule(ref JUrl, ConvertCategory.SXSUpdate, i, ConvertItem.TargetRegex, ref ruleTargetReg);

                        if (bTargetReg == true)
                        {
                            sUpdatetHashReplace = ruleTargetReg.Replace("{0}", sMasterSourceHash);
                        }

                        if (bSourceReg && bTargetReg)
                        {
                            reg = new Regex(sBeforeHashReplace);
                            articleTargetContent = reg.Replace(articleTargetContent, sUpdatetHashReplace);
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
            string parentpath = CommonFun.GetConfigurationValue("GlobalTargetIncludeDir", ref message);

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
            string diskpath = CommonFun.GetConfigurationValue("GlobalTaregetArticleDir", ref message);
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

            FileStream fsSource = new FileStream(this.FullMasterPath, FileMode.OpenOrCreate);

            FileStream fsTarget = new FileStream(this.Fullpath, FileMode.OpenOrCreate);
            StreamReader sr = null;
            StreamWriter sw = null;
            string error = "";
            try
            {
                sr = new StreamReader(fsSource);
                string fullsourcecontent = sr.ReadToEnd();
                sr.Close();

                sr = new StreamReader(fsTarget);
                string fulltargetcontent = sr.ReadToEnd();
                sr.Close();


                ConvertCategory category = this.ProcessCategory;
                Console.WriteLine(string.Format("Processing Thread[{0}] : {1}", this.Id, this.Fullpath));

                this.ProcessConvertJson(ref fullsourcecontent,ref fulltargetcontent);
                
                sw = new StreamWriter(this.Fullpath,false);
                sw.Write(fulltargetcontent);
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

                if (fsSource != null)
                {
                    fsSource.Close();
                }

                if (fsTarget != null)
                {
                    fsTarget.Close();
                }
            }

            this.Status = ProcessStatus.Complete;
            Console.WriteLine("Check Status of the Thread[{0}] : {1}", this.Id, this.Status.ToString());


        }

       

    }


}
