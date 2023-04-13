// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.CommandLine;
using System.CommandLine.Invocation;
using EnsureThat;
using Google.Apis.Fitness.v1.Data;
using Microsoft.Health.Fhir.Ingest.Template;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.FitOnFhir.GoogleFit.Mapping.Console
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Messages will not be localized.")]
    public static class Program
    {
        public static void Main(string[] args)
        {
            ParseArgumentsAndExecute(args);
        }

        private static void ParseArgumentsAndExecute(string[] args)
        {
            var rootCommand = new RootCommand();

            // Create a mapping generation command with options.
            var generateMappingsCommand = new Command("generateMappings", "Generate Device Content and FHIR Mapping templates for use with Med Tech Service")
            {
                new Option<string>(
                    new string[] { "--dataSourcePath", "-d" },
                    getDefaultValue: () => string.Empty,
                    description: "The path to a Google Fitness DataSources API response json used to generate the mappings."),
                new Option<string>(
                    new string[] { "--outputPath", "-o" },
                    getDefaultValue: () => null,
                    description: "The path where the mappings should be written."),
                new Option<string>(
                    new string[] { "--includedDataStreams", "-i" },
                    getDefaultValue: () => null,
                    description: "An optional comma separated list of regex strings used to select specific data streams from the DataSources API response. If not provided, all DataSources will be used."),
            };
            generateMappingsCommand.Handler = CommandHandler.Create<string, string, string>(GenerateMappings);

            rootCommand.AddCommand(generateMappingsCommand);
            rootCommand.Invoke(args);
        }

        private static async Task GenerateMappings(string dataSourcePath, string outputPath, string includedDataStreams)
        {
            if (string.IsNullOrWhiteSpace(dataSourcePath))
            {
                // dataSourcePath is a required parameter.
                throw new ArgumentNullException(nameof(dataSourcePath));
            }

            if (string.IsNullOrWhiteSpace(outputPath))
            {
                // outputPath is a required parameter.
                throw new ArgumentNullException(nameof(outputPath));
            }

            // Ensure the paths provided are valid.
            string serializedDataSource = GetData(dataSourcePath);
            string fullOutputPath = GetFullPath(outputPath, false);
            string[] includedDataStreamExpressions = GetIncludedDataStreamExpressions(includedDataStreams);

            JArray dataSources;

            try
            {
                JObject json = JObject.Parse(serializedDataSource);
                dataSources = json["dataSource"] as JArray;
            }
            catch (JsonReaderException)
            {
                dataSources = JArray.Parse(serializedDataSource);
            }

            // Generate devicecontent.
            var deviceContentGenerator = new DeviceContentTemplateCollectionGenerator(includedDataStreamExpressions);
            JObject deviceContent = await deviceContentGenerator.GenerateTemplateCollection(dataSources.ToObject<IEnumerable<DataSource>>(), CancellationToken.None);
            IList<CalculatedFunctionContentTemplate> deviceTemplates = GetTemplates<CalculatedFunctionContentTemplate>(deviceContent).ToList();

            // Check if any Device Content templates were generated.
            if (!deviceTemplates.Any())
            {
                System.Console.WriteLine($"\nNo Device Content templates could be generated with the DataSource at path: '{dataSourcePath}'\n");
                return;
            }

            string deviceContentPath = Path.Combine(fullOutputPath, "devicecontent.json");
            WriteData(deviceContentPath, deviceContent.ToString());
            System.Console.WriteLine($"\n{deviceTemplates.Count} Device Content templates generated and saved to: '{deviceContentPath}'\n");

            // Generate fhirmapping.
            var fhirMappingGenerator = new FhirMappingTemplateCollectionGenerator();
            JObject fhirMapping = await fhirMappingGenerator.GenerateTemplateCollection(deviceTemplates, CancellationToken.None);
            IList<CodeValueFhirTemplate> fhirTemplates = GetTemplates<CodeValueFhirTemplate>(fhirMapping).ToList();

            // Check if any FHIR Mapping templates were generated.
            if (!fhirTemplates.Any())
            {
                System.Console.WriteLine($"\nNo FHIR Mapping templates could be generated with the DataSource at path: '{dataSourcePath}'");
                System.Console.WriteLine($"Check the devicecontent.json to ensure there are no errors.");
                return;
            }

            string fhirMappingPath = Path.Combine(fullOutputPath, "fhirmapping.json");
            WriteData(fhirMappingPath, fhirMapping.ToString());
            System.Console.WriteLine($"\n{fhirTemplates.Count} FHIR Mapping templates generated and saved to: '{fhirMappingPath}'\n");
        }

        private static string[] GetIncludedDataStreamExpressions(string includedDataStreams)
        {
            string[] includedDataStreamExpressions = null;

            if (!string.IsNullOrWhiteSpace(includedDataStreams))
            {
                includedDataStreams = includedDataStreams.Replace(" ", string.Empty, StringComparison.OrdinalIgnoreCase);
                includedDataStreamExpressions = includedDataStreams.Split(',');
            }

            return includedDataStreamExpressions;
        }

        private static string GetFullPath(string path, bool isFile)
        {
            path = path.Replace(" ", string.Empty, StringComparison.Ordinal);

            string finalPath = Path.IsPathRooted(path) ? path : Path.Combine(Directory.GetCurrentDirectory(), path);

            if (isFile)
            {
                if (!File.Exists(finalPath))
                {
                    throw new FileNotFoundException($"--dataSourcePath {finalPath} not found.");
                }
            }
            else
            {
                if (!Directory.Exists(path))
                {
                    throw new DirectoryNotFoundException($"--outputPath {path} not found.");
                }
            }

            return finalPath;
        }

        private static IEnumerable<T> GetTemplates<T>(JObject containerJson)
            where T : Template
        {
            var templates = new List<T>();

            TemplateContainer templateContainer = containerJson.ToObject<TemplateContainer>();

            if (templateContainer.Template is JArray jsonTemplates)
            {
                foreach (var jsonTemplate in jsonTemplates)
                {
                    templates.Add(jsonTemplate["template"].ToObject<T>());
                }
            }

            return templates;
        }

        private static string GetData(string filePath)
        {
            EnsureArg.IsNotNullOrWhiteSpace(filePath, nameof(filePath));

            string path = GetFullPath(filePath, true);

            return File.ReadAllText(path);
        }

        private static void WriteData(string directoryPath, string content, bool overwriteExisting = true)
        {
            EnsureArg.IsNotNullOrWhiteSpace(directoryPath, nameof(directoryPath));
            EnsureArg.IsNotNullOrWhiteSpace(content, nameof(content));

            if (!File.Exists(directoryPath) || overwriteExisting)
            {
                File.WriteAllText(directoryPath, content);
            }
        }
    }
}
