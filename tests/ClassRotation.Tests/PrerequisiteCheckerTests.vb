Imports Xunit
Imports ClassRotation.Domain

Public Class PrerequisiteCheckerTests

    Private Function BuildData() As (safety As Prerequisite, security As Prerequisite, cls As CourseClass)
        Dim safety As New Prerequisite With {.Name = "Safety"}
        Dim security As New Prerequisite With {.Name = "Security"}
        Dim cls As New CourseClass With {.Name = "Class A"}
        cls.RequiredPrerequisiteIds.AddRange({safety.Id, security.Id})
        Return (safety, security, cls)
    End Function

    <Fact>
    Public Sub Green_When_All_Prerequisites_Completed()
        Dim d = BuildData()
        Dim student As New Student With {.Name = "Alice"}
        student.CompletedPrerequisiteIds.AddRange({d.safety.Id, d.security.Id})

        Dim result = PrerequisiteChecker.Evaluate(student, d.cls, {d.safety, d.security})

        Assert.Equal(PrerequisiteStatus.Green, result.Status)
        Assert.True(result.IsGreen)
        Assert.Empty(result.MissingPrerequisites)
    End Sub

    <Fact>
    Public Sub Red_And_Lists_Missing_When_Incomplete()
        Dim d = BuildData()
        Dim student As New Student With {.Name = "Bob"}
        student.CompletedPrerequisiteIds.Add(d.safety.Id)

        Dim result = PrerequisiteChecker.Evaluate(student, d.cls, {d.safety, d.security})

        Assert.Equal(PrerequisiteStatus.Red, result.Status)
        Assert.False(result.IsGreen)
        Assert.Single(result.MissingPrerequisites)
        Assert.Equal("Security", result.MissingPrerequisites(0).Name)
    End Sub

    <Fact>
    Public Sub Green_When_Class_Has_No_Requirements()
        Dim student As New Student With {.Name = "Carol"}
        Dim cls As New CourseClass With {.Name = "No Reqs"}

        Dim result = PrerequisiteChecker.Evaluate(student, cls, New Prerequisite() {})

        Assert.True(result.IsGreen)
    End Sub

End Class
