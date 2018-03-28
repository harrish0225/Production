using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.IO;
using System.Text.RegularExpressions;
using CLIReplacement.ProcessLibrary;
using System.Threading;
using System.Collections;

namespace CLIReplacement
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
            if (iCount > 1)
            {
                ShowUseageTip();
                return;

            }else if (iCount==1)
            {
                string sParam = args[0].TrimStart('-').ToUpper();
                switch (sParam)
                {
                    case "CLI":
                        category = ConvertCategory.CLIReplacement;
                        break;
                    default:
                        ShowUseageTip();
                        return;
                }
            }
            

            string error = "";
            string configfile = CommonFun.GetConfigurationValue("customerfilepath", ref error);
            if (error.Length > 0)
            {
                return;
            }
         
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

#if debug
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
                foreach (FileCustomize curtFile in fileList)
                {
                    if (curtFile.ArticleCategory == FileCategory.Includes)
                    {
                        Console.WriteLine("The parent file of {0} is {1}", curtFile.File, curtFile.ParentFile);
                    }
                }
            }

            Console.WriteLine("Program run finished, Press <Enter> to exit....");

            ExitWithUserConfirm();

        }

        static void ShowUseageTip()
        {
            Console.WriteLine("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
            Console.WriteLine("Useage: CLIReplacement [-CLI]");
            Console.WriteLine("-CLI means find the one refrenced parent's articles");
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
