Imports System.Collections.Generic
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Linq
Imports System.Windows.Forms
Imports ClassRotation.Domain

Namespace UI

    ''' <summary>
    ''' A Gantt-style yearly timeline. Each class occupies one row and is drawn as a bar split into
    ''' its Academics phase (blue) and Hands-on phase (orange). Bars are clickable: clicking one
    ''' raises <see cref="ClassClicked"/> so the caller can open the day-by-day detail view.
    ''' </summary>
    Public Class TimelineControl
        Inherits Panel

        Private _classes As New List(Of CourseClass)
        Private _instructors As New Dictionary(Of String, Instructor)
        Private _year As Integer = Date.Today.Year
        Private ReadOnly _hitAreas As New List(Of (Bounds As Rectangle, Cls As CourseClass))

        Private Const LabelWidth As Integer = 210
        Private Const HeaderHeight As Integer = 28
        Private Const RowHeight As Integer = 34
        Private Const TopPad As Integer = 8

        ''' <summary>Raised when the user clicks a class bar (or its label row).</summary>
        Public Event ClassClicked(cls As CourseClass)

        Public Sub New()
            DoubleBuffered = True
            AutoScroll = True
            BackColor = Color.White
        End Sub

        Public Property Year As Integer
            Get
                Return _year
            End Get
            Set(value As Integer)
                _year = value
                Invalidate()
            End Set
        End Property

        Public Sub SetData(classes As IEnumerable(Of CourseClass), instructors As IEnumerable(Of Instructor))
            _classes = classes.OrderBy(Function(x) x.OverallStart).ToList()
            _instructors = instructors.ToDictionary(Function(i) i.Id, Function(i) i)
            Invalidate()
        End Sub

        Private Function InstructorName(id As String) As String
            Dim ins As Instructor = Nothing
            If _instructors.TryGetValue(id, ins) Then Return ins.Name
            Return "(unassigned)"
        End Function

        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            MyBase.OnPaint(e)
            Dim g = e.Graphics
            g.TranslateTransform(AutoScrollPosition.X, AutoScrollPosition.Y)
            g.SmoothingMode = SmoothingMode.AntiAlias
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit

            _hitAreas.Clear()

            Dim yearStart As New Date(_year, 1, 1)
            Dim yearEnd As New Date(_year, 12, 31)
            Dim totalDays = (yearEnd - yearStart).TotalDays + 1

            Dim chartLeft = LabelWidth
            Dim chartWidth = Math.Max(300, Width - LabelWidth - 20)

            Dim rowY = HeaderHeight + TopPad
            For Each cls In _classes
                DrawClassRow(g, cls, chartLeft, chartWidth, yearStart, totalDays, rowY)
                rowY += RowHeight
            Next

            DrawMonthGrid(g, chartLeft, chartWidth, yearStart, totalDays, rowY)
            AutoScrollMinSize = New Size(chartLeft + chartWidth + 20, rowY + 10)
            DrawLegend(g)
        End Sub

        Private Sub DrawMonthGrid(g As Graphics, chartLeft As Integer, chartWidth As Integer,
                                  yearStart As Date, totalDays As Double, bottom As Integer)
            Dim gridBottom = Math.Max(bottom, Height)
            Using gridPen As New Pen(Color.FromArgb(230, 230, 230))
                For m = 1 To 12
                    Dim monthStart As New Date(_year, m, 1)
                    Dim x = chartLeft + CInt((monthStart - yearStart).TotalDays / totalDays * chartWidth)
                    g.DrawLine(gridPen, x, HeaderHeight, x, gridBottom)
                    g.DrawString(monthStart.ToString("MMM"), Font, Brushes.Gray, New PointF(x + 2, 6))
                Next
            End Using
            Using border As New Pen(Color.FromArgb(200, 200, 200))
                g.DrawLine(border, chartLeft, HeaderHeight, chartLeft + chartWidth, HeaderHeight)
            End Using

            If Date.Today.Year = _year Then
                Dim tx = chartLeft + CInt((Date.Today - yearStart).TotalDays / totalDays * chartWidth)
                Using todayPen As New Pen(Color.FromArgb(120, 200, 60, 60), 1)
                    todayPen.DashStyle = DashStyle.Dash
                    g.DrawLine(todayPen, tx, HeaderHeight, tx, gridBottom)
                End Using
            End If
        End Sub

        Private Sub DrawClassRow(g As Graphics, cls As CourseClass,
                                 chartLeft As Integer, chartWidth As Integer,
                                 yearStart As Date, totalDays As Double, rowY As Integer)
            g.DrawString(cls.Name, Font, Brushes.Black, New PointF(6, rowY + 3))
            Using subFont As New Font(Font.FontFamily, 7.5F)
                g.DrawString(InstructorName(cls.InstructorId), subFont, Brushes.Gray, New PointF(6, rowY + 17))
            End Using

            ' Academics segment.
            Dim acadRect = DrawSegment(g, cls.AcademicsStart, cls.AcademicsEnd, Theme.AcademicColor,
                                       chartLeft, chartWidth, yearStart, totalDays, rowY)
            ' Hands-on segment.
            Dim rowBounds As Rectangle = acadRect
            If cls.HasHandsOn Then
                Dim handsRect = DrawSegment(g, cls.HandsOnStart, cls.HandsOnEnd, Theme.OjtColor,
                                            chartLeft, chartWidth, yearStart, totalDays, rowY)
                rowBounds = Rectangle.Union(acadRect, handsRect)
            End If

            ' The whole row (label + bars) is clickable.
            Dim clickable As New Rectangle(0, rowY, chartLeft + chartWidth, RowHeight)
            _hitAreas.Add((clickable, cls))
        End Sub

        Private Function DrawSegment(g As Graphics, startDate As Date, endDate As Date, color As Color,
                                     chartLeft As Integer, chartWidth As Integer,
                                     yearStart As Date, totalDays As Double, rowY As Integer) As Rectangle
            Dim startOffset = Math.Max(0, (startDate.Date - yearStart).TotalDays)
            Dim endOffset = Math.Min(totalDays, (endDate.Date - yearStart).TotalDays + 1)
            If endOffset <= startOffset Then endOffset = startOffset + 1

            Dim x1 = chartLeft + CInt(startOffset / totalDays * chartWidth)
            Dim x2 = chartLeft + CInt(endOffset / totalDays * chartWidth)
            Dim rect As New Rectangle(x1, rowY + 6, Math.Max(4, x2 - x1), RowHeight - 14)

            Using b As New SolidBrush(color)
                Using path = RoundedRect(rect, 5)
                    g.FillPath(b, path)
                End Using
            End Using
            Return rect
        End Function

        Private Sub DrawLegend(g As Graphics)
            Dim y = 6
            Dim x = Width - 230
            If x < LabelWidth Then Return
            DrawSwatch(g, x, y, Theme.AcademicColor, "Academics")
            DrawSwatch(g, x + 100, y, Theme.OjtColor, "Hands-on")
        End Sub

        Private Sub DrawSwatch(g As Graphics, x As Integer, y As Integer, c As Color, text As String)
            Using b As New SolidBrush(c)
                g.FillRectangle(b, x, y + 2, 12, 12)
            End Using
            g.DrawString(text, Font, Brushes.Black, New PointF(x + 16, y))
        End Sub

        Private Function ClassAt(clientPoint As Point) As CourseClass
            ' Translate client coordinates into the scrolled drawing space.
            Dim p As New Point(clientPoint.X - AutoScrollPosition.X, clientPoint.Y - AutoScrollPosition.Y)
            For Each area In _hitAreas
                If area.Bounds.Contains(p) Then Return area.Cls
            Next
            Return Nothing
        End Function

        Protected Overrides Sub OnMouseClick(e As MouseEventArgs)
            MyBase.OnMouseClick(e)
            If e.Button <> MouseButtons.Left Then Return
            Dim cls = ClassAt(e.Location)
            If cls IsNot Nothing Then RaiseEvent ClassClicked(cls)
        End Sub

        Protected Overrides Sub OnMouseMove(e As MouseEventArgs)
            MyBase.OnMouseMove(e)
            Cursor = If(ClassAt(e.Location) IsNot Nothing, Cursors.Hand, Cursors.Default)
        End Sub

        Private Shared Function RoundedRect(r As Rectangle, radius As Integer) As GraphicsPath
            Dim path As New GraphicsPath()
            Dim d = radius * 2
            path.AddArc(r.X, r.Y, d, d, 180, 90)
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90)
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90)
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90)
            path.CloseFigure()
            Return path
        End Function

        Protected Overrides Sub OnResize(eventargs As EventArgs)
            MyBase.OnResize(eventargs)
            Invalidate()
        End Sub

    End Class

End Namespace
