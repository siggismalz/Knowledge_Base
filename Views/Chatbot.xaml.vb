Imports System.Diagnostics
Imports System.Globalization
Imports System.IO
Imports System.Net.Http
Imports System.Threading.Tasks
Imports Newtonsoft.Json
Imports System.Windows.Media
Imports System.ComponentModel
Imports System.Collections.ObjectModel
Imports System.Data.SQLite
Imports System.Text

Public Class ChatMessage
        Implements INotifyPropertyChanged

        Private _message As String

        Public Property Sender As String

        Public Property Message As String
            Get
                Return _message
            End Get
            Set(value As String)
                If _message <> value Then
                    _message = value
                    OnPropertyChanged("Message")
                End If
            End Set
        End Property

        Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

        Protected Sub OnPropertyChanged(name As String)
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(name))
        End Sub
    End Class

    ' Flask-Client-Klasse
    Public Class FlaskClient
        Private ReadOnly ServerUrl As String = "http://127.0.0.1:5000/qa" ' Ihre Flask-Endpunkt-URL
        Private ReadOnly HttpClient As HttpClient

        Public Sub New()
            Me.HttpClient = New HttpClient()
            Me.HttpClient.Timeout = TimeSpan.FromSeconds(60) ' Erhöhen Sie das Timeout auf 60 Sekunden
        End Sub

        ' Methode zum Senden von Fragen
        Public Async Function SendQuestionAsync(question As String, context As String) As Task(Of String)
            Dim payload = New With {
            Key .question = question,
            Key .context = context
        }

            Dim jsonPayload As String = JsonConvert.SerializeObject(payload)
            Dim content As New StringContent(jsonPayload, Encoding.UTF8, "application/json")

            Dim response As HttpResponseMessage = Await HttpClient.PostAsync(ServerUrl, content)

            If response.IsSuccessStatusCode Then
                Dim responseData As String = Await response.Content.ReadAsStringAsync()
                Dim result = JsonConvert.DeserializeObject(Of Dictionary(Of String, Object))(responseData)
                If result.ContainsKey("answer") Then
                    Return result("answer").ToString()
                Else
                    Return "Keine Antwort erhalten."
                End If
            Else
                Dim errorContent As String = Await response.Content.ReadAsStringAsync()
                Throw New Exception($"API-Aufruf fehlgeschlagen: {response.StatusCode} - {errorContent}")
            End If
        End Function

        ' Methode zur Überprüfung, ob der Server läuft
        Public Async Function IsServerRunningAsync() As Task(Of Boolean)
            Try
                ' Sende eine einfache GET-Anfrage an die Root-URL des Flask-Servers
                Dim response As HttpResponseMessage = Await HttpClient.GetAsync("http://127.0.0.1:5000/")
                If response.IsSuccessStatusCode Then
                    ' Optional: Weitere Überprüfungen durchführen, z.B. Inhalt der Antwort
                    Return True
                Else
                    Return False
                End If
            Catch ex As Exception
                Return False
            End Try
        End Function
    End Class

    ' Flask-Server-Manager-Klasse
    Public Class FlaskServerManager
        Private PythonProcess As Process
        Private ReadOnly PythonExePath As String
        Private ReadOnly FlaskScriptPath As String

    Public Sub New()
        ' Prüfen, ob die Anwendung als EXE läuft oder im Entwicklungsmodus ist
        Dim isRunningAsExe As Boolean = Path.GetExtension(AppDomain.CurrentDomain.BaseDirectory).Equals(".exe", StringComparison.OrdinalIgnoreCase)

        ' Pfade zum Python-Interpreter und Flask-Skript basierend auf dem Modus festlegen
        If isRunningAsExe Then
            PythonExePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tools", "python.exe")
            FlaskScriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tools", "KB_Offline_LLM", "server.py")
        Else
            PythonExePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "3.12.0", "tools", "python.exe")
            FlaskScriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "3.12.0", "tools", "KB_Offline_LLM", "server.py")
        End If
    End Sub

    ' Methode zum Starten des Flask-Servers
    Public Sub StartServer()
            ' Prüfen, ob der Python-Interpreter existiert
            If Not File.Exists(PythonExePath) Then
                Throw New FileNotFoundException($"Python-Interpreter nicht gefunden unter: {PythonExePath}")
            End If

            ' Prüfen, ob das Flask-Skript existiert
            If Not File.Exists(FlaskScriptPath) Then
                Throw New FileNotFoundException($"Flask-Skript 'server.py' nicht gefunden unter: {FlaskScriptPath}")
            End If

            ' Prüfen, ob der Server bereits läuft
            If IsRunning() Then
                Debug.WriteLine("Flask-Server läuft bereits.")
                Return
            End If

            ' Flask-Server-Prozess starten
            PythonProcess = New Process()
            PythonProcess.StartInfo.FileName = PythonExePath
            PythonProcess.StartInfo.Arguments = $" ""{FlaskScriptPath}"""
            PythonProcess.StartInfo.WorkingDirectory = Path.GetDirectoryName(FlaskScriptPath)
            PythonProcess.StartInfo.UseShellExecute = False
            PythonProcess.StartInfo.RedirectStandardOutput = True
            PythonProcess.StartInfo.RedirectStandardError = True
            PythonProcess.StartInfo.CreateNoWindow = True

            ' Ereignis-Handler für Ausgabe und Fehler
            AddHandler PythonProcess.OutputDataReceived, AddressOf OutputHandler
            AddHandler PythonProcess.ErrorDataReceived, AddressOf ErrorHandler

            ' Prozess starten
            PythonProcess.Start()
            PythonProcess.BeginOutputReadLine()
            PythonProcess.BeginErrorReadLine()

            Debug.WriteLine("Flask-Server gestartet.")
        End Sub

        ' Ereignis-Handler für Standardausgabe
        Private Sub OutputHandler(sender As Object, e As DataReceivedEventArgs)
            If Not String.IsNullOrEmpty(e.Data) Then
                Debug.WriteLine($"[Flask Output]: {e.Data}")
            End If
        End Sub

        ' Ereignis-Handler für Fehlerausgabe
        Private Sub ErrorHandler(sender As Object, e As DataReceivedEventArgs)
            If Not String.IsNullOrEmpty(e.Data) Then
                Debug.WriteLine($"[Flask Error]: {e.Data}")
            End If
        End Sub

        ' Methode zum Überprüfen, ob der Prozess noch läuft
        Public Function IsRunning() As Boolean
            If PythonProcess Is Nothing Then
                Return False
            End If
            Return Not PythonProcess.HasExited
        End Function

        ' Methode zum Beenden des Flask-Servers
        Public Sub StopServer()
            If PythonProcess IsNot Nothing AndAlso Not PythonProcess.HasExited Then
                Try
                    PythonProcess.Kill()
                    PythonProcess.WaitForExit()
                    PythonProcess.Dispose()
                    PythonProcess = Nothing
                    Debug.WriteLine("Flask-Server beendet.")
                Catch ex As Exception
                    Debug.WriteLine($"Fehler beim Beenden des Flask-Servers: {ex.Message}")
                End Try
            End If
        End Sub
    End Class

    Partial Public Class Chatbot
        Inherits Page

        Public Property ChatMessages As ObservableCollection(Of ChatMessage)
        Private TypingIndicator As ChatMessage
        Private ReadOnly FlaskClientInstance As FlaskClient
        Private ReadOnly FlaskManager As FlaskServerManager
        Private GesamterKontext As String

        Public Sub New()
            InitializeComponent()
            ChatMessages = New ObservableCollection(Of ChatMessage)()
            ChatListBox.ItemsSource = ChatMessages
            FlaskClientInstance = New FlaskClient()
            FlaskManager = New FlaskServerManager()

            ' Gesamten Kontext laden
            GesamterKontext = LadeRelevantenKontext()

            ' Starten des Flask-Servers, wenn er nicht läuft
            StartFlaskServerIfNotRunning()
        End Sub

        ' Methode zum Starten des Flask-Servers, falls er nicht läuft
        Private Async Sub StartFlaskServerIfNotRunning()
            If Not FlaskManager.IsRunning() Then
                Try
                    ' Überprüfen, ob der Server bereits läuft
                    Dim isRunning As Boolean = Await FlaskClientInstance.IsServerRunningAsync()
                    If Not isRunning Then
                        ' Server starten
                        FlaskManager.StartServer()

                        ' Warten Sie eine Weile, bis der Server hochgefahren ist
                        ' (Passen Sie die Wartezeit je nach Bedarf an)
                        Await Task.Delay(5000)

                        ' Erneut überprüfen, ob der Server läuft
                        isRunning = Await FlaskClientInstance.IsServerRunningAsync()
                        If isRunning Then
                            Debug.WriteLine("Flask-Server erfolgreich gestartet.")
                        End If
                    Else
                        Debug.WriteLine("Flask-Server läuft bereits.")
                    End If
                Catch ex As Exception
                    MessageBox.Show($"Fehler beim Starten des Flask-Servers: {ex.Message}")
                End Try
            Else
                Debug.WriteLine("Flask-Server läuft bereits.")
            End If
        End Sub

        ' Senden-Button Klick
        Private Async Sub SendButton_Click(sender As Object, e As RoutedEventArgs)
            Dim userInput As String = UserInputTextBox.Text.Trim()
            If Not String.IsNullOrEmpty(userInput) Then
                ' Nachricht hinzufügen (Benutzer)
                ChatMessages.Add(New ChatMessage() With {.Sender = "Du", .Message = userInput})

                ' "Tippt..." Indikator hinzufügen
                TypingIndicator = New ChatMessage() With {.Sender = "Chatbot", .Message = "Tippt..."}
                ChatMessages.Add(TypingIndicator)
                ChatListBox.ScrollIntoView(TypingIndicator)

                Try
                    ' API-Aufruf mit aktuellem Kontext
                    Dim context As String = GesamterKontext ' Optional: Dynamisch ausgewählten Kontext verwenden
                    Dim response As String = Await FlaskClientInstance.SendQuestionAsync(userInput, context)

                    ' Chatbot-Nachricht hinzufügen
                    ChatMessages.Add(New ChatMessage() With {.Sender = "Chatbot", .Message = response})
                    ChatListBox.ScrollIntoView(ChatMessages.Last())

                Catch ex As TaskCanceledException
                    ' Timeout-Fehler
                    If TypingIndicator IsNot Nothing Then
                        TypingIndicator.Message = "Die Anfrage hat zu lange gedauert. Bitte versuchen Sie es erneut."
                    End If
                Catch ex As HttpRequestException
                    ' Netzwerkbezogene Fehler
                    If TypingIndicator IsNot Nothing Then
                        TypingIndicator.Message = "Fehler beim Verbinden mit dem Server. Bitte überprüfen Sie, ob der Server läuft."
                    End If
                Catch ex As FileNotFoundException
                    ' Datei- oder Pfadbezogene Fehler
                    MessageBox.Show($"Dateifehler: {ex.Message}")
                Catch ex As Exception
                    ' Allgemeine Fehler
                    If TypingIndicator IsNot Nothing Then
                        TypingIndicator.Message = $"Ein Fehler ist aufgetreten: {ex.Message}"
                    End If
                Finally
                    ' "Tippt..." Indikator entfernen
                    If TypingIndicator IsNot Nothing Then
                        ChatMessages.Remove(TypingIndicator)
                        TypingIndicator = Nothing
                    End If
                End Try

                ' Eingabe zurücksetzen
                UserInputTextBox.Text = String.Empty
            End If
        End Sub


        ' Relevanten Kontext laden
        Private Function LadeRelevantenKontext() As String
            Dim dbFilePath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "KnowledgeBase.db")
            If Not File.Exists(dbFilePath) Then
                MessageBox.Show($"Datenbankdatei nicht gefunden unter: {dbFilePath}")
                Return String.Empty
            End If

            Dim connectionString As String = $"Data Source={dbFilePath};Version=3;"
            Dim kontextBuilder As New Text.StringBuilder()

            Using verbindung As New SQLiteConnection(connectionString)
                verbindung.Open()

                Dim query As String = "
                SELECT Titel, Artikel_Inhalt
                FROM T_Knowledge_Base_Artikelverwaltung;"

                Using cmd As New SQLiteCommand(query, verbindung)
                    Using reader As SQLiteDataReader = cmd.ExecuteReader()
                        While reader.Read()
                            Dim titel As String = reader.GetString(reader.GetOrdinal("Titel"))
                            Dim inhalt As String = reader.GetString(reader.GetOrdinal("Artikel_Inhalt"))
                            kontextBuilder.AppendLine($"**Titel:** {titel}")
                            kontextBuilder.AppendLine($"**Inhalt:** {inhalt}")
                            kontextBuilder.AppendLine(Environment.NewLine & "=== Artikelende ===" & Environment.NewLine)
                        End While
                    End Using
                End Using
            End Using

            Return kontextBuilder.ToString()
        End Function

        ' Ereignis beim Entladen der Seite
        Private Sub Page_Unloaded(sender As Object, e As RoutedEventArgs) Handles Me.Unloaded
            ' Flask-Server beenden, wenn die Anwendung geschlossen wird
            FlaskManager.StopServer()
        End Sub
    End Class