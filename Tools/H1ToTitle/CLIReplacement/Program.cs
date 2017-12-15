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
            ConvertCategory category = ConvertCategory.H1ToTitle;
            if (iCount > 1)
            {
                ShowUseageTip();
                return;

            }else if (iCount==1)
            {
                switch (args[0].TrimStart('-'))
                {
                    case "R":
                    case "r":
                        category = ConvertCategory.H1ToTitle;
                        break;
                    default:
                        ShowUseageTip();
                        return;
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

            //Get the thread count
            int threadCount = 0;

            CollectAllFileByService fileByService = new CollectAllFileByService();
            ArrayList arrFile = new ArrayList();
            arrFile = fileByService.GetAllFileByService();

            threadCount = arrFile.Count;
            Thread[] newThreads = new Thread[threadCount];

            for (int i=0; i < threadCount; i++)
            {
                curtFullName = arrFile[i].ToString();
                filename = Path.GetFileName(curtFullName);
                directory = curtFullName.Substring(fileprefix.Length,curtFullName.Length- fileprefix.Length- filename.Length-1).Replace("\\","/");
               

                FileCustomize curtFile = new FileCustomize(i, filename, directory, customizedate, category);
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

            Console.WriteLine("Program run finished, Press <Enter> to exit....");

            ExitWithUserConfirm();

        }

        static void ShowUseageTip()
        {
            Console.WriteLine("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
            Console.WriteLine("Useage: H1ToTitle [-R|-r]");
            Console.WriteLine("-R means replace title with H1 description");
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
