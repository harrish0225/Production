using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.IO;
using System.Text.RegularExpressions;
using CheckBrokenLink.ProcessLibrary;
using System.Threading;
using System.Collections;

namespace CheckBrokenLink
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
            ConvertCategory category = ConvertCategory.CheckBrokenLinkByService;
            ConvertProcess process = ConvertProcess.ShowResult;

            if (iCount > 2)
            {
                ShowUseageTip();
                return;

            }else
            {
                switch (args[0].TrimStart('-'))
                {
                    case "S":
                    case "s":
                        category = ConvertCategory.CheckBrokenLinkByService;
                        break;
                    case "F":
                    case "f":
                        category = ConvertCategory.CheckBrokenLinkByFile;
                        break;
                    default:
                        ShowUseageTip();
                        return;
                }

                if(iCount==2)
                {
                    switch (args[1].TrimStart('-'))
                    {
                        case "H":
                        case "h":
                            process = ConvertProcess.ShowHistory;
                            break;
                        case "R":
                        case "r":
                            process = ConvertProcess.ShowResult;
                            break;
                        default:
                            ShowUseageTip();
                            return;
                    }
                }
                
             
            }
            

            string error = "";
         
            string fileprefix= CommonFun.GetConfigurationValue("GlobalRepository", ref error);
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

            List<FileCustomize> fileList = new List<FileCustomize>();
            ArrayList arrFile = new ArrayList();

            //Get the thread count
            int threadCount = 0;

            switch (category)
            {
                case ConvertCategory.CheckBrokenLinkByService:
                    arrFile = GetFileListByService();
                    break;
                case ConvertCategory.CheckBrokenLinkByFile:
                    arrFile = GetFileListByArticles();
                    break;
            }


            threadCount = arrFile.Count;
            Thread[] newThreads = new Thread[threadCount];

            for (int i=0; i < threadCount; i++)
            {
                curtFullName = arrFile[i].ToString();
                filename = Path.GetFileName(curtFullName);
                directory = curtFullName.Substring(fileprefix.Length,curtFullName.Length- fileprefix.Length- filename.Length-1).Replace("\\","/");
               

                FileCustomize curtFile = new FileCustomize(i, filename, directory, customizedate, category, process);
                fileList.Add(curtFile);
                newThreads[i] = new Thread(new ThreadStart(curtFile.ProcessFileCustomize));
                newThreads[i].Start();


                //newThreads[i].Join();
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

            if (category == ConvertCategory.ALL || category == ConvertCategory.H1ToTitle)
            {
                Console.WriteLine();
                Console.WriteLine(String.Format("**********************Check Process Result({0})**********************", ConvertCategory.H1ToTitle.ToString()));

                foreach (FileCustomize curtFile in fileList)
                {
                    if (!string.IsNullOrEmpty(curtFile.WarningMessage))
                    {
                        Console.WriteLine(curtFile.Fullpath);
                        Console.WriteLine("File: \t{0} \tMessage: \t{1}",curtFile.Fullpath, curtFile.WarningMessage);
                    }
                }
            }

            if (category == ConvertCategory.CheckBrokenLinkByFile || category == ConvertCategory.CheckBrokenLinkByService)
            {
                StringBuilder sbText = new StringBuilder();

                Console.WriteLine();
                sbText.AppendLine(String.Format("**********************Check Process Result({0})**********************", category.ToString()));
                //Console.WriteLine(String.Format("**********************Check Process Result({0})**********************", category.ToString()));

                int iIdx = 1;

                foreach (FileCustomize curtFile in fileList)
                {
                    if (!string.IsNullOrEmpty(curtFile.BrokenLink))
                    {
                        //Console.WriteLine("{0} : {1} ",iIdx++,  curtFile.File);
                        //Console.WriteLine(curtFile.BrokenLink);
                        sbText.AppendLine(string.Format("{0} : {1} ", iIdx++, curtFile.File));
                        string[] param = curtFile.BrokenLink.Split('\n');
                        foreach(string curtText in param)
                        {
                            sbText.AppendLine(curtText);
                        }
                        
                    }
                }

                CommonFun.GenerateDownloadFile(category, ref sbText);


            }

            Console.WriteLine("Program run finished, Press <Enter> to exit....");

            ExitWithUserConfirm();

        }

        public static ArrayList GetFileListByService()
        {
            CollectAllFileByService fileByService = new CollectAllFileByService();
            ArrayList arrFile = new ArrayList();
            arrFile = fileByService.GetAllFileByService();

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
            string fullfilename = string.Empty;

            string globalpath = string.Empty;


            globalpath = CommonFun.GetConfigurationValue("GlobalRepository", ref error);


            StreamReader sr = File.OpenText(configfile);
            string row = "";
            row = sr.ReadLine();

            while (row != null)
            {
                string[] para = row.Split(new Char[] { '\t' });

                filename = para[0];
                directory = para[1];

                switch (directory)
                {
                    case "includes":
                        fullfilename = string.Format("{0}{1}/{2}", globalpath, directory,filename);
                        
                        break;
                    default:
                        fullfilename = string.Format("{0}{1}/{2}", globalpath, directory, filename);
                        break;
                }
                arrFile.Add(fullfilename);

                row = sr.ReadLine();

            }


            return arrFile;

        }

        static void ShowUseageTip()
        {
            Console.WriteLine("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
            Console.WriteLine("Useage: CheckBrokenLink -S|-F -H|-R");
            Console.WriteLine("-S means Check broken link by service ");
            Console.WriteLine("-F means Check broken link by filelist ");
            Console.WriteLine("-H means display all result which include successfully ");
            Console.WriteLine("-R means display the result ");
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
        

        
    }
}
