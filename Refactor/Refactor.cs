using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Refactor.dto;
using Refactor.utils;

namespace Refactor;

public class Refactor(Dictionary<string, List<Relationship>> relationships, string outputPath)
{
    private const string INTERFACE_NAMESPACE = "refactor.interfaces";
    private const string CLASSES_NAMESPACE = "refactor.classes";
    private const string INTERFACE_NAME = "GeneratedInterface";
    
    private readonly string _classPath = $"{outputPath}/classes";
    private readonly string _interfacePath = $"{outputPath}/interfaces";
    
    private int _generatedInterfaceNum = 0;
    private List<InterfaceMembers> _interfaceMembersList;
    
    public void GenerateRefactor()
    {
        FilesManager.InitOutputDirectory(_classPath);
        FilesManager.InitOutputDirectory(_interfacePath);
        
        _interfaceMembersList = GetMergedMethodsAndClasses();
        RemoveDuplicatedInterfaceMembers();

        foreach (var interfaceMembers in _interfaceMembersList)
        {
            GenerateInterface(interfaceMembers);
            GenerateClasses(interfaceMembers);

            _generatedInterfaceNum++;
        }
        
    }
    
    private List<InterfaceMembers> GetMergedMethodsAndClasses()
    {
        var interfaceMembers = new List<InterfaceMembers>();

        foreach (var key in relationships.Keys)
        {
            var methodDeclarations = relationships[key].Select(m => m.methodDeclaration).ToList();
            var classDeclarations = relationships[key].Select(r => r.classDeclaration).ToList();
            bool canAdd = true;
            
            foreach (var members in interfaceMembers)
            {
                bool hasSameClasses = classDeclarations.All(c => members.fileClassDeclarationsList.Contains(c));
                
                if (hasSameClasses)
                {
                    members.methodDeclarationSyntaxList.AddRange(methodDeclarations);
                    canAdd = false;
                    break;
                }
            }
            
            foreach (var members in interfaceMembers)
            {
                bool hasSameClasses = classDeclarations.All(c => members.fileClassDeclarationsList.Contains(c));
                bool hasAllMethods = methodDeclarations.All(m => members.methodDeclarationSyntaxList.Contains(m));
                if (hasSameClasses && hasAllMethods)
                {
                    canAdd = false;
                    break;
                }
            }

            if (canAdd)
            {
                var interfaceMember = new InterfaceMembers(classDeclarations, methodDeclarations);
                interfaceMembers.Add(interfaceMember);
            }
        }

        return interfaceMembers;
    }

    private void RemoveDuplicatedInterfaceMembers()
    {
        foreach (var interfaceMembers in _interfaceMembersList)
        {
            interfaceMembers.methodDeclarationSyntaxList = interfaceMembers.methodDeclarationSyntaxList.Distinct(new MethodDeclarationSyntaxComparer()).ToList();
            interfaceMembers.fileClassDeclarationsList = interfaceMembers.fileClassDeclarationsList.Distinct().ToList();
        }
    }

    private void GenerateInterface(InterfaceMembers interfaceMembers)
    {
        
        
        // Create the interface declaration
        var interfaceDeclaration = SyntaxFactory.InterfaceDeclaration(GetInterfaceName())
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));

        var methodDeclarations = interfaceMembers.methodDeclarationSyntaxList;

        foreach (var methodDeclaration in methodDeclarations)
        {
            var methodIdentifier = methodDeclaration.Identifier;
            var methodParams = methodDeclaration.ParameterList.Parameters;
            var returnType = methodDeclaration.ReturnType;

            // Add methods to the interface
            var methodSignature = SyntaxFactory.MethodDeclaration(returnType, methodIdentifier)
                .AddParameterListParameters(methodParams.ToArray())
                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));

            interfaceDeclaration = interfaceDeclaration.AddMembers(methodSignature);
        }
        

        // Create the namespace and add the interface to it
        var namespaceDeclaration = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(INTERFACE_NAMESPACE))
            .AddMembers(interfaceDeclaration);

        // Create the compilation unit and add the namespace to it
        var compilationUnit = SyntaxFactory.CompilationUnit()
            .AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System")))
            .AddMembers(namespaceDeclaration)
            .NormalizeWhitespace();

        // Generate the code
        var code = compilationUnit.ToFullString();
        FilesManager.SaveFile(_interfacePath, $"{GetInterfaceName()}.cs", code);
    }

    void GenerateClasses(InterfaceMembers interfaceMembers)
    {
        foreach (var fileClassDeclaration in interfaceMembers.fileClassDeclarationsList)
        {
            var classDeclaration = fileClassDeclaration.classDeclaration;
            var root = classDeclaration.SyntaxTree.GetRoot();

            var modifiedClass = classDeclaration.AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(GetInterfaceName())));
            var usingDirective = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(INTERFACE_NAMESPACE));
            var modifiedRoot = root.ReplaceNode(classDeclaration, modifiedClass);
            
            var namespaceDeclaration =
                modifiedRoot.DescendantNodes().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();

            var newNamespaceDeclaration = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(CLASSES_NAMESPACE))
                .WithMembers(namespaceDeclaration.Members)
                .NormalizeWhitespace();

            modifiedRoot = modifiedRoot.ReplaceNode(namespaceDeclaration, newNamespaceDeclaration);
            
            modifiedRoot = ((CompilationUnitSyntax)modifiedRoot).AddUsings(usingDirective);
            
            var modifiedCode = modifiedRoot.NormalizeWhitespace().ToFullString();
            FilesManager.SaveFile(_classPath, $"{classDeclaration.Identifier.Text}.cs", modifiedCode);
        }
    }
    
    private string GetInterfaceName()
    {
        return $"{INTERFACE_NAME}{_generatedInterfaceNum}";
    }
}