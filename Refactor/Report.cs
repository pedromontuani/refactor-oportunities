using Microsoft.CodeAnalysis.CSharp.Syntax;
using Refactor.dto;

namespace Refactor;

public static class Report
{
    public static void GenerateReport(int count, string outputPath)
    {
        if (count > 0)
        {
            Console.WriteLine($"Foram encontradas {count} oportunidades de implementação do padrão Strategy");
            Console.WriteLine($"Os novos arquivos foram gerados na pasta {outputPath}");
        }
        else
        {
            Console.WriteLine("Não foram encontradas opotunidades de implementação do padrão Strategy");
        }
    }
}