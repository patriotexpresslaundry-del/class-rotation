Imports System.Collections.Generic
Imports System.IO
Imports System.Linq
Imports ClassRotation.Domain

Namespace Data

    ''' <summary>Outcome of importing a roster file.</summary>
    Public Class RosterImportResult
        Public Property Imported As New List(Of Student)
        Public Property MatchedExisting As Integer
        Public Property CreatedNew As Integer
        Public Property Warnings As New List(Of String)
    End Class

    ''' <summary>
    ''' Imports class rosters from a CSV file. Expected columns (header row, case-insensitive):
    ''' Name, EmployeeId, CompletedPrerequisites (optional; ';' separated prerequisite names).
    ''' Students already known (matched by EmployeeId, else Name) are reused; new ones are created.
    ''' </summary>
    Public Module RosterImporter

        Public Function ImportCsv(filePath As String,
                                  cls As CourseClass,
                                  data As AppData) As RosterImportResult
            Dim lines = File.ReadAllLines(filePath).ToList()
            Return ImportLines(lines, cls, data)
        End Function

        ''' <summary>Core parser exposed separately so it can be unit-tested without a file.</summary>
        Public Function ImportLines(lines As IEnumerable(Of String),
                                    cls As CourseClass,
                                    data As AppData) As RosterImportResult
            Dim result As New RosterImportResult()
            Dim rows = lines.Where(Function(l) Not String.IsNullOrWhiteSpace(l)).ToList()
            If rows.Count = 0 Then
                result.Warnings.Add("File is empty.")
                Return result
            End If

            Dim header = ParseCsvLine(rows(0)).Select(Function(h) h.Trim().ToLowerInvariant()).ToList()
            Dim nameIdx = header.IndexOf("name")
            Dim empIdx = header.IndexOf("employeeid")
            If empIdx < 0 Then empIdx = header.IndexOf("employee id")
            Dim prereqIdx = header.IndexOf("completedprerequisites")
            If prereqIdx < 0 Then prereqIdx = header.IndexOf("completed prerequisites")

            If nameIdx < 0 Then
                result.Warnings.Add("No 'Name' column found in header; nothing imported.")
                Return result
            End If

            Dim prereqByName = data.Prerequisites _
                .GroupBy(Function(p) p.Name.Trim().ToLowerInvariant()) _
                .ToDictionary(Function(g) g.Key, Function(g) g.First())

            For i = 1 To rows.Count - 1
                Dim fields = ParseCsvLine(rows(i))
                Dim name = FieldAt(fields, nameIdx)
                If String.IsNullOrWhiteSpace(name) Then Continue For
                Dim empId = FieldAt(fields, empIdx)

                Dim student = FindStudent(data, name, empId)
                If student Is Nothing Then
                    student = New Student With {.Name = name.Trim(), .EmployeeId = empId.Trim()}
                    data.Students.Add(student)
                    result.CreatedNew += 1
                Else
                    result.MatchedExisting += 1
                End If

                If prereqIdx >= 0 Then
                    Dim prereqField = FieldAt(fields, prereqIdx)
                    For Each pname In prereqField.Split(";"c, "|"c)
                        Dim key = pname.Trim().ToLowerInvariant()
                        If key.Length = 0 Then Continue For
                        Dim prereq As Prerequisite = Nothing
                        If prereqByName.TryGetValue(key, prereq) Then
                            If Not student.CompletedPrerequisiteIds.Contains(prereq.Id) Then
                                student.CompletedPrerequisiteIds.Add(prereq.Id)
                            End If
                        Else
                            result.Warnings.Add($"Unknown prerequisite '{pname.Trim()}' for {name.Trim()} (ignored).")
                        End If
                    Next
                End If

                If Not cls.EnrolledStudentIds.Contains(student.Id) Then
                    cls.EnrolledStudentIds.Add(student.Id)
                End If
                result.Imported.Add(student)
            Next

            Return result
        End Function

        Private Function FindStudent(data As AppData, name As String, empId As String) As Student
            If Not String.IsNullOrWhiteSpace(empId) Then
                Dim byEmp = data.Students.FirstOrDefault(
                    Function(s) String.Equals(s.EmployeeId, empId.Trim(), StringComparison.OrdinalIgnoreCase))
                If byEmp IsNot Nothing Then Return byEmp
            End If
            Return data.Students.FirstOrDefault(
                Function(s) String.Equals(s.Name, name.Trim(), StringComparison.OrdinalIgnoreCase))
        End Function

        Private Function FieldAt(fields As List(Of String), idx As Integer) As String
            If idx < 0 OrElse idx >= fields.Count Then Return ""
            Return fields(idx)
        End Function

        ''' <summary>Minimal CSV line parser supporting quoted fields and escaped quotes.</summary>
        Public Function ParseCsvLine(line As String) As List(Of String)
            Dim fields As New List(Of String)
            Dim sb As New System.Text.StringBuilder()
            Dim inQuotes = False
            Dim i = 0
            While i < line.Length
                Dim c = line(i)
                If inQuotes Then
                    If c = """"c Then
                        If i + 1 < line.Length AndAlso line(i + 1) = """"c Then
                            sb.Append(""""c)
                            i += 1
                        Else
                            inQuotes = False
                        End If
                    Else
                        sb.Append(c)
                    End If
                Else
                    If c = """"c Then
                        inQuotes = True
                    ElseIf c = ","c Then
                        fields.Add(sb.ToString())
                        sb.Clear()
                    Else
                        sb.Append(c)
                    End If
                End If
                i += 1
            End While
            fields.Add(sb.ToString())
            Return fields
        End Function

    End Module

End Namespace
