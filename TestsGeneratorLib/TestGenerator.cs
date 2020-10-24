using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using System.Reflection;
using Microsoft.CSharp;
using System.CodeDom;
using System.Text.RegularExpressions;

namespace TestsGeneratorLib
{
    public static class TestGenerator
    {
        private static SyntaxList<UsingDirectiveSyntax> GetTemplateUsing(string namespaceName)
        {
            List<UsingDirectiveSyntax> template = new List<UsingDirectiveSyntax>
            {
                UsingDirective(
                    IdentifierName("System")),
                UsingDirective(
                    QualifiedName(
                        IdentifierName("System"),
                        IdentifierName("Linq"))),
                UsingDirective(
                    QualifiedName(
                        QualifiedName(
                            IdentifierName("System"), 
                            IdentifierName("Collections")),
                        IdentifierName("Generic"))),
                UsingDirective(
                    QualifiedName(
                        IdentifierName("NUnit"),
                        IdentifierName("Framework"))),
                UsingDirective(
                    IdentifierName("Moq")),
                UsingDirective(IdentifierName(namespaceName))
            };

            return List(template);
        }

        private static string GetFullTypeName(string typeName)
        {
            var mscorlib = Assembly.GetAssembly(typeof(int));

            using (var provider = new CSharpCodeProvider())
            {
                foreach (var type in mscorlib.DefinedTypes)
                {
                    if (string.Equals(type.Namespace, "System"))
                    {
                        var typeRef = new CodeTypeReference(type);
                        var csTypeName = provider.GetTypeOutput(typeRef);
                        if (typeName == csTypeName)
                        {
                            return type.FullName;
                        }
                    }
                }
            }
            return null;
        }

        private static LiteralExpressionSyntax GetTypeLiteral(string typeName)
        {
            object defalutValue;
            try
            {
                Type type = Type.GetType(typeName) ?? Type.GetType(GetFullTypeName(typeName));

                if (type.IsValueType && Nullable.GetUnderlyingType(type) == null)
                    defalutValue = Activator.CreateInstance(type);
                else
                    defalutValue = null;
            }
            catch
            {
                defalutValue = null;
            }

            if (defalutValue != null)
            {
                return 
                    LiteralExpression(
                        SyntaxKind.DefaultLiteralExpression,
                        ParseToken(defalutValue.ToString()));
            }
            else
            {
                return 
                    LiteralExpression(
                      SyntaxKind.NullLiteralExpression);
            }

        }

        private static SyntaxList<MemberDeclarationSyntax> GetTemplateMethods(ClassDeclarationSyntax clsInfo, SemanticModel model)
        {
            List<MemberDeclarationSyntax> classMethods = new List<MemberDeclarationSyntax>();

            string templateAttribute = "Test";

            //add setUp
            var constructor = (ConstructorDeclarationSyntax)clsInfo.ChildNodes().FirstOrDefault(n => n.Kind() == SyntaxKind.ConstructorDeclaration);
            if (constructor != null)
            {
                if (!clsInfo.Modifiers.Where(m => m.Kind().Equals(SyntaxKind.StaticKeyword)).Any())
                {
                    var constructorMethod = AddSetUpMethod(clsInfo, model, constructor);
                    classMethods.Add(constructorMethod);
                }
            }

            var publicMethods = clsInfo.DescendantNodes().OfType<MethodDeclarationSyntax>().Where(
                method => method.Modifiers.Any(modifier => modifier.ValueText == "public"));
            foreach (var method in publicMethods)
            {
                if (method != null)
                {
                    string classname = char.ToLower(clsInfo.Identifier.ValueText[0]) + clsInfo.Identifier.ValueText.Substring(1);
                    var testMethod = AddMethod(classname, model, method, method.Identifier.ValueText + "Test", templateAttribute);
                    classMethods.Add(testMethod);
                }
            }

            return List(classMethods);
        }

        private static string RemoveAllNamespacePrefixes(string variable)
        {
            return Regex.Replace(variable, @"\w+\.", "");
        }

        private static MethodDeclarationSyntax AddSetUpMethod(
            ClassDeclarationSyntax clsInfo,
            SemanticModel model,
            ConstructorDeclarationSyntax methodToAnalyze
            )
        {
            MethodDeclarationSyntax declaration;
            List<StatementSyntax> statements = new List<StatementSyntax>();
            List<ArgumentSyntax> methodParams = new List<ArgumentSyntax>();

            //arrange
            var parameters = methodToAnalyze.ParameterList.ChildNodes().Cast<ParameterSyntax>();
            foreach (ParameterSyntax parameter in parameters)
            {
                var typeSymbol = model.GetSymbolInfo(parameter.Type).Symbol as INamedTypeSymbol;
                AddVariable(statements, methodParams, typeSymbol, parameter.Identifier.ValueText);
            }

            //call method to test
            var expressionToAdd = ExpressionStatement(
               AssignmentExpression(
                     SyntaxKind.SimpleAssignmentExpression,
                     IdentifierName(char.ToLower(clsInfo.Identifier.ValueText[0]) + clsInfo.Identifier.ValueText.Substring(1)),
                     ObjectCreationExpression(
                         IdentifierName(clsInfo.Identifier.ValueText))
                         .WithArgumentList(
                            ArgumentList(
                               SeparatedList<ArgumentSyntax>(
                                 methodParams)))));

            statements.Add(expressionToAdd);

            //add test method
            declaration = GetMethodTemplate(statements, "SetUp", "SetUp");

            return declaration;
        }
        private static MethodDeclarationSyntax AddMethod(
            string className,
            SemanticModel model,
            MethodDeclarationSyntax methodToAnalyze,
            string methodName,
            string attributeName
            )
        {
            string templateAssertFailBody = "Assert.Fail(\"autogenerated\");";
            string templateAssertThatBody = "Assert.That(actual, Is.EqualTo(expected));";
            MethodDeclarationSyntax declaration;
            List<StatementSyntax> statements = new List<StatementSyntax>();
            List<ArgumentSyntax> methodParams = new List<ArgumentSyntax>();

            //arrange
            var parameters = methodToAnalyze.ParameterList.ChildNodes().Cast<ParameterSyntax>();
            foreach (ParameterSyntax parameter in parameters)
            {
                var typeSymbol = model.GetSymbolInfo(parameter.Type).Symbol as INamedTypeSymbol;
                AddVariable(statements, methodParams, typeSymbol, parameter.Identifier.ValueText);
            }

            //call method to test
            var returnTypeSymbol = model.GetSymbolInfo(methodToAnalyze.ReturnType).Symbol as INamedTypeSymbol;

            
            var returnTypeName = RemoveAllNamespacePrefixes(returnTypeSymbol.ToString());//.Substring(returnTypeSymbol.ToString().LastIndexOf(".") + 1);

            //act
            var invocationExpression = InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName(className),
                    IdentifierName(methodToAnalyze.Identifier.ValueText)))
                .WithArgumentList(
                    ArgumentList(SeparatedList<ArgumentSyntax>(methodParams)));
            if (returnTypeName != "void")
            {
                var expressionToAdd = LocalDeclarationStatement(
                VariableDeclaration(
                    IdentifierName(returnTypeName))
                .WithVariables(
                    SingletonSeparatedList<VariableDeclaratorSyntax>(
                        VariableDeclarator(
                            Identifier("actual"))
                        .WithInitializer(
                            EqualsValueClause(invocationExpression)))));
                statements.Add(expressionToAdd);
            }
            else
            {
                statements.Add(ExpressionStatement(invocationExpression));
            }

            

            //assert
            LocalDeclarationStatementSyntax methodCallDeclaration;
            if (returnTypeName != "void")
            {
                methodCallDeclaration =
                    LocalDeclarationStatement(
                        VariableDeclaration(
                            IdentifierName(returnTypeName),
                            SeparatedList(new[] {
                                        VariableDeclarator(
                                            Identifier("expected"),
                                            null,
                                            EqualsValueClause(GetTypeLiteral(returnTypeSymbol.ToString())))
                            })
                    ));

                statements.Add(methodCallDeclaration);
                statements.Add(ParseStatement(templateAssertThatBody));
            }        
            statements.Add(ParseStatement(templateAssertFailBody));

            //add test method
            declaration = GetMethodTemplate(statements, methodName, attributeName);

            return declaration;
        }

        private static MethodDeclarationSyntax GetMethodTemplate(List<StatementSyntax> body, string methodName, string attributeName)
        {
            return MethodDeclaration(
                            PredefinedType(
                                Token(SyntaxKind.VoidKeyword)),
                            Identifier(methodName))
                            .WithAttributeLists(
                                SingletonList(
                                    AttributeList(
                                        SingletonSeparatedList(
                                            Attribute(
                                                IdentifierName(attributeName))))))
                            .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
                            .WithBody(Block(body));
        }

        private static void AddVariable(List<StatementSyntax> statements, List<ArgumentSyntax> methodParams, INamedTypeSymbol typeSymbol, string variableName = null)
        {

            string typeName = RemoveAllNamespacePrefixes(typeSymbol.ToString()); //typeSymbol.ToString().Substring(typeSymbol.ToString().LastIndexOf(".") + 1);
            
            if (typeName.First() == 'I')
            {
                variableName ??= (char.ToLower(typeName[1]) + typeName.Substring(2));
                var variable = ExpressionStatement(
                    AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        IdentifierName(variableName),
                        ObjectCreationExpression(
                            GenericName(
                                Identifier("Mock"))
                            .WithTypeArgumentList(
                                TypeArgumentList(
                                    SingletonSeparatedList<TypeSyntax>(
                                        IdentifierName(typeName)))))
                        .WithArgumentList(
                            ArgumentList())));
                methodParams.Add(Argument(IdentifierName(variableName)));
                statements.Add(variable);
            }
            else
            {
                variableName ??= (char.ToLower(typeName[0]) + typeName.Substring(1));
                var variable =
                    LocalDeclarationStatement(
                        VariableDeclaration(
                            ParseTypeName(typeName),
                            SeparatedList(new[] {
                                        VariableDeclarator(
                                            Identifier(variableName),
                                            null,
                                            EqualsValueClause(GetTypeLiteral(typeSymbol.ToString())))
                            })
                    ));

                methodParams.Add(Argument(IdentifierName(variableName)));
                statements.Add(variable);
            }
        }

        private static SyntaxList<MemberDeclarationSyntax> GetTemplateFields(ClassDeclarationSyntax clsInfo, SemanticModel model)
        {           
            List<MemberDeclarationSyntax> classFields = new List<MemberDeclarationSyntax>();

            //add main object
            var className = clsInfo.Identifier.ValueText;
            FieldDeclarationSyntax field = FieldDeclaration(
                            VariableDeclaration(
                                ParseTypeName(className),
                                SeparatedList(new[] { VariableDeclarator(Identifier(char.ToLower(className[0]) + className.Substring(1))) })
                            ))
                            .AddModifiers(Token(SyntaxKind.PrivateKeyword));
            classFields.Add(field);

            //breakdown constructor / add mock objects
            var constructor = (ConstructorDeclarationSyntax)clsInfo.ChildNodes().FirstOrDefault(n => n.Kind() == SyntaxKind.ConstructorDeclaration);
            if (constructor != null)
            {
                var parameters = constructor.ParameterList.ChildNodes().Cast<ParameterSyntax>();
                FieldDeclarationSyntax lastAddedField = null;
                foreach (ParameterSyntax parameter in parameters)
                {
                    var typeSymbol = model.GetSymbolInfo(parameter.Type).Symbol as INamedTypeSymbol;
                    var typeName = RemoveAllNamespacePrefixes(typeSymbol.ToString()); //typeSymbol.ToString().Substring(typeSymbol.ToString().LastIndexOf(".") + 1);
                    if (typeName.First() == 'I')
                    {
                        lastAddedField = field = GetMockObject(typeName);
                        
                        classFields.Add(field);
                    }
                }
        //        lastAddedField?.WithTrailingTrivia(SyntaxFactory.Whitespace("\n"));
            }

            return List(classFields);
        }

        private static FieldDeclarationSyntax GetMockObject(string typeName)
        {
            FieldDeclarationSyntax field = FieldDeclaration(
                                        VariableDeclaration(
                                            ParseTypeName($"Mock<{typeName}>"),
                                            SeparatedList(new[] { VariableDeclarator(Identifier(char.ToLower(typeName[1]) + typeName.Substring(2))) })
                                        ))
                                        .AddModifiers(Token(SyntaxKind.PrivateKeyword));
            return field;
        }

        public static Task<List<GeneratedTestClass>> Generate(string code)
        {
            return Task.Run(() => {

                List<GeneratedTestClass> generatedClasses = new List<GeneratedTestClass>();

                var tree = CSharpSyntaxTree.ParseText(code);
                var syntaxRoot = tree.GetRoot();

                var mscorlib = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
                var compilation = CSharpCompilation.Create("MyCompilation",
                    syntaxTrees: new[] { tree }, references: new[] { mscorlib });
                var model = compilation.GetSemanticModel(tree);

                var classDeclarations = syntaxRoot.DescendantNodes().OfType<ClassDeclarationSyntax>();

                foreach (var clsInfo in classDeclarations)
                {
                    string className = clsInfo.Identifier.ValueText;
                    string clsNamespace = ((NamespaceDeclarationSyntax)clsInfo.Parent).Name.ToString();

                    NamespaceDeclarationSyntax template_namespace = NamespaceDeclaration(
                        QualifiedName(
                            IdentifierName(className), IdentifierName("Test")));

                    var template_usings = GetTemplateUsing(clsNamespace);

                    var template_methods = GetTemplateMethods(clsInfo, model);
                    var template_fields = GetTemplateFields(clsInfo, model);
                    var template_members = List(template_fields.Concat(template_methods));

                    var template_classname = className + "Tests";                

                    //Class declaration
                    var classTemplate =
                      CompilationUnit()
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
                                 .WithMembers(template_members)))));

                    string generatedCode = classTemplate.NormalizeWhitespace().ToFullString();
                    string generatedName = template_classname + ".cs";

                    generatedClasses.Add(new GeneratedTestClass(generatedName, generatedCode));

                }

                return generatedClasses;

            });
        }
    }
}
