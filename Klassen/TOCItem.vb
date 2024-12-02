Imports System.Collections.ObjectModel

Public Class TOCItem
    Public Property Titel As String
    Public Property Level As Integer
    Public Property HeadingId As String
    Public Property Children As ObservableCollection(Of TOCItem)

    Public Sub New()
        Children = New ObservableCollection(Of TOCItem)()
    End Sub
End Class
