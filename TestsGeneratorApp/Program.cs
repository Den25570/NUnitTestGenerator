using System;
using System.IO;
using System.Threading.Tasks.Dataflow;
using TestsGeneratorLib;

namespace TestsGeneratorApp
{
    class Program
    {
        static void Main(string[] args)
        {
            // Getting Configuration
            Config config;

            try
            {
                config = new Config();
            }
            catch (ArgumentException e)
            {
                Console.WriteLine(e.Message);
                Console.ReadKey();
                return;
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occured reading a configuration file");
                Console.ReadKey();
                return;
            }

            ExecutionDataflowBlockOptions inputOptions = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = config.InputParallelismDegree
            };

            ExecutionDataflowBlockOptions processingOptions = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = config.ProcessingParallelismDegree
            };

            ExecutionDataflowBlockOptions outputOptions = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = config.OutputParallelismDegree
            };

            var getFileNames = new TransformManyBlock<string, string>(path =>
            {
                Console.WriteLine("path - {0}", path);
                return Directory.GetFiles(path);
            });

            var loadFile = new TransformBlock<string, string>(async filename =>
            {
                Console.WriteLine("filename - {0}", filename);
                using (StreamReader reader = new StreamReader(filename))
                {
                    return await reader.ReadToEndAsync();
                }
            }, inputOptions);

            var generateTestClass = new TransformManyBlock<string, GeneratedTestClass>(async classText =>
            {
                Console.WriteLine("Test - {0}", classText);
                return await TestGenerator.Generate(classText);
            }, processingOptions); 

            var writeGeneratedFile = new ActionBlock<GeneratedTestClass>(async testClass =>
            {
                string fullpath = Path.Combine(config.OutputPath, testClass.Name);
                Console.WriteLine("fullpath - {0}", fullpath);
                using (StreamWriter writer = new StreamWriter(fullpath))
                {
                    await writer.WriteAsync(testClass.Code);
                }
            }, outputOptions);

            Console.WriteLine("0");

            getFileNames.LinkTo(loadFile);
            loadFile.LinkTo(generateTestClass);
            generateTestClass.LinkTo(writeGeneratedFile);

            getFileNames.Post(config.InputPath);            
            getFileNames.Complete();
            writeGeneratedFile.Completion.Wait();

        }
    }
}
