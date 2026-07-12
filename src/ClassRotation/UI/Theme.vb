Imports System.Drawing
Imports ClassRotation.Domain

Namespace UI

    ''' <summary>Central colours so academics vs OJT (and Green/Red status) look consistent everywhere.</summary>
    Public Module Theme

        Public ReadOnly AcademicColor As Color = Color.FromArgb(41, 98, 255)    ' blue
        Public ReadOnly OjtColor As Color = Color.FromArgb(255, 145, 0)         ' orange
        Public ReadOnly GreenColor As Color = Color.FromArgb(46, 160, 67)
        Public ReadOnly RedColor As Color = Color.FromArgb(210, 60, 60)

        Public Function ColorFor(t As ClassType) As Color
            Return If(t = ClassType.Academic, AcademicColor, OjtColor)
        End Function

        Public Function StatusColor(s As PrerequisiteStatus) As Color
            Return If(s = PrerequisiteStatus.Green, GreenColor, RedColor)
        End Function

        Public Function TypeLabel(t As ClassType) As String
            Return If(t = ClassType.Academic, "Academics", "OJT")
        End Function

    End Module

End Namespace
