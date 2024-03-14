using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Xml;
using System.Reflection.Metadata;

#region הפקודה bundle
var bundleCommand = new Command("bundle", "Bundle code files to a single file");
#region הגדרת האופציות
// אופציה לפלט
var bundleOption = new Option<FileInfo>("--output", "File path and name (required)") { IsRequired = true};
// אופציה להוספת הערות
var noteOption = new Option<bool>("--note", "Include source files name");
noteOption.SetDefaultValue(false);
// אופציה לבחירת שפה
var langOption = new Option<string>("--language", "Select programming languages to include (required)") { IsRequired=true };
string[] filesToRead = { "" };
//אופציה למיון עפ"י שם או סוג קובץ
var sortOption = new Option<string>("--sort", "Sort by name or by type)");
sortOption.SetDefaultValue("name");
//אופציה למחיקת שורות ריקות
var relOption = new Option<bool>("--remove-empty-lines", "remove empty lines");
relOption.SetDefaultValue(false);
//אופציה להוסיף את שם המשתמש ככותרת לקובץ
var outherOption = new Option<string>("--outher", "your name to tytle");
#endregion

#region הוספת קיצורי alias
bundleOption.AddAlias("-op");
langOption.AddAlias("-l");
noteOption.AddAlias("-n");
sortOption.AddAlias("-s");
relOption.AddAlias("-r-e-l");
outherOption.AddAlias("-ot");
#endregion

#region הוספת האופציות לאופציה הראשית
bundleCommand.AddOption(bundleOption);
bundleCommand.AddOption(noteOption);
bundleCommand.AddOption(langOption);
bundleCommand.AddOption(sortOption);
bundleCommand.AddOption(relOption);
bundleCommand.AddOption(outherOption);
#endregion
// טיפול בפקודה
bundleCommand.SetHandler((output, selLang, note, sort, rel, outher) =>
{
    try
    {
        string[] files = { };
        try
        {
            // קבלת רשימת קבצים, סינון ומיון
            files = GetSortedFiles(selLang, sort);
        }
        catch (ArgumentException ex) { Console.WriteLine(ex); }
        catch { Console.WriteLine("Opps, the language is invalid :("); }
        // יצירת קובץ חדש
        using (StreamWriter newFile = new StreamWriter(output.FullName))
        {
            // נתיב התיקייה
            string folderPath = Path.GetDirectoryName(output.FullName);

            //הכנסת כותרת שם המשתמש אם הכניס שם
            if (outher != null) { }
                newFile.WriteLine($"//========={outher}============");

            // עיבוד קבצים ממוינים
            foreach (string file in files)
            {
                string fileName, fileExtension = Path.GetExtension(file);
                // בדיקה האם שם הקובץ הנוכחי זהה לקובץ היעד
                if (Path.GetFileName(file) == Path.GetFileName(output.FullName))
                    continue; // דילוג על קובץ היעד 
                // הוספת שם הקובץ המלא ככותרת או שורת סימון מעבר לקובץ אחר לפי בחירת המשתמש
                if (note)
                    newFile.WriteLine("//==== " + Path.GetFullPath(file) + " ====");
                else
                    newFile.WriteLine("//===========================================");
                //ירידת שורה ופתיחת region בכל מקרה
                newFile.Write(Environment.NewLine + Region(file));

                // קריאה והעתקת תוכן הקובץ
                using (StreamReader fileToCopy = new StreamReader(file))
                {
                    string content = fileToCopy.ReadToEnd();

                    if (rel)    //מחיקת שורות ריקות לפי בקשת המשתמש
                        content = RemoveEmptyLines(content);
                    //endregion לפי שפה
                    content += Environment.NewLine;
                    content += Endregion(file);
                    content += Environment.NewLine;
                    newFile.Write(content);
                }
            }
        }
        Console.WriteLine("The file was created successfully");
    }
    catch (Exception ex){ Console.WriteLine($"Error: {ex.Message}"); }
}, bundleOption, langOption, noteOption, sortOption, relOption, outherOption);

#endregion

#region הפקודה response-file
var RspCommand = new Command("response-file", "create response file to bundle command");
//טיפול בפקודה
RspCommand.SetHandler(() =>
{
    Console.WriteLine("Enter your response file name");
    string fileName = Console.ReadLine();
    string output, languages, sort, outher = "";
    char note, removeEmptyLines, ifOuther;
    StreamWriter file = new StreamWriter($"{fileName}.rsp");
    //קליטת הנתונים
    Console.WriteLine("Enter file name");
    output = Console.ReadLine();
    while (output == "")
    {
        Console.WriteLine("This field is required!");
        output = Console.ReadLine();
    }
    Console.WriteLine("Which languages you want to include? (to choose to include all enter 'all')");
    languages = Console.ReadLine();
    while (languages == "")
    {
        Console.WriteLine("This field is reqired!");
        languages = Console.ReadLine();
    }
    Console.WriteLine("Choose type sort (name / type)");
    sort = Console.ReadLine();
    Console.WriteLine("Do you want to write the source file? (y/n)");
    note = char.Parse(Console.ReadLine());
    Console.WriteLine("Do you want to write outher name? (y/n)");
    ifOuther = char.Parse(Console.ReadLine());
    if (ifOuther == 'y' || ifOuther == 'Y')
    {
        Console.WriteLine("Enter outher name");
        outher = Console.ReadLine();
    }
    Console.WriteLine("Do you want to remove empty lines? (y/n)");
    removeEmptyLines = char.Parse(Console.ReadLine());

    //כתיבת הנתונים לקובץ
    file.Write("bundle");
    file.Write(" --output " + output);
    file.Write($" --language {languages} ");
    if (note.Equals("y") || note.Equals("Y"))
        file.Write(" --note true ");
    if (sort != "")
        file.Write(" --sort " + sort);
    if (removeEmptyLines.Equals("y") || removeEmptyLines.Equals("Y"))
        file.Write(" --remove-empty-lines true ");
    if (outher != "")
        file.Write(" --outher " + outher);
    file.Close();
    Console.WriteLine($"The response file {fileName}.rsp created succesfully! :)");
    Console.WriteLine("you can use it, good luck!!!!");
});
#endregion

// הפקודה הראשית
var rootCommand = new RootCommand("Root command for file bundler CLI");

rootCommand.AddCommand(bundleCommand);
rootCommand.AddCommand(RspCommand);

rootCommand.InvokeAsync(args);

#region פונקציות
static string[] GetFilesToRead(string selLang, string[] files)
{
    // מערך שפות
    string[] langs = { "c", "c++", "c++", "c#", "java", "html", "javascript", "css", "python" };
    // מערך סיומות מתאים
    string[] endsLangs = { ".c", ".cpp", ".h", ".cs", ".java", ".html", ".js", ".css", ".py" };
    // הפרדת שפות
    string[] selectedLanguages = selLang.Split(' ');
    // בדיקת תקינות
    foreach (string lang in selectedLanguages)
    {
        if (!Array.Exists(langs, l => l == lang) && lang != "all")
            throw new ArgumentException($"invalid language: {lang}");
    }
    // סינון קבצים
    List<string> filteredFiles = new List<string>();
    if (selectedLanguages.Contains("all"))
    {
        // סינון לפי כל הסיומות
        filteredFiles.AddRange(files.Where(file => Array.Exists(endsLangs, extension => extension == Path.GetExtension(file))));
    }
    else
    {
        // סינון לפי סיומות ספציפיות
        foreach (string lang in selectedLanguages)
        {
            int index = Array.IndexOf(langs, lang);
            if (index >= 0)
            {
                string extension = endsLangs[index];
                filteredFiles.AddRange(files.Where(file => Path.GetExtension(file) == extension));
            }
        }
    }
    return filteredFiles.ToArray();
}
string[] GetSortedFiles(string selLang, string sort)
{
    // קבלת רשימת קבצים
    string[] files = Directory.GetFiles(Directory.GetCurrentDirectory());
    // סינון לפי שפה
    files = GetFilesToRead(selLang, files);
    // מיון
    if (sort == "name")
        Array.Sort(files); // מיון לפי שם קובץ
    else if (sort == "type")
    {
        // מיון לפי סיומת קובץ
        files = files.OrderBy(file => Path.GetExtension(file)).ToArray();
    }
    else
    {
        // הודעת שגיאה
        Console.WriteLine($"sort option invalid: {sort}");
        return null;
    }
    return files;
}
static string RemoveEmptyLines(string text)
{
    // חלוקת הטקסט למערך שורות
    string[] lines = text.Split('\n');
    // מחרוזת חדשה להכלת השורות המלאות
    string nonEmptyLines = "";
    // מעבר על השורות והכנסת השורות המלאות למחרוזת
    for (int i = 0; i < lines.Length; i++)
    {
        if (!string.IsNullOrWhiteSpace(lines[i]))
        {
            nonEmptyLines += lines[i] + "\n";
        }
    }
    return nonEmptyLines;
}

/// פונקציה המקבלת שם קובץ ומחזירה מחרוזת Region מתאימה.
static string Region(string file)
{
    string s = Path.GetExtension(file);
    switch (s)
    {
        case ".cs":
            return "#region --";
        case ".c":
        case ".cpp":
        case ".h":
            return "#pragma region";
        case ".java":
            return "// ==== " + Path.GetFileName(file) + " =====";
        case ".html":
        case ".js":
        case ".css":
        case ".py":
            return "";
        default:
            return "";
    }
}
static string Endregion(string file)
{
    string s = Path.GetExtension(file);
    switch (s)
    {
        case ".cs":
            return "#endregion";
        case ".c":
        case ".cpp":
        case ".h":
            return "#pragma endregion";
        case ".java":
            return ""; // Java לא דורשת סיום אזור מפורש
        case ".html":
        case ".js":
        case ".css":
        case ".py":
            return "";
        default:
            return "";
    }
}
#endregion
