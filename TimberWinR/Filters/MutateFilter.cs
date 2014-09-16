using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TimberWinR.Parser
{ 
     public partial class Mutate : LogstashFilter
     {
         public override JObject ToJson()
         {
             JObject json = new JObject(
                new JProperty("mutate",
                    new JObject(
                        new JProperty("condition", Condition),
                        new JProperty("splits", Split),
                        new JProperty("rename", Rename),
                        new JProperty("replace", Replace)                       
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

             ApplySplits(json);
             ApplyRenames(json);
             ApplyReplace(json);
             return true;
         }

         private void ApplyRenames(JObject json)
         {
             if (Rename != null && Rename.Length > 0)
             {
                 for (int i = 0; i < Rename.Length; i += 2)
                 {
                     string oldName = ExpandField(Rename[i], json);
                     string newName = ExpandField(Rename[i + 1], json);
                     RenameProperty(json, oldName, newName);
                 }
             }
         }

         private void ApplySplits(JObject json)
         {
             if (Split != null && Split.Length > 0)
             {
                 for (int i = 0; i < Split.Length; i += 2)
                 {
                     string fieldName = Split[i];
                     string splitChar = Split[i + 1];

                     JArray array = null;
                     if (json[fieldName] != null)
                     {
                         string valueToSplit = json[fieldName].ToString();
                         string[] values = valueToSplit.Split(new string[] {splitChar}, StringSplitOptions.None);
                         foreach (string value in values)
                         {
                             if (array == null)
                                 array = new JArray(value);
                             else
                                 array.Add(value);
                         }

                     }
                 }
             }
         }


         private void ApplyReplace(JObject json)
         {
             if (Replace != null && Replace.Length > 0)
             {
                 for (int i = 0; i < Replace.Length; i += 2)
                 {
                     string fieldName = Replace[0];
                     string replaceValue = ExpandField(Replace[i + 1], json);
                     ReplaceProperty(json, fieldName, replaceValue);
                 }
             }
         }  
     }
}
