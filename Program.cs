Option Strict On

' This program compares two files or two folders to check whether the
' start of the files match, the end of the files match, and selected sections inside the files also match
'
' Useful for comparing large files without reading the entire file
'
' -------------------------------------------------------------------------------
' Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA)
' Program started in April 2013
'
' E-mail: matthew.monroe@pnnl.gov or matt@alchemistmatt.com
' Website: http://panomics.pnnl.gov/ or http://omics.pnl.gov
' -------------------------------------------------------------------------------
' 
' Licensed under the Apache License, Version 2.0; you may not use this file except
' in compliance with the License.  You may obtain a copy of the License at 
' http://www.apache.org/licenses/LICENSE-2.0

Imports System.IO

Module modMain
	Public Const PROGRAM_DATE As String = "August 22, 2014"

	Private mInputFileOrFolderPath As String
	Private mComparisonFileOrFolderPath As String

	Private mParameterFilePath As String						' Optional

	Private mLogMessagesToFile As Boolean
	Private mLogFilePath As String = String.Empty
	Private mLogFolderPath As String = String.Empty

	Private mNumberOfSamples As Integer
	Private mSampleSizeBytes As Int64

	Private mQuietMode As Boolean

	Private WithEvents mProcessingClass As clsSampledFileComparer
	Private mLastProgressReportTime As DateTime
	Private mLastProgressReportValue As Integer

	''' <summary>
	''' Program entry point
	''' </summary>
	''' <returns>0 if no error, error code if an error</returns>
	''' <remarks></remarks>
	Public Function Main() As Integer

		Dim intReturnCode As Integer
		Dim objParseCommandLine As New clsParseCommandLine

		Dim blnProceed As Boolean
		Dim blnSuccess As Boolean

		' Initialize the options
		mInputFileOrFolderPath = String.Empty
		mComparisonFileOrFolderPath = String.Empty

		mParameterFilePath = String.Empty

		mNumberOfSamples = clsSampledFileComparer.DEFAULT_NUMBER_OF_SAMPLES
		mSampleSizeBytes = clsSampledFileComparer.DEFAULT_SAMPLE_SIZE_BYTES

		mQuietMode = False
		mLogMessagesToFile = False
		mLogFilePath = String.Empty
		mLogFolderPath = String.Empty

		Try
			blnProceed = False
			If objParseCommandLine.ParseCommandLine Then
				If SetOptionsUsingCommandLineParameters(objParseCommandLine) Then blnProceed = True
			End If

			If Not blnProceed OrElse _
			   objParseCommandLine.NeedToShowHelp OrElse _
			   objParseCommandLine.ParameterCount + objParseCommandLine.NonSwitchParameterCount = 0 OrElse _
			   mInputFileOrFolderPath.Length = 0 Then
				ShowProgramHelp()
				intReturnCode = -1
			Else

				mProcessingClass = New clsSampledFileComparer

				' Note: the following settings will be overridden if mParameterFilePath points to a valid parameter file that has these settings defined
				With mProcessingClass

					.NumberOfSamples = mNumberOfSamples
					.SampleSizeBytes = mSampleSizeBytes
					.IgnoreErrorsWhenUsingWildcardMatching = True

					.ShowMessages = Not mQuietMode
					.LogMessagesToFile = mLogMessagesToFile
					.LogFilePath = mLogFilePath
					.LogFolderPath = mLogFolderPath
				End With

				If String.IsNullOrWhiteSpace(mInputFileOrFolderPath) Then
					ShowErrorMessage("Base file or folder to compare is empty")
					Return -1
				End If

				If String.Compare(mInputFileOrFolderPath, "DMS", True) = 0 AndAlso mComparisonFileOrFolderPath.IndexOf("\", StringComparison.Ordinal) < 0 Then
					' DMS Dataset
					blnSuccess = mProcessingClass.ProcessDMSDataset(mComparisonFileOrFolderPath, mParameterFilePath)
				Else
					' Comparing two files or two folders
					blnSuccess = mProcessingClass.ProcessFilesWildcard(mInputFileOrFolderPath, mComparisonFileOrFolderPath, mParameterFilePath)
				End If

				If blnSuccess Then
					intReturnCode = 0
				Else
					intReturnCode = mProcessingClass.ErrorCode
					If intReturnCode <> 0 AndAlso Not mQuietMode Then
						ShowErrorMessage("Error while processing: " & mProcessingClass.GetErrorMessage())
					End If
				End If

				DisplayProgressPercent(mLastProgressReportValue, True)
			End If

		Catch ex As Exception
			ShowErrorMessage("Error occurred in modMain->Main: " & Environment.NewLine & ex.Message)
			intReturnCode = -1
		End Try

		Return intReturnCode

	End Function

	Private Sub DisplayProgressPercent(ByVal intPercentComplete As Integer, ByVal blnAddCarriageReturn As Boolean)
		If blnAddCarriageReturn Then
			Console.WriteLine()
		End If
		If intPercentComplete > 100 Then intPercentComplete = 100
		Console.Write("Processing: " & intPercentComplete.ToString() & "% ")
		If blnAddCarriageReturn Then
			Console.WriteLine()
		End If
	End Sub

	Private Function GetAppVersion() As String
		Return clsProcessFilesBaseClass.GetAppVersion(PROGRAM_DATE)
	End Function

	Private Function SetOptionsUsingCommandLineParameters(ByVal objParseCommandLine As clsParseCommandLine) As Boolean
		' Returns True if no problems; otherwise, returns false

		Dim strValue As String = String.Empty
		Dim lstValidParameters As List(Of String) = New List(Of String) From {"N", "Bytes", "P", "L", "LogFolder", "Q"}
		Dim intValue As Integer
		Dim intValue64 As Int64

		Try
			' Make sure no invalid parameters are present
			If objParseCommandLine.InvalidParametersPresent(lstValidParameters) Then
				ShowErrorMessage("Invalid commmand line parameters",
				  (From item In objParseCommandLine.InvalidParameters(lstValidParameters) Select "/" + item).ToList())
				Return False
			Else
				With objParseCommandLine
					' Query objParseCommandLine to see if various parameters are present

					If .NonSwitchParameterCount < 2 Then
						ShowErrorMessage("You must specify two files or two folders or a file match spec and a folder path or the word DMS followed by a dataset name")
						Return False
					End If

					mInputFileOrFolderPath = .RetrieveNonSwitchParameter(0)
					mComparisonFileOrFolderPath = .RetrieveNonSwitchParameter(1)

					If .RetrieveValueForParameter("P", strValue) Then mParameterFilePath = strValue

					If .RetrieveValueForParameter("N", strValue) Then
						If Integer.TryParse(strValue, intValue) Then
							mNumberOfSamples = intValue
						Else
							ShowErrorMessage("Non-numeric value: /N:" & strValue)
						End If
					End If

					If .RetrieveValueForParameter("Bytes", strValue) Then
						If Int64.TryParse(strValue, intValue64) Then
							mSampleSizeBytes = intValue64
						Else
							ShowErrorMessage("Non-numeric value: /Bytes:" & strValue)
						End If
					End If

					If .RetrieveValueForParameter("L", strValue) Then
						mLogMessagesToFile = True
						If Not String.IsNullOrEmpty(strValue) Then
							mLogFilePath = strValue
						End If
					End If

					If .RetrieveValueForParameter("LogFolder", strValue) Then
						mLogMessagesToFile = True
						If Not String.IsNullOrEmpty(strValue) Then
							mLogFolderPath = strValue
						End If
					End If
					If .RetrieveValueForParameter("Q", strValue) Then mQuietMode = True
				End With

				Return True
			End If

		Catch ex As Exception
			ShowErrorMessage("Error parsing the command line parameters: " & Environment.NewLine & ex.Message)
		End Try

		Return False

	End Function

	Private Sub ShowErrorMessage(ByVal strMessage As String)
		Const strSeparator As String = "------------------------------------------------------------------------------"

		Console.WriteLine()
		Console.WriteLine(strSeparator)
		Console.WriteLine(strMessage)
		Console.WriteLine(strSeparator)
		Console.WriteLine()

		WriteToErrorStream(strMessage)
	End Sub

	Private Sub ShowErrorMessage(ByVal strTitle As String, ByVal items As IEnumerable(Of String))
		Const strSeparator As String = "------------------------------------------------------------------------------"
		Dim strMessage As String

		Console.WriteLine()
		Console.WriteLine(strSeparator)
		Console.WriteLine(strTitle)
		strMessage = strTitle & ":"

		For Each item As String In items
			Console.WriteLine("   " + item)
			strMessage &= " " & item
		Next
		Console.WriteLine(strSeparator)
		Console.WriteLine()

		WriteToErrorStream(strMessage)
	End Sub

	Private Sub ShowProgramHelp()

		Try

			Console.WriteLine("This program compares two or more files (typically in separate folders) to check whether the " &
			   "start of the files match, the end of the files match, and selected sections inside the files also match. " &
			   "Useful for comparing large files without reading the entire file.  " &
			   "Alternatively, you can provide two folder paths and the program will compare all of the files in the first folder to the identically named files in the second folder.")
			Console.WriteLine()
			Console.WriteLine("Program syntax 1:" & Environment.NewLine & Path.GetFileName(clsProcessFilesBaseClass.GetAppPath()))
			Console.WriteLine(" FilePath1 FilePath2 [/N:NumberOfSamples] [/Bytes:SampleSizeBytes]")
			Console.WriteLine(" [/P:ParameterFilePath] [/Q]")
			Console.WriteLine(" [/L[:LogFilePath]] [/LogFolder:LogFolderPath]")

			Console.WriteLine()
			Console.WriteLine("Program syntax 2:" & Environment.NewLine & Path.GetFileName(clsProcessFilesBaseClass.GetAppPath()))
			Console.WriteLine(" FolderPath1 FolderPath2 [/N:NumberOfSamples]  [/Bytes:SampleSizeBytes]")
			Console.WriteLine(" [/P:ParameterFilePath] [/Q] [/L] [/LogFolder]")

			Console.WriteLine()
			Console.WriteLine("Program syntax 3:" & Environment.NewLine & Path.GetFileName(clsProcessFilesBaseClass.GetAppPath()))
			Console.WriteLine(" FileMatchSpec FolderPathToExamine [/N:NumberOfSamples]  [/Bytes:SampleSizeBytes]")
			Console.WriteLine(" [/P:ParameterFilePath] [/Q] [/L] [/LogFolder]")

			Console.WriteLine()
			Console.WriteLine("Program syntax 4:" & Environment.NewLine & Path.GetFileName(clsProcessFilesBaseClass.GetAppPath()))
			Console.WriteLine(" DMS DatasetNameToCheck [/N:NumberOfSamples]  [/Bytes:SampleSizeBytes]")
			Console.WriteLine(" [/P:ParameterFilePath] [/Q] [/L] [/LogFolder]")

			Console.WriteLine()
			Console.WriteLine("Use Syntax 1 to compare two files; in this case the filenames cannot have wildcards")
			Console.WriteLine("Use Syntax 2 to compare two folders (including all subfolders)")
			Console.WriteLine("Use Syntax 3 to compare a set of files in one folder to identically named files in a separate folder.  Use wildcards in FileMatchSpec to specify the files to examine")
			Console.WriteLine("Use Syntax 4 to compare a DMS dataset's files between the storage server and the archive.  The first word must be DMS; the second word is the Dataset Name.")

			Console.WriteLine()
			Console.WriteLine("Use /N to specify the number of portions of a file to examine.  The default is " & clsSampledFileComparer.DEFAULT_NUMBER_OF_SAMPLES & "; the minimum is 2, indicating the beginning and the end")
			Console.WriteLine("Use /Bytes to indicate the number of bytes to read from each file portion; default is " & clsSampledFileComparer.DEFAULT_SAMPLE_SIZE_BYTES & " bytes")
			Console.WriteLine()
			Console.WriteLine("The parameter file path is optional.  If included, it should point to a valid XML parameter file (currently ignored).")
			Console.WriteLine()
			Console.WriteLine("Use /L to log messages to a file.  Use the optional /Q switch will suppress all error messages.")
			Console.WriteLine()

			Console.WriteLine("Program written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA) in 2013")
			Console.WriteLine("Version: " & GetAppVersion())
			Console.WriteLine()

			Console.WriteLine("E-mail: matthew.monroe@pnnl.gov or matt@alchemistmatt.com")
			Console.WriteLine("Website: http://panomics.pnnl.gov/ or http://omics.pnl.gov")
			Console.WriteLine()

			' Delay for 750 msec in case the user double clicked this file from within Windows Explorer (or started the program via a shortcut)
			Threading.Thread.Sleep(750)

		Catch ex As Exception
			ShowErrorMessage("Error displaying the program syntax: " & ex.Message)
		End Try

	End Sub

	Private Sub WriteToErrorStream(strErrorMessage As String)
		Try
			Using swErrorStream As StreamWriter = New StreamWriter(Console.OpenStandardError())
				swErrorStream.WriteLine(strErrorMessage)
			End Using
		Catch ex As Exception
			' Ignore errors here
		End Try
	End Sub

	Private Sub mProcessingClass_ProgressChanged(ByVal taskDescription As String, ByVal percentComplete As Single) Handles mProcessingClass.ProgressChanged
		Const PERCENT_REPORT_INTERVAL As Integer = 25
		Const PROGRESS_DOT_INTERVAL_MSEC As Integer = 250

		If percentComplete >= mLastProgressReportValue Then
			If mLastProgressReportValue > 0 Then
				Console.WriteLine()
			End If
			DisplayProgressPercent(mLastProgressReportValue, False)
			mLastProgressReportValue += PERCENT_REPORT_INTERVAL
			mLastProgressReportTime = DateTime.UtcNow
		Else
			If DateTime.UtcNow.Subtract(mLastProgressReportTime).TotalMilliseconds > PROGRESS_DOT_INTERVAL_MSEC Then
				mLastProgressReportTime = DateTime.UtcNow
				Console.Write(".")
			End If
		End If
	End Sub

	Private Sub mProcessingClass_ProgressReset() Handles mProcessingClass.ProgressReset
		mLastProgressReportTime = DateTime.UtcNow
		mLastProgressReportValue = 0
	End Sub
End Module
