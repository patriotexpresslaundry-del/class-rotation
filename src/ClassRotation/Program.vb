Imports System.Windows.Forms
Imports ClassRotation.UI

Friend Module Program

    <STAThread>
    Sub Main()
        Application.EnableVisualStyles()
        Application.SetCompatibleTextRenderingDefault(False)
        Application.SetHighDpiMode(HighDpiMode.SystemAware)
        Application.Run(New MainForm())
    End Sub

End Module
