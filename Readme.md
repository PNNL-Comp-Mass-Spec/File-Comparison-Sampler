# File Comparison Sampler

This program compares two or more files (typically in separate directories) to
check whether the start of the files match, the end of the files match, and
selected sections inside the files also match. Useful for comparing large files
without reading the entire file. Alternatively, you can provide two directory
paths and the program will compare all of the files in the first directory to the
identically named files in the second directory.

### Continuous Integration

The latest version of the application is available on the [AppVeyor CI server](https://ci.appveyor.com/project/PNNLCompMassSpec/file-comparison-sampler/build/artifacts)

[![Build status](https://ci.appveyor.com/api/projects/status/n4ebkr0xco519ecb?svg=true)](https://ci.appveyor.com/project/PNNLCompMassSpec/file-comparison-sampler)

## Installation

* Download File-Comparison-Sampler.zip from [AppVeyor](https://ci.appveyor.com/project/PNNLCompMassSpec/file-comparison-sampler/build/artifacts)
* Extract the files
* Run FileComparisonSampler.exe

## Console Switches

The File Comparison Sampler must be run from the Windows command line.  Syntax:

Program syntax 1:
```
 FileComparisonSampler.exe FilePath1 FilePath2
 [/N:NumberOfSamples] [/Bytes:SampleSizeBytes]
 [/KB:SizeKB] [/MB:SizeMB] [/GB:SizeGB]
 [/L[:LogFilePath]] [/LogFolder:LogFolderPath]
```

Program syntax 2:
```
 FileComparisonSampler.exe DirectoryPath1 DirectoryPath2
 [/N:NumberOfSamples] [/Bytes:SampleSizeBytes]
 [/L] [/LogFolder]
```

Program syntax 3:
```
 FileComparisonSampler.exe FileMatchSpec DirectoryPathToExamine
 [/N:NumberOfSamples] [/Bytes:SampleSizeBytes]
 [/L] [/LogFolder]
```

Program syntax 4:
```
 FileComparisonSampler.exe DMS DatasetNameToCheck
 [/N:NumberOfSamples] [/Bytes:SampleSizeBytes]
 [/L] [/LogFolder]
```

Use Syntax 1 to compare two files; in this case the filenames cannot have wildcards

Use Syntax 2 to compare two directories (including all subdirectories)

Use Syntax 3 to compare a set of files in one directory to identically named
files in a separate directory. Use wildcards in FileMatchSpec to specify the
files to examine

Use Syntax 4 to compare a DMS dataset's files between the storage server and the
archive. The first argument must be DMS; the second argument is the Dataset Name.

Use /N to specify the number of portions of a file to examine. The default is 10;
the minimum is 2, indicating the beginning and the end

Use /Bytes, /KB, /MB, or /GB to indicate the number of bytes to read from each
file portion; The default is 512 KB

Use /L to log messages to a file. Optionally specify the log folder using /LogFolder

## Contacts

Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA) \
E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov\
Website: https://panomics.pnnl.gov/ or https://omics.pnl.gov

## License

The File Comparison Sampler is licensed under the Apache License, Version 2.0; 
you may not use this file except in compliance with the License.  You may obtain 
a copy of the License at https://opensource.org/licenses/Apache-2.0
