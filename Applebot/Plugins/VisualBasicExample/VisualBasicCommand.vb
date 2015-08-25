Imports ApplebotAPI

<PlatformRegistrar(GetType(Platform))> Public Class VisualBasicCommand
    Inherits Command

    Public Sub New()
        MyBase.New("Visual Basic Command")
    End Sub

    Public Overrides Sub HandleMessage(Of T1 As Message, T2 As Platform)(message As T1, sender As T2)
        Logger.Log("Response from Visual Basic Command")
    End Sub
End Class
