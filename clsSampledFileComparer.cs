using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using PRISM;

namespace FileComparisonSampler
{

    class clsSampledFileComparer : PRISM.FileProcessor.ProcessFilesBase
    {
        #region "Constants and enums"

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
        public enum eFileComparerErrorCodes
        {
            NoError = 0,
            ErrorReadingInputFile = 1,
            UnspecifiedError = -1,
        }

        #endregion

        #region "Classwide variables"

        protected int mNumberOfSamples;

        protected long mSampleSizeBytes;

        protected string mLastParameterDisplayValues = string.Empty;

        protected eFileComparerErrorCodes mLocalErrorCode;

        public eFileComparerErrorCodes LocalErrorCode => mLocalErrorCode;

        #endregion

        #region "Properties"

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

        #endregion


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
        /// <returns></returns>
        protected string BytesToHumanReadable(long bytes)
        {
            if (bytes < 10000)
            {
                return bytes.ToString();
            }

            double dblBytes = bytes;
            var lstPrefixes = new List<string> {string.Empty, "KB", "MB", "GB", "TB", "PB"};

            var intPrefixIndex = 0;
            while (dblBytes >= 10000 && intPrefixIndex < lstPrefixes.Count)
            {
                dblBytes /= 1024;
                intPrefixIndex++;
            }

            return Math.Round(dblBytes, 0).ToString("0") + " " + lstPrefixes[intPrefixIndex];

        }

        /// <summary>
        /// Compares two files
        /// </summary>
        /// <param name="inputFilePathBase"></param>
        /// <param name="inputFilePathToCompare"></param>
        /// <returns>True if they Match; false if they do not match (same length, same beginning, same middle, and same end)</returns>
        /// <remarks></remarks>
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
        /// <remarks></remarks>
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
        /// <returns>True if they Match; false if they do not match (same length, same beginning, same middle, and same end)</returns>
        /// <remarks></remarks>
        public bool CompareFiles(
            string inputFilePathBase,
            string inputFilePathToCompare,
            int numberOfSamples,
            long sampleSizeBytes,
            bool showMessageIfMatch)
        {
            const int FIVE_HUNDRED_MB = 1024 * 1024 * 512;
            bool blnSuccess;

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
                    SetBaseClassErrorCode(eProcessFilesErrorCodes.InvalidInputFilePath);
                    return false;
                }

                if (string.IsNullOrWhiteSpace(inputFilePathToCompare))
                {
                    ShowErrorMessage("Input file path to compare is empty");
                    SetBaseClassErrorCode(eProcessFilesErrorCodes.InvalidInputFilePath);
                    return false;
                }

                ShowMessage("Comparing " + Path.GetFileName(inputFilePathBase));
                var fiFilePathBase = new FileInfo(inputFilePathBase);
                var fiFilePathToCompare = new FileInfo(inputFilePathToCompare);

                string strComparisonResult;

                if (numberOfSamples * sampleSizeBytes > fiFilePathBase.Length)
                {
                    // Do a full comparison
                    blnSuccess = CompareFilesComplete(fiFilePathBase, fiFilePathToCompare, out strComparisonResult);
                }
                else
                {
                    blnSuccess = CompareFilesSampled(fiFilePathBase, fiFilePathToCompare, out strComparisonResult, numberOfSamples,
                                                     sampleSizeBytes);
                }

                var pathsCompared = clsPathUtils.CompactPathString(inputFilePathBase) + "  vs. " +
                                    clsPathUtils.CompactPathString(inputFilePathToCompare);

                if (!blnSuccess)
                {
                    if (string.IsNullOrEmpty(strComparisonResult))
                    {
                        LogMessage("Files do not match: " + pathsCompared, eMessageTypeConstants.Warning);
                        Console.WriteLine(" ... *** files do not match ***");
                    }
                    else
                    {
                        LogMessage(strComparisonResult + ": " + pathsCompared, eMessageTypeConstants.Warning);
                        Console.WriteLine(" ... *** " + strComparisonResult + " ***");
                    }

                }
                else if (showMessageIfMatch && !string.IsNullOrEmpty(strComparisonResult))
                {
                    if (string.IsNullOrEmpty(strComparisonResult))
                    {
                        LogMessage("Files match: " + pathsCompared);
                        Console.WriteLine(" ... files match");
                    }
                    else
                    {
                        LogMessage(strComparisonResult + ": " + pathsCompared);
                        Console.WriteLine(" ... " + strComparisonResult);
                    }

                }

            }
            catch (Exception ex)
            {
                HandleException("Error in CompareFiles", ex);
                return false;
            }

            return blnSuccess;
        }

        /// <summary>
        /// Perform a full byte-by-byte comparison of the two files
        /// </summary>
        /// <param name="fiFilePathBase"></param>
        /// <param name="fiFilePathToCompare"></param>
        /// <param name="strComparisonResult">'Files Match' if the files match; otherwise user-readable description of why the files don't match</param>
        /// <returns>True if the files match; otherwise false</returns>
        /// <remarks></remarks>
        public bool CompareFilesComplete(FileInfo fiFilePathBase, FileInfo fiFilePathToCompare, out string strComparisonResult)
        {
            if (!FileLengthsMatch(fiFilePathBase, fiFilePathToCompare, out strComparisonResult))
            {
                return false;
            }

            using (var brBaseFile = new BinaryReader(new FileStream(fiFilePathBase.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            using (var brComparisonFile = new BinaryReader(new FileStream(fiFilePathToCompare.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                var dtLastStatusTime = DateTime.UtcNow;
                var blnFilesMatch = CompareFileSection(brBaseFile, brComparisonFile, out strComparisonResult, -1, -1, "Full comparison",
                                                       ref dtLastStatusTime);
                return blnFilesMatch;
            }
        }

        /// <summary>
        /// Perform a full byte-by-byte comparison of a section of two files
        /// </summary>
        /// <param name="brBaseFile"></param>
        /// <param name="brComparisonFile"></param>
        /// <param name="strComparisonResult">'Files Match' if the files match; otherwise user-readable description of why the files don't match</param>
        /// <param name="intStartOffset">File offset to start the comparison; use -1 to specify that the entire file should be read</param>
        /// <param name="sampleSizeBytes">Number of bytes to compare; ignored if intStartOffset is less than 0</param>
        /// <param name="strSampleDescription">Description of the current section of the file being compared</param>
        /// <param name="dtLastStatusTime">The last time that a status message was shown (UTC time)</param>
        /// <returns>True if the files match; otherwise false</returns>
        /// <remarks></remarks>
        protected bool CompareFileSection(BinaryReader brBaseFile, BinaryReader brComparisonFile, out string strComparisonResult, long intStartOffset,
                                          long sampleSizeBytes, string strSampleDescription, ref DateTime dtLastStatusTime)
        {
            // 50 MB
            const int CHUNK_SIZE_BYTES = 1024 * 1024 * 50;

            var dblPercentShown = false;
            try
            {
                strComparisonResult = string.Empty;
                long intEndOffset;
                if (intStartOffset < 0)
                {
                    // Compare the entire file
                    intEndOffset = brBaseFile.BaseStream.Length;
                }
                else
                {
                    if (sampleSizeBytes < 1)
                    {
                        sampleSizeBytes = 1;
                    }

                    intEndOffset = intStartOffset + sampleSizeBytes;
                    if (intEndOffset > brBaseFile.BaseStream.Length)
                    {
                        intEndOffset = brBaseFile.BaseStream.Length;
                    }

                    if (intStartOffset > 0)
                    {
                        if (intStartOffset > brBaseFile.BaseStream.Length)
                        {
                            strComparisonResult = "StartOffset is beyond the end of the base file";
                            return false;
                        }

                        if (intStartOffset > brComparisonFile.BaseStream.Length)
                        {
                            strComparisonResult = "StartOffset is beyond the end of the comparison file";
                            return false;
                        }

                        brBaseFile.BaseStream.Position = intStartOffset;
                        brComparisonFile.BaseStream.Position = intStartOffset;
                    }

                }

                while (brBaseFile.BaseStream.Position < brBaseFile.BaseStream.Length &&
                       brBaseFile.BaseStream.Position < intEndOffset)
                {
                    var intOffsetPriorToRead = brBaseFile.BaseStream.Position;
                    int intBytesToRead;
                    if (brBaseFile.BaseStream.Position + CHUNK_SIZE_BYTES <= intEndOffset)
                    {
                        intBytesToRead = CHUNK_SIZE_BYTES;
                    }
                    else
                    {
                        intBytesToRead = (int)(intEndOffset - brBaseFile.BaseStream.Position);
                    }

                    if (intBytesToRead == 0)
                    {
                        break;
                    }

                    var bytFile1 = brBaseFile.ReadBytes(intBytesToRead);
                    var bytFile2 = brComparisonFile.ReadBytes(intBytesToRead);
                    for (var intIndex = 0; intIndex <= bytFile1.Length - 1; intIndex++)
                    {
                        if (bytFile2[intIndex] != bytFile1[intIndex])
                        {
                            strComparisonResult = "Mismatch at offset " + intOffsetPriorToRead + intIndex;
                            return false;
                        }

                    }

                    if (DateTime.UtcNow.Subtract(dtLastStatusTime).TotalSeconds >= 2)
                    {
                        dtLastStatusTime = DateTime.UtcNow;
                        var dblPercentComplete = (brBaseFile.BaseStream.Position - intStartOffset) / (double)(intEndOffset - intStartOffset) * 100;
                        if (dblPercentComplete < 100 || dblPercentShown)
                        {
                            dblPercentShown = true;
                            Console.WriteLine("   " + strSampleDescription + ", " + dblPercentComplete.ToString("0.0") + "%");
                        }
                        else
                        {
                            Console.WriteLine("   " + strSampleDescription);
                        }

                    }

                }

                strComparisonResult = "Files match";
            }
            catch (Exception ex)
            {
                strComparisonResult = "Error in CompareFileSection";
                HandleException(strComparisonResult, ex);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Compares the beginning, end, and optionally one or more middle sections of a file
        /// </summary>
        /// <param name="fiFilePathBase"></param>
        /// <param name="fiFilePathToCompare"></param>
        /// <param name="strComparisonResult">'Files Match' if the files match; otherwise user-readable description of why the files don't match</param>
        /// <param name="numberOfSamples">Number of samples; minimum 2 (for beginning and end)</param>
        /// <param name="sampleSizeBytes">Bytes to compare for each sample (minimum 64 bytes)</param>
        /// <returns>True if the files match; otherwise false</returns>
        /// <remarks></remarks>
        public bool CompareFilesSampled(FileInfo fiFilePathBase, FileInfo fiFilePathToCompare, out string strComparisonResult, int numberOfSamples,
                                        long sampleSizeBytes)
        {
            var intSampleNumber = 0;
            long intBytesExamined = 0;

            if (!FileLengthsMatch(fiFilePathBase, fiFilePathToCompare, out strComparisonResult))
            {
                return false;
            }

            using (var brBaseFile = new BinaryReader(new FileStream(fiFilePathBase.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            using (var brComparisonFile =
                new BinaryReader(new FileStream(fiFilePathToCompare.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {

                long intStartOffset = 0;
                intBytesExamined = intBytesExamined + sampleSizeBytes;

                intSampleNumber++;
                var strSampleDescription = "Sample " + intSampleNumber + " of " + numberOfSamples;
                var dtLastStatusTime = DateTime.UtcNow;

                var blnMatchAtStart = CompareFileSection(brBaseFile, brComparisonFile, out var strComparisonResultAtStart, intStartOffset,
                                                         sampleSizeBytes, strSampleDescription, ref dtLastStatusTime);

                // Update the start offset to be SampleSizeBytes before the end of the file
                intStartOffset = fiFilePathBase.Length - sampleSizeBytes;
                if (intStartOffset < 0)
                {
                    intBytesExamined = intBytesExamined + sampleSizeBytes + intStartOffset;
                    intStartOffset = 0;
                }
                else
                {
                    intBytesExamined = intBytesExamined + sampleSizeBytes;
                }

                intSampleNumber++;
                strSampleDescription = "Sample " + intSampleNumber + " of " + numberOfSamples;

                var blnMatchAtEnd = CompareFileSection(brBaseFile, brComparisonFile, out var strComparisonResultAtEnd, intStartOffset, sampleSizeBytes,
                                                       strSampleDescription, ref dtLastStatusTime);

                if (blnMatchAtStart && !blnMatchAtEnd)
                {
                    strComparisonResult = "Files match at the beginning but not at the end; " + strComparisonResultAtEnd;
                    return false;
                }

                if (blnMatchAtEnd && !blnMatchAtStart)
                {
                    strComparisonResult = "Files match at the end but not at the beginning; " + strComparisonResultAtStart;
                    return false;
                }

                if (numberOfSamples > 2 && fiFilePathBase.Length > sampleSizeBytes * 2)
                {
                    var intMidSectionSamples = numberOfSamples - 2;
                    var dblSeekLength = fiFilePathBase.Length / (double)(intMidSectionSamples + 1);
                    var dblCurrentOffset = dblSeekLength - sampleSizeBytes / 2.0;

                    while (dblCurrentOffset < fiFilePathBase.Length)
                    {
                        intStartOffset = (long)Math.Round(dblCurrentOffset, 0);
                        if (intStartOffset < 0)
                        {
                            intStartOffset = 0;
                        }

                        intSampleNumber++;
                        strSampleDescription = "Sample " + intSampleNumber + " of " + numberOfSamples;

                        var blnMatchInMiddle = CompareFileSection(brBaseFile, brComparisonFile, out strComparisonResult, intStartOffset,
                                                                  sampleSizeBytes, strSampleDescription, ref dtLastStatusTime);

                        if (!blnMatchInMiddle)
                        {
                            strComparisonResult = "Files match at the beginning and end, but not in the middle; " + strComparisonResult;
                            return false;
                        }

                        dblCurrentOffset = dblCurrentOffset + dblSeekLength;
                        intBytesExamined = intBytesExamined + sampleSizeBytes;
                    }

                }
            }



            var dblPercentExamined = intBytesExamined / (double)fiFilePathBase.Length * 100;
            if (dblPercentExamined > 100)
            {
                dblPercentExamined = 100;
            }

            strComparisonResult = "Files match (examined " + dblPercentExamined.ToString("0.00") + "% of the file)";
            return true;

        }

        /// <summary>
        /// Compares each file in folder inputFolderPath1 to files in folder inputFolderPath2
        /// </summary>
        /// <param name="inputFolderPath1"></param>
        /// <param name="inputFolderPath2"></param>
        /// <param name="numberOfSamples">Number of samples; minimum 2 (for beginning and end)</param>
        /// <param name="sampleSizeBytes">Bytes to compare for each sample (minimum 64 bytes)</param>
        /// <returns>True if the folders Match; false if they do not match</returns>
        /// <remarks></remarks>
        public bool CompareFolders(string inputFolderPath1, string inputFolderPath2, int numberOfSamples, long sampleSizeBytes)
        {
            var intSourceFilesFound = 0;
            var intMatchedFileCount = 0;
            var intMissingFileCount = 0;
            var intMismatchedFileCount = 0;

            try
            {
                inputFolderPath1 = inputFolderPath1.TrimEnd(Path.DirectorySeparatorChar);
                inputFolderPath2 = inputFolderPath2.TrimEnd(Path.DirectorySeparatorChar);

                var diBaseFolder = new DirectoryInfo(inputFolderPath1);

                if (!diBaseFolder.Exists)
                {
                    ShowErrorMessage("Base folder to compare not found: " + inputFolderPath1);
                    SetBaseClassErrorCode(eProcessFilesErrorCodes.InvalidInputFilePath);
                    return false;
                }

                var diComparisonFolder = new DirectoryInfo(inputFolderPath2);
                if (!diComparisonFolder.Exists)
                {
                    ShowErrorMessage("Comparison folder not found: " + inputFolderPath2);
                    SetBaseClassErrorCode(eProcessFilesErrorCodes.InvalidInputFilePath);
                    return false;
                }

                ShowMessage("Comparing folders: " + diBaseFolder.FullName);
                ShowMessage("               vs. " + diComparisonFolder.FullName);

                Console.WriteLine();
                ShowParameters();

                foreach (var fiFile in diBaseFolder.GetFiles("*.*", SearchOption.AllDirectories))
                {
                    // Look for the corresponding item in the comparison folder
                    FileInfo fiComparisonFile;
                    if (fiFile.Directory == null)
                    {
                        ShowMessage("Cannot determine the parent directory path; skipping " + fiFile.FullName);
                        continue;
                    }

                    if (fiFile.Directory.FullName == diBaseFolder.FullName)
                    {
                        fiComparisonFile = new FileInfo(Path.Combine(inputFolderPath2, fiFile.Name));
                    }
                    else
                    {
                        var strSubdirectoryAddon = fiFile.Directory.FullName.Substring(diBaseFolder.FullName.Length + 1);
                        fiComparisonFile = new FileInfo(Path.Combine(inputFolderPath2, strSubdirectoryAddon, fiFile.Name));
                    }

                    if (!fiComparisonFile.Exists)
                    {
                        ShowMessage("  File " + fiFile.Name + " not found in the comparison folder");
                        intMissingFileCount++;
                    }
                    else if (CompareFiles(fiFile.FullName, fiComparisonFile.FullName, numberOfSamples, sampleSizeBytes, true))
                    {
                        intMatchedFileCount++;
                    }
                    else
                    {
                        intMismatchedFileCount++;
                    }

                    intSourceFilesFound++;
                }

            }
            catch (Exception ex)
            {
                HandleException("Error in CompareFolders", ex);
                return false;
            }

            Console.WriteLine();
            if (intSourceFilesFound == 0)
            {
                ShowErrorMessage("Base folder was empty; nothing to compare: " + inputFolderPath1);
                return true;
            }

            if (intMissingFileCount == 0 && intMismatchedFileCount == 0)
            {
                ShowMessage("Folders match; checked " + intSourceFilesFound + " file(s)");
                return true;
            }

            if (intMissingFileCount == 0 && intMismatchedFileCount > 0)
            {
                ShowMessage("Folders do not match; Mis-matched file count: " + intMismatchedFileCount + "; Matched file count: " + intMatchedFileCount);
                return false;
            }

            if (intMissingFileCount > 0)
            {
                ShowMessage("Comparison folder is missing " + intMissingFileCount + " file(s) that the base folder contains");
                ShowMessage("Mis-matched file count: " + intMismatchedFileCount + "; Matched file count: " + intMatchedFileCount);
                return false;
            }

            Console.WriteLine("Note: unexpected logic encountered in If-Else-EndIf block in CompareFolders");
            ShowMessage("Folders do not match; Mis-matched file count: " + intMismatchedFileCount + "; Matched file count: " + intMatchedFileCount);
            return false;

        }

        /// <summary>
        /// Compare the size of two files
        /// </summary>
        /// <param name="fiFilePathBase"></param>
        /// <param name="fiFilePathToCompare"></param>
        /// <param name="strComparisonResult"></param>
        /// <returns>Trueif the sizes match, otherwise false</returns>
        public bool FileLengthsMatch(FileInfo fiFilePathBase, FileInfo fiFilePathToCompare, out string strComparisonResult)
        {
            if (!fiFilePathBase.Exists)
            {
                strComparisonResult = "Base file to compare not found: " + fiFilePathBase.FullName;
                return false;
            }

            if (!fiFilePathToCompare.Exists)
            {
                strComparisonResult = "Comparison file not found: " + fiFilePathToCompare.FullName;
                return false;
            }

            if (fiFilePathBase.Length != fiFilePathToCompare.Length)
            {
                strComparisonResult = string.Format("Base file is {0:#,##0.0} KB; comparison file is {1:#,##0.0} KB",
                                                    fiFilePathBase.Length / 1024.0,
                                                    fiFilePathToCompare.Length / 1024.0);



                return false;
            }

            strComparisonResult = "File lengths match";
            return true;

        }

        /// <summary>
        /// Get the error message
        /// </summary>
        /// <returns>Error message, or empty string if no error</returns>
        public override string GetErrorMessage()
        {

            string strErrorMessage;
            if (ErrorCode == eProcessFilesErrorCodes.LocalizedError ||
                ErrorCode == eProcessFilesErrorCodes.NoError)
            {
                switch (mLocalErrorCode)
                {
                    case eFileComparerErrorCodes.NoError:
                        strErrorMessage = "";
                        break;
                    case eFileComparerErrorCodes.ErrorReadingInputFile:
                        strErrorMessage = "Error reading input file";
                        break;
                    default:
                        strErrorMessage = "Unknown error state";
                        break;
                }
            }
            else
            {
                strErrorMessage = GetBaseClassErrorMessage();
            }

            return strErrorMessage;
        }

        private void InitializeLocalVariables()
        {
            mNumberOfSamples = DEFAULT_NUMBER_OF_SAMPLES;
            mSampleSizeBytes = DEFAULT_SAMPLE_SIZE_KB * 1024;
            mLocalErrorCode = eFileComparerErrorCodes.NoError;
        }

        private bool LoadParameterFileSettings(string parameterFilePath)
        {
            const string OPTIONS_SECTION = "Options";
            var objSettingsFile = new XmlSettingsFileAccessor();

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

                    parameterFilePath = Path.Combine(GetAppFolderPath(), Path.GetFileName(parameterFilePath));
                    if (!File.Exists(parameterFilePath))
                    {
                        SetBaseClassErrorCode(eProcessFilesErrorCodes.ParameterFileNotFound);
                        return false;
                    }

                }

                if (objSettingsFile.LoadSettings(parameterFilePath))
                {
                    if (!objSettingsFile.SectionPresent(OPTIONS_SECTION))
                    {
                        ShowWarning("The node <section name=\"" + OPTIONS_SECTION + "\"> " +
                                   "was not found in the parameter file: " + parameterFilePath);

                        SetBaseClassErrorCode(eProcessFilesErrorCodes.InvalidParameterFile);
                        return false;
                    }

                    // Me.SettingName = objSettingsFile.GetParam(OPTIONS_SECTION, "HeaderLineChar", Me.SettingName)

                }

            }
            catch (Exception ex)
            {
                HandleException("Error in LoadParameterFileSettings", ex);
                return false;
            }

            return true;
        }

        private bool LookupDatasetFolderPaths(string strDatasetName, out string strStorageServerFolderPath, out string strArchiveFolderPath)
        {
            return LookupDatasetFolderPaths(strDatasetName, out strStorageServerFolderPath, out strArchiveFolderPath, "Gigasax", "DMS5");
        }

        private bool LookupDatasetFolderPaths(
            string strDatasetName,
            out string strStorageServerFolderPath,
            out string strArchiveFolderPath,
            string strDMSServer,
            string strDMSDatabase)
        {

            strStorageServerFolderPath = string.Empty;
            strArchiveFolderPath = string.Empty;

            try
            {
                var connectionString = "Data Source=" + strDMSServer + ";Initial Catalog=" + strDMSDatabase + ";Integrated Security=SSPI;";

                var dbTools = new clsDBTools(connectionString);


                var sqlQuery = "SELECT Dataset_Folder_Path, Archive_Folder_Path " +
                               "FROM V_Dataset_Folder_Paths " +
                               "WHERE (Dataset = '" + strDatasetName + "')";

                dbTools.GetQueryResults(sqlQuery, out var lstResults, "LookupDatasetFolderPaths");

                if (lstResults.Count > 0)
                {
                    var firstResult = lstResults.First();

                    strStorageServerFolderPath = firstResult[0];
                    strArchiveFolderPath = firstResult[1];
                    if (string.IsNullOrEmpty(strStorageServerFolderPath))
                    {
                        ShowErrorMessage("Dataset '" + strDatasetName + "' has an empty Dataset_Folder_Path " +
                                         "(using " + strDMSDatabase + ".dbo.V_Dataset_Folder_Paths on server " + strDMSServer + ")");
                    }
                    else if (string.IsNullOrEmpty(strArchiveFolderPath))
                    {
                        ShowErrorMessage("Dataset '" + strDatasetName + "' has an empty Archive_Folder_Path " +
                                         "(using " + strDMSDatabase + ".dbo.V_Dataset_Folder_Paths on server " + strDMSServer + ")");
                    }
                    else
                    {
                        return true;
                    }

                }
                else
                {
                    ShowErrorMessage("Dataset '" + strDatasetName + "' " +
                                     "not found in " + strDMSDatabase + ".dbo.V_Dataset_Folder_Paths " +
                                     "on server " + strDMSServer);
                }

                return false;
            }
            catch (Exception ex)
            {
                HandleException("Error in LookupDatasetFolderPaths", ex);
                return false;
            }


        }

        /// <summary>
        /// Process a DMS dataset, comparing files in the dataset folder to the archive
        /// ToDo: Update to work with MyEMSL
        /// </summary>
        /// <param name="strDatasetName"></param>
        /// <returns>True if all files match, otherwise false</returns>
        public bool ProcessDMSDataset(string strDatasetName)
        {
            if (!LookupDatasetFolderPaths(strDatasetName, out var strStorageServerFolderPath, out var strArchiveFolderPath))
            {
                return false;
            }

            return ProcessFile(strStorageServerFolderPath, strArchiveFolderPath, "", true);
        }

        /// <summary>
        /// Compares the two specified files or two specified folders
        /// </summary>
        /// <param name="inputFilePath">Base file or folder to read</param>
        /// <param name="outputFolderPath">File or folder to compare to inputFilePath</param>
        /// <param name="parameterFilePath">Parameter file path (unused)</param>
        /// <param name="resetErrorCode"></param>
        /// <returns></returns>
        /// <remarks>If inputFilePath is a file but outputFolderPath is a folder, then looks for a file named inputFilePath in folder outputFolderPath</remarks>
        public override bool ProcessFile(string inputFilePath, string outputFolderPath, string parameterFilePath, bool resetErrorCode)
        {
            try
            {
                if (resetErrorCode)
                {
                    SetLocalErrorCode(eFileComparerErrorCodes.NoError);
                }

                Console.WriteLine();
                if (!LoadParameterFileSettings(parameterFilePath))
                {
                    ShowErrorMessage("Parameter file load error: " + parameterFilePath);
                    if (ErrorCode == eProcessFilesErrorCodes.NoError)
                    {
                        SetBaseClassErrorCode(eProcessFilesErrorCodes.InvalidParameterFile);
                    }

                    return false;
                }

                if (string.IsNullOrWhiteSpace(inputFilePath))
                {
                    ShowErrorMessage("Base file path to compare is empty; unable to continue");
                    SetBaseClassErrorCode(eProcessFilesErrorCodes.InvalidInputFilePath);
                    return false;
                }

                var diBaseFolder = new DirectoryInfo(inputFilePath);
                if (diBaseFolder.Exists)
                {
                    // Comparing folder contents
                    if (string.IsNullOrWhiteSpace(outputFolderPath))
                    {
                        ShowErrorMessage("Base item is a folder (" + inputFilePath + "), but the comparison item is empty; unable to continue");
                        SetBaseClassErrorCode(eProcessFilesErrorCodes.InvalidInputFilePath);
                        return false;
                    }

                    var diComparisonFolder = new DirectoryInfo(outputFolderPath);
                    if (!diComparisonFolder.Exists)
                    {
                        ShowErrorMessage("Base item is a folder (" + inputFilePath + "), but the comparison folder was not found: " +
                                         outputFolderPath);
                        SetBaseClassErrorCode(eProcessFilesErrorCodes.InvalidInputFilePath);
                        return false;
                    }

                    return CompareFolders(diBaseFolder.FullName, diComparisonFolder.FullName, mNumberOfSamples, mSampleSizeBytes);
                }
                else
                {
                    // Comparing files
                    var fiBaseFile = new FileInfo(inputFilePath);
                    if (!fiBaseFile.Exists)
                    {
                        ShowErrorMessage("Base file to compare not found: " + inputFilePath);
                        SetBaseClassErrorCode(eProcessFilesErrorCodes.InvalidInputFilePath);
                        return false;
                    }

                    if (string.IsNullOrWhiteSpace(outputFolderPath))
                    {
                        ShowErrorMessage("Base item is a file (" + inputFilePath + "), but the comparison item is empty; unable to continue");
                        SetBaseClassErrorCode(eProcessFilesErrorCodes.InvalidInputFilePath);
                        return false;
                    }

                    var diComparisonFolder = new DirectoryInfo(outputFolderPath);
                    string strComparisonFilePath;
                    if (diComparisonFolder.Exists)
                    {
                        // Will look for a file in the comparison folder with the same name as the input file
                        strComparisonFilePath = Path.Combine(diComparisonFolder.FullName, Path.GetFileName(inputFilePath));
                    }
                    else
                    {
                        var fiComparisonFile = new FileInfo(outputFolderPath);
                        if (!fiComparisonFile.Exists)
                        {
                            ShowErrorMessage("Base item is a file (" + inputFilePath + "), but the comparison item was not found: " +
                                             outputFolderPath);
                            SetBaseClassErrorCode(eProcessFilesErrorCodes.InvalidInputFilePath);
                            return false;
                        }

                        strComparisonFilePath = fiComparisonFile.FullName;

                    }

                    ShowParameters();

                    return CompareFiles(inputFilePath, strComparisonFilePath, mNumberOfSamples, mSampleSizeBytes, true);
                }

            }
            catch (Exception ex)
            {
                HandleException("Error in ProcessFile", ex);
                return false;
            }

        }

        private void SetLocalErrorCode(eFileComparerErrorCodes eNewErrorCode, bool blnLeaveExistingErrorCodeUnchanged = false)
        {
            if (blnLeaveExistingErrorCodeUnchanged && mLocalErrorCode != eFileComparerErrorCodes.NoError)
            {
                // An error code is already defined; do not change it
                return;
            }

            mLocalErrorCode = eNewErrorCode;
            if (eNewErrorCode == eFileComparerErrorCodes.NoError)
            {
                if (ErrorCode == eProcessFilesErrorCodes.LocalizedError)
                {
                    SetBaseClassErrorCode(eProcessFilesErrorCodes.NoError);
                }

            }
            else
            {
                SetBaseClassErrorCode(eProcessFilesErrorCodes.LocalizedError);
            }

        }

        protected void ShowParameters()
        {
            var strDisplayValues = mNumberOfSamples + "_" + mSampleSizeBytes;

            if (mLastParameterDisplayValues == strDisplayValues)
            {
                // Values have already been displayed
            }
            else
            {
                ShowMessage("Number of samples: " + mNumberOfSamples);
                ShowMessage("Sample Size:       " + BytesToHumanReadable(mSampleSizeBytes));
                Console.WriteLine();
                mLastParameterDisplayValues = strDisplayValues;
            }
        }

    }
}
