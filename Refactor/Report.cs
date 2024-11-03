using Microsoft.CodeAnalysis.CSharp.Syntax;
using Refactor.dto;

namespace Refactor;

public static class Report
{
    public static void GenerateReport(IEnumerable<RefactorOportunity> refactorOportunities) 
    {
        foreach (var refactorOportunity in refactorOportunities)
        {
            var fileA = refactorOportunity.fileA.parent.path;
            var fileB = refactorOportunity.fileB.parent.path;
            var classNameA = refactorOportunity.fileA.classDeclaration.Identifier.Text;
            var classNameB = refactorOportunity.fileB.classDeclaration.Identifier.Text;
            var returnTypeA = refactorOportunity.methodA.ReturnType.ToString();
            var methodParamsA = refactorOportunity.methodA.ParameterList.Parameters;
            var methodA = refactorOportunity.methodA.Identifier.Text;
            var methodB = refactorOportunity.methodB.Identifier.Text;
            
            var formattedParamsA = GetFormattedParamsList(methodParamsA);
            
            var newClassName = $"{classNameA}{classNameB}Superclass";
            var newMethodName = $"{methodA}{methodB}CommonMethod";
            
            Console.WriteLine($"Oportunidade de refatoração encontrada nas classes {classNameA} e {classNameB}");
            Console.WriteLine($"Métodos {methodA} e {methodB} são similares");
            Console.WriteLine();
            Console.WriteLine("Sugestão de refatoração:");
            Console.WriteLine("Crie uma nova classe e implemente um método comum. Ex.:");
            Console.WriteLine($"class {newClassName}");
            Console.WriteLine("{");
            Console.WriteLine($"    public {returnTypeA} {newMethodName}({formattedParamsA})");
            Console.WriteLine("    {");
            Console.WriteLine("        // Implementação do método comum");
            Console.WriteLine("    }");
            Console.WriteLine("}");
            Console.WriteLine();
            
            Console.WriteLine($"Modifique as classes {classNameA} e {classNameB} para utilizarem a nova classe:");
            Console.WriteLine($"class {classNameA} : {newClassName}");
            Console.WriteLine("{");
            Console.WriteLine($"    public {returnTypeA} {newMethodName}({formattedParamsA}) : base({formattedParamsA})");
            Console.WriteLine("          // Mantenha apenas membros específicos dessa classe");
            Console.WriteLine("     }");
            Console.WriteLine("}");
            Console.WriteLine();
            Console.WriteLine($"class {classNameB} : {newClassName}");
            Console.WriteLine("{");
            Console.WriteLine($"    public {returnTypeA} {newMethodName}({formattedParamsA}) : base({formattedParamsA})");
            Console.WriteLine("          // Mantenha apenas membros específicos dessa classe");
            Console.WriteLine("     }");
            Console.WriteLine("}");
            
            Console.WriteLine("\n----------------------------------------------------------------\n");
        }
    }
    
    private static string GetFormattedParamsList(IEnumerable<ParameterSyntax> parameters)
    {
        return string.Join(", ", parameters.Select(p =>  $"{p.Type} {p.Identifier}"));;
    }
}