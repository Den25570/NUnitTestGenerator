using System;
using System.Collections.Generic;
using System.Text;

namespace NamespaceForTests.Test1
{
    public class ClassForTest1
    {
        public ClassForTest1(int a, IDisposable disposable)
        {

        }

        public int TestMethod(int a = 0) { return 0; }

        public string Name { get; }
        public string Code { get; }
    }
}

namespace NamespaceForTests.Test2
{
    public class ClassForTest2
    {
        public string Name { get; }
        public string Code { get; }

        public void TestMethod2(int a = 0) { }

        public void TestMethod1(int a = 0) { }

        public ClassForTest2(string name, string code)
        {
            Name = name;
            Code = code;
        }
    }
}
