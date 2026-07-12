Imports System.Collections.Generic
Imports System.Linq

Namespace Domain

    ''' <summary>Why an instructor may be unable to host a class.</summary>
    Public Class AvailabilityConflict
        Public Property Kind As String = ""   ' "Leave" or "Class"
        Public Property Description As String = ""
    End Class

    ''' <summary>Result of checking whether an instructor can host a given class.</summary>
    Public Class AvailabilityResult
        Public Property CanHost As Boolean
        Public Property Conflicts As New List(Of AvailabilityConflict)

        Public ReadOnly Property Summary As String
            Get
                If CanHost Then Return "Available"
                Return "Unavailable - " & String.Join("; ", Conflicts.Select(Function(c) c.Description))
            End Get
        End Property
    End Class

    ''' <summary>
    ''' Determines whether an instructor is free to host a class by comparing the class dates
    ''' against the instructor's leave and any other classes already assigned to them.
    ''' </summary>
    Public Module InstructorAvailability

        Public Function Check(instructor As Instructor,
                              cls As CourseClass,
                              allClasses As IEnumerable(Of CourseClass)) As AvailabilityResult
            Dim result As New AvailabilityResult()

            For Each leave In instructor.LeavePeriods
                If leave.Overlaps(cls.StartDate, cls.EndDate) Then
                    Dim reason = If(String.IsNullOrWhiteSpace(leave.Reason), "leave", leave.Reason)
                    result.Conflicts.Add(New AvailabilityConflict With {
                        .Kind = "Leave",
                        .Description = $"On {reason} {leave.StartDate:d} - {leave.EndDate:d}"
                    })
                End If
            Next

            For Each other In allClasses
                If other.Id = cls.Id Then Continue For
                If other.InstructorId <> instructor.Id Then Continue For
                If RangesOverlap(cls.StartDate, cls.EndDate, other.StartDate, other.EndDate) Then
                    result.Conflicts.Add(New AvailabilityConflict With {
                        .Kind = "Class",
                        .Description = $"Already teaching '{other.Name}' {other.StartDate:d} - {other.EndDate:d}"
                    })
                End If
            Next

            result.CanHost = result.Conflicts.Count = 0
            Return result
        End Function

        Private Function RangesOverlap(aStart As Date, aEnd As Date, bStart As Date, bEnd As Date) As Boolean
            Return aStart.Date <= bEnd.Date AndAlso bStart.Date <= aEnd.Date
        End Function

    End Module

End Namespace
