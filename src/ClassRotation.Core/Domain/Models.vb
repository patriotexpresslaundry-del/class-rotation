Imports System.Collections.Generic

Namespace Domain

    ''' <summary>Distinguishes classroom academics from on-the-job training.</summary>
    Public Enum ClassType
        Academic = 0
        OJT = 1
    End Enum

    ''' <summary>Whether a student has met the requirements to attend a class.</summary>
    Public Enum PrerequisiteStatus
        ' All required prerequisites completed.
        Green = 0
        ' One or more required prerequisites missing.
        Red = 1
    End Enum

    ''' <summary>A named requirement a student must complete before attending certain classes.</summary>
    Public Class Prerequisite
        Public Property Id As String = Guid.NewGuid().ToString("N")
        Public Property Name As String = ""
        Public Property Description As String = ""

        Public Overrides Function ToString() As String
            Return Name
        End Function
    End Class

    ''' <summary>A student who can be enrolled in classes and tracked against prerequisites.</summary>
    Public Class Student
        Public Property Id As String = Guid.NewGuid().ToString("N")
        Public Property Name As String = ""
        Public Property EmployeeId As String = ""
        ''' <summary>Ids of the prerequisites this student has completed.</summary>
        Public Property CompletedPrerequisiteIds As New List(Of String)

        Public Overrides Function ToString() As String
            If String.IsNullOrWhiteSpace(EmployeeId) Then Return Name
            Return $"{Name} ({EmployeeId})"
        End Function
    End Class

    ''' <summary>A block of time an instructor is unavailable (leave, TDY, etc.).</summary>
    Public Class LeavePeriod
        Public Property Id As String = Guid.NewGuid().ToString("N")
        Public Property StartDate As Date
        Public Property EndDate As Date
        Public Property Reason As String = ""

        ''' <summary>True when the given date range overlaps this leave period (inclusive).</summary>
        Public Function Overlaps(rangeStart As Date, rangeEnd As Date) As Boolean
            Return StartDate.Date <= rangeEnd.Date AndAlso EndDate.Date >= rangeStart.Date
        End Function
    End Class

    ''' <summary>A person who can be assigned to host/teach a class.</summary>
    Public Class Instructor
        Public Property Id As String = Guid.NewGuid().ToString("N")
        Public Property Name As String = ""
        Public Property LeavePeriods As New List(Of LeavePeriod)

        Public Overrides Function ToString() As String
            Return Name
        End Function
    End Class

    ''' <summary>A scheduled class with a date range, type, instructor, prerequisites and roster.</summary>
    Public Class CourseClass
        Public Property Id As String = Guid.NewGuid().ToString("N")
        Public Property Name As String = ""
        Public Property Type As ClassType = ClassType.Academic
        Public Property StartDate As Date = Date.Today
        Public Property EndDate As Date = Date.Today
        Public Property InstructorId As String = ""
        ''' <summary>Prerequisite ids a student must have completed to be "Green" for this class.</summary>
        Public Property RequiredPrerequisiteIds As New List(Of String)
        ''' <summary>Ids of students enrolled / on the imported roster for this class.</summary>
        Public Property EnrolledStudentIds As New List(Of String)

        Public Overrides Function ToString() As String
            Return Name
        End Function
    End Class

    ''' <summary>Root container persisted to disk holding all application data.</summary>
    Public Class AppData
        Public Property Prerequisites As New List(Of Prerequisite)
        Public Property Students As New List(Of Student)
        Public Property Instructors As New List(Of Instructor)
        Public Property Classes As New List(Of CourseClass)
    End Class

End Namespace
