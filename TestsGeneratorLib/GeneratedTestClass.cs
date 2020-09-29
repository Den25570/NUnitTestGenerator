using System;
using System.Collections.Generic;
using System.Text;

namespace TestsGeneratorLib
{
    public class GeneratedTestClass
    {
        public GeneratedTestClass(IDisposable disposable)
        {

        }

        public void TestShit() { }

        public string Name { get; }
        public string Code { get; }

        public GeneratedTestClass(string name, string code)
        {
            Name = name;
            Code = code;
        }
    }
}
