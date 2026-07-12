Imports System.Linq
Imports Xunit
Imports ClassRotation.Data
Imports ClassRotation.Domain

Public Class RosterImporterTests

    <Fact>
    Public Sub Imports_New_Students_And_Enrols_Them()
        Dim data As New AppData()
        Dim cls As New CourseClass With {.Name = "Class A"}
        data.Classes.Add(cls)

        Dim lines = {
            "Name,EmployeeId",
            "John Smith,E1",
            "Maria Lopez,E2"
        }

        Dim result = RosterImporter.ImportLines(lines, cls, data)

        Assert.Equal(2, result.CreatedNew)
        Assert.Equal(0, result.MatchedExisting)
        Assert.Equal(2, cls.EnrolledStudentIds.Count)
        Assert.Equal(2, data.Students.Count)
    End Sub

    <Fact>
    Public Sub Matches_Existing_Student_By_EmployeeId()
        Dim data As New AppData()
        Dim existing As New Student With {.Name = "John Smith", .EmployeeId = "E1"}
        data.Students.Add(existing)
        Dim cls As New CourseClass With {.Name = "Class A"}
        data.Classes.Add(cls)

        Dim lines = {"Name,EmployeeId", "Johnny S,E1"}
        Dim result = RosterImporter.ImportLines(lines, cls, data)

        Assert.Equal(1, result.MatchedExisting)
        Assert.Equal(0, result.CreatedNew)
        Assert.Single(data.Students)
        Assert.Contains(existing.Id, cls.EnrolledStudentIds)
    End Sub

    <Fact>
    Public Sub Maps_Completed_Prerequisites_By_Name()
        Dim data As New AppData()
        Dim safety As New Prerequisite With {.Name = "Safety"}
        data.Prerequisites.Add(safety)
        Dim cls As New CourseClass With {.Name = "Class A"}
        data.Classes.Add(cls)

        Dim lines = {
            "Name,EmployeeId,CompletedPrerequisites",
            "John Smith,E1,Safety;Unknown Prereq"
        }
        Dim result = RosterImporter.ImportLines(lines, cls, data)

        Dim student = data.Students.Single()
        Assert.Contains(safety.Id, student.CompletedPrerequisiteIds)
        Assert.NotEmpty(result.Warnings)
    End Sub

    <Fact>
    Public Sub Parses_Quoted_Fields_With_Commas()
        Dim fields = RosterImporter.ParseCsvLine("""Smith, John"",E1,""a;b""")
        Assert.Equal(3, fields.Count)
        Assert.Equal("Smith, John", fields(0))
        Assert.Equal("E1", fields(1))
    End Sub

End Class
