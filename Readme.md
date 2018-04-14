This program compares two or more files (typically in separate folders) to check 
whether the start of the files match, the end of the files match, and selected 
sections inside the files also match. It is useful for comparing large files 
without reading the entire file.  Alternatively, you can provide two folder 
paths and the program will compare all of the files in the first folder 
to the identically named files in the second folder.

Program syntax 1:
FileComparisonSampler.exe
 FilePath1 FilePath2 [/N:NumberOfSamples] [/Bytes:SampleSizeBytes]
 [/P:ParameterFilePath] [/Q]
 [/L[:LogFilePath]] [/LogFolder:LogFolderPath]

Program syntax 2:
FileComparisonSampler.exe
 FolderPath1 FolderPath2 [/N:NumberOfSamples]  [/Bytes:SampleSizeBytes]
 [/P:ParameterFilePath] [/Q] [/L] [/LogFolder]

Program syntax 3:
FileComparisonSampler.exe
 FileMatchSpec FolderPathToExamine [/N:NumberOfSamples]  [/Bytes:SampleSizeBytes]
 [/P:ParameterFilePath] [/Q] [/L] [/LogFolder]

Use Syntax 1 to compare two files; in this case the filenames cannot have wildcards
Use Syntax 2 to compare two folders (including all subfolders)
Use Syntax 3 to compare a set of files in one folder to identically named files in a separate folder.  Use wildcards in FileM
atchSpec to specify the files to examine

Use /N to specify the number of portions of a file to examine.  The default is 10; the minimum is 2, indicating the beginning
 and the end
Use /Bytes to indicate the number of bytes to read from each file portion; default is 524288 bytes

The parameter file path is optional.  If included, it should point to a valid XML parameter file (currently ignored).

Use /L to log messages to a file.  Use the optional /Q switch will suppress all error messages.

-------------------------------------------------------------------------------
Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA)
Copyright 2013, Battelle Memorial Institute.  All Rights Reserved.

E-mail: matthew.monroe@pnnl.gov or matt@alchemistmatt.com
Website: http://panomics.pnnl.gov/ or http://omics.pnl.gov
-------------------------------------------------------------------------------

Licensed under the Apache License, Version 2.0; you may not use this file except 
in compliance with the License.  You may obtain a copy of the License at 
http://www.apache.org/licenses/LICENSE-2.0
