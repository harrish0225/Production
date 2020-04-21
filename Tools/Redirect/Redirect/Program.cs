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
            ConvertCategory category = ConvertCategory.CheckRedirectByFile;
            ConvertProcess process = ConvertProcess.ShowResult;

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
                                        category = ConvertCategory.CheckRedirectByService;
                                        break;
                                    case "F":
                                        category = ConvertCategory.CheckRedirectByFile;
                                        break;
                                    default:
                                        curtpara = CommandPara.VerifyFail;
                                        break;
                                }
                                break;
                            case CommandPara.Customize:
                                switch (args[i].ToUpper().Trim())
                                {
                                    case "R":
                                        process = ConvertProcess.ShowResult;
                                        break;
                                    case "H":
                                        process = ConvertProcess.ShowHistory;
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

                        if (curtpara == CommandPara.VerifyFail)
                        {
                            ShowUseageTip();
                            return;
                        }
                        break;
                    }
            }

            



            //Get the following config file , redirect file(both global and mooncake repository)
            bool errFlag = false;
            string error = "";
            string configfile = CommonFun.GetConfigurationValue("CustomerFilePath", ref error);
            if (error.Length > 0)
            {
                Console.WriteLine(error);
                ExitWithUserConfirm();
                return;
            }

            string fileGlobal = CommonFun.GetConfigurationValue("GlobalRedirectFile", ref error);
            if (error.Length > 0)
            {
                Console.WriteLine(error);
                ExitWithUserConfirm();
                return;
            }

            string fileMooncakeDir = CommonFun.GetConfigurationValue("MooncakeRedirectDir", ref error);
            if (error.Length > 0)
            {
                Console.WriteLine(error);
                ExitWithUserConfirm();
                return;
            }

            string fileGlobalDir = CommonFun.GetConfigurationValue("GlobalRedirectDir", ref error);
            if (error.Length > 0)
            {
                Console.WriteLine(error);
                ExitWithUserConfirm();
                return;
            }

            string fileMooncake = CommonFun.GetConfigurationValue("MooncakeRedirectFile", ref error);
            if (error.Length > 0)
            {
                Console.WriteLine(error);
                ExitWithUserConfirm();
                return;
            }


            string mooncakeSite = CommonFun.GetConfigurationValue("MooncakeSite", ref error);
            if (error.Length > 0)
            {
                Console.WriteLine(error);
                ExitWithUserConfirm();
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
            ArrayList arrFile = new ArrayList();

            //Get the thread count
            int threadCount = 0;


            errFlag = CommonFun.GetConfigFileRowCount(configfile, ref threadCount);

            if (errFlag == false)
            {
                return;
            }

            string customizedate = string.Empty;
            switch (category)
            {
                case ConvertCategory.CheckRedirectByService:
                    customizedate = DateTime.Now.ToString("MM/dd/yyyy");
                    arrFile = GetFileListByService(customizedate);
                    break;
                case ConvertCategory.CheckRedirectByFile:
                    arrFile = GetFileListByArticles();
                    break;
            }

            threadCount = arrFile.Count;

            Thread[] newThreads = new Thread[threadCount];

           
            

            string filename = "";
            string directory = "";

            string[] para = new string[] { };

            for (int i=0;i<threadCount;i++)
            {
                para = (string[])arrFile[i];

                filename = para[0];
                directory = para[1];
                customizedate = para[2];
                CollectRedirectFileByArticle curtFile = new CollectRedirectFileByArticle(i, filename, directory, customizedate, category, mooncakeSite, fileGlobalContent, fileMooncakeContent);
                fileList.Add(curtFile);
                newThreads[i] = new Thread(new ThreadStart(curtFile.ProcessFileCustomize));
                newThreads[i].Name= string.Format("{0}/{1}", directory, filename);
                newThreads[i].Start();
                Console.WriteLine(string.Format("Start the Thread[{0}] in application...", i));
#if DEBUG
                newThreads[i].Join();
#endif
                // Console.WriteLine(string.Format("Join the {0} thread in application", threadIdx));
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

            if ( category == ConvertCategory.CheckRedirectByService || category == ConvertCategory.CheckRedirectByFile)
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
                        Console.WriteLine(curtFile.RelativeFile);
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
            Console.WriteLine("Useage: Redirect -S (S|F) -C (H|R)");
            Console.WriteLine("-S means Check articles by service ");
            Console.WriteLine("-F means Check articles by filelist ");
            Console.WriteLine("-H means display all result which include successfully ");
            Console.WriteLine("-R means display the result ");
            Console.WriteLine("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");

            Console.WriteLine("Press <Enter> to exit....");

            ExitWithUserConfirm();
        }

        static void ExitWithUserConfirm()
        {

            Console.WriteLine("Press <Enter> to exit the application....");
            while (Console.ReadKey().Key != ConsoleKey.Enter)
            {

                return;

            }
        }

        public static ArrayList GetFileListByArticles()
        {
            string error = "";
            ArrayList arrFile = new ArrayList();
            string configfile = CommonFun.GetConfigurationValue("CustomerFilePath", ref error);
            if (error.Length > 0)
            {
                return null;
            }

            string filename = string.Empty;
            string directory = string.Empty;
            string customizedate = string.Empty;
            string fullfilename = string.Empty;

            string globalpath = string.Empty;


            //globalpath = CommonFun.GetConfigurationValue("GlobalRedirectDir", ref error);


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
                        fullfilename = string.Format("{0}\\{1}\\{2}", globalpath, directory, filename);

                        break;
                    default:
                        fullfilename = string.Format("{0}\\{1}\\{2}", globalpath, directory, filename).Replace("/","\\");
                        break;
                }
                arrFile.Add(new string[] { filename, directory,customizedate});

                row = sr.ReadLine();

            }


            return arrFile;

        }

        public static ArrayList GetFileListByService(string customizedate)
        {

            CollectAllFileByService fileByService = new CollectAllFileByService();
            Hashtable htbKey = new Hashtable();
            string sPrefixService = string.Empty;

            foreach (InvolvedService curtService in Enum.GetValues(typeof(InvolvedService)))
            {
                sPrefixService = curtService.ToString().Replace("_", "-").ToLower();
                switch (sPrefixService)
                {
                    case "includes":
                        break;
                    default:
                        if (htbKey.ContainsKey(sPrefixService) == false)
                        {
                            htbKey.Add(sPrefixService, sPrefixService);
                        }
                        break;
                }

            }

            fileByService.HTBService = htbKey;

            ArrayList arrFile = new ArrayList();
            arrFile = fileByService.GetAllFileByService(customizedate);

            return arrFile;

        }


    }
}
