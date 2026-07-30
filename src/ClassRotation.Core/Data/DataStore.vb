Imports System.IO
Imports System.Text.Json
Imports ClassRotation.Domain

Namespace Data

    ''' <summary>
    ''' Loads and saves all application data as a single JSON document under the user's
    ''' application-data folder. Keeps the app dependency-free (no database required).
    ''' </summary>
    Public Class DataStore

        Private ReadOnly _path As String
        Private Shared ReadOnly _options As New JsonSerializerOptions With {
            .WriteIndented = True
        }

        Public ReadOnly Property FilePath As String
            Get
                Return _path
            End Get
        End Property

        Public Sub New(Optional filePath As String = Nothing)
            If String.IsNullOrWhiteSpace(filePath) Then
                Dim baseDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "ClassRotation")
                Directory.CreateDirectory(baseDir)
                _path = Path.Combine(baseDir, "class-rotation-data.json")
            Else
                _path = filePath
            End If
        End Sub

        Public Function Load() As AppData
            If Not File.Exists(_path) Then
                Return SeedData.CreateSample()
            End If
            Try
                Dim json = File.ReadAllText(_path)
                Dim data = JsonSerializer.Deserialize(Of AppData)(json, _options)
                data = If(data, New AppData())
                MigrateClasses(data)
                Return data
            Catch ex As Exception
                ' Corrupt or unreadable file: fall back to an empty data set rather than crashing.
                Return New AppData()
            End Try
        End Function

        ''' <summary>
        ''' Upgrades data saved before the two-phase model: a class that only has the legacy
        ''' StartDate/EndDate span (no academics phase set) has its academics phase populated,
        ''' and an OJT-typed class is treated as hands-on.
        ''' </summary>
        Private Shared Sub MigrateClasses(data As AppData)
            For Each c In data.Classes
                Dim hasPhase = c.AcademicsEnd > c.AcademicsStart
                If Not hasPhase AndAlso c.EndDate > c.StartDate Then
                    If c.Type = ClassType.OJT Then
                        c.AcademicsStart = c.StartDate
                        c.AcademicsEnd = c.StartDate
                        c.HasHandsOn = True
                        c.HandsOnStart = c.StartDate
                        c.HandsOnEnd = c.EndDate
                    Else
                        c.AcademicsStart = c.StartDate
                        c.AcademicsEnd = c.EndDate
                    End If
                    c.RecomputeSpan()
                End If
            Next
        End Sub

        Public Sub Save(data As AppData)
            Dim dir = Path.GetDirectoryName(_path)
            If Not String.IsNullOrEmpty(dir) Then Directory.CreateDirectory(dir)
            Dim json = JsonSerializer.Serialize(data, _options)
            File.WriteAllText(_path, json)
        End Sub

    End Class

End Namespace
