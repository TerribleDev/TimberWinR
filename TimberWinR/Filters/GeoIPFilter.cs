using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MaxMind.GeoIP2;
using MaxMind.Db;
using MaxMind.GeoIP2.Exceptions;
using NLog;

namespace TimberWinR.Parser
{
    public partial class GeoIP : LogstashFilter
    {
        private string DatabaseFileName { get; set; }
        private DatabaseReader dr;
        public override JObject ToJson()
        {
            JObject json = new JObject(
               new JProperty("geoip",
                   new JObject(
                       new JProperty("source", Source),
                       new JProperty("type", Type),
                       new JProperty("condition", Condition),
                       new JProperty("target", Target)
                       )));
            return json;
        }

        public GeoIP()
        {
            Target = "geoip";
            DatabaseFileName = Path.Combine(AssemblyDirectory, "GeoLite2City.mmdb");
            dr = new DatabaseReader(DatabaseFileName);
        }

        private static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

        public override bool Apply(JObject json)
        {
            if (!string.IsNullOrEmpty(Type))
            {
                JToken json_type = json["type"];
                if (json_type != null && json_type.ToString() != Type)
                    return true; // Filter does not apply.
            }

            if (Condition != null)
            {
                var expr = EvaluateCondition(json, Condition);
                if (!expr)
                    return true;
            }

            var source = json[Source];

            if (source != null && !string.IsNullOrEmpty(source.ToString()))
            {
                try
                {                  
                    var l = dr.City(source.ToString());
                    if (l != null)
                    {
                        JObject geo_json = new JObject(
                            new JProperty(Target,
                                new JObject(
                                    new JProperty("ip", source.ToString()),
                                    new JProperty("country_code2", l.Country.IsoCode),                                   
                                    new JProperty("country_name", l.Country.Name),
                                    new JProperty("continent_code", l.Continent.Code),
                                    new JProperty("region_name", l.MostSpecificSubdivision.IsoCode),
                                    new JProperty("city_name", l.City.Name),
                                    new JProperty("postal_code", l.Postal.Code),
                                    new JProperty("latitude", l.Location.Latitude),
                                    new JProperty("longitude", l.Location.Longitude),
                                    new JProperty("dma_code", l.Location.MetroCode),                                   
                                    new JProperty("timezone", l.Location.TimeZone),
                                    new JProperty("real_region_name", l.MostSpecificSubdivision.Name),
                                    new JProperty("location",
                                        new JArray(l.Location.Longitude, l.Location.Latitude)
                                        ))));
                                   
                        json.Merge(geo_json, new JsonMergeSettings
                        {
                            MergeArrayHandling = MergeArrayHandling.Union
                        });                       
                    }
                    else
                    {
                        json["_geoiperror"] = string.Format("IP Address not found: {0}", source.ToString());
                    }
                }
                catch (Exception ex)
                {
                    json["_geoiperror"] = string.Format("IP Address not found: {0} ({1})", source.ToString(), ex.ToString());
                    return true;
                }
            }

            AddFields(json);
            AddTags(json);
            RemoveFields(json);
            RemoveTags(json);      

            return true;
        }

        private void AddFields(Newtonsoft.Json.Linq.JObject json)
        {
            if (AddField != null && AddField.Length > 0)
            {
                for (int i = 0; i < AddField.Length; i += 2)
                {
                    string fieldName = ExpandField(AddField[i], json);
                    string fieldValue = ExpandField(AddField[i + 1], json);
                    AddOrModify(json, fieldName, fieldValue);
                }
            }
        }

        private void RemoveFields(Newtonsoft.Json.Linq.JObject json)
        {
            if (RemoveField != null && RemoveField.Length > 0)
            {
                for (int i = 0; i < RemoveField.Length; i++)
                {
                    string fieldName = ExpandField(RemoveField[i], json);
                    RemoveProperties(json, new string[] { fieldName });
                }
            }
        }

        private void AddTags(Newtonsoft.Json.Linq.JObject json)
        {
            if (AddTag != null && AddTag.Length > 0)
            {
                for (int i = 0; i < AddTag.Length; i++)
                {
                    string value = ExpandField(AddTag[i], json);

                    JToken tags = json["tags"];
                    if (tags == null)
                        json.Add("tags", new JArray(value));
                    else
                    {
                        JArray a = tags as JArray;
                        a.Add(value);
                    }
                }
            }
        }

        private void RemoveTags(Newtonsoft.Json.Linq.JObject json)
        {
            if (RemoveTag != null && RemoveTag.Length > 0)
            {
                JToken tags = json["tags"];
                if (tags != null)
                {
                    List<JToken> children = tags.Children().ToList();
                    for (int i = 0; i < RemoveTag.Length; i++)
                    {
                        string tagName = ExpandField(RemoveTag[i], json);
                        foreach (JToken token in children)
                        {
                            if (token.ToString() == tagName)
                                token.Remove();
                        }
                    }
                }
            }
        }

    }
}
