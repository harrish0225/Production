using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace H1ToTitle.ProcessLibrary
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
    }
}
