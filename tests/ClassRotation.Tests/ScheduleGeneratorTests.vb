Imports System.Linq
Imports Xunit
Imports ClassRotation.Domain

Public Class ScheduleGeneratorTests

    Private Function TwoPhaseClass() As CourseClass
        Dim c As New CourseClass With {
            .Name = "TECH 1",
            .AcademicsStart = New Date(2026, 1, 26),
            .AcademicsEnd = New Date(2026, 1, 30),
            .HasHandsOn = True,
            .HandsOnStart = New Date(2026, 2, 2),
            .HandsOnEnd = New Date(2026, 2, 6)
        }
        c.RecomputeSpan()
        Return c
    End Function

    <Fact>
    Public Sub Generates_Weekdays_Only_By_Default()
        Dim c = TwoPhaseClass()
        Dim days = ScheduleGenerator.Generate(c)

        ' Jan 26-30 (Mon-Fri) + Feb 2-6 (Mon-Fri) = 10 weekdays, no weekends.
        Assert.Equal(10, days.Count)
        Assert.DoesNotContain(days, Function(d) d.Date.DayOfWeek = DayOfWeek.Saturday OrElse d.Date.DayOfWeek = DayOfWeek.Sunday)
    End Sub

    <Fact>
    Public Sub Tags_Periods_With_Correct_Phase()
        Dim c = TwoPhaseClass()
        Dim days = ScheduleGenerator.Generate(c)

        Dim academicsDay = days.First(Function(d) d.Date = New Date(2026, 1, 26))
        Assert.All(academicsDay.Periods, Sub(p) Assert.Equal(PhaseType.Academics, p.Phase))

        Dim handsDay = days.First(Function(d) d.Date = New Date(2026, 2, 2))
        Assert.All(handsDay.Periods, Sub(p) Assert.Equal(PhaseType.HandsOn, p.Phase))
    End Sub

    <Fact>
    Public Sub Uses_Default_Period_Template_Per_Day()
        Dim c = TwoPhaseClass()
        Dim days = ScheduleGenerator.Generate(c)

        Assert.All(days, Sub(d) Assert.Equal(ScheduleGenerator.DefaultPeriods.Length, d.Periods.Count))
    End Sub

    <Fact>
    Public Sub Includes_Weekends_When_Requested()
        Dim c = TwoPhaseClass()
        Dim days = ScheduleGenerator.Generate(c, includeWeekends:=True)

        ' Jan 26 - Jan 30 = 5, Feb 2 - Feb 6 = 5; with weekends the academics span stays
        ' contiguous so no weekend falls inside either phase here, but Jan 31/Feb 1 are outside.
        Assert.Equal(10, days.Count)
    End Sub

    <Fact>
    Public Sub Academics_Only_Class_Has_No_HandsOn_Days()
        Dim c As New CourseClass With {
            .Name = "Academics Only",
            .AcademicsStart = New Date(2026, 3, 2),
            .AcademicsEnd = New Date(2026, 3, 6),
            .HasHandsOn = False
        }
        c.RecomputeSpan()
        Dim days = ScheduleGenerator.Generate(c)

        Assert.Equal(5, days.Count)
        Assert.All(days, Sub(d) Assert.All(d.Periods, Sub(p) Assert.Equal(PhaseType.Academics, p.Phase)))
    End Sub

    <Fact>
    Public Sub OverallSpan_Covers_Both_Phases()
        Dim c = TwoPhaseClass()
        Assert.Equal(New Date(2026, 1, 26), c.OverallStart)
        Assert.Equal(New Date(2026, 2, 6), c.OverallEnd)
        Assert.Equal(c.OverallStart, c.StartDate)
        Assert.Equal(c.OverallEnd, c.EndDate)
    End Sub

End Class
