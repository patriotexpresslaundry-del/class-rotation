Imports System.Linq
Imports System.Windows.Forms
Imports System.Drawing
Imports ClassRotation.Domain

Namespace UI

    Partial Public Class MainForm

        Private InstructorsTab As TabPage
        Private WithEvents InstructorList As ListBox
        Private InstructorNameBox As TextBox
        Private LeaveGrid As DataGridView
        Private WithEvents AvailClassBox As ComboBox
        Private AvailLabel As Label
        Private _suppressInstructorEvents As Boolean

        Private Sub BuildInstructorsTab()
            InstructorsTab = New TabPage("Instructors & Leave")

            Dim split As New SplitContainer With {.Dock = DockStyle.Fill, .SplitterDistance = 260}
            InstructorsTab.Controls.Add(split)

            InstructorList = New ListBox With {.Dock = DockStyle.Fill, .IntegralHeight = False}
            Dim leftButtons As New FlowLayoutPanel With {.Dock = DockStyle.Bottom, .Height = 40}
            Dim addBtn As New Button With {.Text = "Add"}
            AddHandler addBtn.Click, AddressOf AddInstructor
            Dim delBtn As New Button With {.Text = "Delete"}
            AddHandler delBtn.Click, AddressOf DeleteInstructor
            leftButtons.Controls.AddRange({addBtn, delBtn})
            Dim leftPanel As New Panel With {.Dock = DockStyle.Fill}
            leftPanel.Controls.Add(InstructorList)
            leftPanel.Controls.Add(leftButtons)
            split.Panel1.Controls.Add(leftPanel)

            Dim right As New Panel With {.Dock = DockStyle.Fill, .Padding = New Padding(12)}

            Dim nameLbl As New Label With {.Text = "Name:", .AutoSize = True, .Location = New Point(0, 8)}
            InstructorNameBox = New TextBox With {.Location = New Point(60, 5), .Width = 300}

            Dim leaveLbl As New Label With {.Text = "Leave periods (unavailable to host):", .AutoSize = True, .Location = New Point(0, 45)}
            LeaveGrid = New DataGridView With {
                .Location = New Point(0, 68),
                .Width = 560, .Height = 200,
                .AllowUserToAddRows = True,
                .RowHeadersVisible = False,
                .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
            }
            Dim startCol As New DataGridViewTextBoxColumn With {.HeaderText = "Start (yyyy-MM-dd)", .Name = "Start", .Width = 150}
            Dim endCol As New DataGridViewTextBoxColumn With {.HeaderText = "End (yyyy-MM-dd)", .Name = "End", .Width = 150}
            Dim reasonCol As New DataGridViewTextBoxColumn With {.HeaderText = "Reason", .Name = "Reason", .Width = 240}
            LeaveGrid.Columns.AddRange({startCol, endCol, reasonCol})

            Dim saveBtn As New Button With {.Text = "Save Instructor", .Location = New Point(0, 280), .Width = 140}
            AddHandler saveBtn.Click, AddressOf SaveInstructor

            Dim checkLbl As New Label With {.Text = "Can this instructor host:", .AutoSize = True, .Location = New Point(0, 330)}
            AvailClassBox = New ComboBox With {.DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(160, 326), .Width = 300}
            AvailLabel = New Label With {.AutoSize = True, .MaximumSize = New Size(560, 0), .Location = New Point(0, 360)}

            right.Controls.AddRange({nameLbl, InstructorNameBox, leaveLbl, LeaveGrid, saveBtn, checkLbl, AvailClassBox, AvailLabel})
            split.Panel2.Controls.Add(right)
        End Sub

        Private Sub RefreshInstructorsTab()
            Dim selectedId = SelectedInstructor()?.Id
            _suppressInstructorEvents = True
            InstructorList.Items.Clear()
            For Each ins In _data.Instructors.OrderBy(Function(x) x.Name)
                InstructorList.Items.Add(ins)
            Next
            AvailClassBox.Items.Clear()
            For Each c In _data.Classes.OrderBy(Function(x) x.StartDate)
                AvailClassBox.Items.Add(c)
            Next
            _suppressInstructorEvents = False

            If selectedId IsNot Nothing Then
                SelectInstructorById(selectedId)
            ElseIf InstructorList.Items.Count > 0 Then
                InstructorList.SelectedIndex = 0
            Else
                InstructorNameBox.Text = ""
                LeaveGrid.Rows.Clear()
            End If
        End Sub

        Private Function SelectedInstructor() As Instructor
            Return TryCast(InstructorList.SelectedItem, Instructor)
        End Function

        Private Sub SelectInstructorById(id As String)
            For i = 0 To InstructorList.Items.Count - 1
                If DirectCast(InstructorList.Items(i), Instructor).Id = id Then
                    InstructorList.SelectedIndex = i
                    Return
                End If
            Next
        End Sub

        Private Sub InstructorList_SelectedIndexChanged(sender As Object, e As EventArgs) Handles InstructorList.SelectedIndexChanged
            If _suppressInstructorEvents Then Return
            LoadInstructorIntoEditor(SelectedInstructor())
        End Sub

        Private Sub LoadInstructorIntoEditor(ins As Instructor)
            LeaveGrid.Rows.Clear()
            If ins Is Nothing Then
                InstructorNameBox.Text = ""
                Return
            End If
            InstructorNameBox.Text = ins.Name
            For Each lp In ins.LeavePeriods
                LeaveGrid.Rows.Add(lp.StartDate.ToString("yyyy-MM-dd"), lp.EndDate.ToString("yyyy-MM-dd"), lp.Reason)
            Next
            UpdateInstructorAvailabilityCheck()
        End Sub

        Private Sub AddInstructor(sender As Object, e As EventArgs)
            Dim name = InputDialog.Ask(Me, "New Instructor", "Instructor name:")
            If String.IsNullOrWhiteSpace(name) Then Return
            Dim ins As New Instructor With {.Name = name}
            _data.Instructors.Add(ins)
            SaveData()
            RefreshAll()
            SelectInstructorById(ins.Id)
            SetStatus($"Added instructor '{name}'")
        End Sub

        Private Sub SaveInstructor(sender As Object, e As EventArgs)
            Dim ins = SelectedInstructor()
            If ins Is Nothing Then Return
            ins.Name = InstructorNameBox.Text.Trim()
            ins.LeavePeriods.Clear()
            For Each row As DataGridViewRow In LeaveGrid.Rows
                If row.IsNewRow Then Continue For
                Dim sVal = CStr(row.Cells("Start").Value)
                Dim eVal = CStr(row.Cells("End").Value)
                Dim reason = CStr(row.Cells("Reason").Value)
                Dim sd As Date, ed As Date
                If Date.TryParse(sVal, sd) AndAlso Date.TryParse(eVal, ed) Then
                    If ed < sd Then
                        MessageBox.Show(Me, $"Leave end {eVal} is before start {sVal}.", "Invalid leave", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                        Return
                    End If
                    ins.LeavePeriods.Add(New LeavePeriod With {.StartDate = sd, .EndDate = ed, .Reason = If(reason, "")})
                ElseIf Not String.IsNullOrWhiteSpace(sVal) OrElse Not String.IsNullOrWhiteSpace(eVal) Then
                    MessageBox.Show(Me, $"Could not parse leave dates '{sVal}' / '{eVal}'. Use yyyy-MM-dd.", "Invalid leave", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    Return
                End If
            Next
            SaveData()
            RefreshAll()
            SelectInstructorById(ins.Id)
            SetStatus($"Saved instructor '{ins.Name}'")
        End Sub

        Private Sub DeleteInstructor(sender As Object, e As EventArgs)
            Dim ins = SelectedInstructor()
            If ins Is Nothing Then Return
            If MessageBox.Show(Me, $"Delete instructor '{ins.Name}'? Classes assigned to them will become unassigned.", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes Then
                For Each c In _data.Classes.Where(Function(x) x.InstructorId = ins.Id)
                    c.InstructorId = ""
                Next
                _data.Instructors.Remove(ins)
                SaveData()
                RefreshAll()
                SetStatus("Instructor deleted")
            End If
        End Sub

        Private Sub AvailClassBox_SelectedIndexChanged(sender As Object, e As EventArgs) Handles AvailClassBox.SelectedIndexChanged
            UpdateInstructorAvailabilityCheck()
        End Sub

        Private Sub UpdateInstructorAvailabilityCheck()
            Dim ins = SelectedInstructor()
            Dim cls = TryCast(AvailClassBox.SelectedItem, CourseClass)
            If ins Is Nothing OrElse cls Is Nothing Then
                AvailLabel.Text = ""
                Return
            End If
            Dim result = InstructorAvailability.Check(ins, cls, _data.Classes)
            AvailLabel.Text = $"{cls.Name} ({cls.StartDate:d} - {cls.EndDate:d}): {result.Summary}"
            AvailLabel.ForeColor = If(result.CanHost, Theme.GreenColor, Theme.RedColor)
        End Sub

    End Class

End Namespace
