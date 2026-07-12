Imports System.Collections.Generic
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Linq
Imports System.Windows.Forms
Imports ClassRotation.Domain

Namespace UI

    ''' <summary>
    ''' A simple Gantt-style yearly timeline. Each class is drawn as a horizontal bar spanning
    ''' its start/end dates, colour-coded by type so classroom Academics and OJT are visually
    ''' distinct. Supports separate lanes so the two training types can be compared at a glance.
    ''' </summary>
    Public Class TimelineControl
        Inherits Panel

        Private _classes As New List(Of CourseClass)
        Private _instructors As New Dictionary(Of String, Instructor)
        Private _year As Integer = Date.Today.Year
        Private _separateLanes As Boolean = True

        Private Const LabelWidth As Integer = 210
        Private Const HeaderHeight As Integer = 28
        Private Const RowHeight As Integer = 34
        Private Const TopPad As Integer = 8

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

        ''' <summary>When true, Academics and OJT are grouped into separate labelled bands.</summary>
        Public Property SeparateLanes As Boolean
            Get
                Return _separateLanes
            End Get
            Set(value As Boolean)
                _separateLanes = value
                Invalidate()
            End Set
        End Property

        Public Sub SetData(classes As IEnumerable(Of CourseClass), instructors As IEnumerable(Of Instructor))
            _classes = classes.OrderBy(Function(x) x.StartDate).ToList()
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
            g.SmoothingMode = SmoothingMode.AntiAlias
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit

            Dim yearStart As New Date(_year, 1, 1)
            Dim yearEnd As New Date(_year, 12, 31)
            Dim totalDays = (yearEnd - yearStart).TotalDays + 1

            Dim chartLeft = LabelWidth
            Dim chartWidth = Math.Max(300, Width - LabelWidth - 20)

            DrawMonthGrid(g, chartLeft, chartWidth, yearStart, totalDays)

            Dim rowY = HeaderHeight + TopPad

            If _separateLanes Then
                rowY = DrawBand(g, "CLASSROOM ACADEMICS", ClassType.Academic, chartLeft, chartWidth, yearStart, totalDays, rowY)
                rowY += 6
                rowY = DrawBand(g, "ON-THE-JOB TRAINING", ClassType.OJT, chartLeft, chartWidth, yearStart, totalDays, rowY)
            Else
                For Each cls In _classes
                    DrawClassBar(g, cls, chartLeft, chartWidth, yearStart, totalDays, rowY)
                    rowY += RowHeight
                Next
            End If

            ' Ensure scrollbars size to content.
            AutoScrollMinSize = New Size(chartLeft + chartWidth + 20, rowY + 10)

            DrawLegend(g)
        End Sub

        Private Function DrawBand(g As Graphics, title As String, t As ClassType,
                                  chartLeft As Integer, chartWidth As Integer,
                                  yearStart As Date, totalDays As Double, startY As Integer) As Integer
            Dim items = _classes.Where(Function(c) c.Type = t).ToList()
            Using headerFont As New Font(Font.FontFamily, 8.5F, FontStyle.Bold)
                Using accent As New SolidBrush(Theme.ColorFor(t))
                    g.FillRectangle(accent, 6, startY + 6, 6, RowHeight - 12)
                End Using
                g.DrawString(title, headerFont, Brushes.DimGray, New PointF(18, startY + 8))
            End Using
            Dim rowY = startY + RowHeight
            If items.Count = 0 Then
                g.DrawString("(none scheduled)", Font, Brushes.Silver, New PointF(24, rowY + 4))
                Return rowY + RowHeight
            End If
            For Each cls In items
                DrawClassBar(g, cls, chartLeft, chartWidth, yearStart, totalDays, rowY)
                rowY += RowHeight
            Next
            Return rowY
        End Function

        Private Sub DrawMonthGrid(g As Graphics, chartLeft As Integer, chartWidth As Integer,
                                  yearStart As Date, totalDays As Double)
            Using gridPen As New Pen(Color.FromArgb(230, 230, 230))
                For m = 1 To 12
                    Dim monthStart As New Date(_year, m, 1)
                    Dim x = chartLeft + CInt((monthStart - yearStart).TotalDays / totalDays * chartWidth)
                    g.DrawLine(gridPen, x, HeaderHeight, x, Height)
                    g.DrawString(monthStart.ToString("MMM"), Font, Brushes.Gray, New PointF(x + 2, 6))
                Next
            End Using
            Using border As New Pen(Color.FromArgb(200, 200, 200))
                g.DrawLine(border, chartLeft, HeaderHeight, chartLeft + chartWidth, HeaderHeight)
            End Using

            ' Today marker.
            If Date.Today.Year = _year Then
                Dim tx = chartLeft + CInt((Date.Today - yearStart).TotalDays / totalDays * chartWidth)
                Using todayPen As New Pen(Color.FromArgb(120, 200, 60, 60), 1)
                    todayPen.DashStyle = DashStyle.Dash
                    g.DrawLine(todayPen, tx, HeaderHeight, tx, Height)
                End Using
            End If
        End Sub

        Private Sub DrawClassBar(g As Graphics, cls As CourseClass,
                                 chartLeft As Integer, chartWidth As Integer,
                                 yearStart As Date, totalDays As Double, rowY As Integer)
            g.DrawString($"{cls.Name}", Font, Brushes.Black, New PointF(6, rowY + 3))
            Using subFont As New Font(Font.FontFamily, 7.5F)
                g.DrawString(InstructorName(cls.InstructorId), subFont, Brushes.Gray, New PointF(6, rowY + 17))
            End Using

            Dim startOffset = Math.Max(0, (cls.StartDate.Date - yearStart).TotalDays)
            Dim endOffset = Math.Min(totalDays, (cls.EndDate.Date - yearStart).TotalDays + 1)
            If endOffset <= startOffset Then endOffset = startOffset + 1

            Dim x1 = chartLeft + CInt(startOffset / totalDays * chartWidth)
            Dim x2 = chartLeft + CInt(endOffset / totalDays * chartWidth)
            Dim barRect As New Rectangle(x1, rowY + 6, Math.Max(4, x2 - x1), RowHeight - 14)

            Using b As New SolidBrush(Theme.ColorFor(cls.Type))
                Using path = RoundedRect(barRect, 5)
                    g.FillPath(b, path)
                End Using
            End Using
            Dim label = $"{cls.StartDate:MMM d} - {cls.EndDate:MMM d}"
            Using lblFont As New Font(Font.FontFamily, 7.5F, FontStyle.Bold)
                Dim sz = g.MeasureString(label, lblFont)
                If sz.Width < barRect.Width - 6 Then
                    g.DrawString(label, lblFont, Brushes.White, New PointF(barRect.X + 4, barRect.Y + 2))
                End If
            End Using
        End Sub

        Private Sub DrawLegend(g As Graphics)
            Dim y = 6
            Dim x = Width - 190
            If x < LabelWidth Then Return
            DrawSwatch(g, x, y, Theme.AcademicColor, "Academics")
            DrawSwatch(g, x + 95, y, Theme.OjtColor, "OJT")
        End Sub

        Private Sub DrawSwatch(g As Graphics, x As Integer, y As Integer, c As Color, text As String)
            Using b As New SolidBrush(c)
                g.FillRectangle(b, x, y + 2, 12, 12)
            End Using
            g.DrawString(text, Font, Brushes.Black, New PointF(x + 16, y))
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
