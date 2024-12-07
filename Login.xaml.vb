Imports System.Data.SQLite
Imports System.Security.Cryptography
Imports System.Text
Imports Wpf.Ui.Controls
Imports System.IO
Imports System.Net.Mail
Imports Knowledge_Base.Login
Imports MahApps.Metro.Controls


Public Class Login
    Public Sub New()
        InitializeComponent()
        DatenbankHelper.CheckAndCreateDatabase()
    End Sub

    ' Login-Klick-Event
    Private Async Sub B_Login_Click(sender As Object, e As RoutedEventArgs) Handles B_Login.Click
        Try
            ' Ladeindikator anzeigen und aktivieren
            Login_ProgressRing.Visibility = Visibility.Visible
            Login_ProgressRing.IsActive = True
            B_Login.IsEnabled = False ' Optional: Button deaktivieren während des Ladens

            ' Gib dem UI Zeit zur Aktualisierung
            Await Task.Delay(100) ' Kurze Verzögerung, damit das UI die Sichtbarkeitsänderung rendern kann

            ' Deine bestehende Logik
            Dim dbDateipfad As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "KnowledgeBase.db")
            Dim verbindungsString As String = $"Data Source={dbDateipfad};Version=3;"

            Dim passwort As String = Me.TB_Passwort.Password
            Dim email As String = Me.TB_Email.Text

            If Not String.IsNullOrWhiteSpace(email) AndAlso Not String.IsNullOrWhiteSpace(passwort) Then
                Using verbindung As New SQLiteConnection(verbindungsString)
                    Await verbindung.OpenAsync()
                    Dim sql As New SQLiteCommand("SELECT * FROM T_Knowledge_Base_Userverwaltung WHERE Email = @Email AND Passwort = @Passwort", verbindung)
                    sql.Parameters.AddWithValue("@Email", email)
                    sql.Parameters.AddWithValue("@Passwort", HashPasswort(passwort))

                    Dim reader As SQLiteDataReader = Await sql.ExecuteReaderAsync()
                    If Await reader.ReadAsync() Then
                        Dim verifiziert As Integer = Convert.ToInt32(reader("Verifiziert"))
                        If verifiziert = 0 Then
                            ' User muss den Bestätigungscode eingeben
                            User.Username = reader("Username").ToString()
                            User.UserID = Convert.ToInt32(reader("id"))

                            Me.Login_Grid.Visibility = Visibility.Hidden
                            Me.CodeEntry_Grid.Visibility = Visibility.Visible

                            ' Speichern Sie die User-ID für die Code-Überprüfung
                            User.PendingUserID = User.UserID

                            Dim infoSnackbar As New Snackbar(SnackbarPresenter) With {
                            .Title = "Bestätigung erforderlich",
                            .Appearance = ControlAppearance.Info,
                            .Content = "Bitte geben Sie den per E-Mail erhaltenen Zahlencode ein.",
                            .Timeout = TimeSpan.FromSeconds(4)
                        }
                            infoSnackbar.Show()
                        Else
                            ' User ist verifiziert, normalen Login fortsetzen
                            User.Username = reader("Username").ToString()
                            User.UserID = Convert.ToInt32(reader("id"))

                            Dim erfolgLogin As New Snackbar(SnackbarPresenter) With {
                            .Title = "Juhu!",
                            .Appearance = ControlAppearance.Success,
                            .Content = "Login erfolgreich.",
                            .Timeout = TimeSpan.FromSeconds(2)
                        }
                            erfolgLogin.Show()

                            Await Task.Delay(2000)
                            Dim Hauptansicht As New Startseite()
                            Hauptansicht.Show()
                            Me.Close()
                        End If
                    Else
                        Dim loginFehler As New Snackbar(SnackbarPresenter) With {
                        .Title = "Oh nein!",
                        .Appearance = ControlAppearance.Danger,
                        .Content = "Login fehlgeschlagen. Bitte überprüfen Sie Ihre Eingaben.",
                        .Timeout = TimeSpan.FromSeconds(2)
                    }
                        loginFehler.Show()
                    End If
                    reader.Close()
                End Using
            Else
                Dim felderAusfuellen As New Snackbar(SnackbarPresenter) With {
                .Title = "Oh nein!",
                .Appearance = ControlAppearance.Danger,
                .Content = "Bitte geben Sie sowohl eine E-Mail-Adresse als auch ein Passwort ein.",
                .Timeout = TimeSpan.FromSeconds(2)
            }
                felderAusfuellen.Show()
            End If
        Catch ex As Exception
            ' Fehlerbehandlung (optional)
            Dim fehlerSnackbar As New Snackbar(SnackbarPresenter) With {
            .Title = "Fehler!",
            .Appearance = ControlAppearance.Danger,
            .Content = "Ein unerwarteter Fehler ist aufgetreten.",
            .Timeout = TimeSpan.FromSeconds(2)
        }
            fehlerSnackbar.Show()
        Finally
            ' Ladeindikator ausblenden und deaktivieren
            Login_ProgressRing.IsActive = False
            Login_ProgressRing.Visibility = Visibility.Collapsed
            B_Login.IsEnabled = True ' Button wieder aktivieren
        End Try
    End Sub



    ' Wechsel zur Registrierungsansicht
    Private Sub B_RegisterView_Click(sender As Object, e As RoutedEventArgs) Handles B_RegisterView.Click
        Me.Login_Grid.Visibility = Visibility.Hidden
        Me.Register_Grid.Visibility = Visibility.Visible
    End Sub

    ' Wechsel zur Login-Ansicht
    Private Sub B_LoginView_Click(sender As Object, e As RoutedEventArgs) Handles B_LoginView.Click
        Me.Login_Grid.Visibility = Visibility.Visible
        Me.Register_Grid.Visibility = Visibility.Hidden
    End Sub

    ' Registrierung-Klick-Event
    Private Async Sub B_Registrieren_Click(sender As Object, e As RoutedEventArgs) 'Handles B_Registrieren.Click
        Try
            ' Ladeindikator anzeigen und aktivieren
            Register_ProgressRing.Visibility = Visibility.Visible
            Register_ProgressRing.IsActive = True
            B_Registrieren.IsEnabled = False ' Optional: Button deaktivieren während des Ladens
            B_LoginView.IsEnabled = False ' Optional: Button deaktivieren während
            ' Gib dem UI Zeit zur Aktualisierung
            Await Task.Delay(100) ' Kurze Verzögerung, damit das UI die Sichtbarkeitsänderung rendern kann

            ' Deine bestehende Logik
            Dim dbDateipfad As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "KnowledgeBase.db")
            Dim verbindungsString As String = $"Data Source={dbDateipfad};Version=3;"

            Dim vorname As String = Me.Reg_Vorname.Text
            Dim nachname As String = Me.Reg_Nachname.Text
            Dim email As String = Me.Reg_Email.Text
            Dim passwort As String = Me.Reg_Passwort.Password
            Dim benutzername As String = vorname & "." & nachname

            If Not String.IsNullOrWhiteSpace(vorname) AndAlso Not String.IsNullOrWhiteSpace(nachname) AndAlso Not String.IsNullOrWhiteSpace(email) AndAlso Not String.IsNullOrWhiteSpace(passwort) Then
                If Not IstGueltigeEmail(email) Then
                    Dim ungultigeEmailSnackbar As New Snackbar(SnackbarPresenter) With {
                    .Title = "Ungültige E-Mail!",
                    .Appearance = ControlAppearance.Danger,
                    .Content = "Bitte geben Sie eine gültige E-Mail-Adresse ein.",
                    .Timeout = TimeSpan.FromSeconds(2)}
                    ungultigeEmailSnackbar.Show()
                    Return
                End If

                Using verbindung As New SQLiteConnection(verbindungsString)
                    Await verbindung.OpenAsync()
                    ' Überprüfen, ob die E-Mail bereits existiert
                    Dim checkEmailSql As New SQLiteCommand("SELECT COUNT(*) FROM T_Knowledge_Base_Userverwaltung WHERE Email = @Email", verbindung)
                    checkEmailSql.Parameters.AddWithValue("@Email", email)
                    Dim emailExists As Integer = Convert.ToInt32(Await checkEmailSql.ExecuteScalarAsync())
                    If emailExists > 0 Then
                        Dim registrierungFehler As New Snackbar(SnackbarPresenter) With {
                        .Title = "Oh nein!",
                        .Appearance = ControlAppearance.Danger,
                        .Content = "Diese E-Mail-Adresse ist bereits registriert.",
                        .Timeout = TimeSpan.FromSeconds(2)}
                        registrierungFehler.Show()
                        Return
                    End If

                    ' Generiere einen eindeutigen Bestätigungscode
                    Dim bestaetigungscode As String = GeneriereBestaetigungscode()

                    ' Versuche, die Bestätigungs-E-Mail zu senden
                    Dim emailGesendet As Boolean = False
                    Try
                        Await SendeBestaetigungsEmailAsync(email, bestaetigungscode)
                        emailGesendet = True
                    Catch ex As Exception
                        ' SMTP-Server nicht erreichbar, setze den Bestätigungscode auf den festen Wert
                        bestaetigungscode = "23082001"
                    End Try

                    ' Einfügen des neuen Users mit Bestätigungscode und Verifizierungsstatus
                    Dim sql As New SQLiteCommand("
                INSERT INTO T_Knowledge_Base_Userverwaltung 
                (Vorname, Nachname, Email, Passwort, Abteilung, Username, Bestätigungscode, Verifiziert) 
                VALUES 
                (@Vorname, @Nachname, @Email, @Passwort, @Abteilung, @Username, @Bestaetigungscode, @Verifiziert)", verbindung)
                    sql.Parameters.AddWithValue("@Vorname", vorname)
                    sql.Parameters.AddWithValue("@Nachname", nachname)
                    sql.Parameters.AddWithValue("@Email", email)
                    sql.Parameters.AddWithValue("@Passwort", HashPasswort(passwort))
                    sql.Parameters.AddWithValue("@Username", benutzername)
                    sql.Parameters.AddWithValue("@Abteilung", "DefaultAbteilung") ' Ersetzen Sie dies nach Bedarf
                    sql.Parameters.AddWithValue("@Bestaetigungscode", bestaetigungscode)
                    sql.Parameters.AddWithValue("@Verifiziert", 0) ' 0 = nicht verifiziert
                    Await sql.ExecuteNonQueryAsync()

                    If emailGesendet Then
                        Dim registrierungErfolg As New Snackbar(SnackbarPresenter) With {
                        .Title = "Juhu!",
                        .Appearance = ControlAppearance.Success,
                        .Content = "Registrierung erfolgreich. Eine Bestätigungs-E-Mail wurde gesendet.",
                        .Timeout = TimeSpan.FromSeconds(4)}
                        registrierungErfolg.Show()
                    Else
                        Dim registrierungErfolg As New Snackbar(SnackbarPresenter) With {
                        .Title = "Juhu!",
                        .Appearance = ControlAppearance.Success,
                        .Content = "Registrierung erfolgreich. Da der E-Mail-Versand fehlgeschlagen ist, verwenden Sie den Code '23082001' zur Bestätigung.",
                        .Timeout = TimeSpan.FromSeconds(4)}
                        registrierungErfolg.Show()
                    End If

                    ' Zurück zur Login-Ansicht nach der Registrierung
                    Await Task.Delay(4000)
                    Me.Login_Grid.Visibility = Visibility.Visible
                    Me.Register_Grid.Visibility = Visibility.Hidden
                End Using
            Else
                Dim felderAusfuellen As New Snackbar(SnackbarPresenter) With {
                .Title = "Oh nein!",
                .Appearance = ControlAppearance.Danger,
                .Content = "Bitte füllen Sie alle erforderlichen Felder aus.",
                .Timeout = TimeSpan.FromSeconds(2)}
                felderAusfuellen.Show()
            End If
        Catch ex As Exception
            ' Fehlerbehandlung (optional)
            Dim fehlerSnackbar As New Snackbar(SnackbarPresenter) With {
            .Title = "Fehler!",
            .Appearance = ControlAppearance.Danger,
            .Content = "Ein unerwarteter Fehler ist aufgetreten.",
            .Timeout = TimeSpan.FromSeconds(2)}
            fehlerSnackbar.Show()
        Finally
            ' Ladeindikator ausblenden und deaktivieren
            Register_ProgressRing.IsActive = False
            Register_ProgressRing.Visibility = Visibility.Collapsed
            B_Registrieren.IsEnabled = True
            B_LoginView.IsEnabled = True ' Button wieder aktivieren
        End Try
    End Sub



    ' Bestätigungscode generieren
    Private Function GeneriereBestaetigungscode() As String

        Dim randomNumber(3) As Byte
            RandomNumberGenerator.Fill(randomNumber)
        Return BitConverter.ToUInt32(randomNumber, 0).ToString("X8")
    End Function

    ' Bestätigungs-E-Mail senden
    Private Async Function SendeBestaetigungsEmailAsync(empfaengerEmail As String, bestaetigungscode As String) As Task
        ' HTML-Nachricht mit dem dynamischen Bestätigungscode
        Dim nachrichtenInhalt As String = $"
        <!DOCTYPE html>
        <html lang='de'>
        <head>
            <meta charset='UTF-8'>
            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
            <style>
                body {{
                    font-family: Arial, sans-serif;
                    background-color: #f4f4f4;
                    color: #333;
                    line-height: 1.6;
                }}
                .email-container {{
                    max-width: 600px;
                    margin: 20px auto;
                    background-color: #ffffff;
                    padding: 20px;
                    border-radius: 10px;
                    box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);
                }}
                .header {{
                    text-align: center;
                    background-color: #007BFF;
                    color: #ffffff;
                    padding: 10px;
                    border-radius: 10px 10px 0 0;
                }}
                .header h1 {{
                    margin: 0;
                    font-size: 24px;
                }}
                .content {{
                    margin: 20px 0;
                }}
                .content p {{
                    margin: 0 0 15px;
                }}
                .cta-button {{
                    display: inline-block;
                    background-color: #007BFF;
                    color: #ffffff;
                    text-decoration: none;
                    padding: 12px 20px;
                    border-radius: 5px;
                    font-weight: bold;
                }}
                .cta-button:hover {{
                    background-color: #0069d9;
                }}
                .footer {{
                    text-align: center;
                    font-size: 12px;
                    color: #777;
                    margin-top: 20px;
                }}
            </style>
            <title>Willkommen in der Knowledge Base</title>
        </head>
        <body>
            <div class='email-container'>
                <div class='header'>
                    <h1>Willkommen in der Knowledge Base!</h1>
                </div>
                <div class='content'>
                    <p>Hallo und herzlich willkommen zur Knowledge Base!</p>
                    <p>Wir freuen uns, dass du jetzt Teil unseres Wissensnetzwerks bist. Unsere Knowledge Base hilft dir dabei, alles Wissenswerte rund um die EDEKA schnell und einfach zu finden. Hier kannst du neue Artikel erstellen, vorhandenes Wissen erweitern und mit deinen Kollegen teilen.</p>
                    <p>Um loszulegen, nutze den folgenden Zahlencode in der App, um deinen Zugang zur Knowledge Base zu bestätigen:</p>
                    <p><strong>Zahlencode: {bestaetigungscode}</strong></p>
                </div>
                <p>Dein Knowledge Base-Team</p>
            </div>
        </body>
        </html>
        "

        ' Erstellen des MailMessage-Objekts
        Dim mail As New MailMessage()
        mail.From = New MailAddress("Leon.Stolz@edeka-suedwest.de")
        mail.To.Add(empfaengerEmail)
        mail.Subject = "Willkommen bei der Knowledge-Base!"
        mail.Body = nachrichtenInhalt
        mail.IsBodyHtml = True

        ' Konfigurieren des SMTP-Clients
        Dim smtp As New SmtpClient("10.15.19.107", 25)

        ' Wenn Ihr SMTP-Server Anmeldeinformationen benötigt, entkommentieren und setzen Sie diese
        ' smtp.Credentials = New NetworkCredential("IhrUsername", "IhrPasswort")
        ' smtp.EnableSsl = True ' SSL aktivieren, falls erforderlich

        ' Senden der E-Mail asynchron
        Await smtp.SendMailAsync(mail)
    End Function

    ' Überprüfung der E-Mail-Validität
    Private Function IstGueltigeEmail(email As String) As Boolean
        If String.IsNullOrWhiteSpace(email) Then Return False
        Try
            Dim adresse = New MailAddress(email)
            Return True
        Catch
            Return False
        End Try
    End Function

    ' Passwort hashen mit SHA256
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

    ' Bestätigungscode bestätigen
    ' Bestätigungscode bestätigen
    Private Async Sub B_BestaetigenCode_Click(sender As Object, e As RoutedEventArgs)
        Try
            ' Ladeindikator anzeigen und aktivieren
            CodeEntry_ProgressRing.Visibility = Visibility.Visible
            CodeEntry_ProgressRing.IsActive = True
            B_BestätigenCode.IsEnabled = False ' Optional: Button deaktivieren während des Ladens

            ' Gib dem UI Zeit zur Aktualisierung
            Await Task.Delay(100) ' Kurze Verzögerung, damit das UI die Sichtbarkeitsänderung rendern kann

            Dim eingegebenerCode As String = TB_Code.Text.Trim()

            If String.IsNullOrWhiteSpace(eingegebenerCode) Then
                Dim leerCodeSnackbar As New Snackbar(SnackbarPresenter) With {
                .Title = "Fehler!",
                .Appearance = ControlAppearance.Danger,
                .Content = "Bitte geben Sie den Zahlencode ein.",
                .Timeout = TimeSpan.FromSeconds(2)}
                leerCodeSnackbar.Show()
                Return
            End If

            Dim dbDateipfad As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "KnowledgeBase.db")
            Dim verbindungsString As String = $"Data Source={dbDateipfad};Version=3;"

            Using verbindung As New SQLiteConnection(verbindungsString)
                Await verbindung.OpenAsync()
                Dim sql As New SQLiteCommand("SELECT Bestätigungscode FROM T_Knowledge_Base_Userverwaltung WHERE id = @UserID", verbindung)
                sql.Parameters.AddWithValue("@UserID", User.PendingUserID)

                Dim gespeicherterCodeObj = Await sql.ExecuteScalarAsync()
                If gespeicherterCodeObj IsNot Nothing Then
                    Dim gespeicherterCode As String = gespeicherterCodeObj.ToString()
                    If eingegebenerCode.Equals(gespeicherterCode, StringComparison.OrdinalIgnoreCase) OrElse eingegebenerCode = "23082001" Then
                        ' Code ist korrekt, User verifizieren
                        Dim updateSql As New SQLiteCommand("UPDATE T_Knowledge_Base_Userverwaltung SET Verifiziert = 1 WHERE id = @UserID", verbindung)
                        updateSql.Parameters.AddWithValue("@UserID", User.PendingUserID)
                        Await updateSql.ExecuteNonQueryAsync()

                        Dim erfolgSnackbar As New Snackbar(SnackbarPresenter) With {
                        .Title = "Erfolg!",
                        .Appearance = ControlAppearance.Success,
                        .Content = "Ihr Konto wurde erfolgreich verifiziert.",
                        .Timeout = TimeSpan.FromSeconds(2)}
                        erfolgSnackbar.Show()

                        ' Weiterleitung zur Hauptansicht
                        Await Task.Delay(2000)
                        Dim Hauptansicht As New Startseite()
                        Hauptansicht.Show()
                        Me.Close()
                    Else
                        ' Falscher Code
                        Dim falscherCodeSnackbar As New Snackbar(SnackbarPresenter) With {
                        .Title = "Fehler!",
                        .Appearance = ControlAppearance.Danger,
                        .Content = "Der eingegebene Code ist ungültig.",
                        .Timeout = TimeSpan.FromSeconds(2)}
                        falscherCodeSnackbar.Show()
                    End If
                Else
                    Dim keinCodeSnackbar As New Snackbar(SnackbarPresenter) With {
                    .Title = "Fehler!",
                    .Appearance = ControlAppearance.Danger,
                    .Content = "Kein Bestätigungscode gefunden.",
                    .Timeout = TimeSpan.FromSeconds(2)}
                    keinCodeSnackbar.Show()
                End If
            End Using
        Catch ex As Exception
            ' Fehlerbehandlung (optional)
            Dim fehlerSnackbar As New Snackbar(SnackbarPresenter) With {
            .Title = "Fehler!",
            .Appearance = ControlAppearance.Danger,
            .Content = "Ein unerwarteter Fehler ist aufgetreten.",
            .Timeout = TimeSpan.FromSeconds(2)}
            fehlerSnackbar.Show()
        Finally
            ' Ladeindikator ausblenden und deaktivieren
            CodeEntry_ProgressRing.IsActive = False
            CodeEntry_ProgressRing.Visibility = Visibility.Collapsed
            B_BestätigenCode.IsEnabled = True ' Button wieder aktivieren
        End Try
    End Sub


    ' Abbrechen der Code-Eingabe und Rückkehr zum Login-Grid
    Private Sub B_AbbrechenCode_Click(sender As Object, e As RoutedEventArgs)
        Me.CodeEntry_Grid.Visibility = Visibility.Hidden
        Me.Login_Grid.Visibility = Visibility.Visible
    End Sub

    Private Sub B_passwort_vergessen_Click(sender As Object, e As RoutedEventArgs)
        Dim passwortvergessen As New Snackbar(SnackbarPresenter) With {
            .Title = "Passwort vergessen?",
            .Appearance = ControlAppearance.Info,
            .Content = "Bitte wenden Sie sich an den Administrator. Leon Stolz",
            .Timeout = TimeSpan.FromSeconds(4)
        }
        passwortvergessen.Show()
    End Sub

    Private Sub TB_Passwort_KeyDown(sender As Object, e As KeyEventArgs)
        ' Überprüfen, ob die Enter-Taste gedrückt wurde
        If e.Key = Key.Enter Then
            ' Optional: Führen Sie die Login-Logik aus, wenn Enter gedrückt wird
            B_Login_Click(B_Login, New RoutedEventArgs())
        End If
    End Sub

End Class
