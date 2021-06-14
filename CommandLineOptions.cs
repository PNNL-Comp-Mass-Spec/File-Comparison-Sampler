using System.Collections.Generic;
using System.Linq;
using PRISM;

namespace FileComparisonSampler
{
    internal class CommandLineOptions
    {
        // Ignore Spelling: wildcards


        [Option("InputFile", "F1", ArgPosition = 1, Required = true, HelpShowsDefault = false,
            HelpText = "File or directory path (can include wildcards in certain modes; see usage examples).")]
        public string InputFileOrDirectoryPath { get; set; }

        [Option("ComparisonFile", "F2", ArgPosition = 2, Required = true, HelpShowsDefault = false,
            HelpText = "File or directory path, or dataset name (see usage examples)")]
        public string ComparisonFileOrDirectoryPath { get; set; }

        public bool LogMessagesToFile { get; set; }

        [Option("Log", "L", ArgExistsProperty = nameof(LogMessagesToFile), HelpShowsDefault = false,
            HelpText = "Log messages to a file. Can provide a file path.")]
        public string LogFilePath { get; set; }

        [Option("LogDirectory", "LogDir", "LogFolder", ArgExistsProperty = nameof(LogMessagesToFile), HelpShowsDefault = false,
            HelpText = "The directory where the log file should be written")]
        public string LogDirectoryPath { get; set; }

        [Option("Samples", "N", HelpShowsDefault = false,
            HelpText = "The number of portions of a file to examine. The minimum is 2, indicating the beginning and the end", Min = 2)]
        public int NumberOfSamples { get; set; }

        [Option("Bytes", HelpShowsDefault = false,
            HelpText = "The number of bytes to read from each file portion. Largest byte value is used.")]
        public long SampleSizeBytes { get; set; }

        [Option("KB",
            HelpText = "The number of kilobytes to read from each file portion. Largest byte value is used.")]
        public long SampleSizeKBytes { get; set; }

        [Option("MB", HelpShowsDefault = false,
            HelpText = "The number of megabytes to read from each file portion. Largest byte value is used.")]
        public long SampleSizeMBytes { get; set; }

        [Option("GB", HelpShowsDefault = false,
            HelpText = "The number of gigabytes to read from each file portion. Largest byte value is used.")]
        public long SampleSizeGBytes { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public CommandLineOptions()
        {
            // Initialize the options
            InputFileOrDirectoryPath = string.Empty;
            ComparisonFileOrDirectoryPath = string.Empty;
            // Unused: ParameterFilePath = string.Empty;
            NumberOfSamples = SampledFileComparer.DEFAULT_NUMBER_OF_SAMPLES;
            SampleSizeKBytes = SampledFileComparer.DEFAULT_SAMPLE_SIZE_KB;
            LogMessagesToFile = false;
            LogFilePath = string.Empty;
            LogDirectoryPath = string.Empty;
        }

        public bool Validate()
        {
            if (string.IsNullOrWhiteSpace(InputFileOrDirectoryPath))
            {
                ShowErrorMessage("Base file or directory to compare is empty");
                return false;
            }

            if (string.IsNullOrWhiteSpace(ComparisonFileOrDirectoryPath))
            {
                ShowErrorMessage("Comparison file or directory to compare is empty");
                return false;
            }

            if ((SampleSizeBytes > 0 || SampleSizeMBytes > 0 || SampleSizeGBytes > 0) && SampleSizeKBytes == SampledFileComparer.DEFAULT_SAMPLE_SIZE_KB)
            {
                SampleSizeKBytes = 0;
            }

            var sizes = new List<long>(4)
            {
                SampleSizeBytes,
                SampleSizeKBytes * 1024,
                SampleSizeMBytes * 1024 * 1024,
                SampleSizeGBytes * 1024 * 1024 * 1024
            };

            SampleSizeBytes = sizes.Max();

            return true;
        }

        private static void ShowErrorMessage(string message)
        {
            ConsoleMsgUtils.ShowError(message);
        }
    }
}
