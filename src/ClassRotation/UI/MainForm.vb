Imports System.Windows.Forms
Imports System.Drawing
Imports ClassRotation.Data
Imports ClassRotation.Domain

Namespace UI

    ''' <summary>Application shell: owns the data, hosts the tabs, and handles load/save.</summary>
    Partial Public Class MainForm
        Inherits Form

        Private ReadOnly _store As DataStore
        Private _data As AppData

        Private WithEvents Tabs As TabControl
        Private StatusBar As StatusStrip
        Private StatusLabel As ToolStripStatusLabel

        ' Timeline tab
        Private TimelineTab As TabPage
        Private Timeline As TimelineControl
        Private WithEvents YearPicker As NumericUpDown
        Private WithEvents LanesCheck As CheckBox

        Public Sub New()
            _store = New DataStore()
            _data = _store.Load()
            InitializeLayout()
            RefreshAll()
        End Sub

        Private Sub InitializeLayout()
            Text = "Class Rotation Scheduler"
            StartPosition = FormStartPosition.CenterScreen
            MinimumSize = New Size(1000, 640)
            Width = 1200
            Height = 760
            Font = New Font("Segoe UI", 9.0F)

            BuildMenu()

            Tabs = New TabControl With {.Dock = DockStyle.Fill}
            Controls.Add(Tabs)

            BuildTimelineTab()
            BuildClassesTab()
            BuildRosterTab()
            BuildInstructorsTab()
            BuildPrerequisitesTab()

            Tabs.TabPages.AddRange({TimelineTab, ClassesTab, RosterTab, InstructorsTab, PrereqTab})

            StatusBar = New StatusStrip()
            StatusLabel = New ToolStripStatusLabel("Ready")
            StatusBar.Items.Add(StatusLabel)
            Controls.Add(StatusBar)
            ' StatusStrip must be added last but shown at bottom; ensure Tabs fills above it.
            StatusBar.Dock = DockStyle.Bottom
            Tabs.BringToFront()
        End Sub

        Private Sub BuildMenu()
            Dim menu As New MenuStrip()
            Dim fileMenu As New ToolStripMenuItem("&File")
            Dim saveItem As New ToolStripMenuItem("&Save", Nothing, Sub() SaveData()) With {.ShortcutKeys = Keys.Control Or Keys.S}
            Dim openFolderItem As New ToolStripMenuItem("Open &Data Folder", Nothing, Sub() OpenDataFolder())
            Dim exitItem As New ToolStripMenuItem("E&xit", Nothing, Sub() Close())
            fileMenu.DropDownItems.AddRange({saveItem, openFolderItem, New ToolStripSeparator(), exitItem})
            menu.Items.Add(fileMenu)
            MainMenuStrip = menu
            Controls.Add(menu)
        End Sub

        Private Sub OpenDataFolder()
            Try
                Dim dir = IO.Path.GetDirectoryName(_store.FilePath)
                Process.Start(New ProcessStartInfo(dir) With {.UseShellExecute = True})
            Catch
                MessageBox.Show(Me, $"Data file: {_store.FilePath}", "Data Location")
            End Try
        End Sub

        Public Sub SetStatus(text As String)
            StatusLabel.Text = text
        End Sub

        Public Sub SaveData()
            _store.Save(_data)
            SetStatus($"Saved to {_store.FilePath}")
        End Sub

        ''' <summary>Refresh every tab after data changes.</summary>
        Private Sub RefreshAll()
            RefreshTimeline()
            RefreshClassesTab()
            RefreshRosterTab()
            RefreshInstructorsTab()
            RefreshPrereqTab()
        End Sub

        Private Sub BuildTimelineTab()
            TimelineTab = New TabPage("Timeline")

            Dim toolbar As New Panel With {.Dock = DockStyle.Top, .Height = 40}
            Dim lbl As New Label With {.Text = "Year:", .AutoSize = True, .Location = New Point(10, 12)}
            YearPicker = New NumericUpDown With {
                .Minimum = 2000, .Maximum = 2100, .Value = Date.Today.Year,
                .Location = New Point(50, 8), .Width = 70}
            LanesCheck = New CheckBox With {
                .Text = "Separate Academics / OJT lanes", .Checked = True,
                .Location = New Point(140, 10), .AutoSize = True}
            toolbar.Controls.AddRange({lbl, YearPicker, LanesCheck})

            Timeline = New TimelineControl With {.Dock = DockStyle.Fill}

            TimelineTab.Controls.Add(Timeline)
            TimelineTab.Controls.Add(toolbar)
        End Sub

        Private Sub RefreshTimeline()
            Timeline.Year = CInt(YearPicker.Value)
            Timeline.SeparateLanes = LanesCheck.Checked
            Timeline.SetData(_data.Classes, _data.Instructors)
        End Sub

        Private Sub YearPicker_ValueChanged(sender As Object, e As EventArgs) Handles YearPicker.ValueChanged
            RefreshTimeline()
        End Sub

        Private Sub LanesCheck_CheckedChanged(sender As Object, e As EventArgs) Handles LanesCheck.CheckedChanged
            RefreshTimeline()
        End Sub

        Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
            SaveData()
            MyBase.OnFormClosing(e)
        End Sub

    End Class

End Namespace
