Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Linq
Imports System.Windows.Forms
Imports System.Drawing
Imports ClassRotation.Domain

Namespace UI

    ''' <summary>
    ''' Day-by-day, period-by-period detail view for a single class. The left pane lists every
    ''' training day; the right pane shows an editable timetable of periods for the selected day.
    ''' Edits mutate the class in place; the caller persists on close.
    ''' </summary>
    Public Class ClassDetailForm
        Inherits Form

        Private ReadOnly _cls As CourseClass
        Private ReadOnly _data As AppData

        Private ReadOnly _dayList As New ListBox()
        Private ReadOnly _grid As New DataGridView()
        Private ReadOnly _headerLabel As New Label()
        Private ReadOnly _instructorOptions As New List(Of InstructorOption)
        Private _binding As BindingList(Of ClassPeriod)
        Private _suppress As Boolean

        Private Class InstructorOption
            Public Property Id As String
            Public Property Name As String
        End Class

        Public Sub New(cls As CourseClass, data As AppData)
            _cls = cls
            _data = data
            BuildLayout()
            RefreshDayList()
        End Sub

        Private Sub BuildLayout()
            Text = $"Schedule - {_cls.Name}"
            StartPosition = FormStartPosition.CenterParent
            MinimumSize = New Size(900, 560)
            Width = 1040
            Height = 640
            Font = New Font("Segoe UI", 9.0F)

            _headerLabel.Dock = DockStyle.Top
            _headerLabel.Height = 46
            _headerLabel.Padding = New Padding(10, 8, 10, 4)
            _headerLabel.Text = HeaderText()

            Dim split As New SplitContainer With {.Dock = DockStyle.Fill, .SplitterDistance = 240}

            ' Left: days + buttons.
            _dayList.Dock = DockStyle.Fill
            _dayList.IntegralHeight = False
            AddHandler _dayList.SelectedIndexChanged, AddressOf DaySelected

            Dim leftButtons As New FlowLayoutPanel With {.Dock = DockStyle.Bottom, .Height = 76, .FlowDirection = FlowDirection.TopDown, .WrapContents = False}
            Dim genBtn As New Button With {.Text = "Generate Weekday Skeleton", .Width = 210}
            AddHandler genBtn.Click, AddressOf GenerateSkeleton
            Dim addDayBtn As New Button With {.Text = "Add Day", .Width = 100}
            AddHandler addDayBtn.Click, AddressOf AddDay
            Dim delDayBtn As New Button With {.Text = "Remove Day", .Width = 100}
            AddHandler delDayBtn.Click, AddressOf RemoveDay
            Dim dayBtnRow As New FlowLayoutPanel With {.AutoSize = True}
            dayBtnRow.Controls.AddRange({addDayBtn, delDayBtn})
            leftButtons.Controls.AddRange({genBtn, dayBtnRow})

            Dim leftPanel As New Panel With {.Dock = DockStyle.Fill}
            leftPanel.Controls.Add(_dayList)
            leftPanel.Controls.Add(leftButtons)
            split.Panel1.Controls.Add(leftPanel)

            ' Right: periods grid + buttons.
            ConfigureGrid()
            Dim rightButtons As New FlowLayoutPanel With {.Dock = DockStyle.Bottom, .Height = 40}
            Dim addPeriodBtn As New Button With {.Text = "Add Period"}
            AddHandler addPeriodBtn.Click, AddressOf AddPeriod
            Dim delPeriodBtn As New Button With {.Text = "Remove Period"}
            AddHandler delPeriodBtn.Click, AddressOf RemovePeriod
            Dim closeBtn As New Button With {.Text = "Close"}
            AddHandler closeBtn.Click, Sub() Close()
            rightButtons.Controls.AddRange({addPeriodBtn, delPeriodBtn, closeBtn})

            Dim rightPanel As New Panel With {.Dock = DockStyle.Fill}
            rightPanel.Controls.Add(_grid)
            rightPanel.Controls.Add(rightButtons)
            split.Panel2.Controls.Add(rightPanel)

            Controls.Add(split)
            Controls.Add(_headerLabel)
        End Sub

        Private Function HeaderText() As String
            Dim s = $"{_cls.Name}    Academics: {_cls.AcademicsStart:MMM d, yyyy} - {_cls.AcademicsEnd:MMM d, yyyy}"
            If _cls.HasHandsOn Then
                s &= $"    Hands-on: {_cls.HandsOnStart:MMM d, yyyy} - {_cls.HandsOnEnd:MMM d, yyyy}"
            End If
            Return s
        End Function

        Private Sub ConfigureGrid()
            _grid.Dock = DockStyle.Fill
            _grid.AutoGenerateColumns = False
            _grid.AllowUserToAddRows = False
            _grid.AllowUserToDeleteRows = False
            _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect
            _grid.RowHeadersVisible = False

            _instructorOptions.Clear()
            _instructorOptions.Add(New InstructorOption With {.Id = "", .Name = "(unassigned)"})
            For Each ins In _data.Instructors.OrderBy(Function(x) x.Name)
                _instructorOptions.Add(New InstructorOption With {.Id = ins.Id, .Name = ins.Name})
            Next

            _grid.Columns.Add(New DataGridViewTextBoxColumn With {
                .HeaderText = "Start", .DataPropertyName = "StartTime", .Width = 70})
            _grid.Columns.Add(New DataGridViewTextBoxColumn With {
                .HeaderText = "End", .DataPropertyName = "EndTime", .Width = 70})

            Dim phaseCol As New DataGridViewComboBoxColumn With {
                .HeaderText = "Phase", .DataPropertyName = "Phase", .Width = 100,
                .DataSource = [Enum].GetValues(GetType(PhaseType))}
            _grid.Columns.Add(phaseCol)

            _grid.Columns.Add(New DataGridViewTextBoxColumn With {
                .HeaderText = "Subject", .DataPropertyName = "Subject", .Width = 220})

            Dim insCol As New DataGridViewComboBoxColumn With {
                .HeaderText = "Instructor", .DataPropertyName = "InstructorId", .Width = 150,
                .DataSource = _instructorOptions, .DisplayMember = "Name", .ValueMember = "Id"}
            _grid.Columns.Add(insCol)

            _grid.Columns.Add(New DataGridViewTextBoxColumn With {
                .HeaderText = "Room", .DataPropertyName = "Room", .Width = 90})
            _grid.Columns.Add(New DataGridViewTextBoxColumn With {
                .HeaderText = "Notes", .DataPropertyName = "Notes",
                .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill})

            AddHandler _grid.DataError, Sub(s, e) e.ThrowException = False
        End Sub

        Private Sub RefreshDayList()
            _suppress = True
            Dim prevIndex = _dayList.SelectedIndex
            _dayList.Items.Clear()
            _cls.Schedule.Sort(Function(a, b) a.Date.CompareTo(b.Date))
            For Each d In _cls.Schedule
                _dayList.Items.Add($"{d.Date:ddd MMM d, yyyy}  ({d.Periods.Count})")
            Next
            _suppress = False
            If _dayList.Items.Count > 0 Then
                _dayList.SelectedIndex = Math.Min(Math.Max(0, prevIndex), _dayList.Items.Count - 1)
            Else
                _grid.DataSource = Nothing
            End If
        End Sub

        Private Function SelectedDay() As ClassDay
            Dim i = _dayList.SelectedIndex
            If i < 0 OrElse i >= _cls.Schedule.Count Then Return Nothing
            Return _cls.Schedule(i)
        End Function

        Private Sub DaySelected(sender As Object, e As EventArgs)
            If _suppress Then Return
            Dim day = SelectedDay()
            If day Is Nothing Then
                _grid.DataSource = Nothing
                Return
            End If
            _binding = New BindingList(Of ClassPeriod)(day.Periods)
            _grid.DataSource = _binding
        End Sub

        Private Sub GenerateSkeleton(sender As Object, e As EventArgs)
            If _cls.Schedule.Count > 0 Then
                If MessageBox.Show(Me, "Replace the current schedule with a fresh weekday skeleton?",
                                   "Generate Schedule", MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then
                    Return
                End If
            End If
            _cls.Schedule = ScheduleGenerator.Generate(_cls)
            RefreshDayList()
        End Sub

        Private Sub AddDay(sender As Object, e As EventArgs)
            Dim seed = If(SelectedDay()?.Date, _cls.AcademicsStart).AddDays(1)
            Dim day As New ClassDay With {.Date = seed}
            _cls.Schedule.Add(day)
            RefreshDayList()
            SelectDay(day)
        End Sub

        Private Sub SelectDay(day As ClassDay)
            Dim idx = _cls.Schedule.IndexOf(day)
            If idx >= 0 AndAlso idx < _dayList.Items.Count Then _dayList.SelectedIndex = idx
        End Sub

        Private Sub RemoveDay(sender As Object, e As EventArgs)
            Dim day = SelectedDay()
            If day Is Nothing Then Return
            _cls.Schedule.Remove(day)
            RefreshDayList()
        End Sub

        Private Sub AddPeriod(sender As Object, e As EventArgs)
            Dim day = SelectedDay()
            If day Is Nothing Then
                MessageBox.Show(Me, "Add or select a day first.", "No day selected", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If
            Dim phase = If(_cls.HasHandsOn AndAlso day.Date.Date >= _cls.HandsOnStart.Date, PhaseType.HandsOn, PhaseType.Academics)
            _binding.Add(New ClassPeriod With {.Phase = phase, .InstructorId = _cls.InstructorId, .Subject = "TBD"})
            RefreshDayCountLabel()
        End Sub

        Private Sub RemovePeriod(sender As Object, e As EventArgs)
            If _grid.CurrentRow Is Nothing Then Return
            Dim p = TryCast(_grid.CurrentRow.DataBoundItem, ClassPeriod)
            If p IsNot Nothing AndAlso _binding IsNot Nothing Then
                _binding.Remove(p)
                RefreshDayCountLabel()
            End If
        End Sub

        Private Sub RefreshDayCountLabel()
            Dim i = _dayList.SelectedIndex
            Dim day = SelectedDay()
            If i < 0 OrElse day Is Nothing Then Return
            _suppress = True
            _dayList.Items(i) = $"{day.Date:ddd MMM d, yyyy}  ({day.Periods.Count})"
            _suppress = False
        End Sub

    End Class

End Namespace
