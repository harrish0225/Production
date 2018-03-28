using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.IO;

namespace AuthorCustmization.ProcessLibrary
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

        public static void GenerateDownloadFile(ConvertCategory category, ref StringBuilder sbText)
        {
            FileStream fs = null;

            try
            {

                string downloadPath = AppDomain.CurrentDomain.BaseDirectory;

                string curtTime = string.Format(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")).Replace(":", "-");

                string sFilePath = string.Format("{0}\\{1}-{2}.txt", downloadPath, category.ToString(), curtTime);


                if (!File.Exists(sFilePath))
                {
                    fs = new FileStream(sFilePath, FileMode.CreateNew);
                }
                else
                {
                    fs = new FileStream(sFilePath, FileMode.Append);
                }

                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.WriteLine(sbText.ToString());
                    sw.Flush();
                    sw.Close();
                }

                Console.WriteLine("The file {0} genereare successfully!", sFilePath);

            }
            catch
            {

            }
            finally
            {
                if (fs != null)
                {
                    fs.Dispose();
                }
            }
        }
    }
}
