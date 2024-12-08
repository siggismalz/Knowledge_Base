Imports Wpf.Ui.Controls

Public Class Startseite
    Public Sub New()
        InitializeComponent()
        Me.MainFrame.Navigate(New LandingPage)
    End Sub

    Private Sub B_Profil_Click(sender As Object, e As RoutedEventArgs) Handles B_Profil.Click
        Me.MainFrame.Navigate(New Profil)
    End Sub

    Private Sub B_Startseite_Click(sender As Object, e As RoutedEventArgs) Handles B_Startseite.Click
        Me.MainFrame.Navigate(New LandingPage)
    End Sub

    Private Sub B_Artikel_erstellen_Click(sender As Object, e As RoutedEventArgs) Handles B_Artikel_erstellen.Click
        Me.MainFrame.Navigate(New Artikel_erstellen)
    End Sub

    Private Sub B_Explorer_Click(sender As Object, e As RoutedEventArgs) Handles B_Explorer.Click
        Me.MainFrame.Navigate(New Explorer)
    End Sub

    Private Sub B_Tagsuche_Click(sender As Object, e As RoutedEventArgs) Handles B_Tagsuche.Click
        Me.MainFrame.Navigate(New Tagübersicht)
    End Sub

    Private Sub B_Favortiz_Click(sender As Object, e As RoutedEventArgs) Handles B_Favortiz.Click
        Me.MainFrame.Navigate(New FavoritenAnzeigen)
    End Sub

    Private Sub B_EDEKAI_Click(sender As Object, e As RoutedEventArgs) Handles B_EDEKAI.Click
        Me.MainFrame.Navigate(New Chatbot)
    End Sub
End Class
