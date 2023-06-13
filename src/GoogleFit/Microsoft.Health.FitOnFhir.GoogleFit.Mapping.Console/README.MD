# MedTech service mapping generator

## Introduction

[Azure MedTech service](https://learn.microsoft.com/en-us/azure/healthcare-apis/iot/iot-connector-overview) is used to transform Google Fit data types to FHIR Observations. MedTech service requires some configuration before the transformation can take place. The configuration is done through 2 JSON "mappings", Device mapping, and FHIR Destination mapping. The Microsoft.Health.FitOnFhir.GoogleFit.Mapping.Console project provides a way to generate the "mappings" using the response from the Google Fitness Data Sources API [https://www.googleapis.com/fitness/v1/users/me/dataSources](#).

It's important to note that the Google Fitness Data Sources API does not provide an exhaustive list of possible data sources, but instead, will return a list of data sources that a particular user has collected, and opted to share with the caller. For example, if User A collected heart rate data only, a call made to the Data Sources API will only return heart rate related data sources. Similarly, if User B collected heart rate, blood pressure and blood glucose data and only opted to share blood pressure data, a call Data Sources API will only return blood pressure related data sources.

## Usage

1. Once a request to the Google Fitness Data Sources API has been made, save the response to a JSON file.
1. Invoke the console application* with, at least, the 2 required arguments which are the path to the Data Sources API response (--dataSourcePath or -d) and an output path where the "mappings" will be saved (--outputPath or -o).

*The for ease of use, a compiled console application ([GoogleFitMappingGenerator.exe](GoogleFitMappingGenerator.exe)) can be found in this (Health.FitOnFhir.GoogleFit.Mapping.Console) project folder.

### Examples

```console
GoogleFitMappingGenerator.exe generateMappings --dataSourcePath Samples/DataSources.json --outputPath Samples/
```

or

```console
GoogleFitMappingGenerator.exe generateMappings -d Samples/DataSources.json -o Samples/
```

It is also possible to generate "mappings" for specific data sources in the Google Fitness Data Sources API response by providing a one or more (comma separated) Regex strings with the --includedDataStreams (or -i) argument. Regex strings are matched against the DataStreamId string of response, and only Data Sources where the Regex matches will be mapped.

In the example below, only Data Sources that have DataStreamIds containing "blood_pressure" will be mapped.

```console
GoogleFitMappingGenerator.exe generateMappings --dataSourcePath Samples/DataSources.json --outputPath Samples/ --includedDataStreams blood_pressure
```

or

```console
GoogleFitMappingGenerator.exe generateMappings -d Samples/DataSources.json -o Samples/ -i blood_pressure
```

## Configuring Azure MedTech Service

Once the "mappings" have been generated, the contents of the devicecontent.json file can be copied and pasted to the MedTech service Device Mapping section and the fhirmapping.json can be copied and pasted to the Destination section. The MedTech service will now transform the incoming Google Fit data to FHIR Observations.

**NOTE:** Any data streams that are not "mapped" will be ignored by the MedTech service.
