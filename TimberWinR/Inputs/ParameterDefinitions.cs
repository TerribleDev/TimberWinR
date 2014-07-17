using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace TimberWinR.Inputs
{
    public class ParameterValue
    {
        public ParameterDefinition Parameter { get; private set; }
        protected string CurrentValue { get; set; }

        protected ParameterValue(ParameterDefinition paramDef)
        {
            Parameter = paramDef;
            CurrentValue = Parameter.DefaultValue;
        }

        public override string ToString()
        {
            return string.Format("-{0}:{1}", Parameter.Name, CurrentValue);
        }
    }

    public class BooleanParameterValue : ParameterValue
    {
        public BooleanParameterValue(ParameterDefinition paramDef)
            : base(paramDef)
        {
        }

        public bool Value
        {
            get
            {
                return CurrentValue.Equals(Parameter.LegalValues[Parameter.LegalValues.Count - 1],
                    StringComparison.OrdinalIgnoreCase);
            }
            set
            {
                string v = Parameter.LegalValues[0];
                if (value)
                    v = Parameter.LegalValues[1];

                CurrentValue = v;
            }
        }

    }

    public class EnumParameterValue : ParameterValue
    {
        public EnumParameterValue(ParameterDefinition paramDef)
            : base(paramDef)
        {
        }

        public string Value
        {
            get { return CurrentValue; }
            set
            {
                if (Parameter.LegalValues.Contains(value))
                    CurrentValue = value;
                else
                    throw new ArgumentException(string.Format("Illegal value: '{0}'", value), Parameter.Name);
            }
        }
       
    }

    public class TimestampParameterValue : ParameterValue
    {
        public TimestampParameterValue(ParameterDefinition paramDef)
            : base(paramDef)
        {
        }

        public DateTime Value
        {
            get { return DateTime.Parse(CurrentValue); }
            set { CurrentValue = value.ToString(); }
        }               
    }

    public class ParameterDefinition
    {
        public string Name { get; set; }
        public string DefaultValue { get; set; }
        public List<string> LegalValues { get; set; }
        public Type ParameterType { get; set; }

        public ParameterDefinition(string name, string defaultValue, Type parameterType)
        {
            Name = name;
            DefaultValue = defaultValue;
            LegalValues = null;
            ParameterType = parameterType;
        }

        public ParameterDefinition(string name, string defaultValue, Type parameterType, IEnumerable<string> legalValues)
        {
            Name = name;
            DefaultValue = defaultValue;
            LegalValues = legalValues.ToList();
            ParameterType = parameterType;
        }
    }

    public class ParameterDefinitions : IEnumerable<ParameterDefinition>
    {
        private readonly List<ParameterDefinition> _params;

        public ParameterDefinition this[int index]
        {
            get { return _params[index]; }
            set { _params.Insert(index, value); }
        }

        public void Add(string paramName, string defaultValue, Type parameterType)
        {
            _params.Add(new ParameterDefinition(paramName, defaultValue, parameterType));
        }

        public void Add(string paramName, string defaultValue, Type parameterType, IEnumerable<string> legalValues)
        {
            _params.Add(new ParameterDefinition(paramName, defaultValue, parameterType, legalValues));
        }

        public ParameterDefinitions()
        {
            _params = new List<ParameterDefinition>();
        }

        public IEnumerator<ParameterDefinition> GetEnumerator()
        {
            return _params.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
