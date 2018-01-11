using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.IO;
using System.Text.RegularExpressions;
using AuthorCustmization.ProcessLibrary;
using System.Threading;

namespace AuthorCustmization
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
                switch (args[0].TrimStart('-'))
                {
                    case "A":
                    case "a":
                        category = ConvertCategory.AuthorReplacement;
                        break;
                    case "U":
                    case "u":
                        category = ConvertCategory.URLReplacement;
                        break;
                    case "C":
                    case "c":
                        category = ConvertCategory.URLCorrection;
                        break;
                    case "F":
                    case "f":
                        category = ConvertCategory.FindArticle;
                        break;
                    case "I":
                    case "i":
                        category = ConvertCategory.IncludeParentFile;
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

            StreamReader sr = File.OpenText(configfile);
            string row = "";
            row = sr.ReadLine();

            string filename = "";
            string directory = "";
            string customizedate = "";

            List<FileCustomize> fileList = new List<FileCustomize>();

            //Get the thread count
            int threadCount = 0;

            while (row != null)
            {
                string[] para = row.Split(new Char[] { '\t' });

                filename = para[0];
                directory = para[1];
                customizedate = para[2];

                //fileList.Add(new FileCustomize(filename, directory, customizedate));
                threadCount += 1;
                row = sr.ReadLine();
            }

            //ThreadPool.SetMinThreads(1000, 1000);

            // Declare the application thread.
            Thread[] newThreads = new Thread[threadCount];
            sr.BaseStream.Seek(0, SeekOrigin.Begin);

            int threadIdx = 0;
            row = sr.ReadLine();
            while (row != null)
            {
                string[] para = row.Split(new Char[] { '\t' });

                filename = para[0];
                directory = para[1];
                customizedate = para[2];
                FileCustomize curtFile = new FileCustomize(threadIdx,filename, directory, customizedate,category);
                fileList.Add(curtFile);
                newThreads[threadIdx] = new Thread(new ThreadStart(curtFile.ProcessFileCustomize));
                newThreads[threadIdx].Start();
                Console.WriteLine(string.Format("Start the Thread[{0}] in application", threadIdx));
                newThreads[threadIdx].Join();

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
            

            if(category==ConvertCategory.ALL || category== ConvertCategory.IncludeParentFile )
            {
                foreach(FileCustomize curtFile in fileList)
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
            Console.WriteLine("Useage: AuthorCustmization [-A|-U|-C|-F|-I]");
            Console.WriteLine("without parameter means replace the AuthorReplacement,URLRepalcement,URLCorrection");
            Console.WriteLine("-A means replace the AuthorReplacement");
            Console.WriteLine("-U means replace the URLReplacement");
            Console.WriteLine("-C means replace the URLCorrection");
            Console.WriteLine("-F means find and mark the invloved articles");
            Console.WriteLine("-I means find the one refrenced parent's articles");
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
