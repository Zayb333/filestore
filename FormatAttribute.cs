using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DatabaseConnector.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class FormatAttribute : Attribute
    {
        public Regex FormatRegex { get; set; }
        public string FailMessage { get; set; }
        public FormatAttribute(string regexformat, string failMessage)
        {
            FormatRegex = new Regex(regexformat);
            FailMessage = failMessage;
        }

        public Regex GetRegexFormat => FormatRegex;
    }
}
