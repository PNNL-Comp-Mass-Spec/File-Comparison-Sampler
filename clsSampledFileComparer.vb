Option Strict On

' This class compares two files or two folders to check whether the
' start of the files match, the end of the files match, and selected sections inside the files also match
'
' -------------------------------------------------------------------------------
' Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA)
' Program started April 2, 2004
'
' E-mail: matthew.monroe@pnl.gov or matt@alchemistmatt.com
' Website: http://panomics.pnnl.gov/ or http://www.sysbio.org/resources/staff/
' -------------------------------------------------------------------------------
' 
' Licensed under the Apache License, Version 2.0; you may not use this file except
' in compliance with the License.  You may obtain a copy of the License at 
' http://www.apache.org/licenses/LICENSE-2.0

Public Class clsSampledFileComparer
	Inherits clsProcessFilesBaseClass

	Public Sub New()
		MyBase.mFileDate = "June 11, 2013"
		InitializeLocalVariables()
	End Sub

#Region "Constants and Enums"

	Public Const DEFAULT_NUMBER_OF_SAMPLES As Integer = 10
	Public Const DEFAULT_SAMPLE_SIZE_BYTES As Integer = 524288		' 512 KB

	Protected Const MINIMUM_SAMPLE_SIZE_BYTES As Integer = 64

	' Error codes specialized for this class
	Public Enum eFileComparerErrorCodes
		NoError = 0
		ErrorReadingInputFile = 1
		UnspecifiedError = -1
	End Enum
#End Region

#Region "Structures"
#End Region

#Region "Classwide Variables"

	Protected mNumberOfSamples As Integer
	Protected mSampleSizeBytes As Int64

	Protected mLastParameterDisplayValues As String = String.Empty

	Protected mLocalErrorCode As eFileComparerErrorCodes
#End Region

#Region "Interface Functions"

	Public ReadOnly Property LocalErrorCode() As eFileComparerErrorCodes
		Get
			Return mLocalErrorCode
		End Get
	End Property


	Public Property NumberOfSamples As Integer
		Get
			Return mNumberOfSamples
		End Get
		Set(value As Integer)
			If value < 2 Then value = 2
			mNumberOfSamples = value
		End Set
	End Property

	Public Property SampleSizeBytes As Int64
		Get
			Return mSampleSizeBytes
		End Get
		Set(value As Int64)
			If value < MINIMUM_SAMPLE_SIZE_BYTES Then value = MINIMUM_SAMPLE_SIZE_BYTES
			mSampleSizeBytes = value
		End Set
	End Property

#End Region

	Protected Function BytesToHumanReadable(intBytes As Int64) As String

		If intBytes < 10000 Then
			Return intBytes.ToString()
		Else
			Dim dblBytes As Double = intBytes
			Dim lstPrefixes As Generic.List(Of String) = New Generic.List(Of String) From {String.Empty, "KB", "MB", "GB", "TB", "PB"}
			Dim intPrefixIndex As Integer = 0

			While dblBytes >= 10000 AndAlso intPrefixIndex < lstPrefixes.Count
				dblBytes /= 1024.0
				intPrefixIndex += 1
			End While

			Return Math.Round(dblBytes, 0).ToString("0") & " " & lstPrefixes(intPrefixIndex)
		End If

	End Function


	''' <summary>
	''' Compares two files
	''' </summary>
	''' <param name="strInputFilePathBase"></param>
	''' <param name="strInputFilePathToCompare"></param>
	''' <returns>True if they Match; false if they do not match (same length, same beginning, same middle, and same end)</returns>
	''' <remarks></remarks>
	Public Function CompareFiles(ByVal strInputFilePathBase As String, ByVal strInputFilePathToCompare As String) As Boolean
		Return CompareFiles(strInputFilePathBase, strInputFilePathToCompare, intNumberOfSamples:=DEFAULT_NUMBER_OF_SAMPLES, intSampleSizeBytes:=DEFAULT_SAMPLE_SIZE_BYTES, blnShowMessageIfMatch:=True)
	End Function

	''' <summary>
	''' Compares two files
	''' </summary>
	''' <param name="strInputFilePathBase"></param>
	''' <param name="strInputFilePathToCompare"></param>
	''' <param name="intNumberOfSamples">Number of samples; minimum 2 (for beginning and end)</param>
	''' <returns>True if they Match; false if they do not match (same length, same beginning, same middle, and same end)</returns>
	''' <remarks></remarks>
	Public Function CompareFiles(ByVal strInputFilePathBase As String, ByVal strInputFilePathToCompare As String, ByVal intNumberOfSamples As Integer) As Boolean
		Return CompareFiles(strInputFilePathBase, strInputFilePathToCompare, intNumberOfSamples:=intNumberOfSamples, intSampleSizeBytes:=DEFAULT_SAMPLE_SIZE_BYTES, blnShowMessageIfMatch:=True)
	End Function

	''' <summary>
	''' Compares two files
	''' </summary>
	''' <param name="strInputFilePathBase"></param>
	''' <param name="strInputFilePathToCompare"></param>
	''' <param name="intNumberOfSamples">Number of samples; minimum 2 (for beginning and end)</param>
	''' <param name="intSampleSizeBytes">Bytes to compare for each sample (minimum 64 bytes)</param>
	''' <param name="blnShowMessageIfMatch">When true, then reports that files matched (always reports if files do not match)</param>
	''' <returns>True if they Match; false if they do not match (same length, same beginning, same middle, and same end)</returns>
	''' <remarks></remarks>
	Public Function CompareFiles(ByVal strInputFilePathBase As String, ByVal strInputFilePathToCompare As String, ByVal intNumberOfSamples As Integer, ByVal intSampleSizeBytes As Int64, ByVal blnShowMessageIfMatch As Boolean) As Boolean

		Const FIVE_HUNDRED_MB As Integer = 1024 * 1024 * 512

		Dim blnSuccess As Boolean

		Try
			If intNumberOfSamples < 2 Then intNumberOfSamples = 2
			If intSampleSizeBytes < MINIMUM_SAMPLE_SIZE_BYTES Then intSampleSizeBytes = MINIMUM_SAMPLE_SIZE_BYTES
			If intSampleSizeBytes > FIVE_HUNDRED_MB Then
				intSampleSizeBytes = FIVE_HUNDRED_MB
			End If

			If String.IsNullOrWhiteSpace(strInputFilePathBase) Then
				ShowErrorMessage("Base input file path is empty")
				MyBase.SetBaseClassErrorCode(clsProcessFilesBaseClass.eProcessFilesErrorCodes.InvalidInputFilePath)
				Return False
			ElseIf String.IsNullOrWhiteSpace(strInputFilePathToCompare) Then
				ShowErrorMessage("Input file path to compare is empty")
				MyBase.SetBaseClassErrorCode(clsProcessFilesBaseClass.eProcessFilesErrorCodes.InvalidInputFilePath)
				Return False
			End If

			LogMessage("Comparing " & System.IO.Path.GetFileName(strInputFilePathBase))
			Console.Write("Comparing " & System.IO.Path.GetFileName(strInputFilePathBase))

			Dim fiFilePathBase As IO.FileInfo = New IO.FileInfo(strInputFilePathBase)
			Dim fiFilePathToCompare As IO.FileInfo = New IO.FileInfo(strInputFilePathToCompare)
			Dim strComparisonResult As String = String.Empty

			If intNumberOfSamples * intSampleSizeBytes > fiFilePathBase.Length Then
				' Do a full comparison
				blnSuccess = CompareFilesComplete(fiFilePathBase, fiFilePathToCompare, strComparisonResult)
			Else
				blnSuccess = CompareFilesSampled(fiFilePathBase, fiFilePathToCompare, strComparisonResult, intNumberOfSamples, intSampleSizeBytes)
			End If


			If Not blnSuccess Then
				If String.IsNullOrEmpty(strComparisonResult) Then
					LogMessage("Files do not match: " & strInputFilePathBase & "  vs. " & strInputFilePathToCompare, eMessageTypeConstants.Warning)
					Console.WriteLine(" ... *** files do not match ***")
				Else
					LogMessage(strComparisonResult & ": " & strInputFilePathBase & "  vs. " & strInputFilePathToCompare, eMessageTypeConstants.Warning)
					Console.WriteLine(" ... *** " & strComparisonResult & " ***")
				End If

			ElseIf blnShowMessageIfMatch And Not String.IsNullOrEmpty(strComparisonResult) Then

				If String.IsNullOrEmpty(strComparisonResult) Then
					LogMessage("Files match: " & strInputFilePathBase & "  vs. " & strInputFilePathToCompare)
					Console.WriteLine(" ... files match")
				Else
					LogMessage(strComparisonResult & ": " & strInputFilePathBase & "  vs. " & strInputFilePathToCompare)
					Console.WriteLine(" ... " & strComparisonResult)
				End If

			End If

		Catch ex As Exception
			HandleException("Error in CompareFiles", ex)
			Return False
		End Try

		Return blnSuccess

	End Function

	''' <summary>
	''' Perform a full byte-by-byte comparison of the two files
	''' </summary>
	''' <param name="fiFilePathBase"></param>
	''' <param name="fiFilePathToCompare"></param>
	''' <param name="strComparisonResult">'Files Match' if the files match; otherwise user-readable description of why the files don't match</param>
	''' <returns>True if the files match; otherwise false</returns>
	''' <remarks></remarks>
	Public Function CompareFilesComplete(ByVal fiFilePathBase As IO.FileInfo, ByVal fiFilePathToCompare As IO.FileInfo, ByRef strComparisonResult As String) As Boolean

		If Not FileLengthsMatch(fiFilePathBase, fiFilePathToCompare, strComparisonResult) Then
			Return False
		End If

		Dim blnFilesMatch As Boolean = False

		Using brBaseFile As IO.BinaryReader = New IO.BinaryReader(New IO.FileStream(fiFilePathBase.FullName, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.ReadWrite))
			Using brComparisonFile As IO.BinaryReader = New IO.BinaryReader(New IO.FileStream(fiFilePathToCompare.FullName, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.ReadWrite))
				blnFilesMatch = CompareFileSection(brBaseFile, brComparisonFile, strComparisonResult, -1, -1, "Full comparison", System.DateTime.UtcNow)
			End Using
		End Using

		Return blnFilesMatch

	End Function

	''' <summary>
	''' Perform a full byte-by-byte comparison of a section of two files
	''' </summary>
	''' <param name="brBaseFile"></param>
	''' <param name="brComparisonFile"></param>
	''' <param name="strComparisonResult">'Files Match' if the files match; otherwise user-readable description of why the files don't match</param>
	''' <param name="intStartOffset">File offset to start the comparison; use -1 to specify that the entire file should be read</param>
	''' <param name="intSampleSizeBytes">Number of bytes to compare; ignored if intStartOffset is less than 0</param>
	''' <param name="strSampleDescription">Description of the current section of the file being compared</param>
	''' <param name="dtLastStatusTime">The last time that a status message was shown (UTC time)</param>
	''' <returns>True if the files match; otherwise false</returns>
	''' <remarks></remarks>
	Protected Function CompareFileSection(
	 ByVal brBaseFile As IO.BinaryReader,
	 ByVal brComparisonFile As IO.BinaryReader,
	 ByRef strComparisonResult As String,
	 ByVal intStartOffset As Int64,
	 ByVal intSampleSizeBytes As Int64,
	 ByVal strSampleDescription As String,
	 ByRef dtLastStatusTime As System.DateTime) As Boolean

		Const CHUNK_SIZE_BYTES As Integer = 1024 * 1024 * 50	' 50 MB

		Dim intEndOffset As Int64
		Dim intOffsetPriorToRead As Int64

		Dim intBytesToRead As Integer
		Dim bytFile1() As Byte
		Dim bytFile2() As Byte

		Dim dblPercentShown As Boolean = False

		Try

			strComparisonResult = String.Empty

			If intStartOffset < 0 Then
				' Compare the entire file						
				intEndOffset = brBaseFile.BaseStream.Length
			Else

				If intSampleSizeBytes < 1 Then intSampleSizeBytes = 1

				intEndOffset = intStartOffset + intSampleSizeBytes
				If intEndOffset > brBaseFile.BaseStream.Length Then
					intEndOffset = brBaseFile.BaseStream.Length
				End If

				If intStartOffset > 0 Then
					If intStartOffset > brBaseFile.BaseStream.Length Then
						strComparisonResult = "StartOffset is beyond the end of the base file"
						Return False
					ElseIf intStartOffset > brComparisonFile.BaseStream.Length Then
						strComparisonResult = "StartOffset is beyond the end of the comparison file"
						Return False
					End If

					brBaseFile.BaseStream.Position = intStartOffset
					brComparisonFile.BaseStream.Position = intStartOffset

				End If

			End If

			While brBaseFile.BaseStream.Position < brBaseFile.BaseStream.Length AndAlso brBaseFile.BaseStream.Position < intEndOffset
				intOffsetPriorToRead = brBaseFile.BaseStream.Position

				If brBaseFile.BaseStream.Position + CHUNK_SIZE_BYTES <= intEndOffset Then
					intBytesToRead = CHUNK_SIZE_BYTES
				Else
					intBytesToRead = CType(intEndOffset - brBaseFile.BaseStream.Position, Integer)
				End If

				If intBytesToRead = 0 Then
					Exit While
				End If

				bytFile1 = brBaseFile.ReadBytes(intBytesToRead)
				bytFile2 = brComparisonFile.ReadBytes(intBytesToRead)

				For intIndex As Integer = 0 To bytFile1.Length - 1
					If bytFile2(intIndex) <> bytFile1(intIndex) Then
						strComparisonResult = "Mismatch at offset " & intOffsetPriorToRead + intIndex
						Return False
					End If
				Next

				If System.DateTime.UtcNow.Subtract(dtLastStatusTime).TotalSeconds >= 5 Then
					dtLastStatusTime = System.DateTime.UtcNow

					Dim dblPercentComplete As Double = (brBaseFile.BaseStream.Position - intStartOffset) / (intEndOffset - intStartOffset) * 100

					If dblPercentComplete < 100 OrElse dblPercentShown Then
						dblPercentShown = True
						Console.WriteLine("   " & strSampleDescription & ", " & dblPercentComplete.ToString("0.0") & "%")
					Else
						Console.WriteLine("   " & strSampleDescription)
					End If

				End If
			End While

			strComparisonResult = "Files match"

		Catch ex As Exception
			HandleException("Error in CompareFileSection", ex)
			Return False
		End Try

		Return True
	End Function

	''' <summary>
	''' Compares the beginning, end, and optionally one or more middle sections of a file
	''' </summary>
	''' <param name="fiFilePathBase"></param>
	''' <param name="fiFilePathToCompare"></param>
	''' <param name="strComparisonResult">'Files Match' if the files match; otherwise user-readable description of why the files don't match</param>
	''' <param name="intNumberOfSamples">Number of samples; minimum 2 (for beginning and end)</param>
	''' <param name="intSampleSizeBytes">Bytes to compare for each sample (minimum 64 bytes)</param>
	''' <returns>True if the files match; otherwise false</returns>
	''' <remarks></remarks>
	Public Function CompareFilesSampled(ByVal fiFilePathBase As IO.FileInfo, ByVal fiFilePathToCompare As IO.FileInfo, ByRef strComparisonResult As String, ByVal intNumberOfSamples As Integer, ByVal intSampleSizeBytes As Int64) As Boolean

		Dim blnMatchAtStart As Boolean
		Dim blnMatchAtEnd As Boolean
		Dim blnMatchInMiddle As Boolean

		Dim strComparisonResultAtStart As String = String.Empty
		Dim strComparisonResultAtEnd As String = String.Empty

		Dim strSampleDescription As String
		Dim intSampleNumber As Integer = 0

		Dim intStartOffset As Int64
		Dim intBytesExamined As Int64 = 0

		Dim dtLastStatusTime As System.DateTime = System.DateTime.UtcNow()

		If Not FileLengthsMatch(fiFilePathBase, fiFilePathToCompare, strComparisonResult) Then
			Return False
		End If

		Using brBaseFile As IO.BinaryReader = New IO.BinaryReader(New IO.FileStream(fiFilePathBase.FullName, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.ReadWrite))
			Using brComparisonFile As IO.BinaryReader = New IO.BinaryReader(New IO.FileStream(fiFilePathToCompare.FullName, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.ReadWrite))

				intStartOffset = 0
				intBytesExamined += intSampleSizeBytes

				intSampleNumber += 1
				strSampleDescription = "Sample " & intSampleNumber & " of " & intNumberOfSamples.ToString
				dtLastStatusTime = System.DateTime.UtcNow()

				blnMatchAtStart = CompareFileSection(brBaseFile, brComparisonFile, strComparisonResultAtStart, intStartOffset, intSampleSizeBytes, strSampleDescription, dtLastStatusTime)

				' Update the start offset to be SampleSizeBytes before the end of the file
				intStartOffset = fiFilePathBase.Length - intSampleSizeBytes
				If intStartOffset < 0 Then
					intBytesExamined += intSampleSizeBytes + intStartOffset
					intStartOffset = 0
				Else
					intBytesExamined += intSampleSizeBytes
				End If

				intSampleNumber += 1
				strSampleDescription = "Sample " & intSampleNumber & " of " & intNumberOfSamples.ToString

				blnMatchAtEnd = CompareFileSection(brBaseFile, brComparisonFile, strComparisonResultAtEnd, intStartOffset, intSampleSizeBytes, strSampleDescription, dtLastStatusTime)

				If blnMatchAtStart And Not blnMatchAtEnd Then
					strComparisonResult = "Files match at the beginning but not at the end; " & strComparisonResultAtEnd
					Return False
				ElseIf blnMatchAtEnd And Not blnMatchAtStart Then
					strComparisonResult = "Files match at the end but not at the beginning; " & strComparisonResultAtStart
					Return False
				End If

				If intNumberOfSamples > 2 AndAlso fiFilePathBase.Length > intSampleSizeBytes * 2 Then

					Dim intMidSectionSamples As Integer = intNumberOfSamples - 2
					Dim dblSeekLength As Double = fiFilePathBase.Length / (intMidSectionSamples + 1)
					Dim dblCurrentOffset As Double = dblSeekLength - intSampleSizeBytes / 2

					Do While dblCurrentOffset < fiFilePathBase.Length
						intStartOffset = CType(Math.Round(dblCurrentOffset, 0), Int64)
						If intStartOffset < 0 Then intStartOffset = 0

						intSampleNumber += 1
						strSampleDescription = "Sample " & intSampleNumber & " of " & intNumberOfSamples.ToString

						blnMatchInMiddle = CompareFileSection(brBaseFile, brComparisonFile, strComparisonResult, intStartOffset, intSampleSizeBytes, strSampleDescription, dtLastStatusTime)

						If Not blnMatchInMiddle Then
							strComparisonResult = "Files match at the beginning and end, but not in the middle; " & strComparisonResult
							Return False
						End If

						dblCurrentOffset += dblSeekLength
						intBytesExamined += intSampleSizeBytes
					Loop

				End If
			End Using
		End Using


		Dim dblPercentExamined As Double = intBytesExamined / fiFilePathBase.Length * 100.0
		If dblPercentExamined > 100 Then dblPercentExamined = 100
		strComparisonResult = "Files match (examined " & dblPercentExamined.ToString("0.00") & "% of the file)"

		Return True

	End Function

	''' <summary>
	''' Compares each file in folder strInputFolderPath1 to files in folder strInputFolderPath2
	''' </summary>
	''' <param name="strInputFolderPath1"></param>
	''' <param name="strInputFolderPath2"></param>
	''' <param name="intNumberOfSamples">Number of samples; minimum 2 (for beginning and end)</param>
	''' <param name="intSampleSizeBytes">Bytes to compare for each sample (minimum 64 bytes)</param>
	''' <returns>True if the folders Match; false if they do not match</returns>
	''' <remarks></remarks>
	Public Function CompareFolders(ByVal strInputFolderPath1 As String, ByVal strInputFolderPath2 As String, ByVal intNumberOfSamples As Integer, intSampleSizeBytes As Int64) As Boolean

		Dim intSourceFilesFound As Integer = 0
		Dim intMatchedFileCount As Integer = 0

		Dim intMissingFileCount As Integer = 0
		Dim intMismatchedFileCount As Integer = 0

		Try

			strInputFolderPath1 = strInputFolderPath1.TrimEnd(IO.Path.DirectorySeparatorChar)
			strInputFolderPath2 = strInputFolderPath2.TrimEnd(IO.Path.DirectorySeparatorChar)

			Dim diBaseFolder As IO.DirectoryInfo = New IO.DirectoryInfo(strInputFolderPath1)

			If Not diBaseFolder.Exists Then
				ShowErrorMessage("Base folder to compare not found: " & strInputFolderPath1)
				SetBaseClassErrorCode(eProcessFilesErrorCodes.InvalidInputFilePath)
				Return False
			End If

			Dim diComparisonFolder As IO.DirectoryInfo = New IO.DirectoryInfo(strInputFolderPath2)

			If Not diComparisonFolder.Exists Then
				ShowErrorMessage("Comparison folder not found: " & strInputFolderPath2)
				SetBaseClassErrorCode(eProcessFilesErrorCodes.InvalidInputFilePath)
				Return False
			End If

			ShowMessage("Comparing folders: " & diBaseFolder.FullName)
			ShowMessage("               vs. " & diComparisonFolder.FullName)

			Console.WriteLine()
			ShowParameters()

			For Each fiFile In diBaseFolder.GetFiles("*.*", IO.SearchOption.AllDirectories)
				' Look for the corresponding item in the comparison folder

				Dim fiComparisonFile As IO.FileInfo

				If fiFile.Directory.FullName = diBaseFolder.FullName Then
					fiComparisonFile = New IO.FileInfo(IO.Path.Combine(strInputFolderPath2, fiFile.Name))
				Else

					Dim strSubdirectoryAddon As String
					strSubdirectoryAddon = fiFile.Directory.FullName.Substring(diBaseFolder.FullName.Length + 1)
					fiComparisonFile = New IO.FileInfo(IO.Path.Combine(strInputFolderPath2, strSubdirectoryAddon, fiFile.Name))
				End If

				If Not fiComparisonFile.Exists Then
					ShowMessage("  File " & fiFile.Name & " not found in the comparison folder")
					intMissingFileCount += 1
				Else
					If CompareFiles(fiFile.FullName, fiComparisonFile.FullName, intNumberOfSamples, intSampleSizeBytes, True) Then
						intMatchedFileCount += 1
					Else
						intMismatchedFileCount += 1
					End If

				End If

				intSourceFilesFound += 1
			Next

		Catch ex As Exception
			HandleException("Error in CompareFolders", ex)
			Return False
		End Try

		Console.WriteLine()
		If intSourceFilesFound = 0 Then
			ShowErrorMessage("Base folder was empty; nothing to compare: " & strInputFolderPath1)
			Return True

		ElseIf intMissingFileCount = 0 AndAlso intMismatchedFileCount = 0 Then
			ShowMessage("Folders match; checked " & intSourceFilesFound & " file(s)")
			Return True

		ElseIf intMissingFileCount = 0 AndAlso intMismatchedFileCount > 0 Then
			ShowMessage("Folders do not match; Mis-matched file count: " & intMismatchedFileCount & "; Matched file count: " & intMatchedFileCount)
			Return False

		ElseIf intMissingFileCount > 0 Then
			ShowMessage("Comparison folder is missing " & intMissingFileCount & " file(s) that the base folder contains")
			ShowMessage("Mis-matched file count: " & intMismatchedFileCount & "; Matched file count: " & intMatchedFileCount)
			Return False

		Else
			Console.WriteLine("Note: unexpected logic encountered in If-Else-EndIf block in CompareFolders")

			ShowMessage("Folders do not match; Mis-matched file count: " & intMismatchedFileCount & "; Matched file count: " & intMatchedFileCount)
			Return False
		End If

	End Function

	Public Function FileLengthsMatch(ByVal fiFilePathBase As IO.FileInfo, ByVal fiFilePathToCompare As IO.FileInfo, ByRef strComparisonResult As String) As Boolean

		If Not fiFilePathBase.Exists() Then
			strComparisonResult = "Base file to compare not found: " & fiFilePathBase.FullName
			Return False
		ElseIf Not fiFilePathToCompare.Exists() Then
			strComparisonResult = "Comparison file not found: " & fiFilePathToCompare.FullName
			Return False
		End If

		If fiFilePathBase.Length <> fiFilePathToCompare.Length Then
			strComparisonResult = "Base file is " & (fiFilePathBase.Length / 1024.0).ToString("#,##0.0") & " KB; comparison file is " & (fiFilePathToCompare.Length / 1024.0).ToString("#,##0.0") & " KB"
			Return False
		Else
			Return True
		End If

	End Function

	Public Overrides Function GetErrorMessage() As String
		' Returns "" if no error

		Dim strErrorMessage As String

		If MyBase.ErrorCode = clsProcessFilesBaseClass.eProcessFilesErrorCodes.LocalizedError Or _
		   MyBase.ErrorCode = clsProcessFilesBaseClass.eProcessFilesErrorCodes.NoError Then
			Select Case mLocalErrorCode
				Case eFileComparerErrorCodes.NoError
					strErrorMessage = ""
				Case eFileComparerErrorCodes.ErrorReadingInputFile
					strErrorMessage = "Error reading input file"
				Case Else
					' This shouldn't happen
					strErrorMessage = "Unknown error state"
			End Select
		Else
			strErrorMessage = MyBase.GetBaseClassErrorMessage()
		End If

		Return strErrorMessage

	End Function

	Private Sub InitializeLocalVariables()

		mNumberOfSamples = DEFAULT_NUMBER_OF_SAMPLES
		mSampleSizeBytes = DEFAULT_SAMPLE_SIZE_BYTES

		mLocalErrorCode = eFileComparerErrorCodes.NoError
	End Sub

	Private Function LoadParameterFileSettings(ByVal strParameterFilePath As String) As Boolean

		Const OPTIONS_SECTION As String = "Options"

		Dim objSettingsFile As New XmlSettingsFileAccessor

		Try

			If strParameterFilePath Is Nothing OrElse strParameterFilePath.Length = 0 Then
				' No parameter file specified; nothing to load
				Return True
			End If

			If Not System.IO.File.Exists(strParameterFilePath) Then
				' See if strParameterFilePath points to a file in the same directory as the application
				strParameterFilePath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), _
					System.IO.Path.GetFileName(strParameterFilePath))
				If Not System.IO.File.Exists(strParameterFilePath) Then
					MyBase.SetBaseClassErrorCode(clsProcessFilesBaseClass.eProcessFilesErrorCodes.ParameterFileNotFound)
					Return False
				End If
			End If

			If objSettingsFile.LoadSettings(strParameterFilePath) Then
				If Not objSettingsFile.SectionPresent(OPTIONS_SECTION) Then
					If MyBase.ShowMessages Then
						Console.WriteLine("The node '<section name=""" & OPTIONS_SECTION & """> was not found in the parameter file: " & strParameterFilePath)
					End If
					MyBase.SetBaseClassErrorCode(clsProcessFilesBaseClass.eProcessFilesErrorCodes.InvalidParameterFile)
					Return False
				Else
					' Me.SettingName = objSettingsFile.GetParam(OPTIONS_SECTION, "HeaderLineChar", Me.SettingName)
				End If
			End If

		Catch ex As Exception
			HandleException("Error in LoadParameterFileSettings", ex)
			Return False
		End Try

		Return True

	End Function

	Private Function LookupDatasetFolderPaths(ByVal strDatasetName As String, ByRef strStorageServerFolderPath As String, ByRef strArchiveFolderPath As String) As Boolean
		Return LookupDatasetFolderPaths(strDatasetName, strStorageServerFolderPath, strArchiveFolderPath, "Gigasax", "DMS5")
	End Function

	Private Function LookupDatasetFolderPaths(
	  ByVal strDatasetName As String,
	  ByRef strStorageServerFolderPath As String,
	  ByRef strArchiveFolderPath As String,
	  ByVal strDMSServer As String,
	  ByVal strDMSDatabase As String) As Boolean

		Dim blnSuccess As Boolean = False

		Try
			Dim ConnectionString As String = "Data Source=" + strDMSServer + ";Initial Catalog=" + strDMSDatabase + ";Integrated Security=SSPI;"

			strStorageServerFolderPath = String.Empty
			strArchiveFolderPath = String.Empty

			Using Cn As New System.Data.SqlClient.SqlConnection(ConnectionString)

				Dim SqlStr As New System.Text.StringBuilder()

				SqlStr.Append(" SELECT Dataset_Folder_Path, Archive_Folder_Path ")
				SqlStr.Append(" FROM V_Dataset_Folder_Paths")
				SqlStr.Append(" WHERE (Dataset = '" & strDatasetName & "')")

				Cn.Open()

				Using command As System.Data.SqlClient.SqlCommand = New System.Data.SqlClient.SqlCommand(SqlStr.ToString, Cn)

					Using reader As System.Data.SqlClient.SqlDataReader = command.ExecuteReader()

						' Call Read to read the first row
						If reader.Read() Then
							strStorageServerFolderPath = reader.GetString(0)
							strArchiveFolderPath = reader.GetString(1)

							If String.IsNullOrEmpty(strStorageServerFolderPath) Then
								ShowErrorMessage("Dataset '" & strDatasetName & "' has an empty Dataset_Folder_Path (using " & strDMSDatabase & ".dbo.V_Dataset_Folder_Paths on server " & strDMSServer & ")")
							ElseIf String.IsNullOrEmpty(strArchiveFolderPath) Then
								ShowErrorMessage("Dataset '" & strDatasetName & "' has an empty Archive_Folder_Path (using " & strDMSDatabase & ".dbo.V_Dataset_Folder_Paths on server " & strDMSServer & ")")
							Else
								blnSuccess = True
							End If
						Else
							ShowErrorMessage("Dataset '" & strDatasetName & "' not found in " & strDMSDatabase & ".dbo.V_Dataset_Folder_Paths on server " & strDMSServer)
						End If

					End Using
				End Using

			End Using


		Catch ex As Exception
			HandleException("Error in LookupDatasetFolderPaths", ex)
			Return False
		End Try

		Return blnSuccess

	End Function

	Public Function ProcessDMSDataset(ByVal strDatasetName As String, ByVal strParameterFilePath As String) As Boolean

		Dim strStorageServerFolderPath As String = String.Empty
		Dim strArchiveFolderPath As String = String.Empty

		If Not LookupDatasetFolderPaths(strDatasetName, strStorageServerFolderPath, strArchiveFolderPath) Then
			Return False
		End If

		Return ProcessFile(strStorageServerFolderPath, strArchiveFolderPath, strParameterFilePath, True)

	End Function

	''' <summary>
	''' Compares the two specified files or two specified folders
	''' </summary>
	''' <param name="strInputFilePath">Base file or folder to read</param>
	''' <param name="strOutputFolderPath">File or folder to compare to strInputFilePath</param>
	''' <param name="strParameterFilePath"></param>
	''' <param name="blnResetErrorCode"></param>
	''' <returns></returns>
	''' <remarks>If strInputFilePath is a file but strOutputFolderPath is a folder, then looks for a file named strInputFilePath in folder strOutputFolderPath</remarks>
	Public Overloads Overrides Function ProcessFile(ByVal strInputFilePath As String, ByVal strOutputFolderPath As String, ByVal strParameterFilePath As String, ByVal blnResetErrorCode As Boolean) As Boolean

		Try

			If blnResetErrorCode Then
				SetLocalErrorCode(eFileComparerErrorCodes.NoError)
			End If

			Console.WriteLine()

			If Not LoadParameterFileSettings(strParameterFilePath) Then
				ShowErrorMessage("Parameter file load error: " & strParameterFilePath)
				If MyBase.ErrorCode = clsProcessFilesBaseClass.eProcessFilesErrorCodes.NoError Then
					MyBase.SetBaseClassErrorCode(clsProcessFilesBaseClass.eProcessFilesErrorCodes.InvalidParameterFile)
				End If
				Return False
			End If

			If String.IsNullOrWhiteSpace(strInputFilePath) Then
				ShowErrorMessage("Base file path to compare is empty; unable to continue")
				SetBaseClassErrorCode(eProcessFilesErrorCodes.InvalidInputFilePath)
				Return False
			End If

			Dim diBaseFolder As IO.DirectoryInfo = New IO.DirectoryInfo(strInputFilePath)

			If diBaseFolder.Exists Then

				' Comparing folder contents
				If String.IsNullOrWhiteSpace(strOutputFolderPath) Then
					ShowErrorMessage("Base item is a folder (" & strInputFilePath & "), but the comparison item is empty; unable to continue")
					SetBaseClassErrorCode(eProcessFilesErrorCodes.InvalidInputFilePath)
					Return False
				End If

				Dim diComparisonFolder As IO.DirectoryInfo = New IO.DirectoryInfo(strOutputFolderPath)
				If Not diComparisonFolder.Exists Then
					ShowErrorMessage("Base item is a folder (" & strInputFilePath & "), but the comparison folder was not found: " & strOutputFolderPath)
					SetBaseClassErrorCode(eProcessFilesErrorCodes.InvalidInputFilePath)
					Return False
				End If

				Return CompareFolders(diBaseFolder.FullName, diComparisonFolder.FullName, mNumberOfSamples, mSampleSizeBytes)

			Else
				' Comparing files

				Dim fiBaseFile As IO.FileInfo = New IO.FileInfo(strInputFilePath)

				If Not fiBaseFile.Exists Then
					ShowErrorMessage("Base file to compare not found: " & strInputFilePath)
					SetBaseClassErrorCode(eProcessFilesErrorCodes.InvalidInputFilePath)
					Return False
				End If

				If String.IsNullOrWhiteSpace(strOutputFolderPath) Then
					ShowErrorMessage("Base item is a file (" & strInputFilePath & "), but the comparison item is empty; unable to continue")
					SetBaseClassErrorCode(eProcessFilesErrorCodes.InvalidInputFilePath)
					Return False
				End If

				Dim diComparisonFolder As IO.DirectoryInfo = New IO.DirectoryInfo(strOutputFolderPath)
				Dim strComparisonFilePath As String

				If diComparisonFolder.Exists Then
					' Will look for a file in the comparison folder with the same name as the input file
					strComparisonFilePath = IO.Path.Combine(diComparisonFolder.FullName, IO.Path.GetFileName(strInputFilePath))
				Else
					Dim fiComparisonFile As IO.FileInfo = New IO.FileInfo(strOutputFolderPath)
					If Not fiComparisonFile.Exists Then
						ShowErrorMessage("Base item is a file (" & strInputFilePath & "), but the comparison item was not found: " & strOutputFolderPath)
						SetBaseClassErrorCode(eProcessFilesErrorCodes.InvalidInputFilePath)
						Return False
					Else
						strComparisonFilePath = fiComparisonFile.FullName
					End If
				End If

				ShowParameters()

				Return CompareFiles(strInputFilePath, strComparisonFilePath, mNumberOfSamples, mSampleSizeBytes, blnShowMessageIfMatch:=True)
			End If


		Catch ex As Exception
			HandleException("Error in ProcessFile", ex)
			Return False
		End Try

	End Function

	Private Sub SetLocalErrorCode(ByVal eNewErrorCode As eFileComparerErrorCodes)
		SetLocalErrorCode(eNewErrorCode, False)
	End Sub

	Private Sub SetLocalErrorCode(ByVal eNewErrorCode As eFileComparerErrorCodes, ByVal blnLeaveExistingErrorCodeUnchanged As Boolean)

		If blnLeaveExistingErrorCodeUnchanged AndAlso mLocalErrorCode <> eFileComparerErrorCodes.NoError Then
			' An error code is already defined; do not change it
		Else
			mLocalErrorCode = eNewErrorCode

			If eNewErrorCode = eFileComparerErrorCodes.NoError Then
				If MyBase.ErrorCode = clsProcessFilesBaseClass.eProcessFilesErrorCodes.LocalizedError Then
					MyBase.SetBaseClassErrorCode(clsProcessFilesBaseClass.eProcessFilesErrorCodes.NoError)
				End If
			Else
				MyBase.SetBaseClassErrorCode(clsProcessFilesBaseClass.eProcessFilesErrorCodes.LocalizedError)
			End If
		End If

	End Sub

	Protected Sub ShowParameters()
		Dim strDisplayValues As String

		strDisplayValues = mNumberOfSamples & "_" & mSampleSizeBytes

		If mLastParameterDisplayValues = strDisplayValues Then
			' Values have already been displayed
		Else
			ShowMessage("Number of samples: " & mNumberOfSamples)
			ShowMessage("Sample Size:       " & BytesToHumanReadable(mSampleSizeBytes))
			Console.WriteLine()
			mLastParameterDisplayValues = strDisplayValues
		End If

	End Sub
End Class
