Imports System.Collections.Generic
Imports System.Globalization
Imports System.IO
Imports System.Linq
Imports ClosedXML.Excel
Imports ClassRotation.Domain

Namespace Data

    ''' <summary>Outcome of importing an Excel workbook.</summary>
    Public Class ExcelImportResult
        Public Property ClassesAdded As Integer
        Public Property ClassesUpdated As Integer
        Public Property PeriodsImported As Integer
        Public Property Warnings As New List(Of String)

        Public ReadOnly Property Summary As String
            Get
                Dim s = $"Imported {ClassesAdded} new and {ClassesUpdated} updated class(es), {PeriodsImported} scheduled period(s)."
                If Warnings.Count > 0 Then s &= $" {Warnings.Count} warning(s)."
                Return s
            End Get
        End Property
    End Class

    ''' <summary>
    ''' Reads and writes the editable workbook used to view/edit the schedule off-app.
    ''' The workbook has a "Classes" sheet (one row per class, with Academics/Hands-on phase
    ''' dates) and a "Schedule" sheet (one row per period). Import and Export use the same
    ''' column layout so data round-trips between Excel and the application.
    ''' </summary>
    Public Module ExcelIO

        Private Const ClassesSheet As String = "Classes"
        Private Const ScheduleSheet As String = "Schedule"

        Private ReadOnly ClassHeaders As String() = {
            "Name", "AcademicsStart", "AcademicsEnd", "HasHandsOn",
            "HandsOnStart", "HandsOnEnd", "Instructor", "RequiredPrerequisites"}
        Private ReadOnly ScheduleHeaders As String() = {
            "Class", "Date", "StartTime", "EndTime", "Phase",
            "Subject", "Instructor", "Room", "Notes"}

        ' ---------------- Export ----------------

        Public Sub Export(data As AppData, filePath As String)
            Dim insName = data.Instructors.ToDictionary(Function(i) i.Id, Function(i) i.Name)
            Dim prereqName = data.Prerequisites.ToDictionary(Function(p) p.Id, Function(p) p.Name)

            Using wb As New XLWorkbook()
                Dim cs = wb.Worksheets.Add(ClassesSheet)
                WriteHeader(cs, ClassHeaders)
                Dim r = 2
                For Each c In data.Classes.OrderBy(Function(x) x.OverallStart)
                    cs.Cell(r, 1).Value = c.Name
                    SetDate(cs.Cell(r, 2), c.AcademicsStart)
                    SetDate(cs.Cell(r, 3), c.AcademicsEnd)
                    cs.Cell(r, 4).Value = If(c.HasHandsOn, "TRUE", "FALSE")
                    If c.HasHandsOn Then
                        SetDate(cs.Cell(r, 5), c.HandsOnStart)
                        SetDate(cs.Cell(r, 6), c.HandsOnEnd)
                    End If
                    cs.Cell(r, 7).Value = LookupName(insName, c.InstructorId)
                    cs.Cell(r, 8).Value = String.Join("; ",
                        c.RequiredPrerequisiteIds.Select(Function(id) LookupName(prereqName, id)).Where(Function(n) n <> ""))
                    r += 1
                Next
                cs.Columns().AdjustToContents()
                cs.SheetView.FreezeRows(1)

                Dim ss = wb.Worksheets.Add(ScheduleSheet)
                WriteHeader(ss, ScheduleHeaders)
                Dim sr = 2
                For Each c In data.Classes.OrderBy(Function(x) x.OverallStart)
                    For Each dy In c.Schedule.OrderBy(Function(d) d.Date)
                        For Each p In dy.Periods
                            ss.Cell(sr, 1).Value = c.Name
                            SetDate(ss.Cell(sr, 2), dy.Date)
                            ss.Cell(sr, 3).Value = p.StartTime
                            ss.Cell(sr, 4).Value = p.EndTime
                            ss.Cell(sr, 5).Value = PhaseToText(p.Phase)
                            ss.Cell(sr, 6).Value = p.Subject
                            ss.Cell(sr, 7).Value = LookupName(insName, p.InstructorId)
                            ss.Cell(sr, 8).Value = p.Room
                            ss.Cell(sr, 9).Value = p.Notes
                            sr += 1
                        Next
                    Next
                Next
                ss.Columns().AdjustToContents()
                ss.SheetView.FreezeRows(1)

                Dim dir = Path.GetDirectoryName(filePath)
                If Not String.IsNullOrEmpty(dir) Then Directory.CreateDirectory(dir)
                wb.SaveAs(filePath)
            End Using
        End Sub

        ' ---------------- Import ----------------

        Public Function Import(filePath As String, data As AppData) As ExcelImportResult
            Dim result As New ExcelImportResult()
            Using wb As New XLWorkbook(filePath)
                Dim cs = FindSheet(wb, ClassesSheet)
                If cs IsNot Nothing Then ImportClasses(cs, data, result)

                Dim ss = FindSheet(wb, ScheduleSheet)
                If ss IsNot Nothing Then ImportSchedule(ss, data, result)
            End Using
            Return result
        End Function

        Private Sub ImportClasses(ws As IXLWorksheet, data As AppData, result As ExcelImportResult)
            Dim col = HeaderMap(ws)
            If Not col.ContainsKey("name") Then
                result.Warnings.Add("Classes sheet has no 'Name' column; skipped.")
                Return
            End If

            For Each row In DataRows(ws)
                Dim name = GetString(row, col, "name").Trim()
                If name = "" Then Continue For

                Dim existing = data.Classes.FirstOrDefault(
                    Function(c) String.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase))
                Dim cls = existing
                If cls Is Nothing Then
                    cls = New CourseClass With {.Name = name}
                    data.Classes.Add(cls)
                    result.ClassesAdded += 1
                Else
                    result.ClassesUpdated += 1
                End If

                Dim ds As Date
                If TryGetDate(row, col, "academicsstart", ds) Then cls.AcademicsStart = ds
                If TryGetDate(row, col, "academicsend", ds) Then cls.AcademicsEnd = ds
                cls.HasHandsOn = ParseBool(GetString(row, col, "hashandson"))
                If TryGetDate(row, col, "handsonstart", ds) Then cls.HandsOnStart = ds
                If TryGetDate(row, col, "handsonend", ds) Then cls.HandsOnEnd = ds

                Dim insName = GetString(row, col, "instructor").Trim()
                If insName <> "" AndAlso Not String.Equals(insName, "(unassigned)", StringComparison.OrdinalIgnoreCase) Then
                    cls.InstructorId = ResolveInstructor(data, insName).Id
                End If

                Dim prereqText = GetString(row, col, "requiredprerequisites")
                If prereqText <> "" Then
                    cls.RequiredPrerequisiteIds.Clear()
                    For Each pName In SplitList(prereqText)
                        cls.RequiredPrerequisiteIds.Add(ResolvePrerequisite(data, pName).Id)
                    Next
                End If

                cls.RecomputeSpan()
            Next
        End Sub

        Private Sub ImportSchedule(ws As IXLWorksheet, data As AppData, result As ExcelImportResult)
            Dim col = HeaderMap(ws)
            If Not col.ContainsKey("class") OrElse Not col.ContainsKey("date") Then
                result.Warnings.Add("Schedule sheet needs 'Class' and 'Date' columns; skipped.")
                Return
            End If

            ' Classes whose schedule we've begun replacing this import.
            Dim cleared As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)

            For Each row In DataRows(ws)
                Dim className = GetString(row, col, "class").Trim()
                If className = "" Then Continue For

                Dim cls = data.Classes.FirstOrDefault(
                    Function(c) String.Equals(c.Name, className, StringComparison.OrdinalIgnoreCase))
                If cls Is Nothing Then
                    result.Warnings.Add($"Schedule references unknown class '{className}'; row skipped.")
                    Continue For
                End If

                Dim dt As Date
                If Not TryGetDate(row, col, "date", dt) Then
                    result.Warnings.Add($"Schedule row for '{className}' has an unreadable date; skipped.")
                    Continue For
                End If

                If cleared.Add(cls.Name) Then cls.Schedule.Clear()

                Dim dy = cls.Schedule.FirstOrDefault(Function(d) d.Date.Date = dt.Date)
                If dy Is Nothing Then
                    dy = New ClassDay With {.Date = dt.Date}
                    cls.Schedule.Add(dy)
                End If

                Dim insName = GetString(row, col, "instructor").Trim()
                Dim insId = cls.InstructorId
                If insName <> "" AndAlso Not String.Equals(insName, "(unassigned)", StringComparison.OrdinalIgnoreCase) Then
                    insId = ResolveInstructor(data, insName).Id
                End If

                dy.Periods.Add(New ClassPeriod With {
                    .StartTime = NormalizeTime(GetString(row, col, "starttime")),
                    .EndTime = NormalizeTime(GetString(row, col, "endtime")),
                    .Phase = ParsePhase(GetString(row, col, "phase")),
                    .Subject = GetString(row, col, "subject").Trim(),
                    .InstructorId = insId,
                    .Room = GetString(row, col, "room").Trim(),
                    .Notes = GetString(row, col, "notes").Trim()})
                result.PeriodsImported += 1
            Next

            For Each cls In data.Classes.Where(Function(c) cleared.Contains(c.Name))
                cls.Schedule.Sort(Function(a, b) a.Date.CompareTo(b.Date))
            Next
        End Sub

        ' ---------------- Helpers ----------------

        Private Sub WriteHeader(ws As IXLWorksheet, headers As String())
            For i = 0 To headers.Length - 1
                Dim cell = ws.Cell(1, i + 1)
                cell.Value = headers(i)
                cell.Style.Font.Bold = True
            Next
        End Sub

        Private Sub SetDate(cell As IXLCell, d As Date)
            cell.Value = d
            cell.Style.DateFormat.Format = "yyyy-MM-dd"
        End Sub

        Private Function LookupName(map As Dictionary(Of String, String), id As String) As String
            Dim name As String = Nothing
            If id IsNot Nothing AndAlso map.TryGetValue(id, name) Then Return name
            Return ""
        End Function

        Private Function FindSheet(wb As XLWorkbook, name As String) As IXLWorksheet
            Return wb.Worksheets.FirstOrDefault(
                Function(w) String.Equals(w.Name, name, StringComparison.OrdinalIgnoreCase))
        End Function

        Private Function HeaderMap(ws As IXLWorksheet) As Dictionary(Of String, Integer)
            Dim map As New Dictionary(Of String, Integer)(StringComparer.OrdinalIgnoreCase)
            Dim header = ws.Row(1)
            For Each cell In header.CellsUsed()
                Dim key = cell.GetString().Trim().Replace(" ", "").ToLowerInvariant()
                If key <> "" AndAlso Not map.ContainsKey(key) Then map(key) = cell.Address.ColumnNumber
            Next
            Return map
        End Function

        Private Iterator Function DataRows(ws As IXLWorksheet) As IEnumerable(Of IXLRow)
            Dim used = ws.RangeUsed()
            If used Is Nothing Then Return
            Dim last = used.LastRow().RowNumber()
            For rn = 2 To last
                Yield ws.Row(rn)
            Next
        End Function

        Private Function GetString(row As IXLRow, col As Dictionary(Of String, Integer), key As String) As String
            Dim idx As Integer
            If Not col.TryGetValue(key, idx) Then Return ""
            Return row.Cell(idx).GetString()
        End Function

        Private Function TryGetDate(row As IXLRow, col As Dictionary(Of String, Integer), key As String, ByRef value As Date) As Boolean
            Dim idx As Integer
            If Not col.TryGetValue(key, idx) Then Return False
            Dim cell = row.Cell(idx)
            If cell.IsEmpty() Then Return False
            Dim dt As DateTime
            If cell.TryGetValue(Of DateTime)(dt) Then
                value = dt
                Return True
            End If
            Return Date.TryParse(cell.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.None, value) OrElse
                   Date.TryParse(cell.GetString(), value)
        End Function

        Private Function ParseBool(s As String) As Boolean
            s = s.Trim().ToLowerInvariant()
            Return s = "true" OrElse s = "yes" OrElse s = "y" OrElse s = "1" OrElse s = "x"
        End Function

        Private Function PhaseToText(p As PhaseType) As String
            Return If(p = PhaseType.HandsOn, "Hands-on", "Academics")
        End Function

        Private Function ParsePhase(s As String) As PhaseType
            Dim k = s.Trim().ToLowerInvariant().Replace("-", "").Replace(" ", "")
            If k = "handson" OrElse k = "ojt" OrElse k = "hands" Then Return PhaseType.HandsOn
            Return PhaseType.Academics
        End Function

        Private Function NormalizeTime(s As String) As String
            s = s.Trim()
            If s = "" Then Return ""
            Dim t As DateTime
            If DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, t) OrElse DateTime.TryParse(s, t) Then
                Return t.ToString("HH:mm", CultureInfo.InvariantCulture)
            End If
            Return s
        End Function

        Private Function SplitList(s As String) As IEnumerable(Of String)
            Return s.Split({";"c, ","c}).
                Select(Function(x) x.Trim()).
                Where(Function(x) x <> "")
        End Function

        Private Function ResolveInstructor(data As AppData, name As String) As Instructor
            Dim ins = data.Instructors.FirstOrDefault(
                Function(i) String.Equals(i.Name, name, StringComparison.OrdinalIgnoreCase))
            If ins Is Nothing Then
                ins = New Instructor With {.Name = name}
                data.Instructors.Add(ins)
            End If
            Return ins
        End Function

        Private Function ResolvePrerequisite(data As AppData, name As String) As Prerequisite
            Dim p = data.Prerequisites.FirstOrDefault(
                Function(x) String.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase))
            If p Is Nothing Then
                p = New Prerequisite With {.Name = name}
                data.Prerequisites.Add(p)
            End If
            Return p
        End Function

    End Module

End Namespace
