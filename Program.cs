// This program compares two files or two directories to check whether the
// start of the files match, the end of the files match, and selected sections inside the files also match
//
// Useful for comparing large files without reading the entire file
//
// -------------------------------------------------------------------------------
// Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA)
// Program started in April 2013
//
// E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov
// Website: https://omics.pnl.gov/ or https://panomics.pnnl.gov/
// -------------------------------------------------------------------------------
//
// Licensed under the Apache License, Version 2.0; you may not use this file except
// in compliance with the License.  You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

using System;
using System.IO;
using PRISM;

namespace FileComparisonSampler
{
    /// <summary>
    /// Entry class for the .exe
    /// </summary>
    internal static class Program
    {
        // Ignore Spelling: wildcards

        /// <summary>
        /// Program date
        /// </summary>
        public const string PROGRAM_DATE = "June 14, 2021";

        private static SampledFileComparer mProcessingClass;

        private static DateTime mLastProgressReportTime;

        private static int mLastProgressReportValue;

        /// <summary>
        /// Program entry point
        /// </summary>
        /// <param name="args"></param>
        /// <returns>0 if no error, error code if an error</returns>
        private static int Main(string[] args)
        {
            var programName = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name;
            var exeName = Path.GetFileName(PRISM.FileProcessor.ProcessFilesOrDirectoriesBase.GetAppPath());
            var cmdLineParser = new CommandLineParser<CommandLineOptions>(programName, GetAppVersion())
            {
                ProgramInfo = "This program compares two or more files (typically in separate directories) to check whether the " +
                              "start of the files match, the end of the files match, and selected sections inside the files also match. " +
                              "Useful for comparing large files without reading the entire file. " +
                              "Alternatively, you can provide two directory paths and the program will compare all of the files " +
                              "in the first directory to the identically named files in the second directory.",

                ContactInfo = "Program written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA) in 2013" +
                              Environment.NewLine + Environment.NewLine +
                              "E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov" + Environment.NewLine +
                              "Website: https://omics.pnl.gov/ or https://panomics.pnnl.gov/"
            };

            cmdLineParser.UsageExamples.Add(
                "Program syntax 1: compare two files; in this case the filenames cannot have wildcards" +
                Environment.NewLine +
                " " + exeName + " FilePath1 FilePath2" + Environment.NewLine +
                " [/N:NumberOfSamples] [/Bytes:SampleSizeBytes]" + Environment.NewLine +
                " [/KB:SizeKB] [/MB:SizeMB] [/GB:SizeGB]" + Environment.NewLine +
                " [/L[:LogFilePath]] [/LogDirectory:LogDirectoryPath]");

            cmdLineParser.UsageExamples.Add("Program syntax 2: compare two directories (including all subdirectories)" + Environment.NewLine +
                " " + exeName + " DirectoryPath1 DirectoryPath2" + Environment.NewLine +
                " [/N:NumberOfSamples] [/Bytes:SampleSizeBytes]" + Environment.NewLine +
                " [/L] [/LogDirectory]");

            cmdLineParser.UsageExamples.Add(ConsoleMsgUtils.WrapParagraph(
                "Program syntax 3: compare a set of files in one directory to identically named files in a separate directory. " +
                "Use wildcards in FileMatchSpec to specify the files to examine") + Environment.NewLine +
                " " + exeName + " FileMatchSpec DirectoryPathToExamine" + Environment.NewLine +
                " [/N:NumberOfSamples] [/Bytes:SampleSizeBytes]" + Environment.NewLine +
                " [/L] [/LogDirectory]");

            cmdLineParser.UsageExamples.Add(ConsoleMsgUtils.WrapParagraph(
                "Program syntax 4: compare a DMS dataset's files between the storage server and the archive. " +
                "The first argument must be DMS; the second argument is the Dataset Name.") + Environment.NewLine +
                " " + exeName + " DMS DatasetNameToCheck" + Environment.NewLine +
                " [/N:NumberOfSamples] [/Bytes:SampleSizeBytes]" + Environment.NewLine +
                " [/L] [/LogDirectory]");

            var results = cmdLineParser.ParseArgs(args);
            var options = results.ParsedResults;

            if (!results.Success || !options.Validate())
            {
                //ShowErrorMessage(
                //    "You must specify two files or two directories or a file match spec and a directory path or the word DMS followed by a dataset name");
                ProgRunner.SleepMilliseconds(1000);
                return -1;
            }

            try
            {
                int returnCode;

                mProcessingClass = new SampledFileComparer
                {
                    NumberOfSamples = options.NumberOfSamples,
                    SampleSizeBytes = options.SampleSizeBytes,
                    IgnoreErrorsWhenUsingWildcardMatching = true,
                    LogMessagesToFile = options.LogMessagesToFile,
                    LogFilePath = options.LogFilePath,
                    LogDirectoryPath = options.LogDirectoryPath
                };

                mProcessingClass.ProgressUpdate += ProcessingClass_ProgressChanged;
                mProcessingClass.ProgressReset += ProcessingClass_ProgressReset;

                if (string.IsNullOrWhiteSpace(options.InputFileOrDirectoryPath))
                {
                    ShowErrorMessage("Base file or directory to compare is empty");
                    return -1;
                }

                bool success;

                if (string.Equals(options.InputFileOrDirectoryPath, "DMS", StringComparison.OrdinalIgnoreCase) &&
                    options.ComparisonFileOrDirectoryPath.IndexOf("\\", StringComparison.Ordinal) < 0)
                {
                    // InputFile is "DMS"

                    // Treat ComparisonFile as a dataset name and compare files on the storage server to files in the archive
                    // This feature does not yet support files in MyEMSL
                    success = mProcessingClass.ProcessDMSDataset(options.ComparisonFileOrDirectoryPath);
                }
                else
                {
                    // Comparing two files or two directories
                    success = mProcessingClass.ProcessFilesWildcard(options.InputFileOrDirectoryPath, options.ComparisonFileOrDirectoryPath);
                }

                if (success)
                {
                    returnCode = 0;
                }
                else
                {
                    returnCode = (int)mProcessingClass.ErrorCode;
                    if (returnCode != 0)
                    {
                        ShowErrorMessage("Error while processing: " + mProcessingClass.GetErrorMessage());
                    }
                }

                return returnCode;
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error occurred in modMain->Main: " + ex.Message);
                return -1;
            }
        }

        private static void DisplayProgressPercent(int percentComplete, bool addCarriageReturn)
        {
            if (addCarriageReturn)
            {
                Console.WriteLine();
            }

            if (percentComplete > 100)
            {
                percentComplete = 100;
            }

            Console.Write("Processing: " + percentComplete + "% ");
            if (addCarriageReturn)
            {
                Console.WriteLine();
            }
        }

        private static string GetAppVersion()
        {
            return PRISM.FileProcessor.ProcessFilesOrDirectoriesBase.GetAppVersion(PROGRAM_DATE);
        }

        private static void ShowErrorMessage(string message)
        {
            ConsoleMsgUtils.ShowError(message);
        }

        private static void ProcessingClass_ProgressChanged(string taskDescription, float percentComplete)
        {
            const int PERCENT_REPORT_INTERVAL = 25;
            const int PROGRESS_DOT_INTERVAL_MSEC = 250;

            if (percentComplete >= mLastProgressReportValue)
            {
                if (mLastProgressReportValue > 0)
                {
                    Console.WriteLine();
                }

                DisplayProgressPercent(mLastProgressReportValue, false);
                mLastProgressReportValue += PERCENT_REPORT_INTERVAL;
                mLastProgressReportTime = DateTime.UtcNow;
            }
            else if (DateTime.UtcNow.Subtract(mLastProgressReportTime).TotalMilliseconds > PROGRESS_DOT_INTERVAL_MSEC)
            {
                mLastProgressReportTime = DateTime.UtcNow;
                Console.Write(".");
            }
        }

        private static void ProcessingClass_ProgressReset()
        {
            mLastProgressReportTime = DateTime.UtcNow;
            mLastProgressReportValue = 0;
        }
    }
}
