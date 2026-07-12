Imports System.Collections.Generic
Imports System.Linq

Namespace Domain

    ''' <summary>Result of evaluating a student's prerequisites against a class.</summary>
    Public Class PrerequisiteResult
        Public Property Status As PrerequisiteStatus
        Public Property MissingPrerequisites As New List(Of Prerequisite)

        Public ReadOnly Property IsGreen As Boolean
            Get
                Return Status = PrerequisiteStatus.Green
            End Get
        End Property

        Public ReadOnly Property Summary As String
            Get
                If IsGreen Then Return "Green - all prerequisites met"
                Dim names = String.Join(", ", MissingPrerequisites.Select(Function(p) p.Name))
                Return $"Red - missing: {names}"
            End Get
        End Property
    End Class

    ''' <summary>Evaluates whether students have met the prerequisites required by a class.</summary>
    Public Module PrerequisiteChecker

        ''' <summary>
        ''' Returns Green when the student has completed every prerequisite the class requires,
        ''' otherwise Red along with the list of missing prerequisites.
        ''' </summary>
        Public Function Evaluate(student As Student, cls As CourseClass, allPrerequisites As IEnumerable(Of Prerequisite)) As PrerequisiteResult
            Dim result As New PrerequisiteResult()
            Dim completed As New HashSet(Of String)(student.CompletedPrerequisiteIds)

            Dim byId = allPrerequisites.ToDictionary(Function(p) p.Id, Function(p) p)

            For Each reqId In cls.RequiredPrerequisiteIds
                If Not completed.Contains(reqId) Then
                    Dim prereq As Prerequisite = Nothing
                    If byId.TryGetValue(reqId, prereq) Then
                        result.MissingPrerequisites.Add(prereq)
                    Else
                        result.MissingPrerequisites.Add(New Prerequisite With {.Id = reqId, .Name = "(unknown)"})
                    End If
                End If
            Next

            result.Status = If(result.MissingPrerequisites.Count = 0, PrerequisiteStatus.Green, PrerequisiteStatus.Red)
            Return result
        End Function

    End Module

End Namespace
