namespace TimberWinR.Inputs
{
    using System;
    using System.Collections.Generic;

    using Interop.MSUtil;

    using Newtonsoft.Json.Linq;

    using TimberWinR.Parser;

    public class IisW3CRowReader
    {
        private readonly List<Field> fields;
        private IDictionary<string, int> columnMap;

        public IisW3CRowReader(List<Field> fields)
        {
            this.fields = fields;
        }

        public JObject ReadToJson(ILogRecord row)
        {
            var json = new JObject();
            foreach (var field in this.fields)
            {
                if (this.columnMap.ContainsKey(field.Name))
                {
                    object v = row.getValue(field.Name);
                    if (field.DataType == typeof(DateTime))
                    {
                        DateTime dt = DateTime.Parse(v.ToString());
                        json.Add(new JProperty(field.Name, dt));
                    }
                    else
                    {
                        json.Add(new JProperty(field.Name, v));
                    }
                }
            }

            AddTimestamp(json);

            return json;
        }

        public void ReadColumnMap(ILogRecordset rs)
        {
            this.columnMap = new Dictionary<string, int>();
            for (int col = 0; col < rs.getColumnCount(); col++)
            {
                string colName = rs.getColumnName(col);
                this.columnMap[colName] = col;
            }
        }

        private static void AddTimestamp(JObject json)
        {
            if (json["date"] != null && json["time"] != null)
            {
                var date = DateTime.Parse(json["date"].ToString());
                var time = DateTime.Parse(json["time"].ToString());
                date = new DateTime(date.Year, date.Month, date.Day, time.Hour, time.Minute, time.Second, time.Millisecond);

                json.Add(new JProperty("@timestamp", date.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")));
            }
        }
    }
}
