using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;

namespace BEngine.Testing;

public static class TestReportWriter
{
    public static void WriteJson(string path, TestRunResult result) => Write(path, JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
    public static void WriteJUnit(string path, TestRunResult result)
    {
        var suite = new XElement("testsuite", new XAttribute("name", "BEngine"), new XAttribute("tests", result.Tests.Count), new XAttribute("failures", result.Failed), new XAttribute("skipped", Scan595(result.Tests)), new XAttribute("time", ((result.FinishedAt - result.StartedAt).TotalSeconds).ToString("R", CultureInfo.InvariantCulture)), new XAttribute("timestamp", result.StartedAt.UtcDateTime.ToString("O")));
        [global::BEngine.SkipLoopGuard]
        int Scan595(global::System.Collections.Generic.IEnumerable<global::BEngine.Testing.TestResult> scanSource595)
        {
            ArgumentNullException.ThrowIfNull(scanSource595, "source");
            var count = 0;
            global::System.ReadOnlySpan<global::BEngine.Testing.TestResult> scanValues595;
            if (scanSource595.GetType() == typeof(global::BEngine.Testing.TestResult[]))
                scanValues595 = (global::BEngine.Testing.TestResult[])scanSource595;
            else if (scanSource595.GetType() == typeof(global::System.Collections.Generic.List<global::BEngine.Testing.TestResult>))
                scanValues595 = global::System.Runtime.InteropServices.CollectionsMarshal.AsSpan((global::System.Collections.Generic.List<global::BEngine.Testing.TestResult>)scanSource595);
            else
            {
                foreach (var scanItem595 in scanSource595)
                {
                    if (Match595(scanItem595))
                        count = checked(count + 1);
                }
        
                return count;
            }
        
            foreach (var scanItem595 in scanValues595)
            {
                if (Match595(scanItem595))
                    count = checked(count + 1);
            }
        
            return count;
            bool Match595(global::BEngine.Testing.TestResult test) => test.Status is TestStatus.Skipped or TestStatus.Canceled;
        }
        foreach (var test in result.Tests)
        {
            var split = test.DisplayName.LastIndexOf('.');
            var item = new XElement("testcase", new XAttribute("name", split < 0 ? test.DisplayName : test.DisplayName[(split + 1)..]),
                new XAttribute("classname", split < 0 ? "BEngine" : test.DisplayName[..split]),
                new XAttribute("time", (test.DurationMilliseconds / 1000).ToString("R", CultureInfo.InvariantCulture)),
                new XAttribute("file", test.SourceFile), new XAttribute("line", test.SourceLine));
            if (test.Status is TestStatus.Failed or TestStatus.TimedOut)
                item.Add(new XElement("failure", new XAttribute("message", test.Message), new XAttribute("type", test.Status), test.StackTrace));
            else if (test.Status is TestStatus.Skipped or TestStatus.Canceled)
                item.Add(new XElement("skipped", new XAttribute("message", test.Message)));
            suite.Add(item);
        }
        Write(path, new XDocument(new XElement("testsuites", suite)).ToString());
    }
    private static void Write(string path, string text)
    {
        var fullPath = Path.GetFullPath(path); Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        var temporary = fullPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try { File.WriteAllText(temporary, text); File.Move(temporary, fullPath, overwrite: true); }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
