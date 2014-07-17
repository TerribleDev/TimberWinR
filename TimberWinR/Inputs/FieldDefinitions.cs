using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TimberWinR.Inputs
{
    /// <summary>
    /// Field Definition
    /// </summary>
    public class FieldDefinition
    {
        public string Name { get; set; }
        public Type FieldType { get; set; }

        public DateTime ToDateTime(object o)
        {
            return (DateTime) o;
        }

        public int ToInt(object o)
        {
            return (int) o;
        }

        public string ToString(object o)
        {
            return (string) o;
        }

        public float ToFloat(object o)
        {
            return (float)o;
        }
        public FieldDefinition(string fieldName, Type fieldType)
        {
            Name = fieldName;
            FieldType = fieldType;
        }
    }

    public class FieldDefinitions : IEnumerable<FieldDefinition>
    {
        private List<FieldDefinition> _fields;

        public FieldDefinition this[int index]
        {
            get { return _fields[index]; }
            set { _fields.Insert(index, value); }
        } 

        public void Add(string fieldName, Type fieldType)
        {
            _fields.Add(new FieldDefinition(fieldName, fieldType));
        }

        public FieldDefinitions()
        {
            _fields = new List<FieldDefinition>();
        }

        public IEnumerator<FieldDefinition> GetEnumerator()
        {
            return _fields.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
