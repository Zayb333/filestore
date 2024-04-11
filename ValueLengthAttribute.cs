using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseConnector.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ValueLengthAttribute : Attribute
    {
        int MaxValueLength;

        public ValueLengthAttribute(int maxValueLength)
        {
            MaxValueLength = maxValueLength;
        }

        public int GetMaxValueLength() => MaxValueLength;
    }
}
