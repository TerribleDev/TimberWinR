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
        public override string ToString()
        {
           return String.Format("{0}", Name);
        }

        public override bool Equals(System.Object obj)
        {
            // If parameter is null return false.
            if (obj == null)
            {
                return false;
            }

            // If parameter cannot be cast to Point return false.
            FieldDefinition p = obj as FieldDefinition;
            if ((System.Object)p == null)
            {
                return false;
            }

            // Return true if the fields match:
            return (Name == p.Name) && (FieldType == p.FieldType);
        }

        public bool Equals(FieldDefinition p)
        {
            // If parameter is null return false:
            if ((object)p == null)
            {
                return false;
            }

            // Return true if the fields match:
            return (Name == p.Name) && (FieldType == p.FieldType);
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
