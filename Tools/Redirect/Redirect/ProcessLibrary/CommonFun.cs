using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Threading;
using System.IO;

namespace CLIReplacement.ProcessLibrary
{
    class CommonFun
    {
        public static string GetConfigurationValue(string key, ref string error)
        {
            string keyValue = "";
            try
            {
                keyValue = ConfigurationManager.AppSettings[key];

            }
            catch (Exception ex)
            {
                error = string.Format("Get the appsetting of {0} failed!", key);
                Console.WriteLine(error);
            }

            return keyValue;
        }

        public static bool GetConfigFileRowCount(string configfilepath, ref int iCount)
        {
            bool bSuccess = false;
            string errMsg = string.Empty;

            using (StreamReader sr = File.OpenText(configfilepath))
            {
                Mutex fileMutex = new Mutex(true, GetMetuxFileName(configfilepath));
                fileMutex.WaitOne();

                try
                {
                    string row = "";
                    row = sr.ReadLine();

                    while (row != null)
                    {
                        iCount += 1;
                        row = sr.ReadLine();
                    }
                    bSuccess = true;
                }
                catch(Exception ex)
                {
                    errMsg += "Get the config file wrong:" + "\n";
                    errMsg += string.Format("File Name: {0}", configfilepath) + "\n";
                    errMsg += string.Format("Error Details: {0}", ex.Message.ToString()) + "\n";
                    Console.WriteLine(errMsg);
                    iCount = 0;
                }
                finally
                {
                    if (sr != null)
                    {
                        sr.Close();
                    }
                    fileMutex.ReleaseMutex();
                }
                
            }

            return bSuccess;

        }

        public static bool GetFileContent(string filepath, ref string fileContent)
        {
            bool bSuccess = false;
            string errMsg = string.Empty;

            using (StreamReader sr = File.OpenText(filepath))
            {
                Mutex fileMutex = new Mutex(true, GetMetuxFileName(filepath));
                fileMutex.WaitOne();

                try
                {
                    fileContent = sr.ReadToEnd();
                    sr.Close();
                    bSuccess = true;
                }
                catch (Exception ex)
                {
                    errMsg += "Get the config file wrong:" + "\n";
                    errMsg += string.Format("File Name: {0}", filepath) + "\n";
                    errMsg += string.Format("Error Details: {0}", ex.Message.ToString()) + "\n";
                    Console.WriteLine(errMsg);
                }
                finally
                {
                    if(sr != null)
                    {
                        sr.Close();
                    }
                    fileMutex.ReleaseMutex();
                }

            }

            return bSuccess;

        }

        public static string GetMetuxFileName(string filepath)
        {
            string fileName = string.Empty;
            fileName = filepath.Replace(":", "_").Replace(@"\", "_").Replace("/", "_");
            return fileName;
        }



    }
}
