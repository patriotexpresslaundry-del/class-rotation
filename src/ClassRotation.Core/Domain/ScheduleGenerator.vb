Imports System.Collections.Generic
Imports System.Linq

Namespace Domain

    ''' <summary>
    ''' Builds a day-by-day, period-by-period skeleton schedule for a class from its phase dates.
    ''' One <see cref="ClassDay"/> is produced per training day (weekdays by default), each pre-filled
    ''' with a template of periods tagged to the phase (Academics or Hands-on) that the day falls in.
    ''' The result is meant as an editable starting point, not a final timetable.
    ''' </summary>
    Public Module ScheduleGenerator

        ''' <summary>Default daily periods as (start, end) 24h times, with a lunch gap.</summary>
        Public ReadOnly DefaultPeriods As (StartTime As String, EndTime As String)() = {
            ("08:00", "09:30"),
            ("09:45", "11:15"),
            ("12:15", "13:45"),
            ("14:00", "15:30")
        }

        ''' <summary>
        ''' Generate the schedule for a class. Days from AcademicsStart..AcademicsEnd are tagged
        ''' Academics; days from HandsOnStart..HandsOnEnd (when HasHandsOn) are tagged HandsOn.
        ''' </summary>
        Public Function Generate(cls As CourseClass,
                                 Optional includeWeekends As Boolean = False,
                                 Optional periods As IEnumerable(Of (StartTime As String, EndTime As String)) = Nothing) As List(Of ClassDay)
            Dim template = If(periods, DefaultPeriods).ToArray()
            Dim days As New List(Of ClassDay)

            AddPhaseDays(days, cls.AcademicsStart, cls.AcademicsEnd, PhaseType.Academics, cls.InstructorId, template, includeWeekends)
            If cls.HasHandsOn Then
                AddPhaseDays(days, cls.HandsOnStart, cls.HandsOnEnd, PhaseType.HandsOn, cls.InstructorId, template, includeWeekends)
            End If

            days.Sort(Function(a, b) a.Date.CompareTo(b.Date))
            Return days
        End Function

        Private Sub AddPhaseDays(days As List(Of ClassDay), startDate As Date, endDate As Date,
                                 phase As PhaseType, instructorId As String,
                                 template As (StartTime As String, EndTime As String)(),
                                 includeWeekends As Boolean)
            If endDate < startDate Then Return
            Dim d = startDate.Date
            While d <= endDate.Date
                If includeWeekends OrElse (d.DayOfWeek <> DayOfWeek.Saturday AndAlso d.DayOfWeek <> DayOfWeek.Sunday) Then
                    Dim day As New ClassDay With {.Date = d}
                    For Each p In template
                        day.Periods.Add(New ClassPeriod With {
                            .StartTime = p.StartTime,
                            .EndTime = p.EndTime,
                            .Phase = phase,
                            .InstructorId = instructorId,
                            .Subject = "TBD"
                        })
                    Next
                    days.Add(day)
                End If
                d = d.AddDays(1)
            End While
        End Sub

    End Module

End Namespace
