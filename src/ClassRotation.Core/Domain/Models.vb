Imports System.Collections.Generic

Namespace Domain

    ''' <summary>Distinguishes classroom academics from on-the-job training.</summary>
    Public Enum ClassType
        Academic = 0
        OJT = 1
    End Enum

    ''' <summary>The two phases that make up a class bar: classroom academics then hands-on training.</summary>
    Public Enum PhaseType
        Academics = 0
        HandsOn = 1
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

    ''' <summary>A single scheduled period (class hour) within a training day.</summary>
    Public Class ClassPeriod
        Public Property Id As String = Guid.NewGuid().ToString("N")
        ''' <summary>Start time of day in 24h "HH:mm" form, e.g. "08:00".</summary>
        Public Property StartTime As String = "08:00"
        Public Property EndTime As String = "09:00"
        Public Property Subject As String = ""
        Public Property Phase As PhaseType = PhaseType.Academics
        Public Property InstructorId As String = ""
        Public Property Room As String = ""
        Public Property Notes As String = ""
    End Class

    ''' <summary>One day of a class's detailed schedule, holding its ordered periods.</summary>
    Public Class ClassDay
        Public Property [Date] As Date
        Public Property Periods As New List(Of ClassPeriod)
    End Class

    ''' <summary>
    ''' A scheduled class made of an Academics phase optionally followed by a Hands-on phase,
    ''' with an instructor, prerequisites, roster and an optional day-by-day period schedule.
    ''' </summary>
    Public Class CourseClass
        Public Property Id As String = Guid.NewGuid().ToString("N")
        Public Property Name As String = ""
        Public Property Type As ClassType = ClassType.Academic

        ' Overall span (kept for availability checks and back-compat); derived from the phases.
        Public Property StartDate As Date = Date.Today
        Public Property EndDate As Date = Date.Today

        ' Phase 1: classroom academics.
        Public Property AcademicsStart As Date = Date.Today
        Public Property AcademicsEnd As Date = Date.Today
        ' Phase 2: hands-on / OJT (optional).
        Public Property HasHandsOn As Boolean = False
        Public Property HandsOnStart As Date = Date.Today
        Public Property HandsOnEnd As Date = Date.Today

        Public Property InstructorId As String = ""
        ''' <summary>Prerequisite ids a student must have completed to be "Green" for this class.</summary>
        Public Property RequiredPrerequisiteIds As New List(Of String)
        ''' <summary>Ids of students enrolled / on the imported roster for this class.</summary>
        Public Property EnrolledStudentIds As New List(Of String)
        ''' <summary>Optional detailed day-by-day, period-by-period schedule.</summary>
        Public Property Schedule As New List(Of ClassDay)

        ''' <summary>Earliest date across both phases.</summary>
        Public ReadOnly Property OverallStart As Date
            Get
                Return AcademicsStart
            End Get
        End Property

        ''' <summary>Latest date across both phases.</summary>
        Public ReadOnly Property OverallEnd As Date
            Get
                Return If(HasHandsOn AndAlso HandsOnEnd > AcademicsEnd, HandsOnEnd, AcademicsEnd)
            End Get
        End Property

        ''' <summary>Refresh the overall span from the phase dates.</summary>
        Public Sub RecomputeSpan()
            StartDate = OverallStart
            EndDate = OverallEnd
        End Sub

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
