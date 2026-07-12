Imports System.Linq
Imports System.Windows.Forms
Imports System.Drawing
Imports ClassRotation.Data
Imports ClassRotation.Domain

Namespace UI

    Partial Public Class MainForm

        Private RosterTab As TabPage
        Private WithEvents RosterClassBox As ComboBox
        Private RosterGrid As DataGridView
        Private RosterSummary As Label
        Private _suppressRosterEvents As Boolean

        Private Sub BuildRosterTab()
            RosterTab = New TabPage("Roster & Status")

            Dim toolbar As New Panel With {.Dock = DockStyle.Top, .Height = 44}
            Dim lbl As New Label With {.Text = "Class:", .AutoSize = True, .Location = New Point(10, 14)}
            RosterClassBox = New ComboBox With {.DropDownStyle = ComboBoxStyle.DropDownList, .Width = 320, .Location = New Point(55, 10)}
            Dim importBtn As New Button With {.Text = "Import Roster (CSV)...", .Location = New Point(390, 9), .Width = 160}
            AddHandler importBtn.Click, AddressOf ImportRoster
            Dim addBtn As New Button With {.Text = "Add Existing Student", .Location = New Point(560, 9), .Width = 150}
            AddHandler addBtn.Click, AddressOf AddExistingStudent
            Dim removeBtn As New Button With {.Text = "Remove From Class", .Location = New Point(720, 9), .Width = 150}
            AddHandler removeBtn.Click, AddressOf RemoveFromClass
            Dim editPrereqBtn As New Button With {.Text = "Edit Student Prereqs", .Location = New Point(880, 9), .Width = 160}
            AddHandler editPrereqBtn.Click, AddressOf EditStudentPrereqs
            toolbar.Controls.AddRange({lbl, RosterClassBox, importBtn, addBtn, removeBtn, editPrereqBtn})

            RosterGrid = New DataGridView With {
                .Dock = DockStyle.Fill,
                .AllowUserToAddRows = False,
                .AllowUserToDeleteRows = False,
                .ReadOnly = True,
                .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                .RowHeadersVisible = False,
                .MultiSelect = False
            }
            RosterGrid.Columns.Add("Status", "Status")
            RosterGrid.Columns.Add("Name", "Student")
            RosterGrid.Columns.Add("EmployeeId", "Employee ID")
            RosterGrid.Columns.Add("Detail", "Prerequisite detail")
            RosterGrid.Columns("Detail").AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            RosterGrid.Columns("Status").AutoSizeMode = DataGridViewAutoSizeColumnMode.None
            RosterGrid.Columns("Status").Width = 70

            RosterSummary = New Label With {.Dock = DockStyle.Bottom, .Height = 26, .TextAlign = ContentAlignment.MiddleLeft, .Padding = New Padding(6, 0, 0, 0)}

            RosterTab.Controls.Add(RosterGrid)
            RosterTab.Controls.Add(RosterSummary)
            RosterTab.Controls.Add(toolbar)
        End Sub

        Private Sub RefreshRosterTab()
            Dim selectedId = SelectedRosterClass()?.Id
            _suppressRosterEvents = True
            RosterClassBox.Items.Clear()
            For Each c In _data.Classes.OrderBy(Function(x) x.StartDate)
                RosterClassBox.Items.Add(c)
            Next
            _suppressRosterEvents = False

            If RosterClassBox.Items.Count = 0 Then
                RosterGrid.Rows.Clear()
                RosterSummary.Text = "No classes yet."
                Return
            End If
            Dim idx = 0
            If selectedId IsNot Nothing Then
                For i = 0 To RosterClassBox.Items.Count - 1
                    If DirectCast(RosterClassBox.Items(i), CourseClass).Id = selectedId Then idx = i : Exit For
                Next
            End If
            RosterClassBox.SelectedIndex = idx
        End Sub

        Private Function SelectedRosterClass() As CourseClass
            Return TryCast(RosterClassBox.SelectedItem, CourseClass)
        End Function

        Private Sub RosterClassBox_SelectedIndexChanged(sender As Object, e As EventArgs) Handles RosterClassBox.SelectedIndexChanged
            If _suppressRosterEvents Then Return
            RefreshRosterGrid()
        End Sub

        Private Sub RefreshRosterGrid()
            RosterGrid.Rows.Clear()
            Dim cls = SelectedRosterClass()
            If cls Is Nothing Then Return

            Dim greenCount = 0
            For Each sid In cls.EnrolledStudentIds
                Dim student = _data.Students.FirstOrDefault(Function(s) s.Id = sid)
                If student Is Nothing Then Continue For
                Dim result = PrerequisiteChecker.Evaluate(student, cls, _data.Prerequisites)
                Dim rowIndex = RosterGrid.Rows.Add(
                    If(result.IsGreen, "GREEN", "RED"),
                    student.Name,
                    student.EmployeeId,
                    result.Summary)
                Dim row = RosterGrid.Rows(rowIndex)
                Dim c = Theme.StatusColor(result.Status)
                row.Cells("Status").Style.BackColor = c
                row.Cells("Status").Style.ForeColor = Color.White
                row.Cells("Status").Style.Alignment = DataGridViewContentAlignment.MiddleCenter
                row.Tag = student
                If result.IsGreen Then greenCount += 1
            Next
            RosterSummary.Text = $"{cls.EnrolledStudentIds.Count} enrolled | {greenCount} Green | {cls.EnrolledStudentIds.Count - greenCount} Red"
        End Sub

        Private Sub ImportRoster(sender As Object, e As EventArgs)
            Dim cls = SelectedRosterClass()
            If cls Is Nothing Then
                MessageBox.Show(Me, "Select a class first.", "No class", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If
            Using dlg As New OpenFileDialog With {.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*", .Title = "Import roster"}
                If dlg.ShowDialog(Me) <> DialogResult.OK Then Return
                Try
                    Dim result = RosterImporter.ImportCsv(dlg.FileName, cls, _data)
                    SaveData()
                    RefreshAll()
                    SelectRosterClassById(cls.Id)
                    Dim summary = $"Imported {result.Imported.Count} students ({result.CreatedNew} new, {result.MatchedExisting} matched)."
                    Dim msg = summary
                    If result.Warnings.Count > 0 Then
                        msg &= Environment.NewLine & Environment.NewLine & "Warnings:" & Environment.NewLine &
                               String.Join(Environment.NewLine, result.Warnings.Take(15))
                    End If
                    MessageBox.Show(Me, msg, "Import complete", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    SetStatus(summary)
                Catch ex As Exception
                    MessageBox.Show(Me, "Failed to import: " & ex.Message, "Import error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                End Try
            End Using
        End Sub

        Private Sub SelectRosterClassById(id As String)
            For i = 0 To RosterClassBox.Items.Count - 1
                If DirectCast(RosterClassBox.Items(i), CourseClass).Id = id Then
                    RosterClassBox.SelectedIndex = i
                    Return
                End If
            Next
        End Sub

        Private Sub AddExistingStudent(sender As Object, e As EventArgs)
            Dim cls = SelectedRosterClass()
            If cls Is Nothing Then Return
            Dim available = _data.Students.Where(Function(s) Not cls.EnrolledStudentIds.Contains(s.Id)).ToList()
            If available.Count = 0 Then
                MessageBox.Show(Me, "All students are already on this roster.", "Nothing to add", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If
            Dim picked = PickerDialog.Pick(Me, "Add student to class", available.Cast(Of Object).ToList())
            If picked Is Nothing Then Return
            cls.EnrolledStudentIds.Add(DirectCast(picked, Student).Id)
            SaveData()
            RefreshRosterGrid()
            SetStatus("Student added to class")
        End Sub

        Private Sub EditStudentPrereqs(sender As Object, e As EventArgs)
            If RosterGrid.CurrentRow Is Nothing Then
                MessageBox.Show(Me, "Select a student row first.", "No student", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If
            Dim student = TryCast(RosterGrid.CurrentRow.Tag, Student)
            If student Is Nothing Then Return
            Dim items = _data.Prerequisites.OrderBy(Function(x) x.Name).
                Select(Function(p) New CheckItem With {
                    .Key = p.Id, .Label = p.Name,
                    .Checked = student.CompletedPrerequisiteIds.Contains(p.Id)}).ToList()
            Dim result = CheckListDialog.Edit(Me, $"Completed prerequisites - {student.Name}", "Check prerequisites this student has completed:", items)
            If result Is Nothing Then Return
            student.CompletedPrerequisiteIds = result
            SaveData()
            RefreshRosterGrid()
            SetStatus($"Updated prerequisites for {student.Name}")
        End Sub

        Private Sub RemoveFromClass(sender As Object, e As EventArgs)
            Dim cls = SelectedRosterClass()
            If cls Is Nothing OrElse RosterGrid.CurrentRow Is Nothing Then Return
            Dim student = TryCast(RosterGrid.CurrentRow.Tag, Student)
            If student Is Nothing Then Return
            cls.EnrolledStudentIds.Remove(student.Id)
            SaveData()
            RefreshRosterGrid()
            SetStatus($"Removed {student.Name} from class")
        End Sub

    End Class

End Namespace
