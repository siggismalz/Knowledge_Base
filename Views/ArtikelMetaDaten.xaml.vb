Imports System
Imports System.Collections.ObjectModel
Imports System.Data.SQLite
Imports System.IO
Imports System.Linq
Imports System.Threading.Tasks
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Input
Imports Wpf.Ui.Controls

Public Class ArtikelMetaDaten
    Inherits Window

    Public Property Tags As ObservableCollection(Of String)
    Public Property ArtikelTitel As String
    Public Property OrdnerName As String
    Public Property TagsList As List(Of String)

    ' Verbindung zur SQLite-Datenbank
    Private ReadOnly Property ConnectionString As String
        Get
            Dim dbFilePath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "KnowledgeBase.db")
            Dim verbindungsString As String = $"Data Source={dbFilePath};Version=3;Journal Mode=Wal;Synchronous=Normal;"
            Return verbindungsString
        End Get
    End Property

    ' Neuer Konstruktor für bestehende Artikeldaten
    ' Neuer Konstruktor für bestehende Artikeldaten
    Public Sub New(artikelTitel As String, ordnerName As String, tagsList As List(Of String))
        InitializeComponent()
        Tags = New ObservableCollection(Of String)(tagsList)
        Me.DataContext = Me

        ' Felder mit vorhandenen Daten füllen
        T_Artikeltitel.Text = artikelTitel
        Me.OrdnerName = ordnerName ' Korrekte Zuweisung des Ordnernamens

        ' Event-Handler hinzufügen, um Ordner nach dem Laden des Fensters zu laden
        AddHandler Me.Loaded, AddressOf ArtikelMetaDaten_Loaded
    End Sub


    ' Standardkonstruktor für neue Artikel
    Public Sub New()
        InitializeComponent()
        Tags = New ObservableCollection(Of String)()
        Me.DataContext = Me

        ' Event-Handler hinzufügen, um Ordner nach dem Laden des Fensters zu laden
        AddHandler Me.Loaded, AddressOf ArtikelMetaDaten_Loaded
    End Sub

    ' Event-Handler, der ausgelöst wird, wenn das Fenster geladen wird
    Private Async Sub ArtikelMetaDaten_Loaded(sender As Object, e As RoutedEventArgs)
        Await LoadFolderNamesAsync()
        ' Ausgewählten Ordner setzen
        If Not String.IsNullOrEmpty(OrdnerName) Then
            Drop_ordner.Text = OrdnerName
        End If
    End Sub

    ' Methode zum Laden der Ordnernamen aus der Datenbank
    Private Async Function LoadFolderNamesAsync() As Task
        Try
            Dim folderList As List(Of String) = Await GetFolderNamesAsync()
            Drop_ordner.ItemsSource = folderList
        Catch ex As Exception
            Dim Fehler As New Snackbar(SnackbarPresenter) With {
                .Title = "Fehler beim Laden der Ordner",
                .Appearance = ControlAppearance.Danger,
                .Content = $"Es gab ein Problem beim Laden der Ordner: {ex.Message}",
                .Timeout = TimeSpan.FromSeconds(4)
            }
            Fehler.Show()
        End Try
    End Function

    ' Asynchrone Methode zum Abrufen der Ordnernamen aus der Datenbank
    Private Async Function GetFolderNamesAsync() As Task(Of List(Of String))
        Dim folderNames As New List(Of String)()

        Using connection As New SQLiteConnection(ConnectionString)
            Await connection.OpenAsync()

            Dim query As String = "SELECT Name FROM T_Knowledge_Base_Filesystem WHERE ist_verzeichnis = 1"

            Using command As New SQLiteCommand(query, connection)
                Using reader As SQLiteDataReader = Await command.ExecuteReaderAsync()
                    While Await reader.ReadAsync()
                        folderNames.Add(reader("Name").ToString())
                    End While
                End Using
            End Using

            connection.Close()
        End Using

        Return folderNames
    End Function

    ' Event-Handler für das Drücken der Enter-Taste in der Tags-TextBox
    Private Sub T_Tags_KeyDown(sender As Object, e As KeyEventArgs)
        If e.Key = Key.Enter Then
            Dim input As String = T_Tags.Text.Trim()

            If Not String.IsNullOrEmpty(input) Then
                ' Wenn mehrere Tags durch Kommas eingegeben werden
                Dim newTags As String() = input.Split(New Char() {","c}, StringSplitOptions.RemoveEmptyEntries)
                For Each tag As String In newTags
                    Dim trimmedTag As String = tag.Trim()
                    If Not String.IsNullOrEmpty(trimmedTag) AndAlso Not Tags.Contains(trimmedTag) Then
                        Tags.Add(trimmedTag)
                    End If
                Next

                T_Tags.Clear()
            End If

            e.Handled = True ' Verhindert das "Klingeln" des Enter-Tastes
        End If
    End Sub

    ' Event-Handler zum Entfernen eines Tags
    Private Sub RemoveTag_Click(sender As Object, e As RoutedEventArgs)
        Dim btn As System.Windows.Controls.Button = TryCast(sender, System.Windows.Controls.Button)
        If btn IsNot Nothing AndAlso TypeOf btn.DataContext Is String Then
            Dim tag As String = CType(btn.DataContext, String)
            If Tags.Contains(tag) Then
                Tags.Remove(tag)
            End If
        End If
    End Sub

    ' Event-Handler für den SaveButton
    Private Sub SaveButton_Click(sender As Object, e As RoutedEventArgs)
        ' Validierung der Eingaben
        If String.IsNullOrWhiteSpace(T_Artikeltitel.Text) Then
            Dim Fehler As New Snackbar(SnackbarPresenter) With {
                .Title = "Artikeltitel ist leer",
                .Appearance = ControlAppearance.Info,
                .Content = "Bitte geben Sie den Artikel-Titel ein.",
                .Timeout = TimeSpan.FromSeconds(4)
            }
            Fehler.Show()
            Return
        End If

        If String.IsNullOrWhiteSpace(Drop_ordner.Text) Then
            Dim Fehler As New Snackbar(SnackbarPresenter) With {
                .Title = "Ordner ist leer",
                .Appearance = ControlAppearance.Info,
                .Content = "Bitte wählen Sie einen Ordner aus oder geben Sie einen neuen ein.",
                .Timeout = TimeSpan.FromSeconds(4)
            }
            Fehler.Show()
            Return
        End If

        ' Daten sammeln
        ArtikelTitel = T_Artikeltitel.Text.Trim()
        OrdnerName = Drop_ordner.Text.Trim()
        TagsList = Tags.ToList()

        ' Dialog schließen und DialogResult setzen
        Me.DialogResult = True
        Me.Close()
    End Sub

    ' Event-Handler für den Cancel-Button
    Private Sub CancelButton_Click(sender As Object, e As RoutedEventArgs)
        Me.DialogResult = False
        Me.Close()

    End Sub

    ' Event-Handler zum Verschieben des Fensters durch Klicken auf den Border
    Private Sub Border_MouseDown(sender As Object, e As MouseButtonEventArgs)
        If e.ChangedButton = MouseButton.Left Then
            Me.DragMove()
        End If
    End Sub
End Class
