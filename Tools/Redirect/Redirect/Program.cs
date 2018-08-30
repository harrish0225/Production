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
            //Check the Redirect URL Category
            int iCount = args.Count();
            ConvertCategory category = ConvertCategory.redirections;
            if (iCount > 1)
            {
                ShowUseageTip();
                return;

            }else if (iCount==1)
            {
                string curtArg = args[0].ToUpper().Trim();
                switch (args[0].TrimStart('-'))
                {
                    case "RDR":
                        category = ConvertCategory.redirections;
                        break;
                    default:
                        ShowUseageTip();
                        return;
                }
            }

            //Get the following config file , redirect file(both global and mooncake repository)
            bool errFlag = false;
            string error = "";
            string configfile = CommonFun.GetConfigurationValue("customerfilepath", ref error);
            if (error.Length > 0)
            {
                return;
            }

            string fileGlobal = CommonFun.GetConfigurationValue("GlobalRedirectFile", ref error);
            if (error.Length > 0)
            {
                return;
            }

            string fileMooncake = CommonFun.GetConfigurationValue("MooncakeRedirectFile", ref error);
            if (error.Length > 0)
            {
                return;
            }

            string mooncakeSite = CommonFun.GetConfigurationValue("MooncakeSite", ref error);
            if (error.Length > 0)
            {
                return;
            }


            ////Get the redirect file content(both global and mooncake repository)
            string fileGlobalContent = string.Empty;
            string fileMooncakeContent = string.Empty;

            errFlag = CommonFun.GetFileContent(fileGlobal, ref fileGlobalContent) && CommonFun.GetFileContent(fileMooncake, ref fileMooncakeContent);
            if (errFlag == false)
            {
                return;
            }

            List<CollectRedirectFileByArticle> fileList = new List<CollectRedirectFileByArticle>();

            //Get the thread count
            int threadCount = 0;
            errFlag = CommonFun.GetConfigFileRowCount(configfile, ref threadCount);

            if (errFlag == false)
            {
                return;
            }

            Thread[] newThreads = new Thread[threadCount];

            StreamReader sr = File.OpenText(configfile);
            string row = "";
            

            string filename = "";
            string directory = "";
            string customizedate = "";

            //sr.BaseStream.Seek(0, SeekOrigin.Begin);

            int threadIdx = 0;
            row = sr.ReadLine();
            while (row != null)
            {
                string[] para = row.Split(new Char[] { '\t' });

                filename = para[0];
                directory = para[1];
                customizedate = para[2];
                CollectRedirectFileByArticle curtFile = new CollectRedirectFileByArticle(threadIdx, filename, directory, customizedate, category, mooncakeSite, fileGlobalContent, fileMooncakeContent);
                fileList.Add(curtFile);
                newThreads[threadIdx] = new Thread(new ThreadStart(curtFile.ProcessFileCustomize));
                newThreads[threadIdx].Start();
                Console.WriteLine(string.Format("Start the Thread[{0}] in application", threadIdx));
                //newThreads[threadIdx].Join();
                // Console.WriteLine(string.Format("Join the {0} thread in application", threadIdx));
                row = sr.ReadLine();
                threadIdx += 1;
            }

            sr.Close();


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

            if (category == ConvertCategory.ALL || category == ConvertCategory.redirections)
            {
                foreach (CollectRedirectFileByArticle curtFile in fileList)
                {
                    Console.WriteLine("------------------------");
                    Console.WriteLine(curtFile.RelativeFile);
                    Console.WriteLine("Global Redirections");
                    Console.Write(curtFile.RedirectGContent);
                    Console.WriteLine("Mooncake Redirections");
                    Console.Write(curtFile.RedirectMContent);
                }

                Console.WriteLine("************Result Start for Json**************");
                foreach (CollectRedirectFileByArticle curtFile in fileList)
                {
                    if(curtFile.RedirectGContent.Length>0 && curtFile.RedirectMContent.Length==0)
                    {
                        Console.Write(curtFile.RedirectGContent);
                    }
                }
                Console.WriteLine("************Result End for Json**************");

                Console.WriteLine("************Result Start for Excel**************");
                foreach (CollectRedirectFileByArticle curtFile in fileList)
                {
                    if (curtFile.RedirectGContent.Length > 0 && curtFile.RedirectMContent.Length == 0)
                    {
                        Console.Write(curtFile.RedirectExcelContent);
                    }
                }
                Console.WriteLine("************Result End for Excel**************");

                Console.WriteLine("************Delete File List Start for Excel**************");
                foreach (CollectRedirectFileByArticle curtFile in fileList)
                {
                    if (curtFile.RedirectGContent.Length == 0 && curtFile.RedirectMContent.Length == 0)
                    {
                        Console.WriteLine(curtFile.FullPath);
                    }
                }
                Console.WriteLine("************Delete File List End for Excel**************");

            }

            Console.WriteLine("Program run finished, Press <Enter> to exit....");

            ExitWithUserConfirm();

        }

        static void ShowUseageTip()
        {
            Console.WriteLine("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
            Console.WriteLine("Useage: Redirect [-RDR]");
            Console.WriteLine("-RDR means find the Redirct Article URL");
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
