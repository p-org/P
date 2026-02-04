using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using PChecker;
using Plang.Options;
using UnitTests.Core;
using UnitTests.Runners;

namespace UnitTests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class PCheckerLogGeneratorTests
{
    [Test]
    public void TestLogGenerator()
    {
        var testLogGeneratorPath =  Path.Combine(Constants.ScratchParentDirectory, "TestLogGenerator");
        // Delete the directory if TestLogGenerator already exists from previous runs
        if (Directory.Exists(testLogGeneratorPath))
        {
            Directory.Delete(testLogGeneratorPath, recursive: true);
        }
        var tempDir = Directory.CreateDirectory(testLogGeneratorPath);
        var srcPath = new FileInfo(Path.Combine(Constants.SolutionDirectory, "Tst", "RegressionTests",
            "Feature1SMLevelDecls", "DynamicError", "bug2", "bug2.p"));
        var runner = new PCheckerRunner([srcPath]);
        var dllPath = Path.Combine(Constants.ScratchParentDirectory, "TestLogGenerator", "PChecker", "net8.0", "bug2.dll");
        var expectedPath = Path.Combine(Constants.SolutionDirectory, "Tst", "CorrectLogs", "bugs2");
            
        runner.DoCompile(tempDir);
        
        var configuration = new PCheckerOptions().Parse([dllPath, "-o", tempDir.ToString()]);
        Checker.Run(configuration);

        AssertLog(tempDir+"/BugFinding", expectedPath);
    }

    private void AssertLog(string generatedDir, string expectedDir)
    {
        if (!Directory.Exists(generatedDir) || !Directory.Exists(expectedDir))
        {
            Assert.Fail("One or both directories do not exist.");
        }

        var generatedFiles = Directory.GetFiles(generatedDir).Select(Path.GetFileName).ToHashSet();
        var expectedFiles = Directory.GetFiles(expectedDir).Select(Path.GetFileName).ToHashSet();

        foreach (var fileName in expectedFiles.Intersect(generatedFiles))
        {
            string generatedFilePath = Path.Combine(generatedDir, fileName);
            string expectedFilePath = Path.Combine(expectedDir, fileName);

            if (fileName == "bug2_0_0.trace.json")
            {
                // Perform "Is JSON Included" check for this specific file
                if (!IsJsonContentIncluded(generatedFilePath, expectedFilePath))
                {
                    Assert.Fail($"Test Failed \nContent of {expectedFilePath} is not fully included in {generatedFilePath}");
                }
            }
            else if (fileName == "bug2_0_0.txt")
            {
                // Perform "Is text included" check for this specific file
                var expectedContent = File.ReadAllText(expectedFilePath);
                var generatedContent = File.ReadAllText(generatedFilePath);

                if (!generatedContent.Contains(expectedContent))
                {
                    Assert.Fail($"Test Failed \nExpected content from {expectedFilePath} not found in {generatedFilePath}");
                }
            }
            else
            {
                // Perform exact match for other files
                if (!File.ReadAllBytes(generatedFilePath).SequenceEqual(File.ReadAllBytes(expectedFilePath)))
                {
                    Assert.Fail($"Test Failed \nFiles differ: {fileName}\nGenerated File: {generatedFilePath}\nExpected File: {expectedFilePath}");
                }
            }
        }

        // Check for missing files in generatedDir
        foreach (var file in expectedFiles.Except(generatedFiles))
        {
            Assert.Fail($"Test Failed \nMissing expected file in {generatedDir}: {file}");
        }
        Console.WriteLine("Test Succeeded");
    }

    private static bool IsJsonContentIncluded(string generatedFilePath, string expectedFilePath)
    {
        var generatedJson = JToken.Parse(File.ReadAllText(generatedFilePath));
        var expectedJson = JToken.Parse(File.ReadAllText(expectedFilePath));

        return IsSubset(expectedJson, generatedJson);
    }

    private static bool IsSubset(JToken subset, JToken superset)
    {
        if (JToken.DeepEquals(subset, superset))
        {
            return true;
        }

        if (subset.Type == JTokenType.Object && superset.Type == JTokenType.Object)
        {
            var subsetObj = (JObject)subset;
            var supersetObj = (JObject)superset;

            foreach (var property in subsetObj.Properties())
            {
                if (!supersetObj.TryGetValue(property.Name, out var supersetValue) || !IsSubset(property.Value, supersetValue))
                {
                    return false;
                }
            }

            return true;
        }

        if (subset.Type == JTokenType.Array && superset.Type == JTokenType.Array)
        {
            var subsetArray = (JArray)subset;
            var supersetArray = (JArray)superset;

            foreach (var subsetItem in subsetArray)
            {
                if (!supersetArray.Any(supersetItem => IsSubset(subsetItem, supersetItem)))
                {
                    return false;
                }
            }

            return true;
        }

        return false;
    }
}
