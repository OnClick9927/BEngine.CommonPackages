using System.Buffers;
using System.Text.Json;

namespace BEngine.Testing;

/// <summary>Writes the editor-compatible report without reflection metadata in trimmed Players.</summary>
internal static class PlayerTestReportJson
{
    internal static byte[] Serialize(TestRunResult result)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            writer.WriteString("StartedAt", result.StartedAt);
            writer.WriteString("FinishedAt", result.FinishedAt);
            writer.WriteStartArray("Tests");
            foreach (var test in result.Tests)
            {
                writer.WriteStartObject();
                writer.WriteString("TestId", test.TestId);
                writer.WriteString("DisplayName", test.DisplayName);
                writer.WriteNumber("Mode", (int)test.Mode);
                writer.WriteNumber("Status", (int)test.Status);
                writer.WriteNumber("DurationMilliseconds", test.DurationMilliseconds);
                writer.WriteString("Message", test.Message);
                writer.WriteString("StackTrace", test.StackTrace);
                writer.WriteString("SourceFile", test.SourceFile);
                writer.WriteNumber("SourceLine", test.SourceLine);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WritePropertyName("Coverage");
            if (result.Coverage is { } coverage) WriteCoverage(writer, coverage);
            else writer.WriteNullValue();
            writer.WriteNumber("Passed", result.Passed);
            writer.WriteNumber("Failed", result.Failed);
            writer.WriteNumber("Skipped", result.Skipped);
            writer.WriteBoolean("Succeeded", result.Succeeded);
            writer.WriteEndObject();
        }
        return buffer.WrittenSpan.ToArray();
    }

    private static void WriteCoverage(Utf8JsonWriter writer, CoverageSnapshot coverage)
    {
        writer.WriteStartObject();
        writer.WriteString("CapturedAt", coverage.CapturedAt);
        writer.WriteStartArray("Files");
        foreach (var file in coverage.Files)
        {
            writer.WriteStartObject();
            writer.WriteString("Path", file.Path);
            writer.WriteStartArray("Lines");
            foreach (var line in file.Lines)
            {
                writer.WriteStartObject();
                writer.WriteNumber("Line", line.Line);
                writer.WriteNumber("Hits", line.Hits);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteStartArray("Branches");
            foreach (var branch in file.Branches)
            {
                writer.WriteStartObject();
                writer.WriteNumber("Line", branch.Line);
                writer.WriteNumber("Block", branch.Block);
                writer.WriteNumber("Branch", branch.Branch);
                writer.WriteNumber("Hits", branch.Hits);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteNumber("Total", file.Total);
            writer.WriteNumber("Covered", file.Covered);
            writer.WriteNumber("TotalBranches", file.TotalBranches);
            writer.WriteNumber("CoveredBranches", file.CoveredBranches);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
        writer.WriteNumber("TotalLines", coverage.TotalLines);
        writer.WriteNumber("CoveredLines", coverage.CoveredLines);
        writer.WriteNumber("LineRate", coverage.LineRate);
        writer.WriteNumber("TotalBranches", coverage.TotalBranches);
        writer.WriteNumber("CoveredBranches", coverage.CoveredBranches);
        writer.WriteNumber("BranchRate", coverage.BranchRate);
        writer.WriteEndObject();
    }
}
