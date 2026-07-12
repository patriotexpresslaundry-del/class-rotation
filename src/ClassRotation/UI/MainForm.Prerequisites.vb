Imports System.Linq
Imports System.Windows.Forms
Imports System.Drawing
Imports ClassRotation.Domain

Namespace UI

    Partial Public Class MainForm

        Private PrereqTab As TabPage
        Private WithEvents PrereqList As ListBox
        Private PrereqNameBox As TextBox
        Private PrereqDescBox As TextBox
        Private _suppressPrereqEvents As Boolean

        Private Sub BuildPrerequisitesTab()
            PrereqTab = New TabPage("Prerequisites")

            Dim split As New SplitContainer With {.Dock = DockStyle.Fill, .SplitterDistance = 260}
            PrereqTab.Controls.Add(split)

            PrereqList = New ListBox With {.Dock = DockStyle.Fill, .IntegralHeight = False}
            Dim leftButtons As New FlowLayoutPanel With {.Dock = DockStyle.Bottom, .Height = 40}
            Dim addBtn As New Button With {.Text = "Add"}
            AddHandler addBtn.Click, AddressOf AddPrereq
            Dim delBtn As New Button With {.Text = "Delete"}
            AddHandler delBtn.Click, AddressOf DeletePrereq
            leftButtons.Controls.AddRange({addBtn, delBtn})
            Dim leftPanel As New Panel With {.Dock = DockStyle.Fill}
            leftPanel.Controls.Add(PrereqList)
            leftPanel.Controls.Add(leftButtons)
            split.Panel1.Controls.Add(leftPanel)

            Dim right As New Panel With {.Dock = DockStyle.Fill, .Padding = New Padding(12)}
            Dim nameLbl As New Label With {.Text = "Name:", .AutoSize = True, .Location = New Point(0, 10)}
            PrereqNameBox = New TextBox With {.Location = New Point(90, 7), .Width = 320}
            Dim descLbl As New Label With {.Text = "Description:", .AutoSize = True, .Location = New Point(0, 45)}
            PrereqDescBox = New TextBox With {.Location = New Point(90, 42), .Width = 320, .Height = 90, .Multiline = True}
            Dim saveBtn As New Button With {.Text = "Save", .Location = New Point(90, 145), .Width = 100}
            AddHandler saveBtn.Click, AddressOf SavePrereq
            Dim note As New Label With {
                .Text = "Prerequisites defined here can be required by classes and marked complete per student (Roster tab).",
                .AutoSize = True, .MaximumSize = New Size(420, 0), .ForeColor = Color.Gray, .Location = New Point(0, 190)}
            right.Controls.AddRange({nameLbl, PrereqNameBox, descLbl, PrereqDescBox, saveBtn, note})
            split.Panel2.Controls.Add(right)
        End Sub

        Private Sub RefreshPrereqTab()
            Dim selectedId = SelectedPrereq()?.Id
            _suppressPrereqEvents = True
            PrereqList.Items.Clear()
            For Each p In _data.Prerequisites.OrderBy(Function(x) x.Name)
                PrereqList.Items.Add(p)
            Next
            _suppressPrereqEvents = False
            If selectedId IsNot Nothing Then
                SelectPrereqById(selectedId)
            ElseIf PrereqList.Items.Count > 0 Then
                PrereqList.SelectedIndex = 0
            Else
                PrereqNameBox.Text = ""
                PrereqDescBox.Text = ""
            End If
        End Sub

        Private Function SelectedPrereq() As Prerequisite
            Return TryCast(PrereqList.SelectedItem, Prerequisite)
        End Function

        Private Sub SelectPrereqById(id As String)
            For i = 0 To PrereqList.Items.Count - 1
                If DirectCast(PrereqList.Items(i), Prerequisite).Id = id Then
                    PrereqList.SelectedIndex = i
                    Return
                End If
            Next
        End Sub

        Private Sub PrereqList_SelectedIndexChanged(sender As Object, e As EventArgs) Handles PrereqList.SelectedIndexChanged
            If _suppressPrereqEvents Then Return
            Dim p = SelectedPrereq()
            If p Is Nothing Then Return
            PrereqNameBox.Text = p.Name
            PrereqDescBox.Text = p.Description
        End Sub

        Private Sub AddPrereq(sender As Object, e As EventArgs)
            Dim p As New Prerequisite With {.Name = "New Prerequisite"}
            _data.Prerequisites.Add(p)
            SaveData()
            RefreshAll()
            SelectPrereqById(p.Id)
            SetStatus("Added prerequisite")
        End Sub

        Private Sub SavePrereq(sender As Object, e As EventArgs)
            Dim p = SelectedPrereq()
            If p Is Nothing Then Return
            p.Name = PrereqNameBox.Text.Trim()
            p.Description = PrereqDescBox.Text.Trim()
            SaveData()
            RefreshAll()
            SelectPrereqById(p.Id)
            SetStatus($"Saved prerequisite '{p.Name}'")
        End Sub

        Private Sub DeletePrereq(sender As Object, e As EventArgs)
            Dim p = SelectedPrereq()
            If p Is Nothing Then Return
            If MessageBox.Show(Me, $"Delete prerequisite '{p.Name}'? It will be removed from all classes and students.", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes Then
                _data.Prerequisites.Remove(p)
                For Each c In _data.Classes
                    c.RequiredPrerequisiteIds.Remove(p.Id)
                Next
                For Each s In _data.Students
                    s.CompletedPrerequisiteIds.Remove(p.Id)
                Next
                SaveData()
                RefreshAll()
                SetStatus("Prerequisite deleted")
            End If
        End Sub

    End Class

End Namespace
