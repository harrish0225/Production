using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.IO;
using System.Text.RegularExpressions;
using AuthorCustomization.ProcessLibrary;
using System.Threading;
using System.Collections;

namespace AuthorCustomization
{

    class Program
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            int iCount = args.Count();

            ConvertCategory category = ConvertCategory.ALL;
            CustomizedCategory itercategory = CustomizedCategory.CustomizedByFile;
            CommandPara curtpara = CommandPara.Null;

            for (int i = 0; i < iCount; i++)
            {
                switch (args[i].ToUpper().Trim())
                {
                    case "--SERVICE":
                    case "-S":
                        curtpara = CommandPara.Servcie;
                        break;
                    case "--CUSTOMIZE":
                    case "-C":
                        curtpara = CommandPara.Customize;
                        break;
                    case "--HELP":
                    case "-H":
                        ShowUseageTip();
                        return;
                    default:
                        switch (curtpara)
                        {
                            case CommandPara.Servcie:
                                switch (args[i].ToUpper().Trim())
                                {
                                case "S":
                                    itercategory = CustomizedCategory.CustomizedByService;
                                    break;
                                case "F":
                                    itercategory = CustomizedCategory.CustomizedByFile;
                                    break;
                                default:
                                    curtpara = CommandPara.VerifyFail;
                                    break;
                                }
                                break;
                            case CommandPara.Customize:
                                switch (args[i].ToUpper().Trim())
                                {
                                    case "A":
                                        category = ConvertCategory.AuthorReplacement;
                                        break;
                                    case "U":
                                        category = ConvertCategory.URLReplacement;
                                        break;
                                    case "C":
                                        category = ConvertCategory.URLCorrection;
                                        break;
                                    case "F":
                                        category = ConvertCategory.FindArticle;
                                        break;
                                    case "I":
                                        category = ConvertCategory.IncludeParentFile;
                                        break;
                                    case "T":
                                        category = ConvertCategory.ToolReplacement;
                                        break;
                                    default:
                                        curtpara = CommandPara.VerifyFail;
                                        break;
                                }
                                break;
                            case CommandPara.Null:
                                curtpara = CommandPara.VerifyFail;
                                break;
                            case CommandPara.VerifyFail:
                                ShowUseageTip();
                                return;
                        }
                        break;
                }
                        
            }
          

            

            string error = "";
            string configfile = CommonFun.GetConfigurationValue("customerfilepath", ref error);
            if (error.Length > 0)
            {
                return;
            }

            StreamReader sr = null;

           

            string filename = "";
            string directory = "";
            string customizedate = "";

            List<FileCustomize> fileList = new List<FileCustomize>();

            ArrayList arrFile = new ArrayList();

            int threadCount = 0;

            switch (itercategory)
            {
                case  CustomizedCategory.CustomizedByService:
                    customizedate = DateTime.Now.ToString("MM/dd/yyyy");
                    arrFile = GetFileListByService(customizedate);
                    break;
                case CustomizedCategory.CustomizedByFile:
                    arrFile = GetFileListByArticles();
                    break;
            }
           


                //Get the thread count



            ThreadPool.SetMinThreads(1000, 1000);

            threadCount = arrFile.Count;
            Thread[] newThreads = new Thread[threadCount];

            // Declare the application thread.


            string[] para = new string[] { };

            for (int i=0;i<threadCount;i++)
            {
                para = (string[])arrFile[i];
                filename = para[0];
                directory = para[1];
                customizedate = para[2];
                FileCustomize curtFile = new FileCustomize(i,filename, directory, customizedate,category);
                fileList.Add(curtFile);
                newThreads[i] = new Thread(new ThreadStart(curtFile.ProcessFileCustomize));
                newThreads[i].Name = string.Format("{0}/{1}", directory, filename);
                newThreads[i].Start();
                Console.WriteLine(string.Format("Start the Thread[{0}] in application...", i));


                // Console.WriteLine(string.Format("Join the {0} thread in application", threadIdx));
#if DEBUG
                newThreads[i].Join();
#endif

            }

            bool allThreadOver = false;
            while (allThreadOver == false)
            {
                Thread.Sleep(20000);
                allThreadOver = true;
                for (int i = 0; i < threadCount; i++)
                {
                    if (newThreads[i].ThreadState != ThreadState.Stopped)
                    {
                        allThreadOver = false;
                        Console.WriteLine(string.Format("Checking status of the Thread[{0}] \t : \t {1} -> \t {2}", i, newThreads[i].ThreadState.ToString(), newThreads[i].Name.ToString()));
                        break;
                    }
                }
            }
            

            if(category==ConvertCategory.ALL || category== ConvertCategory.IncludeParentFile )
            {
                //Display the Parent of Include file in Console. 
                string sPath = string.Empty;
                string sFileName = string.Empty;
                int idxLastDash = 0;

                foreach(FileCustomize curtFile in fileList)
                {
                    
                    if (curtFile.ArticleCategory == FileCategory.Includes)
                    {

                        idxLastDash= curtFile.ParentFile.LastIndexOf(@"\");
                        if (idxLastDash >= 0)
                        {
                            sPath = curtFile.ParentFile.Substring(0, idxLastDash);
                            sFileName = curtFile.ParentFile.Substring(idxLastDash + 1);
                        }
                        else
                        {
                            sPath = "";
                            sFileName = "";
                        }
                        
                        Console.WriteLine(string.Format("The parent file of\t{0}\tis\t{1}\t{2}\t.", curtFile.File, sPath,sFileName));
                    }   
                }


                //Save the download file for the parent of Include file in text file.
                StringBuilder sbText = new StringBuilder();

                Console.WriteLine();
                sbText.AppendLine(String.Format("**********************Check Process Result({0})**********************", ConvertCategory.IncludeParentFile.ToString()));
                //Console.WriteLine(String.Format("**********************Check Process Result({0})**********************", category.ToString()));

                foreach (FileCustomize curtFile in fileList)
                {
                    if (curtFile.ArticleCategory == FileCategory.Includes)
                    {
                        //if (!string.IsNullOrEmpty(curtFile.ParentFile))
                        //{
                        idxLastDash = curtFile.ParentFile.LastIndexOf(@"\");
                        if (idxLastDash >= 0)
                        {
                            sPath = curtFile.ParentFile.Substring(0, idxLastDash);
                            sFileName = curtFile.ParentFile.Substring(idxLastDash + 1);
                        }
                        else
                        {
                            sPath = "";
                            sFileName = "";
                        }
                        sbText.AppendLine(string.Format("The parent file of\t{0}\tis\t{1}\t{2}\t.", curtFile.File, sPath, sFileName));
                        //}
                    }
                        
                }

                CommonFun.GenerateDownloadFile(category, ref sbText);
            }

            //Thread.Sleep(5000);
            Console.WriteLine("Program run finished, Press <Enter> to exit....");

            ExitWithUserConfirm();

        }

        static void ShowUseageTip()
        {
            Console.WriteLine("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
            Console.WriteLine("Useage: Your can use eithor of the following commandformat");
            Console.WriteLine("AuthorCustomization --Service [S|F] --Customize [A|U|C|F|I|T]");
            Console.WriteLine("AuthorCustomization -S [S|F] -C [A|U|C|F|I|T]");
            Console.WriteLine("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
            Console.WriteLine("The First Parameter group --Service [S|F] means We customized the file [By Servie|By FileList]");
            Console.WriteLine("The Second Parameter group --Customize [A|U|C|F|I|T] means which format we should use to Customized the specific articles");
            Console.WriteLine("A means replace the AuthorReplacement");
            Console.WriteLine("U means replace the URLReplacement");
            Console.WriteLine("C means replace the URLCorrection");
            Console.WriteLine("F means find and mark the invloved articles");
            Console.WriteLine("I means find the one refrenced parent's articles");
            Console.WriteLine("T means replace the Tool");
            Console.WriteLine("You can alter the specific section in the ConvertRule.json");
            Console.WriteLine("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
            Console.WriteLine("Press <Enter> to exit....");

            ExitWithUserConfirm();
        }

        static void ExitWithUserConfirm()
        {
            while (Console.ReadKey().Key != ConsoleKey.Enter)
            {

                return;

            }

            return;
        }


        public static ArrayList GetFileListByService(string customizedate)
        {
            CollectAllFileByService fileByService = new CollectAllFileByService();
            ArrayList arrFile = new ArrayList();
            arrFile = fileByService.GetAllFileByServiceWithCustomziedate(customizedate);

            return arrFile;

        }

        public static ArrayList GetFileListByArticles()
        {
            string error = "";
            ArrayList arrFile = new ArrayList();
            string configfile = CommonFun.GetConfigurationValue("customerfilepath", ref error);
            if (error.Length > 0)
            {
                return null;
            }

            string filename = string.Empty;
            string directory = string.Empty;
            string customizedate = string.Empty;
            string fullfilename = string.Empty;

            //string globalpath = string.Empty;


            //globalpath = CommonFun.GetConfigurationValue("GlobalRepository", ref error);


            StreamReader sr = File.OpenText(configfile);
            string row = "";
            row = sr.ReadLine();

            while (row != null)
            {
                string[] para = row.Split(new Char[] { '\t' });

                filename = para[0];
                directory = para[1];
                customizedate = para[2];

                //switch (directory)
                //{
                //    case "includes":
                //        fullfilename = string.Format("{0}{1}/{2}", globalpath, directory, filename);

                //        break;
                //    default:
                //        fullfilename = string.Format("{0}{1}/{2}", globalpath, directory, filename);
                //        break;
                //}
                arrFile.Add(new string[] { filename,directory, customizedate});

                row = sr.ReadLine();

            }


            return arrFile;

        }



    }
}
