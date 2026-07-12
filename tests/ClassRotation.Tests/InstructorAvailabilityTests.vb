Imports System.Collections.Generic
Imports Xunit
Imports ClassRotation.Domain

Public Class InstructorAvailabilityTests

    <Fact>
    Public Sub Available_When_No_Leave_Or_Other_Class()
        Dim ins As New Instructor With {.Name = "Alice"}
        Dim cls As New CourseClass With {.StartDate = New Date(2026, 3, 1), .EndDate = New Date(2026, 3, 20), .InstructorId = ins.Id}

        Dim result = InstructorAvailability.Check(ins, cls, {cls})

        Assert.True(result.CanHost)
        Assert.Empty(result.Conflicts)
    End Sub

    <Fact>
    Public Sub Unavailable_When_Leave_Overlaps()
        Dim ins As New Instructor With {.Name = "Alice"}
        ins.LeavePeriods.Add(New LeavePeriod With {.StartDate = New Date(2026, 3, 10), .EndDate = New Date(2026, 3, 15), .Reason = "Leave"})
        Dim cls As New CourseClass With {.StartDate = New Date(2026, 3, 1), .EndDate = New Date(2026, 3, 20), .InstructorId = ins.Id}

        Dim result = InstructorAvailability.Check(ins, cls, {cls})

        Assert.False(result.CanHost)
        Assert.Contains(result.Conflicts, Function(c) c.Kind = "Leave")
    End Sub

    <Fact>
    Public Sub Available_When_Leave_Is_Outside_Class_Dates()
        Dim ins As New Instructor With {.Name = "Alice"}
        ins.LeavePeriods.Add(New LeavePeriod With {.StartDate = New Date(2026, 4, 1), .EndDate = New Date(2026, 4, 10)})
        Dim cls As New CourseClass With {.StartDate = New Date(2026, 3, 1), .EndDate = New Date(2026, 3, 20), .InstructorId = ins.Id}

        Dim result = InstructorAvailability.Check(ins, cls, {cls})

        Assert.True(result.CanHost)
    End Sub

    <Fact>
    Public Sub Unavailable_When_Double_Booked_On_Another_Class()
        Dim ins As New Instructor With {.Name = "Alice"}
        Dim clsA As New CourseClass With {.Name = "A", .StartDate = New Date(2026, 3, 1), .EndDate = New Date(2026, 3, 20), .InstructorId = ins.Id}
        Dim clsB As New CourseClass With {.Name = "B", .StartDate = New Date(2026, 3, 15), .EndDate = New Date(2026, 3, 25), .InstructorId = ins.Id}
        Dim all As New List(Of CourseClass) From {clsA, clsB}

        Dim result = InstructorAvailability.Check(ins, clsB, all)

        Assert.False(result.CanHost)
        Assert.Contains(result.Conflicts, Function(c) c.Kind = "Class")
    End Sub

End Class
