
Imports System.Windows
Public Class Passwortändern
    Public Property NewPassword As String

    Private Sub BtnChange_Click(sender As Object, e As RoutedEventArgs)
        Dim neuesPasswort As String = pwdNewPassword.Password
        Dim bestätigtesPasswort As String = pwdConfirmPassword.Password

        ' Validierung
        If String.IsNullOrWhiteSpace(neuesPasswort) OrElse String.IsNullOrWhiteSpace(bestätigtesPasswort) Then
            MessageBox.Show("Bitte füllen Sie alle Felder aus.", "Warnung", MessageBoxButton.OK, MessageBoxImage.Warning)
            Return
        End If

        If neuesPasswort <> bestätigtesPasswort Then
            MessageBox.Show("Die Passwörter stimmen nicht überein.", "Warnung", MessageBoxButton.OK, MessageBoxImage.Warning)
            Return
        End If

        If neuesPasswort.Length < 6 Then
            MessageBox.Show("Das Passwort muss mindestens 6 Zeichen lang sein.", "Warnung", MessageBoxButton.OK, MessageBoxImage.Warning)
            Return
        End If

        ' Passwort setzen
        NewPassword = neuesPasswort
        DialogResult = True
    End Sub

    Private Sub BtnCancel_Click(sender As Object, e As RoutedEventArgs)
        DialogResult = False
    End Sub

    Private Sub Border_MouseDown(sender As Object, e As MouseButtonEventArgs)
        DragMove()
    End Sub
End Class
