using System;
using System.IO;
using System.Text.RegularExpressions;

namespace FormalMethodsProject
{
    class Program
    {
        static void Main(string[] args)
        {
            //args[0] - bnf file, args[1] - input text file, args[2] - output xml file
            if (args.Length == 0)
                Console.WriteLine("No command line arguments were given");
            else
            {
                try
                {
                    Tree tree = new Tree(args[0], args[2]);
                    StreamReader reader = new StreamReader(args[1]);
                    tree.ConstructTree();
                    Regex regex = new Regex(tree.GetRegex());
                    Console.WriteLine("regex: " + tree.GetRegex());
                    Match match = regex.Match(reader.ReadLine());
                    if (match.Success)
                        tree.SaveAsXml(match.Value);
                    else
                        Console.WriteLine("Match unsuccessful");
                    reader.Close();
                }
                catch (Exception ex) { Console.WriteLine(ex.Message); }
            }
        }
    }
}