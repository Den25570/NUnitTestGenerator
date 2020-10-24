using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using System.Collections.Generic;
using TestsGeneratorLib;
using System.IO;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;

namespace UnitTest
{
    public class GeneratorUnitTests
    {

        private string classOneName;
        private string classTwoName;

        private string classOneCode;
        private string classTwoCode;
        private string sourceClassCode;

        private List<GeneratedTestClass> generatedClasses = null;

        private CompilationUnitSyntax classOneRoot;
        private CompilationUnitSyntax classTwoRoot;

        [SetUp]
        public void TestInit()
        {
            string sourceFilesDirectory = "..\\..\\..\\";

            classOneName = "ClassForTest1Tests.cs";
            classTwoName = "ClassForTest2Tests.cs";

            string souceClassPath = Path.Combine(sourceFilesDirectory, "ClassForTest.cs");

            sourceClassCode = File.ReadAllText(souceClassPath);

            generatedClasses = TestGenerator.Generate(sourceClassCode).Result;

            foreach (var genClass in generatedClasses)
            {
                if (genClass.Name == classOneName)
                {
                    classOneCode = genClass.Code;
                }

                if (genClass.Name == classTwoName)
                {
                    classTwoCode = genClass.Code;
                }

            }

            classOneRoot = CSharpSyntaxTree.ParseText(classOneCode).GetCompilationUnitRoot();
            classTwoRoot = CSharpSyntaxTree.ParseText(classTwoCode).GetCompilationUnitRoot();

        }

        // Test that generator produces two separate files instead of one
        [Test]
        public void TestNumberOfClasses()
        {
            Assert.AreEqual(generatedClasses.Count, 2);
        }

        // Test that code is produced by our generator.
        [Test]
        public void TestProducedCode()
        {
            foreach (GeneratedTestClass gClass in generatedClasses)
            {
                if (gClass.Name == classOneName)
                {
                    Assert.IsTrue(gClass.Code == classOneCode);
                }
                else
                {
                    Assert.IsTrue(gClass.Code == classTwoCode);
                }
            }
        }

        // Test method count in First and Second class
        [Test]
        public void TestMethodCount()
        {

            int methodsOneCount = classOneRoot.DescendantNodes().OfType<MethodDeclarationSyntax>().Count();
            Assert.AreEqual(2, methodsOneCount);

            int methodsTwoCount = classTwoRoot.DescendantNodes().OfType<MethodDeclarationSyntax>().Count();
            Assert.AreEqual(3, methodsTwoCount);

        }

        // Check number of class declarations inside generated classes
        [Test]
        public void TestClassDeclarationCount()
        {
            int classesOneCount = classOneRoot.DescendantNodes().OfType<ClassDeclarationSyntax>().Count();
            Assert.AreEqual(classesOneCount, 1);

            int classesTwoCount = classTwoRoot.DescendantNodes().OfType<ClassDeclarationSyntax>().Count();
            Assert.AreEqual(classesTwoCount, 1);
        }

        // Check that namespaces count is correct
        [Test]
        public void TestNamespacesCount()
        {
            int namespacesOneCount = classOneRoot.DescendantNodes().OfType<NamespaceDeclarationSyntax>().Count();
            Assert.AreEqual(namespacesOneCount, 1);

            int namespacesTwoCount = classTwoRoot.DescendantNodes().OfType<NamespaceDeclarationSyntax>().Count();
            Assert.AreEqual(namespacesTwoCount, 1);
        }

        // Check that class attributes are correct
        [Test]
        public void TestClassAttributes()
        {
            Assert.AreEqual(1, classOneRoot.DescendantNodes().OfType<ClassDeclarationSyntax>()
                .Where((classDeclaration) => classDeclaration.AttributeLists.Any((attributeList) => attributeList.Attributes
                .Any((attribute) => attribute.Name.ToString() == "TestClass"))).Count());

            Assert.AreEqual(1, classTwoRoot.DescendantNodes().OfType<ClassDeclarationSyntax>()
                .Where((classDeclaration) => classDeclaration.AttributeLists.Any((attributeList) => attributeList.Attributes
                .Any((attribute) => attribute.Name.ToString() == "TestClass"))).Count());
        }

        // Check that method attributes are correct
        [Test]
        public void TestMethodAttributes()
        {
            IEnumerable<MethodDeclarationSyntax> methodsOne = classOneRoot.DescendantNodes().OfType<MethodDeclarationSyntax>();

            Assert.AreEqual(1, methodsOne.Where((methodDeclaration) => methodDeclaration.AttributeLists
                .Any((attributeList) => attributeList.Attributes.Any((attribute) => attribute.Name.ToString() == "Test")))
                .Count());

            IEnumerable<MethodDeclarationSyntax> methodsTwo = classTwoRoot.DescendantNodes().OfType<MethodDeclarationSyntax>();

            Assert.AreEqual(2, methodsTwo.Where((methodDeclaration) => methodDeclaration.AttributeLists
                .Any((attributeList) => attributeList.Attributes.Any((attribute) => attribute.Name.ToString() == "Test")))
                .Count());

        }


        [Test]
        public void TestMethodNames()
        {
            IEnumerable<MethodDeclarationSyntax> methodsOne = classOneRoot.DescendantNodes().OfType<MethodDeclarationSyntax>();

            Assert.AreEqual(1, methodsOne.Where((method) => method.Identifier.ToString() == "TestMethodTest").Count());

            IEnumerable<MethodDeclarationSyntax> methodsTwo = classTwoRoot.DescendantNodes().OfType<MethodDeclarationSyntax>();

            Assert.AreEqual(1, methodsTwo.Where((method) => method.Identifier.ToString() == "TestMethod1Test").Count());
            Assert.AreEqual(1, methodsTwo.Where((method) => method.Identifier.ToString() == "TestMethod2Test").Count());

        }

        [Test]
        public void TestDefaultUsing()
        {
            var clsOneUsings = classOneRoot.Usings.Select(x => x.Name.ToString()).ToArray();
            var clsTwoUsings = classTwoRoot.Usings.Select(x => x.Name.ToString()).ToArray();

            var testoneUsings = new string[] { "NUnit.Framework", "System.Linq", "System", "System.Collections.Generic", "NamespaceForTests.Test1", "Moq" };
            var testtwoUsings = new string[] { "NUnit.Framework", "System", "System.Linq",  "System.Collections.Generic", "NamespaceForTests.Test2", "Moq" };


            CollectionAssert.AreEquivalent(clsOneUsings, testoneUsings);
            CollectionAssert.AreEquivalent(clsTwoUsings, testtwoUsings);
        }

        [Test]
        public void TestSetupMethod()
        {
            IEnumerable<MethodDeclarationSyntax> methodsOne = classOneRoot.DescendantNodes().OfType<MethodDeclarationSyntax>();
            var method = methodsOne.Where((method) => method.Identifier.ToString() == "SetUp").First();
            var assignments = method.DescendantNodes().OfType<AssignmentExpressionSyntax>().ToList();
            var variables = method.DescendantNodes().OfType<VariableDeclarationSyntax>().ToList();

            Assert.AreEqual(1, variables.Where((variable) => variable.GetText().ToString().Contains("int a = 0")).Count());
            Assert.AreEqual(1, assignments.Where((variable) => variable.GetText().ToString().Contains("disposable = new Mock<IDisposable>()")).Count());
            Assert.AreEqual(1, assignments.Where((assignment) => assignment.GetText().ToString().Contains("classForTest1 = new ClassForTest1(a, disposable)")).Count());
        }

        [Test]
        public void TestMethodContent()
        {
            IEnumerable<MethodDeclarationSyntax> methodsOne = classOneRoot.DescendantNodes().OfType<MethodDeclarationSyntax>();
            var method = methodsOne.Where((method) => method.Identifier.ToString() == "TestMethodTest").First();
            var variables = method.DescendantNodes().OfType<VariableDeclarationSyntax>();
            var methodCalls = method.DescendantNodes().OfType<InvocationExpressionSyntax>();

            Assert.AreEqual(1, variables.Where((variable) => variable.GetText().ToString().Contains("int a = 0")).Count());
            Assert.AreEqual(1, variables.Where((variable) => variable.GetText().ToString().Contains("int actual = classForTest1.TestMethod(a)")).Count());
            Assert.AreEqual(1, variables.Where((variable) => variable.GetText().ToString().Contains("int expected = 0")).Count());
            Assert.AreEqual(1, methodCalls.Where((methodCall) => methodCall.GetText().ToString().Contains("Assert.That(actual, Is.EqualTo(expected))")).Count());
            Assert.AreEqual(1, methodCalls.Where((methodCall) => methodCall.GetText().ToString().Contains("Assert.Fail(\"autogenerated\")")).Count());
        }
    }
}