using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using TimberWinR.Parser;

namespace TimberWinR
{
    public class LogErrors
    {
        public static JObject LogException(Exception ex)
        {
            return LogException("Exception", ex);
        }

        public static JObject LogException(string errorMessage, Exception ex)
        {
            JObject result = new JObject();
            result["type"] = "TimberWinR-Error";
            result["ErrorMessage"] = errorMessage;
            
            try
            {  
                JsonConvert.DefaultSettings = () => new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    NullValueHandling = NullValueHandling.Ignore
                };

                var exJson = JObject.Parse(JsonConvert.SerializeObject(ex));

                result.Merge(exJson, new JsonMergeSettings
                {
                    MergeArrayHandling = MergeArrayHandling.Replace
                });
                return result;
            }
            catch (Exception ex1)
            {
                result["ErrorMessage"] = ex1.ToString();
                return result;
            }           
        }
    }
}
