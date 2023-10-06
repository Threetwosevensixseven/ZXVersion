using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace ZXVersion
{
    class Program
    {
        private static Syntax Syntax = Syntax.Zeus;
        private static string File;
        private static string Dir;
        private static string DateFormat;
        private static string TimeFormat;
        private static string TimeFormatSecs;
        private static bool UpperCase = false;
        private static int Version = 0;
        private static bool IncludeWidths = false;
        private static bool IncludeMacros = false;
        private static bool IncludeStringLiterals = false;
        private static string Hash = "";
        private static string HashShort = "";

        static void Main(string[] args)
        {
            try
            {
                var now = DateTime.Now;

                // Alternate output file
                string oarg = args.FirstOrDefault(a => a.StartsWith("-o="));
                if (oarg != null)
                {
                    oarg = (oarg.Substring(3) ?? "").Trim();
                    if (oarg.StartsWith("\"") && oarg.EndsWith("\""))
                        oarg = oarg.Substring(1, oarg.Length - 2);
                    File = oarg;
                }

                // Create file and directory
                if (File == null)
                    File = (ConfigurationManager.AppSettings["OutputFile"] ?? "").Trim();
                if (string.IsNullOrEmpty(File)) File = "version.asm";
                Dir = Path.GetDirectoryName(File);
                if (!string.IsNullOrEmpty(Dir) && !Directory.Exists(Dir))
                    Directory.CreateDirectory(Dir);

                // Set formats
                DateFormat = (ConfigurationManager.AppSettings["DateFormat"] ?? "");
                if (string.IsNullOrEmpty(DateFormat)) DateFormat = "dd MMM yyyy";
                TimeFormat = (ConfigurationManager.AppSettings["TimeFormat"] ?? "");
                if (string.IsNullOrEmpty(TimeFormat)) TimeFormat = "HH:mm";
                TimeFormatSecs = (ConfigurationManager.AppSettings["TimeFormatSecs"] ?? "");
                if (string.IsNullOrEmpty(TimeFormat)) TimeFormatSecs = "HH:mm:ss";
                string uc = (ConfigurationManager.AppSettings["UpperCase"] ?? "false");
                bool.TryParse(uc, out UpperCase);
                IncludeWidths = (ConfigurationManager.AppSettings["IncludeWidths"] ?? "").Trim().ToLower() == "true";
                IncludeMacros = (ConfigurationManager.AppSettings["IncludeMacros"] ?? "").Trim().ToLower() == "true";
                IncludeStringLiterals = (ConfigurationManager.AppSettings["IncludeStringLiterals"] ?? "").Trim().ToLower() == "true";
                string syn = (ConfigurationManager.AppSettings["Syntax"] ?? "").Trim().ToLower();
                if (syn == "sjasmplus") Syntax = Syntax.Sjasmplus;

                // Get git version
                try
                {
                    var p = new Process();
                    p.StartInfo.FileName = "git.exe";
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.RedirectStandardOutput = true;
                    p.StartInfo.Arguments = "rev-list --count HEAD";
                    // "git rev-parse --short HEAD" will give "0d03e98" etc
                    p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    p.Start();
                    string output = p.StandardOutput.ReadToEnd();
                    p.WaitForExit();
                    output = output.Replace("\r", "").Replace("\n", "");
                    int.TryParse(output, out Version);
                }
                catch
                {
                    Version = 0;
                }

                // Get git long hash
                try
                {
                    var p = new Process();
                    p.StartInfo.FileName = "git.exe";
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.RedirectStandardOutput = true;
                    p.StartInfo.Arguments = "rev-parse HEAD";
                    // "git rev-parse --short HEAD" will give "0d03e98" etc
                    p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    p.Start();
                    string output = p.StandardOutput.ReadToEnd();
                    p.WaitForExit();
                    output = output.Replace("\r", "").Replace("\n", "");
                    Hash = (output ?? "").Trim();
                }
                catch
                {
                    Hash = "";
                }

                // Get git short hash
                try
                {
                    var p = new Process();
                    p.StartInfo.FileName = "git.exe";
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.RedirectStandardOutput = true;
                    p.StartInfo.Arguments = "rev-parse --short HEAD";
                    // "git rev-parse --short HEAD" will give "0d03e98" etc
                    p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    p.Start();
                    string output = p.StandardOutput.ReadToEnd();
                    p.WaitForExit();
                    output = output.Replace("\r", "").Replace("\n", "");
                    HashShort = Hash.PadRight(36).Substring(0, 7).Trim();
                }
                catch
                {
                    HashShort = "";
                }

                // Create file
                var sb = new StringBuilder();
                sb.AppendLine("; version.asm");
                sb.AppendLine(";");
                sb.AppendLine("; Auto-generated by ZXVersion.exe");
                sb.Append("; On ");
                sb.Append(Upper(now.ToString(DateFormat)));
                sb.Append(" at ");
                sb.AppendLine(Upper(now.ToString(TimeFormat)));
                sb.AppendLine();
                if (IncludeMacros)
                {
                    StartMacro(sb, "BuildNo");
                    sb.Append("                        db \"");
                    sb.Append(Version.ToString());
                    sb.AppendLine("\"");
                    EndMacro(sb);
                    sb.AppendLine();
                }
                sb.Append("BuildNoValue            equ \"");
                sb.Append(Version.ToString());
                sb.AppendLine("\"");
                if (IncludeWidths)
                {
                    sb.Append("BuildNoWidth            equ 0");
                    foreach (var chr in Version.ToString())
                        sb.Append(" + FW" + chr.ToString());
                    sb.AppendLine();
                }
                sb.AppendLine();
                sb.AppendLine();
                sb.AppendLine();

                if (IncludeMacros || IncludeStringLiterals)
                {
                    if (IncludeMacros)
                    {
                        StartMacro(sb, "CommitHash");
                        sb.Append("                        db \"");
                        sb.Append(Upper(Hash));
                        sb.AppendLine("\"");
                        EndMacro(sb);
                        sb.AppendLine();
                        StartMacro(sb, "CommitHashShort");
                        sb.Append("                        db \"");
                        sb.Append(Upper(HashShort));
                        sb.AppendLine("\"");
                        EndMacro(sb);
                        sb.AppendLine();
                    }
                    if (IncludeStringLiterals)
                    {
                        StringLiteral(sb, "CommitHashValue", Upper(Hash));
                        sb.AppendLine();
                        StringLiteral(sb, "CommitHashShortValue", Upper(HashShort));
                        sb.AppendLine();
                    }
                }

                if (IncludeMacros)
                {
                    StartMacro(sb, "BuildDate");
                    sb.Append("                        db \"");
                    sb.Append(Upper(now.ToString(DateFormat)));
                    sb.AppendLine("\"");
                    EndMacro(sb);
                    sb.AppendLine();
                }
                if (IncludeStringLiterals)
                {
                    StringLiteral(sb, "BuildDateValue", Upper(now.ToString(DateFormat)));
                }
                if (IncludeWidths)
                {
                    sb.Append("BuildDateWidth          equ 0");
                    foreach (var chr in Upper(now.ToString(DateFormat)))
                        sb.Append(" + FW" + Name(chr.ToString()));
                    sb.AppendLine();
                }
                sb.AppendLine();
                sb.AppendLine();
                sb.AppendLine();
                if (IncludeMacros)
                {
                    StartMacro(sb, "BuildTime");
                    sb.Append("                        db \"");
                    sb.Append(Upper(now.ToString(TimeFormat)));
                    sb.AppendLine("\"");
                    EndMacro(sb);
                    sb.AppendLine();
                }
                if (IncludeStringLiterals)
                {
                    StringLiteral(sb, "BuildTimeValue", Upper(now.ToString(TimeFormat)));
                }
                if (IncludeWidths)
                {
                    sb.Append("BuildTimeWidth          equ 0");
                    foreach (var chr in Upper(now.ToString(TimeFormat)))
                        sb.Append(" + FW" + Name(chr.ToString()));
                    sb.AppendLine();
                }
                sb.AppendLine();
                sb.AppendLine();
                sb.AppendLine();
                if (IncludeMacros)
                {
                    StartMacro(sb, "BuildTimeSecs");
                    sb.Append("                        db \"");
                    sb.Append(Upper(now.ToString(TimeFormatSecs)));
                    sb.AppendLine("\"");
                    EndMacro(sb);
                    sb.AppendLine();
                }
                if (IncludeStringLiterals)
                {
                    StringLiteral(sb, "BuildTimeSecsValue", Upper(now.ToString(TimeFormatSecs)));
                }
                if (IncludeWidths)
                {
                    sb.Append("BuildTimeSecsWidth      equ 0");
                    foreach (var chr in Upper(now.ToString(TimeFormatSecs)))
                        sb.Append(" + FW" + Name(chr.ToString()));
                    sb.AppendLine();
                }

                // Write file
                System.IO.File.WriteAllText(File, sb.ToString());
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
                Console.Write(ex.StackTrace);
            }
        }

        private static string Upper(string Text)
        {
            if (UpperCase) return (Text ?? "").ToUpper();
            else return (Text ?? "");
        }

        private static string Name(string Value)
        {
            if (string.IsNullOrEmpty(Value)) return "Null";
            if (Value == " ") return "Space";
            else if (Value == ":") return "Colon";
            else if (Value == "-") return "Dash";
            else if (Value == ".") return "Period";
            else return Value;
        }

        private static void StartMacro(StringBuilder sb, string name)
        {
            if (Syntax == Syntax.Sjasmplus)
            {
                sb.AppendLine("    macro " + (name ?? "").Trim());
            }
            else
            {
                sb.Append((name ?? "").Trim().PadRight(24));
                sb.AppendLine("macro()");
            }
        }

        private static void EndMacro(StringBuilder sb)
        {
            if (Syntax == Syntax.Sjasmplus)
            {
                sb.AppendLine("    endm");
            }
            else
            {
                sb.AppendLine("    mend");
            }
        }

        private static void StringLiteral(StringBuilder sb, string Name, string Value)
        {
            if (Syntax == Syntax.Sjasmplus)
            {
                sb.Append("    define ");
                sb.Append((Name ?? "").Trim());
                sb.Append(" \"");
                sb.Append(Value ?? "");
                sb.AppendLine("\"");
            }
            else
            {
                sb.Append((Name ?? "").Trim().PadRight(24));
                sb.Append("equ \"");
                sb.Append(Value ?? "");
                sb.AppendLine("\"");
            }
        }
    }
}
