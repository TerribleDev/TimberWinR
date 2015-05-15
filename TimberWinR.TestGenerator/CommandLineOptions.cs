using System.ComponentModel;
using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommandLine.Text;

namespace TimberWinR.TestGenerator
{
    class CommandLineOptions
    {
       // [Option('r', "read", Required = true, HelpText = "Input file to be processed.")]
      //  public string InputFile { get; set; }
        [Option("timberWinRConfig", DefaultValue = "default.json", HelpText = "Config file/directory to use")]
        public string TimberWinRConfigFile { get; set; }

        [Option("start", HelpText = "Start an instance of TimberWinR")]
        public bool StartTimberWinR { get; set; }

        [Option("testDir", DefaultValue = ".", HelpText = "Test directory to use (created if necessary)")]
        public string TestDir { get; set; }

        [Option("testFile", DefaultValue = "", HelpText = "Config file/directory to use")]
        public string TestFile { get; set; }

        [Option("resultsFile", HelpText = "Expected results Results json file")]
        public string ExpectedResultsFile { get; set; }

        [Option("totalMessages", DefaultValue = 0, HelpText = "The total number of messages to send to the output(s)")]
        public int TotalMessages { get; set; }

        [Option('n', "numMessages", DefaultValue = 1000, HelpText = "The number of messages to send to the output(s)")]
        public int NumMessages { get; set; }

        [Option('l', "logLevel", DefaultValue = "debug", HelpText = "Logging Level Debug|Error|Fatal|Info|Off|Trace|Warn")]
        public string LogLevel { get; set; }

        [Option('v', "verbose", DefaultValue = true, HelpText = "Prints all messages to standard output.")]
        public bool Verbose { get; set; }

        [Option("jsonLogDir", DefaultValue = ".", HelpText = "Json LogGenerator Log directory")]
        public string JsonLogDir { get; set; }

        [OptionArray('j', "json", DefaultValue = new string[] {})]
        public string[] JsonLogFiles { get; set; }

        [OptionArray("jroll", DefaultValue = new string[] { })]
        public string[] JsonRollingLogFiles { get; set; }
        
        [Option("jsonRate", DefaultValue = 30, HelpText = "Json Rate in Milliseconds between generation of log lines")]
        public int JsonRate { get; set; }

        [Option('u', "udp", DefaultValue = 0, HelpText = "Enable UDP generator on this Port")]
        public int Udp { get; set; }

        [Option("udp-host", DefaultValue = "localhost", HelpText = "Host to send Udp data to")]
        public string UdpHost { get; set; }

        [Option("udp-rate", DefaultValue = 10, HelpText = "Udp Rate in Milliseconds between generation of log lines")]
        public int UdpRate { get; set; }

        [Option('t', "tcp", DefaultValue = 0, HelpText = "Enable Tcp generator on this Port")]
        public int Tcp { get; set; }

        [Option("tcp-host", DefaultValue = "localhost", HelpText = "Host to send Tcp data to")]
        public string TcpHost { get; set; }

        [Option("tcp-rate", DefaultValue = 10, HelpText = "Tcp Rate in Milliseconds between generation of log lines")]
        public int TcpRate { get; set; }

        [Option('r', "redis", DefaultValue = 0, HelpText = "Enable Redis generator on this Port")]
        public int Redis { get; set; }

        [Option("redis-host", DefaultValue = "", HelpText = "Host to send Redis data to")]
        public string RedisHost { get; set; }

        [ParserState]
        public IParserState LastParserState { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this,
              (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }
}
