using System;
using System.Collections.Generic;
using System.IO;

/*  http://www.johnyrambo.com
    +38766987654
    https?:(\/\/)(www\.)?(([a-zA-Z0-9]+)+)\.[a-z]+  */

namespace FormalMethodsProject
{
    class BNFFileManager
    {
        private readonly FileStream file;
        private readonly StreamReader reader;
        private readonly StreamWriter writer;
        private LinkedList<string> errors;
        private string email = "<email>::=\"([a-zA-Z]+)([0-9]+)@(gmail|hotmail|yahoo|outlook|aol|yandex)((.com)|(.org)|(.net))\"";
        private string phoneNumber = "<phone_number>::=\"\\+387[0-9]{8}\"";
        private string webLink = "<web_link>::=\"https?:\\/\\/(www\\.)?(([a-zA-Z0-9]+)+)\\.com\"";
        private string numberConstant = "<number_constant>::=\"[0-9]+(.?)[0-9]+\"";
        private string bigCity = "";
        public int LineCount { get; set; }
        public bool EndOfFile { get { return reader.EndOfStream; } }
        public BNFFileManager(string fileName)
        {
            file = new FileStream(fileName, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            reader = new StreamReader(file);
            writer = new StreamWriter(file);
            errors = new LinkedList<string>();
            LineCount = 0;
            ReviseBNF();
            CountLines();
        }
        private void CountLines()
        {
            string line;
            while (!reader.EndOfStream)
            {
                line = reader.ReadLine();
                LineCount++;
            }
            Rewind();
        }
        public bool IsPassable() { return errors.Count == 0; }
        public void Rewind() { file.Position = 0; }
        public void PrintErrors()
        {
            foreach (string error in errors)
                Console.WriteLine(error);
        }
        public LinkedList<string> GetOneLineTokens()
        {
            if (!reader.EndOfStream)
            {
                string fragment = "";
                LinkedList<string> tokens = new LinkedList<string>();
                string line = reader.ReadLine();
                for (int lineIndex = 0, tokenIndex; lineIndex < line.Length; lineIndex++)
                {
                    if (line[lineIndex] == '<')
                    {
                        tokenIndex = lineIndex;
                        do { fragment += line[tokenIndex]; }
                        while (line[tokenIndex++] != '>');
                        tokens.AddLast(fragment);
                    }
                    fragment = "";
                }
                return tokens;
            }
            else return null;
        }
        public LinkedList<string> GetAllTerminals()
        {
            LinkedList<string> terminals = new LinkedList<string>();
            LinkedList<string> lineTokens;
            IEnumerator<string> enumerator;
            while (!reader.EndOfStream)
            {
                lineTokens = GetOneLineTokens();
                enumerator = lineTokens.GetEnumerator();
                enumerator.MoveNext();
                if (lineTokens.Count == 1)
                {
                    if (terminals.Contains(enumerator.Current) == false)
                        terminals.AddLast(enumerator.Current);
                }
            }
            Rewind();
            return terminals;
        }
        private void ReviseBNF() //ne provjerava da li su key tokens vec definisani u samom fajlu
        {
            LinkedList<string> keyTokens = new LinkedList<string>();
            LinkedList<string> lineTokens;
            while (!EndOfFile)
            {
                lineTokens = GetOneLineTokens();
                foreach (string token in lineTokens)
                    if (!keyTokens.Contains(token))
                        if (token.Equals("<email>") || token.Equals("<phone_number>") || token.Equals("<web_link>") || token.Equals("<number_constant>") || token.Equals("<big_city>"))
                            keyTokens.AddLast(token);
            }
            file.Position = file.Length;
            if (keyTokens.Count != 0)
            {
                IEnumerator<string> enumerator = keyTokens.GetEnumerator();
                enumerator.MoveNext();
                writer.Write(Environment.NewLine);
                for (int i = 0; i < keyTokens.Count; i++, enumerator.MoveNext())
                {
                    if (enumerator.Current.Equals("<email>"))
                        writer.Write(email);
                    else if (enumerator.Current.Equals("<phone_number>"))
                        writer.Write(phoneNumber);
                    else if (enumerator.Current.Equals("<web_link>"))
                        writer.Write(webLink);
                    else if (enumerator.Current.Equals("<number_constant>"))
                        writer.Write(numberConstant);
                    else if (enumerator.Current.Equals("<big_city>"))
                        writer.Write(bigCity);
                    if (i < keyTokens.Count - 1)
                        writer.Write(Environment.NewLine);
                }
                writer.Flush();
            }
            Rewind();
        }
        public string GetDefinitionIfTerminal(string token)
        {
            string result = "";
            LinkedList<string> terminals = GetAllTerminals();
            if (terminals.Contains(token))
            {
                while (!EndOfFile)
                {
                    result = reader.ReadLine();
                    if (result.Contains(token) && result.Contains("::=regex("))
                    {
                        result = result.Replace(token, String.Empty).Replace("::=", String.Empty).Replace("regex(", String.Empty).Replace(")", String.Empty);
                        Rewind();
                        return result;
                    }
                    else if (result.Contains(token) && result.Contains("::=\""))
                    {
                        result = result.Replace(token, String.Empty).Replace("::=", String.Empty).Replace("\"", String.Empty);
                        Rewind();
                        return result;
                    }
                }
                Rewind();
            }
            return result;
        }
    }
}
