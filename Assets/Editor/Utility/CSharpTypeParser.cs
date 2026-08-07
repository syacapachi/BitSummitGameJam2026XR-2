using System;
using System.Collections.Generic;
using System.Text;

public static class CSharpTypeParser
{
    private static readonly HashSet<string> TypeKeywords = new()
    {
        "class",
        "struct",
        "interface",
        "record",
        "enum"
    };
    private static readonly List<string> tokenCache = new();

    public static int GetFullNames(string source, List<string> result)
    {
        result ??= new List<string>();
        result.Clear();
        source = RemoveCommentsAndStrings(source);

        Tokenize(source, tokenCache);

        string currentNamespace = "";

        var typeStack = new Stack<(string Name, int BraceDepth)>();

        int braceDepth = 0;

        for (int i = 0; i < tokenCache.Count; i++)
        {
            string token = tokenCache[i];

            switch (token)
            {
                case "{":
                    braceDepth++;
                    break;

                case "}":
                    braceDepth--;

                    while (typeStack.Count > 0 &&
                           typeStack.Peek().BraceDepth > braceDepth)
                    {
                        typeStack.Pop();
                    }

                    break;
            }

            //----------------------------------------
            // namespace
            //----------------------------------------

            if (token == "namespace")
            {
                var sb = new StringBuilder();

                i++;

                while (i < tokenCache.Count)
                {
                    if (tokenCache[i] == "{" || tokenCache[i] == ";")
                        break;

                    sb.Append(tokenCache[i]);

                    i++;
                }

                currentNamespace = sb.ToString();

                continue;
            }

            //----------------------------------------
            // class / struct ...
            //----------------------------------------

            if (!TypeKeywords.Contains(token))
                continue;

            if (i + 1 >= tokenCache.Count)
                continue;

            string typeName = tokenCache[++i];

            while (i + 1 < tokenCache.Count &&
                   tokenCache[i + 1] == "<")
            {
                while (i < tokenCache.Count &&
                       tokenCache[i] != ">")
                    i++;

                if (i >= tokenCache.Count)
                    break;
            }

            typeStack.Push((typeName, braceDepth + 1));

            var names = typeStack.ToArray();
            Array.Reverse(names);

            var sb2 = new StringBuilder();

            if (!string.IsNullOrEmpty(currentNamespace))
            {
                sb2.Append(currentNamespace);
                sb2.Append(".");
            }

            for (int j = 0; j < names.Length; j++)
            {
                if (j != 0)
                    sb2.Append("+");

                sb2.Append(names[j].Name);
            }

            result.Add(sb2.ToString());
        }

        return result.Count;
    }

    /// <summary>
    /// 予約語や変数名,記号を切り出す。
    /// </summary>
    /// <param name="text"></param>
    /// <param name="tokens"></param>
    /// <returns></returns>
    //------------------------------------------------

    private static int Tokenize(string text, List<string> tokens)
    {
        tokens ??= new List<string>();
        tokens.Clear();

        var sb = new StringBuilder();

        foreach (char c in text)
        {
            if (char.IsLetterOrDigit(c) || c == '_')
            {
                sb.Append(c);
            }
            else
            {
                Flush();

                if (!char.IsWhiteSpace(c))
                    tokens.Add(c.ToString());
            }
        }

        Flush();

        return tokens.Count;

        void Flush()
        {
            if (sb.Length == 0)
                return;

            tokens.Add(sb.ToString());
            sb.Clear();
        }
    }
    /// <summary>
    /// コメントや、文字列を削除
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    //------------------------------------------------

    private static string RemoveCommentsAndStrings(string text)
    {
        var sb = new StringBuilder();

        bool lineComment = false;
        bool blockComment = false;
        bool str = false;
        bool verbatim = false;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];

            char next = i + 1 < text.Length ? text[i + 1] : '\0';

            if (lineComment)
            {
                if (c == '\n')
                {
                    lineComment = false;
                    sb.Append('\n');
                }

                continue;
            }

            if (blockComment)
            {
                if (c == '*' && next == '/')
                {
                    blockComment = false;
                    i++;
                }

                continue;
            }

            if (str)
            {
                if (verbatim)
                {
                    if (c == '"' && next == '"')
                    {
                        i++;
                        continue;
                    }

                    if (c == '"')
                    {
                        str = false;
                        verbatim = false;
                    }
                }
                else
                {
                    if (c == '\\')
                    {
                        i++;
                        continue;
                    }

                    if (c == '"')
                        str = false;
                }

                continue;
            }

            if (c == '/' && next == '/')
            {
                lineComment = true;
                i++;
                continue;
            }

            if (c == '/' && next == '*')
            {
                blockComment = true;
                i++;
                continue;
            }

            if (c == '@' && next == '"')
            {
                str = true;
                verbatim = true;
                i++;
                continue;
            }

            if (c == '"')
            {
                str = true;
                continue;
            }

            sb.Append(c);
        }

        return sb.ToString();
    }
}