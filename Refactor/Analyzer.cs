using LevenshteinDistance;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Refactor.dto;
using Soenneker.Utils.String.CosineSimilarity;

namespace Refactor;

public class Analyzer
{
    private const double MAX_ALLOWED_SIMILARITY = 0.8;
    private FileClassDeclarations[] ClassDeclarations { get; }

    private readonly List<RefactorOportunity> _refactorOpportunities  = new();

    public Analyzer(IEnumerable<string> files)
    {
        CsFile[] syntaxTrees = files.Select(f => new CsFile(f)).ToArray();
        ClassDeclarations = syntaxTrees.SelectMany(f => f.classDeclarations).ToArray();
    }

    public void Analyze()
    {
        CheckSimilarNodes();
    }
    
    public List<RefactorOportunity> GetRefactorOpportunities()
    {
        return _refactorOpportunities;
    }

    private void CheckSimilarNodes()
    {
        for (int i = 0; i < ClassDeclarations.Length; i++)
        {
            var aClassMethods = ClassDeclarations[i]
                .classDeclaration.Members.OfType<MethodDeclarationSyntax>()
                .ToList();
            for (int j = i + 1; j < ClassDeclarations.Length; j++)
            {
                var bClassMethods = ClassDeclarations[j]
                    .classDeclaration.Members.OfType<MethodDeclarationSyntax>()
                    .ToList();
                CheckMethodsSimilarity(aClassMethods, bClassMethods, ClassDeclarations[i], ClassDeclarations[j]);
            }
        }
    }

    private void CheckMethodsSimilarity(List<MethodDeclarationSyntax> aClassMethods,
        List<MethodDeclarationSyntax> bClassMethods, FileClassDeclarations a, FileClassDeclarations b)
    {
        foreach (var aMethod in aClassMethods)
        {
            foreach (var bMethod in bClassMethods)
            {
                var sameReturnType = HasSameReturnType(aMethod, bMethod);
                var sameParameters = HasSameParameters(aMethod, bMethod);
                var areSimilar = CheckBodySimilarity(aMethod, bMethod);

                if (sameReturnType && sameParameters && areSimilar)
                {
                    _refactorOpportunities.Add(new RefactorOportunity(a, b, aMethod, bMethod));
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
        var aBody = a.Body?.ToString() ?? "";
        var bBody = b.Body?.ToString() ?? "";
        return RabinKarpSimilarity.CalculateSimilarity(aBody, bBody) >= MAX_ALLOWED_SIMILARITY;
    }
}