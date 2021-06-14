using System;
using System.Collections.Generic;
using System.IO;
using PRISM;
using PRISMDatabaseUtils;

namespace FileComparisonSampler
{
    internal class clsSampledFileComparer : PRISM.FileProcessor.ProcessFilesBase
    {
        /// <summary>
        /// Default number of samples
        /// </summary>
        public const int DEFAULT_NUMBER_OF_SAMPLES = 10;

        /// <summary>
        /// Default sample size
        /// </summary>
        public const int DEFAULT_SAMPLE_SIZE_KB = 512;

        /// <summary>
        /// Minimum sample size (in bytes)
        /// </summary>
        protected const int MINIMUM_SAMPLE_SIZE_BYTES = 64;

        /// <summary>
        /// Error codes specialized for this class
        /// </summary>
        public enum FileComparerErrorCodes
        {
            NoError = 0,
            ErrorReadingInputFile = 1,
            // ReSharper disable once UnusedMember.Global
            UnspecifiedError = -1,
        }

        protected int mNumberOfSamples;

        protected long mSampleSizeBytes;

        protected string mLastParameterDisplayValues = string.Empty;

        protected FileComparerErrorCodes mLocalErrorCode;

        /// <summary>
        /// Local error code
        /// </summary>
        // ReSharper disable once UnusedMember.Global
        public FileComparerErrorCodes LocalErrorCode => mLocalErrorCode;

        /// <summary>
        /// Number of samples (aka number of portions of the file to examine)
        /// </summary>
        /// <remarks>
        /// Default: 10
        /// Minimum: 2 (for beginning and end)
        /// </remarks>
        public int NumberOfSamples
        {
            get => mNumberOfSamples;
            set
            {
                if (value < 2)
                {
                    value = 2;
                }

                mNumberOfSamples = value;
            }
        }

        /// <summary>
        /// Sample size, in bytes
        /// </summary>
        /// <remarks>
        /// Default: 512
        /// Minimum: 64
        /// </remarks>
        public long SampleSizeBytes
        {
            get => mSampleSizeBytes;
            set
            {
                if (value < MINIMUM_SAMPLE_SIZE_BYTES)
                {
                    value = MINIMUM_SAMPLE_SIZE_BYTES;
                }

                mSampleSizeBytes = value;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public clsSampledFileComparer()
        {
            mFileDate = "April 13, 2018";
            InitializeLocalVariables();
        }

        /// <summary>
        /// Convert bytes to a human-readable string
        /// </summary>
        /// <param name="bytes"></param>
        protected string BytesToHumanReadable(long bytes)
        {
            if (bytes < 10000)
            {
                return bytes.ToString();
            }

            double scaledBytes = bytes;
            var sizeUnits = new List<string> { string.Empty, "KB", "MB", "GB", "TB", "PB" };

            var prefixIndex = 0;
            while (scaledBytes >= 10000 && prefixIndex < sizeUnits.Count)
            {
                scaledBytes /= 1024;
                prefixIndex++;
            }

            return Math.Round(scaledBytes, 0).ToString("0") + " " + sizeUnits[prefixIndex];
        }

        /// <summary>
        /// Compares two files
        /// </summary>
        /// <param name="inputFilePathBase"></param>
        /// <param name="inputFilePathToCompare"></param>
        /// <returns>True if they Match; false if they do not match (same length, same beginning, same middle, and same end)</returns>
        // ReSharper disable once UnusedMember.Global
        public bool CompareFiles(string inputFilePathBase, string inputFilePathToCompare)
        {
            return CompareFiles(inputFilePathBase, inputFilePathToCompare, DEFAULT_NUMBER_OF_SAMPLES, DEFAULT_SAMPLE_SIZE_KB * 1024, true);
        }

        /// <summary>
        /// Compares two files
        /// </summary>
        /// <param name="inputFilePathBase"></param>
        /// <param name="inputFilePathToCompare"></param>
        /// <param name="numberOfSamples">Number of samples; minimum 2 (for beginning and end)</param>
        /// <returns>True if they Match; false if they do not match (same length, same beginning, same middle, and same end)</returns>
        // ReSharper disable once UnusedMember.Global
        public bool CompareFiles(string inputFilePathBase, string inputFilePathToCompare, int numberOfSamples)
        {
            return CompareFiles(inputFilePathBase, inputFilePathToCompare, numberOfSamples, DEFAULT_SAMPLE_SIZE_KB * 1024, true);
        }

        /// <summary>
        /// Compares two files
        /// </summary>
        /// <param name="inputFilePathBase"></param>
        /// <param name="inputFilePathToCompare"></param>
        /// <param name="numberOfSamples">Number of samples; minimum 2 (for beginning and end)</param>
        /// <param name="sampleSizeBytes">Bytes to compare for each sample (minimum 64 bytes)</param>
        /// <param name="showMessageIfMatch">When true, then reports that files matched (always reports if files do not match)</param>
        /// <returns>True if they Match; false if they do not match (same length, same beginning, same middle, and same end) or if an error</returns>
        public bool CompareFiles(
            string inputFilePathBase,
            string inputFilePathToCompare,
            int numberOfSamples,
            long sampleSizeBytes,
            bool showMessageIfMatch)
        {
            const int FIVE_HUNDRED_MB = 1024 * 1024 * 512;
            bool success;

            try
            {
                if (numberOfSamples < 2)
                {
                    numberOfSamples = 2;
                }

                if (sampleSizeBytes < MINIMUM_SAMPLE_SIZE_BYTES)
                {
                    sampleSizeBytes = MINIMUM_SAMPLE_SIZE_BYTES;
                }

                if (sampleSizeBytes > FIVE_HUNDRED_MB)
                {
                    sampleSizeBytes = FIVE_HUNDRED_MB;
                }

                if (string.IsNullOrWhiteSpace(inputFilePathBase))
                {
                    ShowErrorMessage("Base input file path is empty");
                    SetBaseClassErrorCode(ProcessFilesErrorCodes.InvalidInputFilePath);
                    return false;
                }

                if (string.IsNullOrWhiteSpace(inputFilePathToCompare))
                {
                    ShowErrorMessage("Input file path to compare is empty");
                    SetBaseClassErrorCode(ProcessFilesErrorCodes.InvalidInputFilePath);
                    return false;
                }

                Console.Write("Comparing " + Path.GetFileName(inputFilePathBase));
                var baseFile = new FileInfo(inputFilePathBase);
                var comparisonFile = new FileInfo(inputFilePathToCompare);

                string comparisonResult;

                if (numberOfSamples * sampleSizeBytes > baseFile.Length)
                {
                    // Do a full comparison
                    success = CompareFilesComplete(baseFile, comparisonFile, out comparisonResult);
                }
                else
                {
                    success = CompareFilesSampled(baseFile, comparisonFile, out comparisonResult, numberOfSamples,
                                                     sampleSizeBytes);
                }

                var pathsCompared = inputFilePathBase + "  vs. " + inputFilePathToCompare;

                if (!success)
                {
                    if (string.IsNullOrEmpty(comparisonResult))
                    {
                        Console.WriteLine(" ... *** files do not match ***");
                        if (LogMessagesToFile)
                        {
                            LogMessage("Files do not match: " + pathsCompared, MessageTypeConstants.Warning);
                        }
                    }
                    else
                    {
                        Console.WriteLine(" ... *** " + comparisonResult + " ***");
                        if (LogMessagesToFile)
                        {
                            LogMessage(comparisonResult + ": " + pathsCompared, MessageTypeConstants.Warning);
                        }
                    }
                }
                else if (showMessageIfMatch && !string.IsNullOrEmpty(comparisonResult))
                {
                    if (string.IsNullOrEmpty(comparisonResult))
                    {
                        Console.WriteLine(" ... files match");
                        if (LogMessagesToFile)
                        {
                            LogMessage("Files match: " + pathsCompared);
                        }
                    }
                    else
                    {
                        Console.WriteLine(" ... " + comparisonResult);
                        if (LogMessagesToFile)
                        {
                            LogMessage(comparisonResult + ": " + pathsCompared);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                HandleException("Error in CompareFiles", ex);
                return false;
            }

            return success;
        }

        /// <summary>
        /// Perform a full byte-by-byte comparison of the two files
        /// </summary>
        /// <param name="baseFile"></param>
        /// <param name="comparisonFile"></param>
        /// <param name="comparisonResult">'Files Match' if the files match; otherwise user-readable description of why the files don't match</param>
        /// <returns>True if the files match; otherwise false</returns>
        public bool CompareFilesComplete(FileInfo baseFile, FileInfo comparisonFile, out string comparisonResult)
        {
            if (!FileLengthsMatch(baseFile, comparisonFile, out comparisonResult))
            {
                return false;
            }

            using var baseFileReader = new BinaryReader(new FileStream(baseFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            using var comparisonFileReader = new BinaryReader(new FileStream(comparisonFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));

            var lastStatusTime = DateTime.UtcNow;
            return CompareFileSection(baseFileReader, comparisonFileReader, out comparisonResult, -1, -1, "Full comparison", ref lastStatusTime);
        }

        /// <summary>
        /// Perform a full byte-by-byte comparison of a section of two files
        /// </summary>
        /// <param name="baseFileReader"></param>
        /// <param name="comparisonFileReader"></param>
        /// <param name="comparisonResult">'Files Match' if the files match; otherwise user-readable description of why the files don't match</param>
        /// <param name="startOffset">File offset to start the comparison; use -1 to specify that the entire file should be read</param>
        /// <param name="sampleSizeBytes">Number of bytes to compare; ignored if startOffset is less than 0</param>
        /// <param name="sampleDescription">Description of the current section of the file being compared</param>
        /// <param name="lastStatusTime">The last time that a status message was shown (UTC time)</param>
        /// <returns>True if the files match; otherwise false</returns>
        protected bool CompareFileSection(BinaryReader baseFileReader, BinaryReader comparisonFileReader, out string comparisonResult, long startOffset,
                                          long sampleSizeBytes, string sampleDescription, ref DateTime lastStatusTime)
        {
            // 50 MB
            const int CHUNK_SIZE_BYTES = 1024 * 1024 * 50;

            var percentShown = false;
            try
            {
                comparisonResult = string.Empty;
                long endOffset;
                if (startOffset < 0)
                {
                    // Compare the entire file
                    endOffset = baseFileReader.BaseStream.Length;
                }
                else
                {
                    if (sampleSizeBytes < 1)
                    {
                        sampleSizeBytes = 1;
                    }

                    endOffset = startOffset + sampleSizeBytes;
                    if (endOffset > baseFileReader.BaseStream.Length)
                    {
                        endOffset = baseFileReader.BaseStream.Length;
                    }

                    if (startOffset > 0)
                    {
                        if (startOffset > baseFileReader.BaseStream.Length)
                        {
                            comparisonResult = "StartOffset is beyond the end of the base file";
                            return false;
                        }

                        if (startOffset > comparisonFileReader.BaseStream.Length)
                        {
                            comparisonResult = "StartOffset is beyond the end of the comparison file";
                            return false;
                        }

                        baseFileReader.BaseStream.Position = startOffset;
                        comparisonFileReader.BaseStream.Position = startOffset;
                    }
                }

                while (baseFileReader.BaseStream.Position < baseFileReader.BaseStream.Length &&
                       baseFileReader.BaseStream.Position < endOffset)
                {
                    var offsetPriorToRead = baseFileReader.BaseStream.Position;
                    int bytesToRead;
                    if (baseFileReader.BaseStream.Position + CHUNK_SIZE_BYTES <= endOffset)
                    {
                        bytesToRead = CHUNK_SIZE_BYTES;
                    }
                    else
                    {
                        bytesToRead = (int)(endOffset - baseFileReader.BaseStream.Position);
                    }

                    if (bytesToRead == 0)
                    {
                        break;
                    }

                    var bytesFile1 = baseFileReader.ReadBytes(bytesToRead);
                    var bytesFile2 = comparisonFileReader.ReadBytes(bytesToRead);
                    for (var index = 0; index <= bytesFile1.Length - 1; index++)
                    {
                        if (bytesFile2[index] != bytesFile1[index])
                        {
                            comparisonResult = "Mismatch at offset " + offsetPriorToRead + index;
                            return false;
                        }
                    }

                    if (DateTime.UtcNow.Subtract(lastStatusTime).TotalSeconds >= 2)
                    {
                        lastStatusTime = DateTime.UtcNow;
                        var percentComplete = (baseFileReader.BaseStream.Position - startOffset) / (double)(endOffset - startOffset) * 100;
                        if (!percentShown)
                        {
                            percentShown = true;
                            Console.WriteLine();
                            Console.Write("   " + sampleDescription + ", " + percentComplete.ToString("0.0") + "%");
                        }
                        else
                        {
                            Console.Write("  " + percentComplete.ToString("0.0") + "%");
                        }
                    }
                }

                comparisonResult = "Files match";
            }
            catch (Exception ex)
            {
                comparisonResult = "Error in CompareFileSection";
                HandleException(comparisonResult, ex);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Compares the beginning, end, and optionally one or more middle sections of a file
        /// </summary>
        /// <param name="baseFile"></param>
        /// <param name="comparisonFile"></param>
        /// <param name="comparisonResult">'Files Match' if the files match; otherwise user-readable description of why the files don't match</param>
        /// <param name="numberOfSamples">Number of samples; minimum 2 (for beginning and end)</param>
        /// <param name="sampleSizeBytes">Bytes to compare for each sample (minimum 64 bytes)</param>
        /// <returns>True if the files match; otherwise false</returns>
        public bool CompareFilesSampled(FileInfo baseFile, FileInfo comparisonFile, out string comparisonResult, int numberOfSamples,
                                        long sampleSizeBytes)
        {
            var sampleNumber = 0;
            long bytesExamined = 0;

            if (!FileLengthsMatch(baseFile, comparisonFile, out comparisonResult))
            {
                return false;
            }

            using (var baseFileReader = new BinaryReader(new FileStream(baseFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            using (var comparisonFileReader =
                new BinaryReader(new FileStream(comparisonFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                long startOffset = 0;
                bytesExamined += sampleSizeBytes;

                sampleNumber++;
                var sampleDescription = "Sample " + sampleNumber + " of " + numberOfSamples;
                var lastStatusTime = DateTime.UtcNow;

                var matchAtStart = CompareFileSection(baseFileReader, comparisonFileReader, out var comparisonResultAtStart, startOffset,
                                                         sampleSizeBytes, sampleDescription, ref lastStatusTime);

                // Update the start offset to be SampleSizeBytes before the end of the file
                startOffset = baseFile.Length - sampleSizeBytes;
                if (startOffset < 0)
                {
                    bytesExamined = bytesExamined + sampleSizeBytes + startOffset;
                    startOffset = 0;
                }
                else
                {
                    bytesExamined += sampleSizeBytes;
                }

                sampleNumber++;
                sampleDescription = "Sample " + sampleNumber + " of " + numberOfSamples;

                var matchAtEnd = CompareFileSection(baseFileReader, comparisonFileReader, out var comparisonResultAtEnd, startOffset, sampleSizeBytes,
                                                    sampleDescription, ref lastStatusTime);

                if (matchAtStart && !matchAtEnd)
                {
                    comparisonResult = "Files match at the beginning but not at the end; " + comparisonResultAtEnd;
                    return false;
                }

                if (matchAtEnd && !matchAtStart)
                {
                    comparisonResult = "Files match at the end but not at the beginning; " + comparisonResultAtStart;
                    return false;
                }

                if (numberOfSamples > 2 && baseFile.Length > sampleSizeBytes * 2)
                {
                    var midSectionSamples = numberOfSamples - 2;
                    var seekLengthDouble = baseFile.Length / (double)(midSectionSamples + 1);
                    var currentOffsetDouble = seekLengthDouble - sampleSizeBytes / 2.0;

                    while (currentOffsetDouble < baseFile.Length)
                    {
                        startOffset = (long)Math.Round(currentOffsetDouble, 0);
                        if (startOffset < 0)
                        {
                            startOffset = 0;
                        }

                        sampleNumber++;
                        sampleDescription = "Sample " + sampleNumber + " of " + numberOfSamples;

                        var matchInMiddle = CompareFileSection(baseFileReader, comparisonFileReader, out comparisonResult, startOffset,
                                                               sampleSizeBytes, sampleDescription, ref lastStatusTime);

                        if (!matchInMiddle)
                        {
                            comparisonResult = "Files match at the beginning and end, but not in the middle; " + comparisonResult;
                            return false;
                        }

                        currentOffsetDouble += seekLengthDouble;
                        bytesExamined += sampleSizeBytes;
                    }
                }
            }

            var percentExamined = bytesExamined / (double)baseFile.Length * 100;
            if (percentExamined > 100)
            {
                percentExamined = 100;
            }

            comparisonResult = "Files match (examined " + percentExamined.ToString("0.00") + "% of the file)";
            return true;
        }

        /// <summary>
        /// Compares each file in directory inputDirectoryPath1 to files in directory inputDirectoryPath2
        /// </summary>
        /// <param name="inputDirectoryPath1"></param>
        /// <param name="inputDirectoryPath2"></param>
        /// <param name="numberOfSamples">Number of samples; minimum 2 (for beginning and end)</param>
        /// <param name="sampleSizeBytes">Bytes to compare for each sample (minimum 64 bytes)</param>
        /// <returns>True if the directories Match; false if they do not match</returns>
        public bool CompareDirectories(string inputDirectoryPath1, string inputDirectoryPath2, int numberOfSamples, long sampleSizeBytes)
        {
            var sourceFilesFound = 0;
            var matchedFileCount = 0;
            var missingFileCount = 0;
            var mismatchedFileCount = 0;

            try
            {
                inputDirectoryPath1 = inputDirectoryPath1.TrimEnd(Path.DirectorySeparatorChar);
                inputDirectoryPath2 = inputDirectoryPath2.TrimEnd(Path.DirectorySeparatorChar);

                var baseDirectory = new DirectoryInfo(inputDirectoryPath1);

                if (!baseDirectory.Exists)
                {
                    ShowErrorMessage("Base directory to compare not found: " + inputDirectoryPath1);
                    SetBaseClassErrorCode(ProcessFilesErrorCodes.InvalidInputFilePath);
                    return false;
                }

                var comparisonDirectory = new DirectoryInfo(inputDirectoryPath2);
                if (!comparisonDirectory.Exists)
                {
                    ShowErrorMessage("Comparison directory not found: " + inputDirectoryPath2);
                    SetBaseClassErrorCode(ProcessFilesErrorCodes.InvalidInputFilePath);
                    return false;
                }

                ShowMessage("Comparing directories: ");
                ShowMessage("    " + baseDirectory.FullName);
                ShowMessage("vs. " + comparisonDirectory.FullName);

                Console.WriteLine();
                ShowParameters();

                foreach (var baseFile in baseDirectory.GetFiles("*.*", SearchOption.AllDirectories))
                {
                    // Look for the corresponding item in the comparison directory
                    FileInfo comparisonFile;
                    if (baseFile.Directory == null)
                    {
                        ShowMessage("Cannot determine the parent directory path; skipping " + baseFile.FullName);
                        continue;
                    }

                    if (baseFile.Directory.FullName == baseDirectory.FullName)
                    {
                        comparisonFile = new FileInfo(Path.Combine(inputDirectoryPath2, baseFile.Name));
                    }
                    else
                    {
                        var subdirectorySuffix = baseFile.Directory.FullName.Substring(baseDirectory.FullName.Length + 1);
                        comparisonFile = new FileInfo(Path.Combine(inputDirectoryPath2, subdirectorySuffix, baseFile.Name));
                    }

                    if (!comparisonFile.Exists)
                    {
                        ShowMessage("  File " + baseFile.Name + " not found in the comparison directory");
                        missingFileCount++;
                    }
                    else if (CompareFiles(baseFile.FullName, comparisonFile.FullName, numberOfSamples, sampleSizeBytes, true))
                    {
                        matchedFileCount++;
                    }
                    else
                    {
                        mismatchedFileCount++;
                    }

                    sourceFilesFound++;
                }
            }
            catch (Exception ex)
            {
                HandleException("Error in CompareDirectories", ex);
                return false;
            }

            Console.WriteLine();
            if (sourceFilesFound == 0)
            {
                ShowErrorMessage("Base directory was empty; nothing to compare: " + inputDirectoryPath1);
                return true;
            }

            if (missingFileCount == 0 && mismatchedFileCount == 0)
            {
                ShowMessage("Directories match; checked " + sourceFilesFound + " file(s)");
                return true;
            }

            if (missingFileCount == 0 && mismatchedFileCount > 0)
            {
                ShowMessage("Directories do not match; Mis-matched file count: " + mismatchedFileCount + "; Matched file count: " + matchedFileCount);
                return false;
            }

            if (missingFileCount > 0)
            {
                ShowMessage("Comparison directory is missing " + missingFileCount + " file(s) that the base directory contains");
                ShowMessage("Mis-matched file count: " + mismatchedFileCount + "; Matched file count: " + matchedFileCount);
                return false;
            }

            Console.WriteLine("Note: unexpected logic encountered in If-Else-EndIf block in CompareDirectories");
            ShowMessage("Directories do not match; Mis-matched file count: " + mismatchedFileCount + "; Matched file count: " + matchedFileCount);
            return false;
        }

        /// <summary>
        /// Compare the size of two files
        /// </summary>
        /// <param name="baseFile"></param>
        /// <param name="comparisonFile"></param>
        /// <param name="comparisonResult"></param>
        /// <returns>True if the sizes match, otherwise false</returns>
        public bool FileLengthsMatch(FileInfo baseFile, FileInfo comparisonFile, out string comparisonResult)
        {
            if (!baseFile.Exists)
            {
                comparisonResult = "Base file to compare not found: " + baseFile.FullName;
                return false;
            }

            if (!comparisonFile.Exists)
            {
                comparisonResult = "Comparison file not found: " + comparisonFile.FullName;
                return false;
            }

            if (baseFile.Length != comparisonFile.Length)
            {
                comparisonResult = string.Format("Base file is {0:#,##0.0} KB; comparison file is {1:#,##0.0} KB",
                                                 baseFile.Length / 1024.0,
                                                 comparisonFile.Length / 1024.0);

                return false;
            }

            comparisonResult = "File lengths match";
            return true;
        }

        /// <summary>
        /// Get the error message
        /// </summary>
        /// <returns>Error message, or empty string if no error</returns>
        public override string GetErrorMessage()
        {
            string errorMessage;
            if (ErrorCode == ProcessFilesErrorCodes.LocalizedError ||
                ErrorCode == ProcessFilesErrorCodes.NoError)
            {
                switch (mLocalErrorCode)
                {
                    case FileComparerErrorCodes.NoError:
                        errorMessage = "";
                        break;
                    case FileComparerErrorCodes.ErrorReadingInputFile:
                        errorMessage = "Error reading input file";
                        break;
                    default:
                        errorMessage = "Unknown error state";
                        break;
                }
            }
            else
            {
                errorMessage = GetBaseClassErrorMessage();
            }

            return errorMessage;
        }

        private void InitializeLocalVariables()
        {
            mNumberOfSamples = DEFAULT_NUMBER_OF_SAMPLES;
            mSampleSizeBytes = DEFAULT_SAMPLE_SIZE_KB * 1024;
            mLocalErrorCode = FileComparerErrorCodes.NoError;
        }

        private bool LoadParameterFileSettings(string parameterFilePath)
        {
            const string OPTIONS_SECTION = "Options";
            var settingsFile = new XmlSettingsFileAccessor();

            try
            {
                if (string.IsNullOrWhiteSpace(parameterFilePath))
                {
                    // No parameter file specified; nothing to load
                    return true;
                }

                if (!File.Exists(parameterFilePath))
                {
                    // See if parameterFilePath points to a file in the same directory as the application

                    parameterFilePath = Path.Combine(GetAppDirectoryPath(), Path.GetFileName(parameterFilePath));
                    if (!File.Exists(parameterFilePath))
                    {
                        SetBaseClassErrorCode(ProcessFilesErrorCodes.ParameterFileNotFound);
                        return false;
                    }
                }

                if (settingsFile.LoadSettings(parameterFilePath))
                {
                    if (!settingsFile.SectionPresent(OPTIONS_SECTION))
                    {
                        ShowWarning("The node <section name=\"" + OPTIONS_SECTION + "\"> " +
                                   "was not found in the parameter file: " + parameterFilePath);

                        SetBaseClassErrorCode(ProcessFilesErrorCodes.InvalidParameterFile);
                        return false;
                    }

                    // Me.SettingName = settingsFile.GetParam(OPTIONS_SECTION, "HeaderLineChar", Me.SettingName)
                }
            }
            catch (Exception ex)
            {
                HandleException("Error in LoadParameterFileSettings", ex);
                return false;
            }

            return true;
        }

        private bool LookupDatasetDirectoryPaths(string datasetName, out string storageServerDirectoryPath, out string archiveDirectoryPath)
        {
            return LookupDatasetDirectoryPaths(datasetName, out storageServerDirectoryPath, out archiveDirectoryPath, "Gigasax", "DMS5");
        }

        private bool LookupDatasetDirectoryPaths(
            string datasetName,
            out string storageServerDirectoryPath,
            out string archiveDirectoryPath,
            string dmsServer,
            string dmsDatabase)
        {
            storageServerDirectoryPath = string.Empty;
            archiveDirectoryPath = string.Empty;

            try
            {
                var connectionString = "Data Source=" + dmsServer + ";Initial Catalog=" + dmsDatabase + ";Integrated Security=SSPI;";

                var dbTools = DbToolsFactory.GetDBTools(connectionString);


                var sqlQuery = "SELECT Dataset_Folder_Path, Archive_Folder_Path " +
                               "FROM V_Dataset_Folder_Paths " +
                               "WHERE (Dataset = '" + datasetName + "')";

                dbTools.GetQueryResults(sqlQuery, out var queryResults);

                var sourceViewAndDatabase = dmsDatabase + ".dbo.V_Dataset_Folder_Paths on server " + dmsServer;

                if (queryResults.Count > 0)
                {
                    var firstResult = queryResults.First();

                    storageServerDirectoryPath = firstResult[0];
                    archiveDirectoryPath = firstResult[1];
                    if (string.IsNullOrEmpty(storageServerDirectoryPath))
                    {
                        ShowErrorMessage("Dataset '" + datasetName + "' has an empty Dataset_Folder_Path " +
                                         "(using " + sourceViewAndDatabase + ")");
                    }
                    else if (string.IsNullOrEmpty(archiveDirectoryPath))
                    {
                        ShowErrorMessage("Dataset '" + datasetName + "' has an empty Archive_Folder_Path " +
                                         "(using " + sourceViewAndDatabase + ")");
                    }
                    else
                    {
                        return true;
                    }
                }
                else
                {
                    ShowErrorMessage("Dataset '" + datasetName + "' not found in " + sourceViewAndDatabase);
                }

                return false;
            }
            catch (Exception ex)
            {
                HandleException("Error in LookupDatasetDirectoryPaths", ex);
                return false;
            }
        }

        /// <summary>
        /// Process a DMS dataset, comparing files in the dataset directory to the archive
        /// ToDo: Update to work with MyEMSL
        /// </summary>
        /// <param name="datasetName"></param>
        /// <returns>True if all files match, otherwise false</returns>
        public bool ProcessDMSDataset(string datasetName)
        {
            if (!LookupDatasetDirectoryPaths(datasetName, out var storageServerDirectoryPath, out var archiveDirectoryPath))
            {
                return false;
            }

            return ProcessFile(storageServerDirectoryPath, archiveDirectoryPath, "", true);
        }

        /// <summary>
        /// Compares the two specified files or two specified directories
        /// </summary>
        /// <param name="inputFilePath">Base file or directory to read</param>
        /// <param name="outputDirectoryPath">File or directory to compare to inputFilePath</param>
        /// <param name="parameterFilePath">Parameter file path (unused)</param>
        /// <param name="resetErrorCode"></param>
        /// <returns>True if they Match; false if they do not match (same length, same beginning, same middle, and same end) or if an error</returns>
        /// <remarks>If inputFilePath is a file but outputDirectoryPath is a directory, looks for a file named inputFilePath in directory outputDirectoryPath</remarks>
        public override bool ProcessFile(string inputFilePath, string outputDirectoryPath, string parameterFilePath, bool resetErrorCode)
        {
            try
            {
                if (resetErrorCode)
                {
                    SetLocalErrorCode(FileComparerErrorCodes.NoError);
                }

                Console.WriteLine();
                if (!LoadParameterFileSettings(parameterFilePath))
                {
                    ShowErrorMessage("Parameter file load error: " + parameterFilePath);
                    if (ErrorCode == ProcessFilesErrorCodes.NoError)
                    {
                        SetBaseClassErrorCode(ProcessFilesErrorCodes.InvalidParameterFile);
                    }

                    return false;
                }

                if (string.IsNullOrWhiteSpace(inputFilePath))
                {
                    ShowErrorMessage("Base file path to compare is empty; unable to continue");
                    SetBaseClassErrorCode(ProcessFilesErrorCodes.InvalidInputFilePath);
                    return false;
                }

                var baseDirectory = new DirectoryInfo(inputFilePath);
                if (baseDirectory.Exists)
                {
                    // Comparing directory contents
                    if (string.IsNullOrWhiteSpace(outputDirectoryPath))
                    {
                        ShowErrorMessage("Base item is a directory (" + inputFilePath + "), but the comparison item is empty; unable to continue");
                        SetBaseClassErrorCode(ProcessFilesErrorCodes.InvalidInputFilePath);
                        return false;
                    }

                    var comparisonDirectory = new DirectoryInfo(outputDirectoryPath);
                    if (!comparisonDirectory.Exists)
                    {
                        ShowErrorMessage("Base item is a directory (" + inputFilePath + "), but the comparison directory was not found: " +
                                         outputDirectoryPath);
                        SetBaseClassErrorCode(ProcessFilesErrorCodes.InvalidInputFilePath);
                        return false;
                    }

                    return CompareDirectories(baseDirectory.FullName, comparisonDirectory.FullName, mNumberOfSamples, mSampleSizeBytes);
                }
                else
                {
                    // Comparing files
                    var baseFile = new FileInfo(inputFilePath);
                    if (!baseFile.Exists)
                    {
                        ShowErrorMessage("Base file to compare not found: " + inputFilePath);
                        SetBaseClassErrorCode(ProcessFilesErrorCodes.InvalidInputFilePath);
                        return false;
                    }

                    if (string.IsNullOrWhiteSpace(outputDirectoryPath))
                    {
                        ShowErrorMessage("Base item is a file (" + inputFilePath + "), but the comparison item is empty; unable to continue");
                        SetBaseClassErrorCode(ProcessFilesErrorCodes.InvalidInputFilePath);
                        return false;
                    }

                    var comparisonDirectory = new DirectoryInfo(outputDirectoryPath);
                    string comparisonFilePath;
                    if (comparisonDirectory.Exists)
                    {
                        // Will look for a file in the comparison directory with the same name as the input file
                        comparisonFilePath = Path.Combine(comparisonDirectory.FullName, Path.GetFileName(inputFilePath));
                    }
                    else
                    {
                        var comparisonFile = new FileInfo(outputDirectoryPath);
                        if (!comparisonFile.Exists)
                        {
                            ShowErrorMessage("Base item is a file (" + inputFilePath + "), but the comparison item was not found: " +
                                             outputDirectoryPath);
                            SetBaseClassErrorCode(ProcessFilesErrorCodes.InvalidInputFilePath);
                            return false;
                        }

                        comparisonFilePath = comparisonFile.FullName;

                    }

                    ShowParameters();

                    return CompareFiles(inputFilePath, comparisonFilePath, mNumberOfSamples, mSampleSizeBytes, true);
                }
            }
            catch (Exception ex)
            {
                HandleException("Error in ProcessFile", ex);
                return false;
            }
        }

        private void SetLocalErrorCode(FileComparerErrorCodes eNewErrorCode, bool leaveExistingErrorCodeUnchanged = false)
        {
            if (leaveExistingErrorCodeUnchanged && mLocalErrorCode != FileComparerErrorCodes.NoError)
            {
                // An error code is already defined; do not change it
                return;
            }

            mLocalErrorCode = eNewErrorCode;
            if (eNewErrorCode == FileComparerErrorCodes.NoError)
            {
                if (ErrorCode == ProcessFilesErrorCodes.LocalizedError)
                {
                    SetBaseClassErrorCode(ProcessFilesErrorCodes.NoError);
                }

            }
            else
            {
                SetBaseClassErrorCode(ProcessFilesErrorCodes.LocalizedError);
            }
        }

        protected void ShowParameters()
        {
            var displayValues = mNumberOfSamples + "_" + mSampleSizeBytes;

            if (mLastParameterDisplayValues == displayValues)
            {
                // Values have already been displayed
            }
            else
            {
                ShowMessage("Number of samples: " + mNumberOfSamples);
                ShowMessage("Sample Size:       " + BytesToHumanReadable(mSampleSizeBytes));
                Console.WriteLine();
                mLastParameterDisplayValues = displayValues;
            }
        }
    }
}
