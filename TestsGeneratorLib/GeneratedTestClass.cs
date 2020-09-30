using System;
using System.Collections.Generic;
using System.Text;

namespace TestsGeneratorLib
{
    public class GeneratedTestClass
    {
        public GeneratedTestClass(int a, IDisposable disposable)
        {

        }

        public void TestShit(int shit = 0) { }

        public string Name { get; }
        public string Code { get; }

        public GeneratedTestClass(string name, string code)
        {
            Name = name;
            Code = code;
        }
    }
}
