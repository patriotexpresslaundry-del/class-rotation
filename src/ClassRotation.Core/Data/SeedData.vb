Imports ClassRotation.Domain

Namespace Data

    ''' <summary>Produces a small sample data set so the app is usable on first launch.</summary>
    Public Module SeedData

        Public Function CreateSample() As AppData
            Dim data As New AppData()
            Dim year = Date.Today.Year

            Dim safety As New Prerequisite With {.Name = "Safety Orientation", .Description = "Mandatory safety briefing"}
            Dim security As New Prerequisite With {.Name = "Security Clearance", .Description = "Active clearance on file"}
            Dim medical As New Prerequisite With {.Name = "Medical Screening", .Description = "Current medical exam"}
            Dim fundamentals As New Prerequisite With {.Name = "Fundamentals Course", .Description = "Completed intro academics"}
            data.Prerequisites.AddRange({safety, security, medical, fundamentals})

            Dim alice As New Instructor With {.Name = "Alice Nguyen"}
            alice.LeavePeriods.Add(New LeavePeriod With {
                .StartDate = New Date(year, 7, 1), .EndDate = New Date(year, 7, 14), .Reason = "Annual Leave"})
            Dim bob As New Instructor With {.Name = "Bob Carter"}
            Dim carol As New Instructor With {.Name = "Carol Diaz"}
            data.Instructors.AddRange({alice, bob, carol})

            Dim s1 As New Student With {.Name = "John Smith", .EmployeeId = "E1001"}
            s1.CompletedPrerequisiteIds.AddRange({safety.Id, security.Id, fundamentals.Id})
            Dim s2 As New Student With {.Name = "Maria Lopez", .EmployeeId = "E1002"}
            s2.CompletedPrerequisiteIds.AddRange({safety.Id})
            Dim s3 As New Student With {.Name = "David Chen", .EmployeeId = "E1003"}
            s3.CompletedPrerequisiteIds.AddRange({safety.Id, security.Id, medical.Id, fundamentals.Id})
            data.Students.AddRange({s1, s2, s3})

            Dim academics As New CourseClass With {
                .Name = "Systems Fundamentals (Academics)",
                .Type = ClassType.Academic,
                .StartDate = New Date(year, 3, 3),
                .EndDate = New Date(year, 3, 28),
                .InstructorId = alice.Id
            }
            academics.RequiredPrerequisiteIds.AddRange({safety.Id, security.Id})
            academics.EnrolledStudentIds.AddRange({s1.Id, s2.Id, s3.Id})

            Dim ojt As New CourseClass With {
                .Name = "Field Operations (OJT)",
                .Type = ClassType.OJT,
                .StartDate = New Date(year, 4, 7),
                .EndDate = New Date(year, 5, 16),
                .InstructorId = bob.Id
            }
            ojt.RequiredPrerequisiteIds.AddRange({safety.Id, security.Id, medical.Id, fundamentals.Id})
            ojt.EnrolledStudentIds.AddRange({s1.Id, s3.Id})

            Dim advanced As New CourseClass With {
                .Name = "Advanced Academics",
                .Type = ClassType.Academic,
                .StartDate = New Date(year, 7, 7),
                .EndDate = New Date(year, 7, 25),
                .InstructorId = alice.Id
            }
            advanced.RequiredPrerequisiteIds.AddRange({fundamentals.Id})
            advanced.EnrolledStudentIds.AddRange({s1.Id, s3.Id})

            data.Classes.AddRange({academics, ojt, advanced})
            Return data
        End Function

    End Module

End Namespace
