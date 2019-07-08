using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.IO;
using System.Text.RegularExpressions;
using H1ToTitle.ProcessLibrary;
using System.Threading;
using System.Collections;

namespace H1ToTitle
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
            ConvertCategory category = ConvertCategory.SXSUpdate;
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
                                    case "X":
                                        category = ConvertCategory.SXSUpdate;
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
         
            string fileprefix= CommonFun.GetConfigurationValue("GlobalTaregetRepository", ref error);
            if (error.Length > 0)
            {
                return;
            }

            string customizedate = CommonFun.GetConfigurationValue("CustomizeDate", ref error);
            if (error.Length > 0)
            {
                return;
            }

            string filename = "";
            string directory = "";
            string curtFullName = "";

            ArrayList arrFile = new ArrayList();

            List<FileCustomize> fileList = new List<FileCustomize>();

            int threadCount = 0;

            switch (itercategory)
            {
                case CustomizedCategory.CustomizedByService:
                    customizedate = DateTime.Now.ToString("MM/dd/yyyy");
                    arrFile = GetFileListByService(customizedate);
                    break;
                case CustomizedCategory.CustomizedByFile:
                    arrFile = GetFileListByArticles();
                    break;
            }

            ThreadPool.SetMinThreads(1000, 1000);

            threadCount = arrFile.Count;
            Thread[] newThreads = new Thread[threadCount];

            string[] para = new string[] { };

            for (int i=0; i < threadCount; i++)
            {
                para = (string[])arrFile[i];
                filename = para[0];
                directory = para[1];
                customizedate = para[2];


                FileCustomize curtFile = new FileCustomize(i, filename, directory, customizedate, category);
                fileList.Add(curtFile);
                newThreads[i] = new Thread(new ThreadStart(curtFile.ProcessFileCustomize));
                newThreads[i].Start();
#if DEBUG
                newThreads[i].Join();
#endif
                
            }
            

            bool allThreadOver = false;
            while (allThreadOver == false)
            {
                Thread.Sleep(10000);
                allThreadOver = true;
                for (int i = 0; i < threadCount; i++)
                {
                    if (newThreads[i].ThreadState != ThreadState.Stopped)
                    {
                        allThreadOver = false;
                        Console.WriteLine(string.Format("Checking status of the Thread[{0}] : {1} ", i, newThreads[i].ThreadState.ToString()));
                        break;
                    }
                }
            }

            if (category == ConvertCategory.ALL || category == ConvertCategory.IncludeParentFile)
            {
                Console.WriteLine();
                Console.WriteLine(String.Format("**********************Check Process Result({0})**********************", ConvertCategory.IncludeParentFile.ToString()));

                foreach (FileCustomize curtFile in fileList)
                {
                    if (curtFile.ArticleCategory == FileCategory.Includes)
                    {

                        Console.WriteLine("The parent file of \t{0} \tis \t{1}", curtFile.File, curtFile.ParentFile);
                    }
                }
            }

            if (category == ConvertCategory.ALL || category == ConvertCategory.SXSUpdate)
            {
                Console.WriteLine();
                Console.WriteLine(String.Format("**********************Check Process Result({0})**********************", ConvertCategory.SXSUpdate.ToString()));

                foreach (FileCustomize curtFile in fileList)
                {
                    if (!string.IsNullOrEmpty(curtFile.WarningMessage))
                    {
                        Console.WriteLine(curtFile.Fullpath);
                        Console.WriteLine("File: \t{0} \tMessage: \t{1}",curtFile.Fullpath, curtFile.WarningMessage);
                    }
                }
            }

            Console.WriteLine("Program run finished, Press <Enter> to exit....");

            ExitWithUserConfirm();

        }

        static void ShowUseageTip()
        {
            Console.WriteLine("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
            Console.WriteLine("Useage: LiveSXSUpdate --Service [S|F] --Customize [X]");
            Console.WriteLine("The First Parameter group --Service [S|F] means We customized the file [By Servie|By FileList]");
            Console.WriteLine("The Second Parameter group --Customize [X] means which format we should use to Customized the specific articles");
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

            string globalpath = string.Empty;


            globalpath = CommonFun.GetConfigurationValue("GlobalTaregetRepository", ref error);


            StreamReader sr = File.OpenText(configfile);
            string row = "";
            row = sr.ReadLine();

            while (row != null)
            {
                string[] para = row.Split(new Char[] { '\t' });

                filename = para[0];
                directory = para[1];
                customizedate = para[2];

                switch (directory)
                {
                    case "includes":
                        fullfilename = string.Format("{0}{1}/{2}", globalpath, directory, filename);

                        break;
                    default:
                        fullfilename = string.Format("{0}{1}/{2}", globalpath, directory, filename);
                        break;
                }
                arrFile.Add(new string[] { filename, directory, customizedate });

                row = sr.ReadLine();

            }


            return arrFile;

        }



    }
}

        
