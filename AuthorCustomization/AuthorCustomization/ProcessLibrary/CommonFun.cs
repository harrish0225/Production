using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.IO;

namespace AuthorCustomization.ProcessLibrary
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

        public static void GenerateErrorDownloadFile(ref string sErrorText)
        {
            FileStream fs = null;

            try
            {

                string downloadPath = AppDomain.CurrentDomain.BaseDirectory;

                string curtTime = string.Format(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")).Replace(":", "-");

                string sFilePath = string.Format("{0}\\{1}-{2}.txt", downloadPath, "ERROR", curtTime);


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
                    sw.WriteLine(sErrorText.ToString());
                    sw.Flush();
                    sw.Close();
                }

                Console.WriteLine("The Error file {0} genereare successfully!", sFilePath);

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

        #region 返回时间差
        public static string DateDiff(ref DateTime DateTime1, ref DateTime DateTime2)
        {
            string dateDiff = null;
            try
            {
                TimeSpan ts1 = new TimeSpan(DateTime1.Ticks);
                TimeSpan ts2 = new TimeSpan(DateTime2.Ticks);
                TimeSpan ts = ts1.Subtract(ts2).Duration();

                string days = ts.Days.ToString();
                string hours = ts.Hours.ToString();
                string minutes = ts.Minutes.ToString();
                string seconds = ts.Seconds.ToString();
                string milisec = ts.Milliseconds.ToString();

                if (ts.Hours < 10)
                {
                    hours = "0" + ts.Hours.ToString();
                }
                if (ts.Minutes < 10)
                {
                    minutes = "0" + ts.Minutes.ToString();
                }
                if (ts.Seconds < 10)
                {
                    seconds = "0" + ts.Seconds.ToString();
                }
                if (ts.Milliseconds<1000)
                {
                    milisec = string.Format("{0:0000}", ts.Milliseconds);
                }


                dateDiff = hours + ":" + minutes + ":" + seconds + "." + milisec;
            }
            catch
            {

            }
            finally
            {
                DateTime2 = DateTime1;
            }


            return dateDiff;
        }
        #endregion
    }
}
