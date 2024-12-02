Public Class ArtikelVersion
    Public Property VersionId As Integer
    Public Property Versioniert_am As DateTime
    Public Property Titel As String

    ' Eigenschaft für die Anzeige in der ListBox
    Public ReadOnly Property AnzeigeText As String
        Get
            If VersionId = 0 Then
                Return "Aktuelle Version"
            Else
                Return Versioniert_am.ToString("dd.MM.yyyy HH:mm")
            End If
        End Get
    End Property
End Class
