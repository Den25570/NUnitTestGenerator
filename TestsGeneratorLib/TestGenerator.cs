﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace TestsGeneratorLib
{
    public static class TestGenerator
    {
        private static SyntaxList<UsingDirectiveSyntax> getTemplateUsing(string namespaceName)
        {
            List<UsingDirectiveSyntax> template = new List<UsingDirectiveSyntax>
            {
                UsingDirective(IdentifierName("System")),
                UsingDirective(QualifiedName(
                    IdentifierName("System"),
                    IdentifierName("Linq"))),
                UsingDirective(QualifiedName(
                    QualifiedName(
                        IdentifierName("System"), IdentifierName("Collections")),
                        IdentifierName("Generic"))),
                UsingDirective(
                    QualifiedName(
                        QualifiedName(
                            QualifiedName(
                                IdentifierName("Microsoft"),
                                IdentifierName("VisualStudio")),
                            IdentifierName("TestTools")),
                    IdentifierName("UnitTesting"))),
                UsingDirective(IdentifierName(namespaceName))
            };

            return List(template);
        }

        private static SyntaxList<MemberDeclarationSyntax> getTemplateMethods(IEnumerable<string> methods)
        {
            List<MemberDeclarationSyntax> classMethods = new List<MemberDeclarationSyntax>();

            string templateBody = "Assert.Fail(\"autogenerated\");";
            string templateAttribute = "TestMethod";

            foreach (var methodName in methods)
            {
                var declaration = MethodDeclaration(
                    PredefinedType(
                        Token(SyntaxKind.VoidKeyword)),
                    Identifier(methodName + "Test"))
                    .WithAttributeLists(
                        SingletonList(
                            AttributeList(
                                SingletonSeparatedList(
                                    Attribute(
                                        IdentifierName(templateAttribute))))))
                    .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
                    .WithBody(Block(ParseStatement(templateBody)));

                classMethods.Add(declaration);

            }

            return List(classMethods);

        }

        public static Task<List<GeneratedTestClass>> Generate(string code)
        {
            return Task.Run(() => {

                List<GeneratedTestClass> generatedClasses = new List<GeneratedTestClass>();

                var tree = CSharpSyntaxTree.ParseText(code);
                var syntaxRoot = tree.GetRoot();

                var classDeclarations = syntaxRoot.DescendantNodes().OfType<ClassDeclarationSyntax>();

                foreach (var clsInfo in classDeclarations)
                {
                    string className = clsInfo.Identifier.ValueText;
                    string clsNamespace = ((NamespaceDeclarationSyntax)clsInfo.Parent).Name.ToString();

                    var publicMethods = clsInfo.DescendantNodes().OfType<MethodDeclarationSyntax>().Where(
                        method => method.Modifiers.Any(modifier => modifier.ValueText == "public")).Select(obj => obj.Identifier.ValueText);

                    NamespaceDeclarationSyntax template_namespace = NamespaceDeclaration(
                        QualifiedName(
                            IdentifierName(className), IdentifierName("Test")));

                    var template_usings = getTemplateUsing(clsNamespace);
                    var template_methods = getTemplateMethods(publicMethods);
                    var template_classname = className + "Tests";

                    var template_class = CompilationUnit()
                        .WithUsings(template_usings)
                        .WithMembers(SingletonList<MemberDeclarationSyntax>(template_namespace
                            .WithMembers(SingletonList<MemberDeclarationSyntax>(ClassDeclaration(template_classname)
                                .WithAttributeLists(
                                    SingletonList(
                                        AttributeList(
                                            SingletonSeparatedList(
                                                Attribute(
                                                    IdentifierName("TestClass"))))))
                                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
                                .WithMembers(template_methods)))));

                    string generatedCode = template_class.NormalizeWhitespace().ToFullString();
                    string generatedName = template_classname + ".cs";

                    generatedClasses.Add(new GeneratedTestClass(generatedName, generatedCode));

                }

                return generatedClasses;

            });
        }
    }
}
