Imports System.Data.SQLite
Imports System.Text
Imports System.Security.Cryptography
Imports System.IO
Imports System.Windows
Imports System.Windows.Controls
Imports Wpf.Ui.Controls

Public Class Profil
    Private Shared verbindung As SQLiteConnection

    Public Sub New()
        InitializeComponent()
        InitialisiereDatenbankverbindung()
        LadeBenutzerdaten()
    End Sub

    Private Sub InitialisiereDatenbankverbindung()
        If verbindung Is Nothing Then
            Try
                Dim dbFilePath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "KnowledgeBase.db")
                If Not File.Exists(dbFilePath) Then
                    Dim db_nicht_gefunden As New Snackbar(SnackbarPresenter) With {
                        .Title = "Fehler!",
                        .Appearance = ControlAppearance.Danger,
                        .Content = "Datenbankdatei nicht gefunden.",
                        .Timeout = TimeSpan.FromSeconds(3)
                    }
                    db_nicht_gefunden.Show()
                    Application.Current.Shutdown()
                End If

                Dim connectionString As String = $"Data Source={dbFilePath};Version=3;"
                verbindung = New SQLiteConnection(connectionString)
                verbindung.Open()
            Catch ex As Exception
                Dim verbindungsfehler As New Snackbar(SnackbarPresenter) With {
                    .Title = "Fehler!",
                    .Appearance = ControlAppearance.Danger,
                    .Content = $"Fehler beim Verbinden zur Datenbank: {ex.Message}",
                    .Timeout = TimeSpan.FromSeconds(3)
                }
                verbindungsfehler.Show()
                Application.Current.Shutdown()
            End Try
        End If
    End Sub

    Private Sub LadeBenutzerdaten()
        Try
            txtName.Text = User.Username
            txtEmail.Text = HoleBenutzerEmail(User.UserID)
            pwdPassword.Password = "********" ' Platzhalter für das Passwort
        Catch ex As Exception
            ShowErrorSnackbar($"Fehler beim Laden der Benutzerdaten: {ex.Message}")
        End Try
    End Sub

    Private Function HashPasswort(passwort As String) As String
        Using sha256 As SHA256 = SHA256.Create()
            Dim bytes As Byte() = sha256.ComputeHash(Encoding.UTF8.GetBytes(passwort))
            Dim builder As New StringBuilder()
            For Each b As Byte In bytes
                builder.Append(b.ToString("x2"))
            Next
            Return builder.ToString()
        End Using
    End Function

    Private Sub AktualisierePasswort(neuesPasswort As String, benutzerID As Integer)
        Try
            ' Passwort hashen
            Dim gehashtesPasswort As String = HashPasswort(neuesPasswort)

            ' Passwort in der Datenbank aktualisieren
            Dim query As String = "UPDATE T_Knowledge_Base_Userverwaltung SET Passwort = @Passwort WHERE id = @UserId"
            Using cmd As New SQLiteCommand(query, verbindung)
                cmd.Parameters.AddWithValue("@Passwort", gehashtesPasswort)
                cmd.Parameters.AddWithValue("@UserId", benutzerID)
                cmd.ExecuteNonQuery()
            End Using

            ' Erfolgs-Snackbar(SnackbarPresenter) anzeigen
            ShowSuccessSnackbar("Passwort erfolgreich aktualisiert.")

            ' Zurücksetzen des Passwort-Platzhalters
            pwdPassword.Password = "********"
        Catch ex As Exception
            ' Fehlermeldung anzeigen
            ShowErrorSnackbar($"Fehler beim Aktualisieren des Passworts: {ex.Message}")
        End Try
    End Sub

    Private Function HoleGespeichertesPasswort(benutzerID As Integer) As String
        Try
            Dim gespeichertesPasswort As String = ""
            Dim query As String = "SELECT Passwort FROM T_Knowledge_Base_Benutzerverwaltung WHERE id = @UserId"
            Using cmd As New SQLiteCommand(query, verbindung)
                cmd.Parameters.AddWithValue("@UserId", benutzerID)
                Using reader As SQLiteDataReader = cmd.ExecuteReader()
                    If reader.Read() Then
                        gespeichertesPasswort = reader("Passwort").ToString()
                    End If
                End Using
            End Using
            Return gespeichertesPasswort
        Catch ex As Exception
            ShowErrorSnackbar($"Fehler beim Abrufen des gespeicherten Passworts: {ex.Message}")
            Return ""
        End Try
    End Function

    Private Function HoleBenutzerEmail(benutzerID As Integer) As String
        Try
            Dim email As String = ""
            Dim query As String = "SELECT Email FROM T_Knowledge_Base_Userverwaltung WHERE id = @UserId"
            Using cmd As New SQLiteCommand(query, verbindung)
                cmd.Parameters.AddWithValue("@UserId", benutzerID)
                Using reader As SQLiteDataReader = cmd.ExecuteReader()
                    If reader.Read() Then
                        email = reader("Email").ToString()
                    End If
                End Using
            End Using
            Return email
        Catch ex As Exception
            ShowErrorSnackbar($"Fehler beim Abrufen der E-Mail: {ex.Message}")
            Return ""
        End Try
    End Function

    Private Async Sub BtnChangePassword_Click(sender As Object, e As RoutedEventArgs)
        ' Öffne das Passwortänderungsfenster als modales Dialogfenster
        Dim changePasswordWindow As New Passwortändern()
        changePasswordWindow.Owner = Window.GetWindow(Me) ' Setze den Owner auf das aktuelle Fenster

        Dim result As Nullable(Of Boolean) = changePasswordWindow.ShowDialog()

        If result = True Then
            Dim neuesPasswort As String = changePasswordWindow.NewPassword
            AktualisierePasswort(neuesPasswort, User.UserID)
        End If
    End Sub

    Private Sub BtnLogout_Click(sender As Object, e As RoutedEventArgs)
        Dim loginWindow As New Login()
        loginWindow.Show()
        Dim currentWindow As Window = Window.GetWindow(Me)
        currentWindow.Close()
    End Sub

    ' Methoden zum Anzeigen von Snackbars
    Private Sub ShowSuccessSnackbar(message As String)
        Dim snackbar As New Snackbar(SnackbarPresenter) With {
            .Title = "Erfolg!",
            .Appearance = ControlAppearance.Success,
            .Content = message,
            .Timeout = TimeSpan.FromSeconds(2)
        }
        snackbar.Show()
    End Sub

    Private Sub ShowErrorSnackbar(message As String)
        Dim snackbar As New Snackbar(SnackbarPresenter) With {
            .Title = "Fehler!",
            .Appearance = ControlAppearance.Danger,
            .Content = message,
            .Timeout = TimeSpan.FromSeconds(3)
        }
        snackbar.Show()
    End Sub

    Private Sub ShowWarningSnackbar(message As String)
        Dim snackbar As New Snackbar(SnackbarPresenter) With {
            .Title = "Warnung!",
            .Appearance = ControlAppearance.Caution,
            .Content = message,
            .Timeout = TimeSpan.FromSeconds(2)
        }
        snackbar.Show()
    End Sub
End Class
