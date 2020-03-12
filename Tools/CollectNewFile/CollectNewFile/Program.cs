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
            ConvertCategory category = ConvertCategory.CollectNewFileByService;
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
                                        category = ConvertCategory.CollectNewFileByService;
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
                case ConvertCategory.CollectNewFileByService:
                    customizedate = DateTime.Now.ToString("MM/dd/yyyy");
                    arrFile = GetFileListByService(customizedate);
                    break;
            }

            threadCount = arrFile.Count;


            string filename = "";
            string directory = "";

            string[] para = new string[] { };
           
            if ( category == ConvertCategory.CollectNewFileByService)
            {
                for (int i = 0; i < threadCount; i++)
                {
                    para = (string[])arrFile[i];

                    filename = para[0];
                    directory = para[1];
                    customizedate = para[2];

                    Console.WriteLine(directory + "/" + filename);

                }
                   
            }

            Console.WriteLine("Program run finished, Press <Enter> to exit....");

            ExitWithUserConfirm();

        }

        static void ShowUseageTip()
        {

            Console.WriteLine("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
            Console.WriteLine("Useage: CollectionNewFile -S (S) -C R ");
            Console.WriteLine("-S means Check articles by service ");
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
