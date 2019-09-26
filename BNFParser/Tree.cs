using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace FormalMethodsProject
{
    class Tree
    {
        private BNFFileManager manager;
        private readonly string outputFile;
        private Node root;
        public int Count { get; set; }
        public Node Root { get { return root; } set { root = value; } }
        public Tree(string bnfFile, string outputFile)
        {
            root = null;
            manager = new BNFFileManager(bnfFile);
            this.outputFile = outputFile;
        }
        public void ConstructTree()
        {
            if (!manager.IsPassable())
                manager.PrintErrors();
            else
            {
                LinkedList<string>[] lines = new LinkedList<string>[manager.LineCount];
                LinkedList<string> terminals = manager.GetAllTerminals(), offsprings;
                Queue<string> toVisit = new Queue<string>();
                IEnumerator<string> enumerator;
                string currentToken;
                Node position;
                for (int i = 0; i < manager.LineCount; i++)
                    lines[i] = manager.GetOneLineTokens();
                manager.Rewind();
                enumerator = lines[0].GetEnumerator();
                enumerator.MoveNext();
                root = new Node(enumerator.Current, "");
                Count++;
                lines[0].RemoveFirst();
                enumerator = lines[0].GetEnumerator();
                enumerator.MoveNext();
                root.AddAllChildren(lines[0], terminals, manager);
                for (int i = 0; i < lines[0].Count; i++, enumerator.MoveNext(), Count++)
                    toVisit.Enqueue(enumerator.Current);
                while (toVisit.Count > 0)
                {
                    currentToken = toVisit.Dequeue();
                    if (!terminals.Contains(currentToken))
                        try
                        {
                            offsprings = SearchDescendants(currentToken);
                            position = SearchNodePosition(currentToken, terminals);
                            position.AddAllChildren(offsprings, terminals, manager);
                            AddToQueue(offsprings, toVisit);
                        }
                        catch (Exception ex) { Console.WriteLine(ex.Message); }
                }
            }
        }
        public LinkedList<string> SearchDescendants(string subroot)
        {
            /*needs to check outside of method if returned list contains only terminal
             because here it throws System.NullReferenceException for some reason*/

            LinkedList<string> list;
            IEnumerator<string> enumerator;
            while (!manager.EndOfFile)
            {
                list = manager.GetOneLineTokens();
                enumerator = list.GetEnumerator();
                enumerator.MoveNext();
                if (enumerator.Current.Equals(subroot))
                {
                    list.RemoveFirst();
                    manager.Rewind();
                    return list;
                }
            }
            manager.Rewind();
            return null;
        }
        public void AddToQueue(LinkedList<string> list, Queue<string> queue)
        {
            foreach (string node in list)
            {
                queue.Enqueue(node);
                Count++;
            }
        }
        private void PreorderPositioning(ref Node save, Node currentNode, string positionTo, LinkedList<string> terminals)
        {
            //searching tree by preorder traversal to find key node and return its reference
            //put flag to stop method when node is found

            if (!terminals.Contains(currentNode.Token))
            {
                foreach (Node node in currentNode.GetDescendantList())
                {
                    if (node.Token.Equals(positionTo))
                        save = node;
                    PreorderPositioning(ref save, node, positionTo, terminals);
                }
            }
        }
        private Node SearchNodePosition(string keyToken, LinkedList<string> terminals)
        {
            Node start = root, saveSpot = null;
            PreorderPositioning(ref saveSpot, start, keyToken, terminals);
            return saveSpot;
        }
        private void ConstructRegex(Node currentNode, LinkedList<string> terminals, ref string regex)
        {
            if (manager.IsPassable())
            {
                if (!terminals.Contains(currentNode.Token))
                    foreach (Node node in currentNode.GetDescendantList())
                        ConstructRegex(node, terminals, ref regex);
                else
                {
                    string tmp = currentNode.Definition.Replace("\"", String.Empty);
                    regex += "(" + tmp + ")";
                }
            }
        }
        public string GetRegex()
        {
            string regex = "";
            ConstructRegex(Root, manager.GetAllTerminals(), ref regex);
            return regex;
        }
        private void SaveAllTerminalDef(Node start, Queue<string> terminalDefinitions)
        {
            if (manager.GetAllTerminals().Contains(start.Token))
                terminalDefinitions.Enqueue(start.Definition.Replace("\"", String.Empty));
            foreach (Node node in start.GetDescendantList())
                SaveAllTerminalDef(node, terminalDefinitions);
        }
        private void DisassembleMatch(Queue<string> matchGroups, Queue<string> terminalDefinitions, string matchedStr)
        {
            while (matchedStr.Length > 0 && terminalDefinitions.Count > 0)
            {
                Regex regex = new Regex(terminalDefinitions.Dequeue());
                Match match = regex.Match(matchedStr);
                if (match.Success)
                {
                    matchGroups.Enqueue(match.Value);
                    matchedStr = matchedStr.Remove(0, match.Value.Length);
                }
                else
                    Console.WriteLine("DISASSEMBLE MATCH ERROR");
            }
        }
        private void WriteToFile(Node currentNode, Queue<string> matchGroups, StreamWriter writer)
        {
            writer.WriteLine(currentNode.Token);
            if (manager.GetAllTerminals().Contains(currentNode.Token))
                writer.WriteLine("\t" + matchGroups.Dequeue());
            foreach (Node node in currentNode.GetDescendantList())
                WriteToFile(node, matchGroups, writer);
            writer.WriteLine(currentNode.Token.Insert(1, "/"));
        }
        public void SaveAsXml(string matchedStr)
        {
            Queue<string> terminalDefinitions = new Queue<string>();
            Queue<string> matchGroups = new Queue<string>();
            StreamWriter writer = new StreamWriter(outputFile);
            SaveAllTerminalDef(this.Root, terminalDefinitions);
            DisassembleMatch(matchGroups, terminalDefinitions, matchedStr);
            WriteToFile(this.Root, matchGroups, writer);
            writer.Close();
        }
    }
    class Node
    {
        private LinkedList<Node> descendants;
        public string Token { get; set; }
        public Node Parent { get; set; }
        public string Definition { get; set; }
        public int DescendantCount { get; set; }
        public Node(string token, string definition, Node parent = null)
        {
            Parent = parent;
            Token = token;
            Definition = definition;
            DescendantCount = 0;
            descendants = new LinkedList<Node>();
        }
        public void AddAllChildren(LinkedList<string> children, LinkedList<string> terminals, BNFFileManager manager)
        {
            foreach (string child in children)
            {
                string definition = manager.GetDefinitionIfTerminal(child);
                descendants.AddLast(new Node(child, definition, this));
            }
            DescendantCount = children.Count;
        }
        public LinkedList<Node> GetDescendantList() { return descendants; }
    }
}
