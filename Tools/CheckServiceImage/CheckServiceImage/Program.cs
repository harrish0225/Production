using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.IO;
using System.Text.RegularExpressions;
using CheckImageService.ProcessLibrary;
using System.Threading;
using System.Collections;

namespace CheckImageService
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
            ConvertCategory category = ConvertCategory.CheckImageByService;
            ConvertProcess process = ConvertProcess.ShowResult;

            DateTime timeStart = DateTime.Now;
            //TimeSpan runTime = new TimeSpan();
            //DateTime timeEnd = new DateTime();
            

            if (iCount != 2)
            {
                ShowUseageTip();
                return;

            }else
            {
                switch (args[0].TrimStart('-'))
                {
                    case "S":
                    case "s":
                        category = ConvertCategory.CheckImageByService;
                        break;
                    case "F":
                    case "f":
                        category = ConvertCategory.CheckImageByFile;
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
                Console.WriteLine(error);
                ExitWithUserConfirm();
                return;
            }

            string customizedate = CommonFun.GetConfigurationValue("CustomizeDate", ref error);
            if (error.Length > 0)
            {
                Console.WriteLine(error);
                ExitWithUserConfirm();
                return;
            }

            bool successFlag = false;
            int MaxCheckRound = 0;
            string configValue = CommonFun.GetConfigurationValue("CheckRound", ref error);
            if (error.Length > 0)
            {
                Console.WriteLine(error);
                ExitWithUserConfirm();
                return;
            }else
            {
                successFlag = int.TryParse(configValue, out MaxCheckRound);
                if (successFlag == true)
                {

                }
                else
                {
                    Console.WriteLine("Convert {0} to int failed!",configValue );
                    ExitWithUserConfirm();
                    return;
                }
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
                case ConvertCategory.CheckImageByService:
                    arrFile = GetFileListByService();
                    break;
                case ConvertCategory.CheckImageByFile:
                    arrFile = GetFileListByArticles();
                    break;
            }


            threadCount = arrFile.Count;
            Thread[] newThreads = new Thread[threadCount];
            //CancellationTokenSource[] threadCancel = new CancellationTokenSource[threadCount];
            string curtImagePath = string.Empty;

            for (int i=0; i < threadCount; i++)
            {
                curtImagePath = arrFile[i].ToString();

                GetDirectoryFromImagePath(curtImagePath, out filename, out directory, ref fileprefix);

                //threadCancel[i] = new CancellationTokenSource();

                FileCustomize curtFile = new FileCustomize(i, curtImagePath, filename,directory, customizedate, category, process);

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
                    if (newThreads[i].ThreadState != ThreadState.Stopped && newThreads[i].ThreadState != ThreadState.Aborted)
                    {
                        allThreadOver = false;
                        Console.WriteLine(string.Format("Checking status of the Thread[{0}] : {1} ", i, newThreads[i].ThreadState.ToString()));
                        fileList[i].CheckRound += 1;

                       

                        if (fileList[i].CheckRound == MaxCheckRound)
                        {
                            newThreads[i].Priority = ThreadPriority.AboveNormal;
                            Console.WriteLine(string.Format("Levelrage the porior of Thread[{0}] to {1} ", i, newThreads[i].Priority.ToString()));
                        }
                        

                        if (fileList[i].CheckRound > MaxCheckRound*2)
                        {
                            fileList[i].BrokenLink += string.Format("Error : The Check Round({0}) Exceed the limit of {1}", fileList[i].CheckRound, MaxCheckRound);
                            Console.WriteLine(string.Format("Abort the Thread[{0}] to {1} ", i, newThreads[i].Priority.ToString()));
                            newThreads[i].Abort();
                        }

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
                        Console.WriteLine(curtFile.FullPath);
                        Console.WriteLine("File: \t{0} \tMessage: \t{1}",curtFile.FullPath, curtFile.WarningMessage);
                    }
                }
            }

            if (category == ConvertCategory.CheckImageByFile || category == ConvertCategory.CheckImageByService)
            {
                StringBuilder sbText = new StringBuilder();

                Console.WriteLine();
                sbText.AppendLine(String.Format("**********************Check Process Result({0})**********************", category.ToString()));
                //Console.WriteLine(String.Format("**********************Check Process Result({0})**********************", category.ToString()));

                int iIdx = 1;

                sbText.AppendLine(string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}", "Index", "Service","File Path", "Total Screenshots", "Adapted Screenshots","Image File Name"));
                foreach (FileCustomize curtFile in fileList)
                {

                        //Console.WriteLine("{0} : {1} ",iIdx++,  curtFile.File);
                        //Console.WriteLine(curtFile.BrokenLink);
                        sbText.AppendLine(string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}", iIdx++, curtFile.ServiceName,curtFile.File,curtFile.TotalImageCount,curtFile.ModifyImageCount,curtFile.ModifyImageName ));

                }


                CommonFun.GenerateDownloadFile(category, ref sbText);


            }


            ExitWithUserConfirm();

        }

        public static ArrayList GetFileListByService()
        {

            CollectAllImageByService fileByService = new CollectAllImageByService();
            Hashtable htbKey = new Hashtable();
            string sPrefixService = string.Empty;

            foreach (InvolvedService curtService in Enum.GetValues(typeof(InvolvedService)))
            {
                sPrefixService = curtService.ToString().Replace("_","-").ToLower();
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
            arrFile = fileByService.GetAllFileByService();

            return arrFile;

        }

        public static void GetDirectoryFromImagePath(string imagepath,out string filename, out string directory,ref string fileprefix)
        {
            string[] arrPara = imagepath.Replace("\\", "/").Split('/');
            filename = arrPara[arrPara.Length - 1];

            directory = imagepath.Substring(fileprefix.Length, imagepath.Length - fileprefix.Length - filename.Length - 1).Replace("\\media\\","\\").Replace("\\", "/");

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
            Console.WriteLine("Useage: CheckServiceImage -S|-F -H|-R");
            Console.WriteLine("-S means Check Modified Image by service ");
            Console.WriteLine("-F means Check Modified Image by filelist ");
            Console.WriteLine("-H means display all result which include successfully ");
            Console.WriteLine("-R means display the result ");
            Console.WriteLine("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");

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
        

        
    }
}
