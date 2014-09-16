using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace TimberWinR.Parser
{ 
     public partial class Json : LogstashFilter
     {
         public override JObject ToJson()
         {
             JObject json = new JObject(
                new JProperty("json",
                    new JObject(
                        new JProperty("condition", Condition),
                        new JProperty("source", Source),
                        new JProperty("target", Target),
                        new JProperty("addfields", AddField),
                        new JProperty("addtags", AddTag),
                        new JProperty("removefields", RemoveField),
                        new JProperty("removetag", RemoveTag)
                        )));
             return json;
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
                     JObject subJson;

                     if (Target != null && !string.IsNullOrEmpty(Target))
                     {
                         subJson = new JObject();
                         subJson[Target] = JObject.Parse(source.ToString());
                     }
                     else
                         subJson = JObject.Parse(source.ToString());

                     json.Merge(subJson, new JsonMergeSettings
                     {
                         MergeArrayHandling = MergeArrayHandling.Union
                     });
                 }
                 catch (Exception ex)
                 {
                     LogManager.GetCurrentClassLogger().Error(ex);
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
