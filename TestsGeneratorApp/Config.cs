using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Text;

namespace TestsGeneratorApp
{
    public class Config
    {
        public readonly string InputPath;
        public readonly string OutputPath;
        public readonly int InputParallelismDegree;
        public readonly int ProcessingParallelismDegree;
        public readonly int OutputParallelismDegree;

        public Config(bool loadParams = true,
            string input = " ", string output = " ", int ip = 0, int op = 0, int pp = 0)
        {
            if (loadParams)
            {
                try
                {
                    InputPath = ConfigurationManager.AppSettings["PathToFiles"];
                    OutputPath = ConfigurationManager.AppSettings["OutputPath"];
                    InputParallelismDegree = Int32.Parse(ConfigurationManager.AppSettings["InputParallelismDegree"]);
                    OutputParallelismDegree = Int32.Parse(ConfigurationManager.AppSettings["OutputParallelismDegree"]);
                    ProcessingParallelismDegree = Int32.Parse(ConfigurationManager.AppSettings["ProcessingParallelismDegree"]);
                }
                catch
                {
                    throw new ArgumentException("Error occured while reading values from configuration file.");
                }
            }
            else
            {
                InputPath = input;
                OutputPath = output;
                InputParallelismDegree = ip;
                OutputParallelismDegree = op;
                ProcessingParallelismDegree = pp;
            }



            if (!Directory.Exists(InputPath))
            {
                throw new ArgumentException("Input directory does not exist.");
            }

            if (!Directory.Exists(OutputPath))
            {
                try
                {
                    Directory.CreateDirectory(OutputPath);
                }
                catch
                {
                    throw new ArgumentException("Output directory does not exist and can't be created");
                }
            }

            if ((InputParallelismDegree <= 0) ||
                (OutputParallelismDegree <= 0) ||
                (ProcessingParallelismDegree <= 0))
            {
                throw new ArgumentException("Parallelism degree must be positive");
            }

        }
    }
}
