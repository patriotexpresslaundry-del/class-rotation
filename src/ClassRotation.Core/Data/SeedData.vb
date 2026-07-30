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

            Dim tech1 = TwoPhaseClass("TECH 1", alice.Id,
                                      New Date(year, 1, 26), New Date(year, 2, 21),
                                      New Date(year, 2, 22), New Date(year, 5, 5))
            tech1.RequiredPrerequisiteIds.AddRange({safety.Id, security.Id})
            tech1.EnrolledStudentIds.AddRange({s1.Id, s2.Id, s3.Id})
            ' Give the first class a generated day-by-day schedule so the detail view has content.
            tech1.Schedule = ScheduleGenerator.Generate(tech1)

            Dim mech1 = TwoPhaseClass("MECH 1", bob.Id,
                                      New Date(year, 1, 26), New Date(year, 2, 21),
                                      New Date(year, 2, 22), New Date(year, 4, 27))
            mech1.RequiredPrerequisiteIds.AddRange({safety.Id, security.Id, medical.Id})
            mech1.EnrolledStudentIds.AddRange({s1.Id, s3.Id})

            Dim asm2 = TwoPhaseClass("ASM 2", carol.Id,
                                     New Date(year, 4, 13), New Date(year, 5, 23),
                                     New Date(year, 5, 24), New Date(year, 8, 8))
            asm2.RequiredPrerequisiteIds.AddRange({fundamentals.Id})
            asm2.EnrolledStudentIds.AddRange({s1.Id, s3.Id})

            data.Classes.AddRange({tech1, mech1, asm2})
            Return data
        End Function

        ''' <summary>Builds a class with an academics phase followed by a hands-on phase.</summary>
        Private Function TwoPhaseClass(name As String, instructorId As String,
                                       acadStart As Date, acadEnd As Date,
                                       handsStart As Date, handsEnd As Date) As CourseClass
            Dim c As New CourseClass With {
                .Name = name,
                .InstructorId = instructorId,
                .AcademicsStart = acadStart,
                .AcademicsEnd = acadEnd,
                .HasHandsOn = True,
                .HandsOnStart = handsStart,
                .HandsOnEnd = handsEnd
            }
            c.RecomputeSpan()
            Return c
        End Function

    End Module

End Namespace
