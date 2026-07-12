Imports System.Collections.Generic
Imports System.Drawing
Imports System.Windows.Forms

Namespace UI

    ''' <summary>A tiny modal list picker returning the chosen item (or Nothing if cancelled).</summary>
    Public Class PickerDialog
        Inherits Form

        Private ReadOnly _list As ListBox

        Private Sub New(title As String, items As IEnumerable(Of Object))
            Text = title
            StartPosition = FormStartPosition.CenterParent
            FormBorderStyle = FormBorderStyle.FixedDialog
            MinimizeBox = False
            MaximizeBox = False
            Width = 360
            Height = 380

            _list = New ListBox With {.Dock = DockStyle.Fill, .IntegralHeight = False}
            For Each it In items
                _list.Items.Add(it)
            Next
            If _list.Items.Count > 0 Then _list.SelectedIndex = 0
            AddHandler _list.DoubleClick, Sub() Accept()

            Dim buttons As New FlowLayoutPanel With {.Dock = DockStyle.Bottom, .Height = 44, .FlowDirection = FlowDirection.RightToLeft, .Padding = New Padding(8)}
            Dim ok As New Button With {.Text = "OK", .DialogResult = DialogResult.OK, .Width = 80}
            AddHandler ok.Click, Sub() Accept()
            Dim cancel As New Button With {.Text = "Cancel", .DialogResult = DialogResult.Cancel, .Width = 80}
            buttons.Controls.AddRange({cancel, ok})

            Controls.Add(_list)
            Controls.Add(buttons)
            AcceptButton = ok
            CancelButton = cancel
        End Sub

        Private Sub Accept()
            If _list.SelectedItem IsNot Nothing Then
                DialogResult = DialogResult.OK
                Close()
            End If
        End Sub

        Public Shared Function Pick(owner As IWin32Window, title As String, items As List(Of Object)) As Object
            Using dlg As New PickerDialog(title, items)
                If dlg.ShowDialog(owner) = DialogResult.OK Then
                    Return dlg._list.SelectedItem
                End If
                Return Nothing
            End Using
        End Function

    End Class

    ''' <summary>An item shown in a <see cref="CheckListDialog"/> with its checked state.</summary>
    Public Class CheckItem
        Public Property Key As String
        Public Property Label As String
        Public Property Checked As Boolean
        Public Overrides Function ToString() As String
            Return Label
        End Function
    End Class

    ''' <summary>A modal multi-select checklist. Returns the keys the user left checked, or Nothing if cancelled.</summary>
    Public Class CheckListDialog
        Inherits Form

        Private ReadOnly _list As CheckedListBox

        Private Sub New(title As String, prompt As String, items As IEnumerable(Of CheckItem))
            Text = title
            StartPosition = FormStartPosition.CenterParent
            FormBorderStyle = FormBorderStyle.FixedDialog
            MinimizeBox = False
            MaximizeBox = False
            Width = 400
            Height = 420

            Dim lbl As New Label With {.Text = prompt, .Dock = DockStyle.Top, .Height = 26, .Padding = New Padding(6, 6, 0, 0)}
            _list = New CheckedListBox With {.Dock = DockStyle.Fill, .CheckOnClick = True, .IntegralHeight = False}
            For Each it In items
                _list.Items.Add(it, it.Checked)
            Next

            Dim buttons As New FlowLayoutPanel With {.Dock = DockStyle.Bottom, .Height = 44, .FlowDirection = FlowDirection.RightToLeft, .Padding = New Padding(8)}
            Dim ok As New Button With {.Text = "OK", .DialogResult = DialogResult.OK, .Width = 80}
            Dim cancel As New Button With {.Text = "Cancel", .DialogResult = DialogResult.Cancel, .Width = 80}
            buttons.Controls.AddRange({cancel, ok})

            Controls.Add(_list)
            Controls.Add(buttons)
            Controls.Add(lbl)
            AcceptButton = ok
            CancelButton = cancel
        End Sub

        Public Shared Function Edit(owner As IWin32Window, title As String, prompt As String, items As List(Of CheckItem)) As List(Of String)
            Using dlg As New CheckListDialog(title, prompt, items)
                If dlg.ShowDialog(owner) <> DialogResult.OK Then Return Nothing
                Dim result As New List(Of String)
                For i = 0 To dlg._list.Items.Count - 1
                    If dlg._list.GetItemChecked(i) Then
                        result.Add(DirectCast(dlg._list.Items(i), CheckItem).Key)
                    End If
                Next
                Return result
            End Using
        End Function

    End Class

    ''' <summary>A single-line text prompt. Returns Nothing when cancelled.</summary>
    Public Class InputDialog
        Inherits Form

        Private ReadOnly _box As TextBox

        Private Sub New(title As String, prompt As String, initial As String)
            Text = title
            StartPosition = FormStartPosition.CenterParent
            FormBorderStyle = FormBorderStyle.FixedDialog
            MinimizeBox = False
            MaximizeBox = False
            Width = 420
            Height = 170

            Dim lbl As New Label With {.Text = prompt, .AutoSize = True, .Location = New Point(12, 15)}
            _box = New TextBox With {.Location = New Point(15, 40), .Width = 375, .Text = initial}

            Dim ok As New Button With {.Text = "OK", .DialogResult = DialogResult.OK, .Location = New Point(230, 85), .Width = 75}
            Dim cancel As New Button With {.Text = "Cancel", .DialogResult = DialogResult.Cancel, .Location = New Point(315, 85), .Width = 75}

            Controls.AddRange({lbl, _box, ok, cancel})
            AcceptButton = ok
            CancelButton = cancel
        End Sub

        Public Shared Function Ask(owner As IWin32Window, title As String, prompt As String, Optional initial As String = "") As String
            Using dlg As New InputDialog(title, prompt, initial)
                If dlg.ShowDialog(owner) = DialogResult.OK Then
                    Return dlg._box.Text.Trim()
                End If
                Return Nothing
            End Using
        End Function

    End Class

End Namespace
