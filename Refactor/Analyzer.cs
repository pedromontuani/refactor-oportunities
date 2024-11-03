using LevenshteinDistance;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Refactor.dto;

namespace Refacor;

public class Analyzer
{
    private CsFile[] _syntaxTrees;
    private Dictionary<ClassDeclarationSyntax, HashSet<FileClassDeclarations>> _classGroups = new();
    private FileClassDeclarations[] _classDeclarations { get; }
    
    private List<HashSet<ClassDeclarationSyntax>> relatedClassGroups = new ();
    private HashSet<ClassDeclarationSyntax> visited = new ();
    private List<RefactorOportunity> refactorOpportunities = new ();
    
    public Analyzer(IEnumerable<string> files)
    {
        _syntaxTrees = files.Select(f => new CsFile(f)).ToArray();
        _classDeclarations = _syntaxTrees.SelectMany(f => f.classDeclarations).ToArray();
    }

    public void Analyze()
    {
        // CompareClasses();
        CheckSimilarNodes();
        // BuildRelationships();
        // GetRefactorOpportunities();
    }

    private void CheckSimilarNodes()
    {
        for(int i=0; i < _classDeclarations.Length; i++)
        {
            var aClassMethods = _classDeclarations[i].classDeclaration.Members.OfType<MethodDeclarationSyntax>().ToList();
            for(int j=i+1; j < _classDeclarations.Length; j++)
            {
                var bClassMethods = _classDeclarations[j].classDeclaration.Members.OfType<MethodDeclarationSyntax>().ToList();
                CheckMethodsSimilarity(aClassMethods, bClassMethods, _classDeclarations[i], _classDeclarations[j]);
            }
        }
    }
    
    private void CheckMethodsSimilarity(List<MethodDeclarationSyntax> aClassMethods, List<MethodDeclarationSyntax> bClassMethods, FileClassDeclarations a, FileClassDeclarations b)
    {
        foreach(var aMethod in aClassMethods)
        {
            foreach(var bMethod in bClassMethods)
            {
                var sameReturnType = HasSameReturnType(aMethod, bMethod);
                var sameParameters = HasSameParameters(aMethod, bMethod);
                var similarity = CheckBodySimilarity(aMethod, bMethod);
                
                if(HasSameReturnType(aMethod, bMethod) && HasSameParameters(aMethod, bMethod) && CheckBodySimilarity(aMethod, bMethod))
                {
                    refactorOpportunities.Add(new RefactorOportunity(a, b, aMethod, bMethod));
                }
            }
        }
    }

    private bool HasSameReturnType(MethodDeclarationSyntax a, MethodDeclarationSyntax b)
    {
        return a.ReturnType.ToString() == b.ReturnType.ToString();
    }
    
    private bool HasSameParameters(MethodDeclarationSyntax a, MethodDeclarationSyntax b)
    {
        var aParams = a.ParameterList.Parameters.Select(p => p.ToString()).ToList();
        var bParams = b.ParameterList.Parameters.Select(p => p.ToString()).ToList();
        return aParams.SequenceEqual(bParams);
    }
    
    private bool CheckBodySimilarity(MethodDeclarationSyntax a, MethodDeclarationSyntax b)
    {
        var aBody = a.Body.ToString();
        var bBody = b.Body.ToString();
        return Levenshtein.Distance(aBody, bBody) <= 3;
    }

    private void GetRefactorOpportunities()
    {
        foreach (var group in relatedClassGroups)
        {
            if (group.Count > 1)
            {
                var classNames = string.Join(", ", group.Select(c => $"{c.Identifier} na linha {GetLineNumber(c)}"));
                Console.WriteLine($"Classes {classNames} têm membros em comum.");
                Console.WriteLine("Sugestão: Considere criar uma classe base para compartilhar membros comuns.");

                // Sugerir o nome da classe base e os membros a serem movidos
                var commonMembers = GetCommonMembers(group);
                Console.WriteLine("\nSugestão de classe base:");
                Console.WriteLine("class BaseClass");
                Console.WriteLine("{");
                foreach (var member in commonMembers)
                {
                    Console.WriteLine($"    {member};");
                }
                Console.WriteLine("}");

                // Sugerir modificações para as classes derivadas
                foreach (var classDeclaration in group)
                {
                    Console.WriteLine($"\nSugestão de modificação para {classDeclaration.Identifier}:");
                    Console.WriteLine($"class {classDeclaration.Identifier} : BaseClass");
                    Console.WriteLine("{");
                    Console.WriteLine("    // Mantenha apenas membros específicos dessa classe");
                    Console.WriteLine("}");
                }
            }
        }
    }

    private void CompareClasses()
    {
        for (int i = 0; i < _classDeclarations.Length; i++)
        {
            for (int j = i + 1; j < _classDeclarations.Length; j++)
            {
                var iClass = _classDeclarations[i].classDeclaration;
                var jClass = _classDeclarations[j].classDeclaration;
                
                if (HaveCommonMembers(_classDeclarations[i], _classDeclarations[j]))
                {
                    if (!_classGroups.ContainsKey(iClass))
                        _classGroups.Add(iClass, new ());;

                    if (!_classGroups.ContainsKey(jClass))
                        _classGroups.Add(jClass, new ());;

                    _classGroups[iClass].Add(_classDeclarations[j]);
                    _classGroups[jClass].Add(_classDeclarations[i]);
                }
            }
        }
    }

    private void BuildRelationships()
    {
        foreach (var classDeclaration in _classGroups.Keys)
        {
            if (!visited.Contains(classDeclaration))
            {
                var group = new HashSet<ClassDeclarationSyntax>();
                BuildClassGroup(classDeclaration, group);
                relatedClassGroups.Add(group);
            }
        }
    }
    
    private bool HaveCommonMembers(FileClassDeclarations class1, FileClassDeclarations class2)
    {
        var a = class1.classDeclaration.Members;
        var b = a[0];
        var members1 = class1.classDeclaration.Members.Select(m => m.ToString()).ToHashSet();
        var members2 = class2.classDeclaration.Members.Select(m => m.ToString()).ToHashSet();
        return members1.Overlaps(members2);
    }
    
    private void BuildClassGroup(ClassDeclarationSyntax classDeclaration, HashSet<ClassDeclarationSyntax> group)
    {
        if (visited.Contains(classDeclaration))
            return;

        visited.Add(classDeclaration);
        group.Add(classDeclaration);

        if (_classGroups.ContainsKey(classDeclaration))
        {
            foreach (var relatedClass in _classGroups[classDeclaration])
            {
                BuildClassGroup(relatedClass.classDeclaration, group);
            }
        }
    }
    
    private int GetLineNumber(SyntaxNode node)
    {
        var lineSpan = node.SyntaxTree.GetLineSpan(node.Span);
        return lineSpan.StartLinePosition.Line + 1;
    }

    private HashSet<string> GetCommonMembers(HashSet<ClassDeclarationSyntax> classes)
    {
        // Identificar membros comuns entre todas as classes no grupo
        var memberSets = classes.Select(c => c.Members.Select(m => m.ToString()).ToHashSet()).ToList();
        var commonMembers = memberSets.Aggregate((set1, set2) => new HashSet<string>(set1.Intersect(set2)));
        return commonMembers;
    }
}