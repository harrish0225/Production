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

namespace CLIReplacement.ProcessLibrary
{
    public enum ConvertItem {
        global,
        mooncake,
        source_path,
        redirect_url,
        redirect_document_id,
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
        redirections,
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
                diskpath = CommonFun.GetConfigurationValue("GlobalArticleDir", ref message);

                parentpath = string.Format("{0}\\{1}", diskpath, curtService.ToString().Replace('_', '-'));

                this.GetAllFilesInDirectory(parentpath);
                
            }

            fileList = this.CheckFileList;
            return fileList;
        }

    }

    public class CollectRedirectFileByArticle : FileCustomize
    {
        public string globalContent = string.Empty;
        public string GlobalContent { get; set; }

        public string mooncakeContent = string.Empty;
        public string MooncakeContent { get; set; }

        public string redirectGContent = string.Empty;
        public string RedirectGContent { get; set; }

        public string redirectMContent = string.Empty;
        public string RedirectMContent { get; set; }

        public CollectRedirectFileByArticle(): base()
        {

        }

        public CollectRedirectFileByArticle(int id, string filename, string directory, string customizedate, ConvertCategory category, string globalContent, string mooncakeContent): base(id, filename, directory, customizedate,  category)
        {
            this.GlobalContent = globalContent;
            this.MooncakeContent = mooncakeContent;
            this.RedirectGContent = string.Empty;
            this.RedirectMContent = string.Empty;
        }

        public override void ProcessFileCustomize() 
        {
            this.Status = ProcessStatus.Start;
            string error = string.Empty;

            try
            {

                ConvertCategory category = this.ProcessCategory;
                Console.WriteLine(string.Format("Processing Thread[{0}] : {1}", this.Id, this.FullPath));
                string globalContent = this.GlobalContent;
                string mooncakeContent = this.MooncakeContent;

                this.ProcessConvertJson(ref globalContent, ref mooncakeContent);

            }
            catch (Exception ex)
            {
                error = ex.Message.ToString();
            }
            finally
            {
               
            }

            this.Status = ProcessStatus.Complete;
            Console.WriteLine("Check Status of the Thread[{0}] : {1}", this.Id, this.Status.ToString());


        }

        public string GenerateRedirectJson(string sourcepath, string redirecturl, string redirect_document_id)
        {

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("\t{");
            sb.AppendLine(string.Format("\t\"source_path\": \"{0}\",", sourcepath));
            if(redirecturl.Substring(0, 6)=="/azure")
            {
                sb.AppendLine(string.Format("\t\"redirect_url\": \"{0}\",", redirecturl.Substring(6)));
            }
            else
            {
                sb.AppendLine(string.Format("\t\"redirect_url\": \"{0}\",", redirecturl));
            }

            //sb.AppendLine(string.Format("\t\t\t\"redirect_url\": \"{0}\",", redirecturl));

            sb.AppendLine(string.Format("\t\"redirect_document_id\": \"{0}\"", redirect_document_id));
            sb.AppendLine("\t},");

            return sb.ToString();

        }

        public void ProcessConvertJson(ref string globalContent, ref string mooncakeContent)
        {

            this.Status = ProcessStatus.Process;

            ConvertCategory category = this.ProcessCategory;

            JObject JGlobal = null;
            JObject JMoonCake = null;
            int iGCount = 0;
            int iMCount = 0;

            //Redirect Section
            if (category == ConvertCategory.redirections)
            {
                try
                {
                    JGlobal = (JObject)JsonConvert.DeserializeObject(globalContent);
                    iGCount = JGlobal[ConvertCategory.redirections.ToString()].Count();
                }
                catch(Exception ex)
                {
                    Console.WriteLine("Convert Global Redirection Json file Failed: ");
                    Console.WriteLine(string.Format("Reason: {0}", ex.Message.ToString()));
                }

                try
                {
                    JMoonCake = (JObject)JsonConvert.DeserializeObject(mooncakeContent);
                    iMCount = JMoonCake[ConvertCategory.redirections.ToString()].Count();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Convert Mooncake Redirection Json file Failed: ");
                    Console.WriteLine(string.Format("Reason: {0}", ex.Message.ToString()));
                }


                Regex reg = null;
                string sourcePath = string.Empty;
                string redirectUrl = string.Empty;
                string redirectDocumentid = string.Empty;

                bool bGlobalFetch = false;
                bool bMoonFetch = false;
                bool bFetch = false;


                for(int i= 0; i < iGCount; i++)
                {
                    bFetch = this.GetProcessConvertRule(ref JGlobal, ConvertCategory.redirections, i, ConvertItem.source_path, ref sourcePath);
                    if (bFetch == true)
                    {
                        if (sourcePath.ToLower() == this.RelativeFile.ToLower())
                        {
                            bFetch = this.GetProcessConvertRule(ref JGlobal, ConvertCategory.redirections, i, ConvertItem.redirect_url, ref redirectUrl) &&
                                this.GetProcessConvertRule(ref JGlobal, ConvertCategory.redirections, i, ConvertItem.redirect_document_id, ref redirectDocumentid);
                            if (bFetch == true)
                            {
                                this.RedirectGContent += this.GenerateRedirectJson(sourcePath, redirectUrl, redirectDocumentid);
                                bGlobalFetch = true;
                                //break;
                            }


                        }
                    }
                }

                for (int i = 0; i < iMCount; i++)
                {
                    bFetch = this.GetProcessConvertRule(ref JMoonCake, ConvertCategory.redirections, i, ConvertItem.source_path, ref sourcePath);
                    if (bFetch == true)
                    {
                        if (sourcePath.ToLower() == this.RelativeFile.ToLower())
                        {
                            bFetch = this.GetProcessConvertRule(ref JMoonCake, ConvertCategory.redirections, i, ConvertItem.redirect_url, ref redirectUrl) &&
                                this.GetProcessConvertRule(ref JMoonCake, ConvertCategory.redirections, i, ConvertItem.redirect_document_id, ref redirectDocumentid);
                            if (bFetch == true)
                            {
                                this.RedirectMContent += this.GenerateRedirectJson(sourcePath, redirectUrl, redirectDocumentid);
                                bMoonFetch = true;
                                //break;
                            }


                        }
                    }
                }

               

            }


            //articleContent += "\n";


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

        string relativefile = "";
        public string RelativeFile { get; set; }

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

                diskpath = CommonFun.GetConfigurationValue("GlobalArticleDir", ref message);
                relativefile = GetRightFileName(para);
                this.FullPath = string.Format(@"{0}\{1}\{2}", diskpath, relativefile, this.File);
                this.RelativeFile = string.Format(@"articles/{0}/{1}", relativefile, this.File).Replace(@"\","/");
                this.ArticleCategory = FileCategory.Article;

            }

            if (para[0].ToLower() == "includes")
            {

                diskpath = CommonFun.GetConfigurationValue("GlobalIncludeDir", ref message);
                relativefile = GetRightFileName(para);
                this.FullPath = string.Format(@"{0}\{2}", diskpath, relativefile, this.File);
                this.RelativeFile = string.Format(@"includes/{0}", this.File).Replace(@"\", "/");
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

            this.CheckFileList = new ArrayList();

        }

        public void RepalceParameter(ref string paraValue)
        {

            if (paraValue.Contains(ReplaceParam.CustimzedDate.ToString()))
            {
                paraValue = paraValue.Replace("{CustimzedDate}", this.CustomizedDate);
            }

        }

        public virtual void ProcessConvertJson(ref string articleContent)
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
            parentInclude = this.FullPath;

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
        public virtual void ProcessFileCustomize()
        {
            this.Status = ProcessStatus.Start;

            FileStream fs = new FileStream(this.FullPath, FileMode.OpenOrCreate);

            StreamReader sr = null;
            StreamWriter sw = null;
            string error = "";
            try
            {
                sr = new StreamReader(fs);
                string fullcontent = sr.ReadToEnd();

                sr.Close();

                ConvertCategory category = this.ProcessCategory;
                Console.WriteLine(string.Format("Processing Thread[{0}] : {1}", this.Id, this.FullPath));

                this.ProcessConvertJson(ref fullcontent);
                
                sw = new StreamWriter(this.FullPath,false);
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
