Imports System.Linq
Imports System.Windows.Forms
Imports System.Drawing
Imports ClassRotation.Domain

Namespace UI

    Partial Public Class MainForm

        Private ClassesTab As TabPage
        Private WithEvents ClassList As ListBox
        Private ClassNameBox As TextBox
        Private ClassStart As DateTimePicker
        Private ClassEnd As DateTimePicker
        Private WithEvents ClassHasHandsOn As CheckBox
        Private ClassHandsStart As DateTimePicker
        Private ClassHandsEnd As DateTimePicker
        Private ClassInstructorBox As ComboBox
        Private ClassPrereqList As CheckedListBox
        Private ClassAvailabilityLabel As Label
        Private _suppressClassEvents As Boolean

        Private Sub BuildClassesTab()
            ClassesTab = New TabPage("Classes")

            Dim split As New SplitContainer With {.Dock = DockStyle.Fill, .SplitterDistance = 280}
            ClassesTab.Controls.Add(split)

            ClassList = New ListBox With {.Dock = DockStyle.Fill, .IntegralHeight = False}
            AddHandler ClassList.DrawItem, AddressOf ClassList_DrawItem
            ClassList.DrawMode = DrawMode.OwnerDrawFixed
            ClassList.ItemHeight = 22

            Dim leftPanel As New Panel With {.Dock = DockStyle.Fill}
            Dim leftButtons As New FlowLayoutPanel With {.Dock = DockStyle.Bottom, .Height = 40}
            Dim addBtn As New Button With {.Text = "Add Class"}
            AddHandler addBtn.Click, AddressOf AddClass
            Dim delBtn As New Button With {.Text = "Delete"}
            AddHandler delBtn.Click, AddressOf DeleteClass
            leftButtons.Controls.AddRange({addBtn, delBtn})
            leftPanel.Controls.Add(ClassList)
            leftPanel.Controls.Add(leftButtons)
            split.Panel1.Controls.Add(leftPanel)

            Dim form As New TableLayoutPanel With {.Dock = DockStyle.Fill, .ColumnCount = 2, .Padding = New Padding(12), .AutoScroll = True}
            form.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 130))
            form.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))

            ClassNameBox = New TextBox With {.Width = 340}
            ClassStart = New DateTimePicker With {.Format = DateTimePickerFormat.Short, .Width = 160}
            ClassEnd = New DateTimePicker With {.Format = DateTimePickerFormat.Short, .Width = 160}
            ClassHasHandsOn = New CheckBox With {.Text = "Class has a hands-on phase", .AutoSize = True}
            ClassHandsStart = New DateTimePicker With {.Format = DateTimePickerFormat.Short, .Width = 160}
            ClassHandsEnd = New DateTimePicker With {.Format = DateTimePickerFormat.Short, .Width = 160}
            ClassInstructorBox = New ComboBox With {.DropDownStyle = ComboBoxStyle.DropDownList, .Width = 260}
            AddHandler ClassInstructorBox.SelectedIndexChanged, Sub() UpdateAvailabilityLabel()
            AddHandler ClassStart.ValueChanged, Sub() UpdateAvailabilityLabel()
            AddHandler ClassEnd.ValueChanged, Sub() UpdateAvailabilityLabel()
            AddHandler ClassHandsStart.ValueChanged, Sub() UpdateAvailabilityLabel()
            AddHandler ClassHandsEnd.ValueChanged, Sub() UpdateAvailabilityLabel()
            ClassPrereqList = New CheckedListBox With {.Width = 340, .Height = 150, .CheckOnClick = True}
            ClassAvailabilityLabel = New Label With {.AutoSize = True, .MaximumSize = New Size(400, 0)}

            AddRow(form, "Name:", ClassNameBox)
            AddRow(form, "Academics start:", ClassStart)
            AddRow(form, "Academics end:", ClassEnd)
            AddRow(form, "Hands-on phase:", ClassHasHandsOn)
            AddRow(form, "Hands-on start:", ClassHandsStart)
            AddRow(form, "Hands-on end:", ClassHandsEnd)
            AddRow(form, "Instructor:", ClassInstructorBox)
            AddRow(form, "Instructor status:", ClassAvailabilityLabel)
            AddRow(form, "Required prerequisites:", ClassPrereqList)

            Dim scheduleBtn As New Button With {.Text = "Open Day-by-Day Schedule...", .Width = 220}
            AddHandler scheduleBtn.Click, Sub() OpenClassDetail(SelectedClass())
            AddRow(form, "Detailed schedule:", scheduleBtn)

            Dim saveBtn As New Button With {.Text = "Save Class", .Width = 120}
            AddHandler saveBtn.Click, AddressOf SaveClass
            AddRow(form, "", saveBtn)

            split.Panel2.Controls.Add(form)
        End Sub

        Private Sub AddRow(form As TableLayoutPanel, label As String, control As Control)
            Dim row = form.RowCount
            form.RowCount += 1
            form.RowStyles.Add(New RowStyle(SizeType.AutoSize))
            Dim lbl As New Label With {.Text = label, .AutoSize = True, .Anchor = AnchorStyles.Left, .Margin = New Padding(3, 8, 3, 3)}
            form.Controls.Add(lbl, 0, row)
            form.Controls.Add(control, 1, row)
        End Sub

        Private Sub ClassList_DrawItem(sender As Object, e As DrawItemEventArgs)
            e.DrawBackground()
            If e.Index < 0 Then Return
            Dim cls = DirectCast(ClassList.Items(e.Index), CourseClass)
            Using swatch As New SolidBrush(Theme.ColorFor(cls.Type))
                e.Graphics.FillRectangle(swatch, e.Bounds.Left + 4, e.Bounds.Top + 5, 12, 12)
            End Using
            Using textBrush As New SolidBrush(e.ForeColor)
                e.Graphics.DrawString(cls.Name, e.Font, textBrush, e.Bounds.Left + 22, e.Bounds.Top + 3)
            End Using
            e.DrawFocusRectangle()
        End Sub

        Private Sub RefreshClassesTab()
            Dim selectedId = SelectedClass()?.Id
            _suppressClassEvents = True
            ClassList.Items.Clear()
            For Each c In _data.Classes.OrderBy(Function(x) x.OverallStart)
                ClassList.Items.Add(c)
            Next
            RefreshInstructorCombo()
            RefreshPrereqCheckList()
            _suppressClassEvents = False

            If selectedId IsNot Nothing Then
                SelectClassById(selectedId)
            ElseIf ClassList.Items.Count > 0 Then
                ClassList.SelectedIndex = 0
            Else
                ClearClassEditor()
            End If
        End Sub

        Private Sub RefreshInstructorCombo()
            ClassInstructorBox.Items.Clear()
            ClassInstructorBox.Items.Add("(unassigned)")
            For Each ins In _data.Instructors.OrderBy(Function(x) x.Name)
                ClassInstructorBox.Items.Add(ins)
            Next
        End Sub

        Private Sub RefreshPrereqCheckList()
            ClassPrereqList.Items.Clear()
            For Each p In _data.Prerequisites.OrderBy(Function(x) x.Name)
                ClassPrereqList.Items.Add(p)
            Next
        End Sub

        Private Function SelectedClass() As CourseClass
            Return TryCast(ClassList.SelectedItem, CourseClass)
        End Function

        Private Sub SelectClassById(id As String)
            For i = 0 To ClassList.Items.Count - 1
                If DirectCast(ClassList.Items(i), CourseClass).Id = id Then
                    ClassList.SelectedIndex = i
                    Return
                End If
            Next
        End Sub

        Private Sub ClassList_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ClassList.SelectedIndexChanged
            If _suppressClassEvents Then Return
            LoadClassIntoEditor(SelectedClass())
        End Sub

        Private Sub ClearClassEditor()
            _suppressClassEvents = True
            ClassNameBox.Text = ""
            ClassHasHandsOn.Checked = False
            ClassHandsStart.Enabled = False
            ClassHandsEnd.Enabled = False
            ClassInstructorBox.SelectedIndex = 0
            For i = 0 To ClassPrereqList.Items.Count - 1
                ClassPrereqList.SetItemChecked(i, False)
            Next
            ClassAvailabilityLabel.Text = ""
            _suppressClassEvents = False
        End Sub

        Private Sub LoadClassIntoEditor(cls As CourseClass)
            If cls Is Nothing Then
                ClearClassEditor()
                Return
            End If
            _suppressClassEvents = True
            ClassNameBox.Text = cls.Name
            ClassStart.Value = ClampDate(cls.AcademicsStart)
            ClassEnd.Value = ClampDate(cls.AcademicsEnd)
            ClassHasHandsOn.Checked = cls.HasHandsOn
            ClassHandsStart.Value = ClampDate(If(cls.HasHandsOn, cls.HandsOnStart, cls.AcademicsEnd.AddDays(1)))
            ClassHandsEnd.Value = ClampDate(If(cls.HasHandsOn, cls.HandsOnEnd, cls.AcademicsEnd.AddDays(1)))
            ClassHandsStart.Enabled = cls.HasHandsOn
            ClassHandsEnd.Enabled = cls.HasHandsOn

            ClassInstructorBox.SelectedIndex = 0
            For i = 1 To ClassInstructorBox.Items.Count - 1
                If DirectCast(ClassInstructorBox.Items(i), Instructor).Id = cls.InstructorId Then
                    ClassInstructorBox.SelectedIndex = i
                    Exit For
                End If
            Next

            For i = 0 To ClassPrereqList.Items.Count - 1
                Dim p = DirectCast(ClassPrereqList.Items(i), Prerequisite)
                ClassPrereqList.SetItemChecked(i, cls.RequiredPrerequisiteIds.Contains(p.Id))
            Next
            _suppressClassEvents = False
            UpdateAvailabilityLabel()
        End Sub

        Private Function ClampDate(d As Date) As Date
            If d < New Date(2000, 1, 1) Then Return Date.Today
            If d > New Date(2100, 12, 31) Then Return Date.Today
            Return d
        End Function

        Private Sub UpdateAvailabilityLabel()
            If _suppressClassEvents Then Return
            Dim ins = TryCast(ClassInstructorBox.SelectedItem, Instructor)
            If ins Is Nothing Then
                ClassAvailabilityLabel.Text = "No instructor selected"
                ClassAvailabilityLabel.ForeColor = Color.Gray
                Return
            End If
            Dim spanEnd = If(ClassHasHandsOn.Checked, ClassHandsEnd.Value, ClassEnd.Value)
            Dim probe As New CourseClass With {
                .Id = If(SelectedClass()?.Id, "probe"),
                .StartDate = ClassStart.Value, .EndDate = spanEnd}
            Dim result = InstructorAvailability.Check(ins, probe, _data.Classes)
            ClassAvailabilityLabel.Text = result.Summary
            ClassAvailabilityLabel.ForeColor = If(result.CanHost, Theme.GreenColor, Theme.RedColor)
        End Sub

        Private Sub ClassHasHandsOn_CheckedChanged(sender As Object, e As EventArgs) Handles ClassHasHandsOn.CheckedChanged
            ClassHandsStart.Enabled = ClassHasHandsOn.Checked
            ClassHandsEnd.Enabled = ClassHasHandsOn.Checked
            UpdateAvailabilityLabel()
        End Sub

        Private Sub AddClass(sender As Object, e As EventArgs)
            Dim c As New CourseClass With {
                .Name = "New Class",
                .AcademicsStart = Date.Today, .AcademicsEnd = Date.Today.AddDays(14)}
            c.RecomputeSpan()
            _data.Classes.Add(c)
            RefreshClassesTab()
            SelectClassById(c.Id)
            SetStatus("Added class")
        End Sub

        Private Sub SaveClass(sender As Object, e As EventArgs)
            Dim cls = SelectedClass()
            If cls Is Nothing Then Return
            If ClassEnd.Value.Date < ClassStart.Value.Date Then
                MessageBox.Show(Me, "Academics end date cannot be before its start date.", "Invalid dates", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If
            If ClassHasHandsOn.Checked AndAlso ClassHandsEnd.Value.Date < ClassHandsStart.Value.Date Then
                MessageBox.Show(Me, "Hands-on end date cannot be before its start date.", "Invalid dates", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If
            cls.Name = ClassNameBox.Text.Trim()
            cls.AcademicsStart = ClassStart.Value.Date
            cls.AcademicsEnd = ClassEnd.Value.Date
            cls.HasHandsOn = ClassHasHandsOn.Checked
            cls.HandsOnStart = ClassHandsStart.Value.Date
            cls.HandsOnEnd = ClassHandsEnd.Value.Date
            cls.RecomputeSpan()
            Dim ins = TryCast(ClassInstructorBox.SelectedItem, Instructor)
            cls.InstructorId = If(ins?.Id, "")
            cls.RequiredPrerequisiteIds.Clear()
            For i = 0 To ClassPrereqList.Items.Count - 1
                If ClassPrereqList.GetItemChecked(i) Then
                    cls.RequiredPrerequisiteIds.Add(DirectCast(ClassPrereqList.Items(i), Prerequisite).Id)
                End If
            Next
            SaveData()
            RefreshAll()
            SelectClassById(cls.Id)
            SetStatus($"Saved class '{cls.Name}'")
        End Sub

        Private Sub DeleteClass(sender As Object, e As EventArgs)
            Dim cls = SelectedClass()
            If cls Is Nothing Then Return
            If MessageBox.Show(Me, $"Delete class '{cls.Name}'?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes Then
                _data.Classes.Remove(cls)
                SaveData()
                RefreshAll()
                SetStatus("Class deleted")
            End If
        End Sub

    End Class

End Namespace
