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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PRISM;

namespace FileComparisonSampler
{
    /// <summary>
    /// Entry class for the .exe
    /// </summary>
    class Program
    {

        /// <summary>
        /// Program date
        /// </summary>
        public const string PROGRAM_DATE = "April 13, 2018";

        private static string mInputFileOrDirectoryPath;

        private static string mComparisonFileOrDirectoryPath;

        // Unused:
        // private static string mParameterFilePath;

        private static bool mLogMessagesToFile;

        private static string mLogFilePath = string.Empty;

        private static string mLogFolderPath = string.Empty;

        private static int mNumberOfSamples;

        private static long mSampleSizeBytes;

        private static clsSampledFileComparer mProcessingClass;

        private static DateTime mLastProgressReportTime;

        private static int mLastProgressReportValue;

        /// <summary>
        /// Program entry point
        /// </summary>
        /// <param name="args"></param>
        /// <returns>0 if no error, error code if an error</returns>
        static int Main(string[] args)
        {
            // Initialize the options
            mInputFileOrDirectoryPath = string.Empty;
            mComparisonFileOrDirectoryPath = string.Empty;
            // Unused: mParameterFilePath = string.Empty;
            mNumberOfSamples = clsSampledFileComparer.DEFAULT_NUMBER_OF_SAMPLES;
            mSampleSizeBytes = clsSampledFileComparer.DEFAULT_SAMPLE_SIZE_KB * 1024;
            mLogMessagesToFile = false;
            mLogFilePath = string.Empty;
            mLogFolderPath = string.Empty;

            try
            {
                var blnProceed = false;
                int intReturnCode;

                var commandLineParser = new clsParseCommandLine();

                if (!commandLineParser.ParseCommandLine())
                {
                    ShowProgramHelp();
                    return -1;
                }

                if (SetOptionsUsingCommandLineParameters(commandLineParser))
                {
                    blnProceed = true;
                }

                if (!blnProceed ||
                    commandLineParser.NeedToShowHelp ||
                    commandLineParser.ParameterCount + commandLineParser.NonSwitchParameterCount == 0 ||
                    mInputFileOrDirectoryPath.Length == 0)
                {
                    ShowProgramHelp();
                    return -1;
                }

                mProcessingClass = new clsSampledFileComparer
                {
                    NumberOfSamples = mNumberOfSamples,
                    SampleSizeBytes = mSampleSizeBytes,
                    IgnoreErrorsWhenUsingWildcardMatching = true,
                    LogMessagesToFile = mLogMessagesToFile,
                    LogFilePath = mLogFilePath,
                    LogFolderPath = mLogFolderPath
                };

                mProcessingClass.ProgressUpdate += mProcessingClass_ProgressChanged;
                mProcessingClass.ProgressReset += mProcessingClass_ProgressReset;

                if (string.IsNullOrWhiteSpace(mInputFileOrDirectoryPath))
                {
                    ShowErrorMessage("Base file or directory to compare is empty");
                    return -1;
                }

                bool blnSuccess;
                if (string.Equals(mInputFileOrDirectoryPath, "DMS", StringComparison.OrdinalIgnoreCase) &&
                    mComparisonFileOrDirectoryPath.IndexOf("\\", StringComparison.Ordinal) < 0)
                {
                    // DMS Dataset
                    blnSuccess = mProcessingClass.ProcessDMSDataset(mComparisonFileOrDirectoryPath);
                }
                else
                {
                    // Comparing two files or two directories
                    blnSuccess = mProcessingClass.ProcessFilesWildcard(mInputFileOrDirectoryPath, mComparisonFileOrDirectoryPath);
                }

                if (blnSuccess)
                {
                    intReturnCode = 0;
                }
                else
                {
                    intReturnCode = (int)mProcessingClass.ErrorCode;
                    if (intReturnCode != 0)
                    {
                        ShowErrorMessage("Error while processing: " + mProcessingClass.GetErrorMessage());
                    }

                }

                DisplayProgressPercent(mLastProgressReportValue, true);

                return intReturnCode;
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error occurred in modMain->Main: " + ex.Message);
                return -1;
            }

        }

        static void DisplayProgressPercent(int intPercentComplete, bool blnAddCarriageReturn)
        {
            if (blnAddCarriageReturn)
            {
                Console.WriteLine();
            }

            if (intPercentComplete > 100)
            {
                intPercentComplete = 100;
            }

            Console.Write("Processing: " + intPercentComplete + "% ");
            if (blnAddCarriageReturn)
            {
                Console.WriteLine();
            }

        }

        private static string GetAppVersion()
        {
            return PRISM.FileProcessor.ProcessFilesOrFoldersBase.GetAppVersion(PROGRAM_DATE);
        }

        private static bool SetOptionsUsingCommandLineParameters(clsParseCommandLine commandLineParser)
        {
            // Returns True if no problems; otherwise, returns false
            var lstValidParameters = new List<string>
            {
                "N",
                "Bytes",
                "KB",
                "MB",
                "GB",
                "L",
                "LogFolder"
            };

            try
            {
                // Make sure no invalid parameters are present
                if (commandLineParser.InvalidParametersPresent(lstValidParameters))
                {
                    ShowErrorMessage("Invalid commmand line parameters",
                                     (from item in commandLineParser.InvalidParameters(lstValidParameters) select "/" + item).ToList());
                    return false;
                }

                // Query commandLineParser to see if various parameters are present
                if (commandLineParser.NonSwitchParameterCount < 2)
                {
                    ShowErrorMessage(
                        "You must specify two files or two directories or a file match spec and a directory path or the word DMS followed by a dataset name");
                    return false;
                }

                mInputFileOrDirectoryPath = commandLineParser.RetrieveNonSwitchParameter(0);
                mComparisonFileOrDirectoryPath = commandLineParser.RetrieveNonSwitchParameter(1);

                if (commandLineParser.RetrieveValueForParameter("N", out var strValue))
                {
                    if (int.TryParse(strValue, out var intValue))
                        mNumberOfSamples = intValue;
                    else
                        ShowErrorMessage("Non-numeric value: /N:" + strValue);
                }

                var byteParams = new List<KeyValuePair<string, long>> {
                    new KeyValuePair<string, long>("Bytes", 1),
                    new KeyValuePair<string, long>( "KB", 1024),
                    new KeyValuePair<string, long>("MB", 1024 * 1024),
                    new KeyValuePair<string, long>("GB", 1024 * 1024 * 1024)
                };

                foreach (var item in byteParams)
                {

                    if (commandLineParser.RetrieveValueForParameter(item.Key, out strValue))
                    {
                        if (long.TryParse(strValue, out var intValue64))
                            mSampleSizeBytes = intValue64 * item.Value;
                        else
                            ShowErrorMessage(string.Format(
                                                 "Non-numeric value: /{0}: {1}",
                                                 item.Key, strValue));
                    }
                }

                if (commandLineParser.RetrieveValueForParameter("L", out strValue))
                {
                    mLogMessagesToFile = true;
                    if (!string.IsNullOrEmpty(strValue))
                        mLogFilePath = strValue;
                }

                if (commandLineParser.RetrieveValueForParameter("LogFolder", out strValue))
                {
                    mLogMessagesToFile = true;

                    if (!string.IsNullOrEmpty(strValue))
                        mLogFolderPath = strValue;

                }

                return true;
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error parsing the command line parameters: " + ex.Message);
                return false;
            }
        }

        static void ShowErrorMessage(string message)
        {
            ConsoleMsgUtils.ShowError(message);
        }

        static void ShowErrorMessage(string title, IEnumerable<string> errorMessages)
        {
            ConsoleMsgUtils.ShowErrors(title, errorMessages);
        }

        private static void ShowProgramHelp()
        {
            try
            {
                var exeName = Path.GetFileName(PRISM.FileProcessor.ProcessFilesOrFoldersBase.GetAppPath());

                Console.WriteLine(ConsoleMsgUtils.WrapParagraph(
                                      "This program compares two or more files (typically in separate directories) to check whether the " +
                                      "start of the files match, the end of the files match, and selected sections inside the files also match. " +
                                      "Useful for comparing large files without reading the entire file. " +
                                      "Alternatively, you can provide two directory paths and the program will compare all of the files " +
                                      "in the first directory to the identically named files in the second directory."));
                Console.WriteLine();
                Console.WriteLine("Program syntax 1:");
                Console.WriteLine(" " + exeName + " FilePath1 FilePath2");
                Console.WriteLine(" [/N:NumberOfSamples] [/Bytes:SampleSizeBytes]");
                Console.WriteLine(" [/KB:SizeKB] [/MB:SizeMB] [/GB:SizeGB]");
                Console.WriteLine(" [/L[:LogFilePath]] [/LogFolder:LogFolderPath]");
                Console.WriteLine();
                Console.WriteLine("Program syntax 2:");
                Console.WriteLine(" " + exeName + " DirectoryPath1 DirectoryPath2");
                Console.WriteLine(" [/N:NumberOfSamples] [/Bytes:SampleSizeBytes]");
                Console.WriteLine(" [/L] [/LogFolder]");
                Console.WriteLine();
                Console.WriteLine("Program syntax 3:");
                Console.WriteLine(" " + exeName + " FileMatchSpec DirectoryPathToExamine");
                Console.WriteLine(" [/N:NumberOfSamples] [/Bytes:SampleSizeBytes]");
                Console.WriteLine(" [/L] [/LogFolder]");
                Console.WriteLine();
                Console.WriteLine("Program syntax 4:");
                Console.WriteLine(" " + exeName + " DMS DatasetNameToCheck");
                Console.WriteLine(" [/N:NumberOfSamples] [/Bytes:SampleSizeBytes]");
                Console.WriteLine(" [/L] [/LogFolder]");
                Console.WriteLine();
                Console.WriteLine("Use Syntax 1 to compare two files; in this case the filenames cannot have wildcards");
                Console.WriteLine("Use Syntax 2 to compare two directories (including all subdirectories)");
                Console.WriteLine(ConsoleMsgUtils.WrapParagraph(
                                      "Use Syntax 3 to compare a set of files in one directory to identically named files in a separate directory. " +
                                      "Use wildcards in FileMatchSpec to specify the files to examine"));
                Console.WriteLine(ConsoleMsgUtils.WrapParagraph(
                                      "Use Syntax 4 to compare a DMS dataset's files between the storage server and the archive. " +
                                      "The first argument must be DMS; the second argument is the Dataset Name."));
                Console.WriteLine();
                Console.WriteLine(ConsoleMsgUtils.WrapParagraph(
                                      "Use /N to specify the number of portions of a file to examine. " +
                                      "The default is " + clsSampledFileComparer.DEFAULT_NUMBER_OF_SAMPLES + "; " +
                                      "the minimum is 2, indicating the beginning and the end"));
                Console.WriteLine();
                Console.WriteLine(ConsoleMsgUtils.WrapParagraph(
                                      "Use /Bytes, /KB, /MB, or /GB to indicate the number of bytes to read from each file portion; " +
                                      "The default is " + clsSampledFileComparer.DEFAULT_SAMPLE_SIZE_KB + " KB"));
                Console.WriteLine();
                Console.WriteLine("Use /L to log messages to a file. Optionally specify the log folder using /LogFolder");
                Console.WriteLine();
                Console.WriteLine("Program written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA) in 2013");
                Console.WriteLine("Version: " + GetAppVersion());
                Console.WriteLine();
                Console.WriteLine("E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov");
                Console.WriteLine("Website: https://omics.pnl.gov/ or https://panomics.pnnl.gov/");
                Console.WriteLine();

                clsProgRunner.SleepMilliseconds(1000);
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error displaying the program syntax: " + ex.Message);
            }

        }

        private static void mProcessingClass_ProgressChanged(string taskDescription, float percentComplete)
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
                mLastProgressReportValue = mLastProgressReportValue + PERCENT_REPORT_INTERVAL;
                mLastProgressReportTime = DateTime.UtcNow;
            }
            else if (DateTime.UtcNow.Subtract(mLastProgressReportTime).TotalMilliseconds > PROGRESS_DOT_INTERVAL_MSEC)
            {
                mLastProgressReportTime = DateTime.UtcNow;
                Console.Write(".");
            }

        }

        private static void mProcessingClass_ProgressReset()
        {
            mLastProgressReportTime = DateTime.UtcNow;
            mLastProgressReportValue = 0;
        }
    }
}
