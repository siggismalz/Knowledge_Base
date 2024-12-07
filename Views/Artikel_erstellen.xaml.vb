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
Imports Newtonsoft.Json
Imports Microsoft.Web.WebView2.Core
Imports HtmlAgilityPack
Imports System.Text.RegularExpressions
Imports Aspose.Words
Imports Aspose.Words.Saving
Imports Microsoft.Win32

Public Class Artikel_erstellen
    Private ArtikelId As Integer?
    Private BreadcrumbItems As ObservableCollection(Of BreadcrumbItem)

    ' Definieren Sie benutzername und benutzer_id als Klassenmitglieder
    Private benutzername As String = "AktuellerBenutzer" ' Beispielwert, ersetzen Sie ihn entsprechend
    Private benutzer_id As Integer = 1 ' Beispielwert, ersetzen Sie ihn entsprechend

    ' Konstruktor für neuen Artikel
    Public Sub New()
        InitializeComponent()
        Me.ArtikelId = Nothing
        CKEditor.Ckeditor_laden(Browser, AddressOf CoreWebView2_WebMessageReceived)
    End Sub

    ' Konstruktor für bestehenden Artikel
    Public Sub New(artikelId As Integer, breadcrumbItems As ObservableCollection(Of BreadcrumbItem))
        InitializeComponent()
        Me.ArtikelId = artikelId
        Me.BreadcrumbItems = breadcrumbItems
        CKEditor.Ckeditor_laden(Browser, AddressOf CoreWebView2_WebMessageReceived)
    End Sub

    Private ReadOnly Property ConnectionString As String
        Get
            Dim dbFilePath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "KnowledgeBase.db")
            ' Hinzufügen von Journal Mode=Wal zur Verbindungszeichenfolge
            Return $"Data Source={dbFilePath};Version=3;Journal Mode=Wal;Synchronous=Normal;"
        End Get
    End Property

    Private Async Sub CoreWebView2_WebMessageReceived(sender As Object, e As CoreWebView2WebMessageReceivedEventArgs)
        Dim message As String = e.TryGetWebMessageAsString()
        If message = "editorReady" Then
            ' CKEditor ist bereit, Artikelinhalt laden
            Await LoadArticleContent()
        End If
    End Sub

    Private Async Sub B_Artikel_speichern_Click(sender As Object, e As RoutedEventArgs) Handles B_Artikel_speichern.Click
        Try
            ' Artikelinhalt abrufen
            Dim editor_inhalt_HTML As String = Await Editorinhalt_mit_HTML()
            Dim editor_inhalt_ohne_HTML As String = Await Editorinhalt_ohne_HTML()
            Me.Browser.Visibility = Visibility.Collapsed
            ' HTML-Inhalt verarbeiten und Bilder sammeln
            Dim processedContent = ProcessHtmlContent(editor_inhalt_HTML)
            Dim modifiedHtmlContent As String = processedContent.ModifiedHtml
            Dim imageDataList As List(Of (Platzhalter As String, ImageData As Byte())) = processedContent.ImageDataList

            Dim aktuelles_Datum As Date = Date.Now

            ' Transaktion starten
            Using verbindung As New SQLiteConnection(ConnectionString)
                Await verbindung.OpenAsync()

                Using transaction = verbindung.BeginTransaction()
                    Try
                        Dim artikeltitel As String = String.Empty
                        Dim ordner As String = String.Empty
                        Dim tagsList As New List(Of String)

                        If ArtikelId.HasValue Then
                            ' BEARBEITUNG EINES BESTEHENDEN ARTIKELS

                            ' Alte Version des Artikels abrufen
                            Dim getOldArticleCmd As New SQLiteCommand("SELECT * FROM T_Knowledge_Base_Artikelverwaltung WHERE Id = @ArtikelId", verbindung, transaction)
                            getOldArticleCmd.Parameters.AddWithValue("@ArtikelId", ArtikelId.Value)
                            Dim oldArticleReader As SQLiteDataReader = Await getOldArticleCmd.ExecuteReaderAsync()
                            Dim oldArticle As New Dictionary(Of String, Object)
                            If Await oldArticleReader.ReadAsync() Then
                                For i As Integer = 0 To oldArticleReader.FieldCount - 1
                                    oldArticle.Add(oldArticleReader.GetName(i), oldArticleReader.GetValue(i))
                                Next
                            End If
                            Await oldArticleReader.CloseAsync()

                            ' Ordnernamen aus der Datenbank abrufen
                            Dim getFolderCmd As New SQLiteCommand("
                                SELECT parent.Name as FolderName
                                FROM T_Knowledge_Base_Filesystem as fs
                                JOIN T_Knowledge_Base_Filesystem as parent ON fs.parent_id = parent.id
                                WHERE fs.Artikel_ID = @ArtikelId", verbindung, transaction)
                            getFolderCmd.Parameters.AddWithValue("@ArtikelId", ArtikelId.Value)
                            Dim folderNameObj As Object = Await getFolderCmd.ExecuteScalarAsync()
                            Dim folderName As String = If(folderNameObj IsNot Nothing, folderNameObj.ToString(), String.Empty)

                            ' Tags des Artikels abrufen
                            Dim getTagsCmd As New SQLiteCommand("
                                SELECT t.TagName
                                FROM T_Knowledge_Base_ArtikelTags at
                                JOIN T_Knowledge_Base_Tags t ON at.TagId = t.TagId
                                WHERE at.ArtikelId = @ArtikelId", verbindung, transaction)
                            getTagsCmd.Parameters.AddWithValue("@ArtikelId", ArtikelId.Value)
                            Dim tagsReader As SQLiteDataReader = Await getTagsCmd.ExecuteReaderAsync()
                            While Await tagsReader.ReadAsync()
                                tagsList.Add(tagsReader("TagName").ToString())
                            End While
                            Await tagsReader.CloseAsync()

                            ' Öffnen des ArtikelMetaDaten-Fensters mit vorhandenen Daten
                            Dim metaDatenFenster As New ArtikelMetaDaten(oldArticle("Titel").ToString(), folderName, tagsList)
                            Dim dialogResult As Boolean? = metaDatenFenster.ShowDialog()

                            If dialogResult = True Then
                                ' Metadaten wurden eingegeben und OK gedrückt
                                artikeltitel = metaDatenFenster.ArtikelTitel
                                ordner = metaDatenFenster.OrdnerName
                                tagsList = metaDatenFenster.TagsList

                                ' Alte Version in Versionierungstabelle speichern
                                Dim insertVersionCmd As New SQLiteCommand("
                                    INSERT INTO T_Knowledge_Base_Artikelversionen
                                    (ArtikelId, Titel, Artikel_Inhalt_HTML, Artikel_Inhalt, User_Id, Erstellt_am, Erstellt_von, Versioniert_am)
                                    VALUES (@ArtikelId, @Titel, @Artikel_Inhalt_HTML, @Artikel_Inhalt, @Benutzer_Id, @Erstellt_am, @Erstellt_von, @Versioniert_am)", verbindung, transaction)

                                insertVersionCmd.Parameters.AddWithValue("@ArtikelId", ArtikelId.Value)
                                insertVersionCmd.Parameters.AddWithValue("@Titel", oldArticle("Titel"))
                                insertVersionCmd.Parameters.AddWithValue("@Artikel_Inhalt_HTML", oldArticle("Artikel_Inhalt_HTML"))
                                insertVersionCmd.Parameters.AddWithValue("@Artikel_Inhalt", oldArticle("Artikel_Inhalt"))
                                insertVersionCmd.Parameters.AddWithValue("@Benutzer_Id", oldArticle("User_Id")) ' Angepasst von "Benutzer_Id" zu "User_Id"
                                insertVersionCmd.Parameters.AddWithValue("@Erstellt_am", oldArticle("Erstellt_am"))
                                insertVersionCmd.Parameters.AddWithValue("@Erstellt_von", oldArticle("Erstellt_von"))
                                insertVersionCmd.Parameters.AddWithValue("@Versioniert_am", aktuelles_Datum)

                                Await insertVersionCmd.ExecuteNonQueryAsync()

                                ' Artikel aktualisieren
                                Dim updateArtikelCmd As New SQLiteCommand("
                                    UPDATE T_Knowledge_Base_Artikelverwaltung
                                    SET Titel = @Titel,
                                        Artikel_Inhalt_HTML = @Artikel_Inhalt_HTML,
                                        Artikel_Inhalt = @Artikel_Inhalt,
                                        Bearbeitet_am = @Bearbeitet_am,
                                        Bearbeitet_von = @Bearbeitet_von
                                    WHERE Id = @ArtikelId", verbindung, transaction)

                                updateArtikelCmd.Parameters.AddWithValue("@Titel", artikeltitel)
                                updateArtikelCmd.Parameters.AddWithValue("@Artikel_Inhalt_HTML", modifiedHtmlContent)
                                updateArtikelCmd.Parameters.AddWithValue("@Artikel_Inhalt", editor_inhalt_ohne_HTML)
                                updateArtikelCmd.Parameters.AddWithValue("@Bearbeitet_am", aktuelles_Datum)
                                updateArtikelCmd.Parameters.AddWithValue("@Bearbeitet_von", benutzername)
                                updateArtikelCmd.Parameters.AddWithValue("@ArtikelId", ArtikelId.Value)

                                Await updateArtikelCmd.ExecuteNonQueryAsync()

                                ' Alte Bilder löschen
                                Dim deleteImagesCmd As New SQLiteCommand("DELETE FROM T_Knowledge_Base_Bilder WHERE Artikel_Id = @ArtikelId", verbindung, transaction)
                                deleteImagesCmd.Parameters.AddWithValue("@ArtikelId", ArtikelId.Value)
                                Await deleteImagesCmd.ExecuteNonQueryAsync()

                                ' Neue Bilder speichern
                                For Each imageDataItem In imageDataList
                                    Await StoreImageInDatabaseAsync(verbindung, transaction, ArtikelId.Value, imageDataItem.Platzhalter, imageDataItem.ImageData)
                                Next

                                ' Tags aktualisieren
                                ' Alte Tags löschen
                                Dim deleteTagsCmd As New SQLiteCommand("DELETE FROM T_Knowledge_Base_ArtikelTags WHERE ArtikelId = @ArtikelId", verbindung, transaction)
                                deleteTagsCmd.Parameters.AddWithValue("@ArtikelId", ArtikelId.Value)
                                Await deleteTagsCmd.ExecuteNonQueryAsync()

                                ' Neue Tags speichern
                                For Each tagName In tagsList
                                    ' Überprüfen, ob der Tag bereits existiert
                                    Dim getTagIdCmd As New SQLiteCommand("SELECT TagId FROM T_Knowledge_Base_Tags WHERE TagName = @TagName", verbindung, transaction)
                                    getTagIdCmd.Parameters.AddWithValue("@TagName", tagName)
                                    Dim tagIdObj As Object = Await getTagIdCmd.ExecuteScalarAsync()
                                    Dim tagId As Integer

                                    If tagIdObj IsNot Nothing Then
                                        tagId = Convert.ToInt32(tagIdObj)
                                    Else
                                        ' Neuer Tag einfügen und die zuletzt eingefügte ID abrufen
                                        Dim insertTagCmd As New SQLiteCommand("
                                            INSERT INTO T_Knowledge_Base_Tags (TagName) 
                                            VALUES (@TagName);
                                            SELECT last_insert_rowid();", verbindung, transaction)

                                        insertTagCmd.Parameters.AddWithValue("@TagName", tagName)

                                        tagId = Convert.ToInt32(Await insertTagCmd.ExecuteScalarAsync())
                                    End If

                                    ' Verknüpfung zwischen Artikel und Tag erstellen
                                    Dim insertArtikelTagCmd As New SQLiteCommand("INSERT INTO T_Knowledge_Base_ArtikelTags (ArtikelId, TagId) VALUES (@ArtikelId, @TagId)", verbindung, transaction)
                                    insertArtikelTagCmd.Parameters.AddWithValue("@ArtikelId", ArtikelId.Value)
                                    insertArtikelTagCmd.Parameters.AddWithValue("@TagId", tagId)
                                    Await insertArtikelTagCmd.ExecuteNonQueryAsync()
                                Next

                                ' Ordner aktualisieren
                                ' Aktuellen Eintrag im Filesystem aktualisieren oder erstellen
                                Dim getFilesystemCmd As New SQLiteCommand("SELECT id FROM T_Knowledge_Base_Filesystem WHERE Artikel_ID = @ArtikelId", verbindung, transaction)
                                getFilesystemCmd.Parameters.AddWithValue("@ArtikelId", ArtikelId.Value)
                                Dim filesystemIdObj As Object = Await getFilesystemCmd.ExecuteScalarAsync()

                                ' Abrufen der parent_id des ausgewählten Ordners
                                Dim getParentIdCmd As New SQLiteCommand("
                                    SELECT id FROM T_Knowledge_Base_Filesystem WHERE Name = @ordnername AND ist_verzeichnis = 1", verbindung, transaction)
                                getParentIdCmd.Parameters.AddWithValue("@ordnername", ordner)
                                Dim parentIdObj As Object = Await getParentIdCmd.ExecuteScalarAsync()
                                Dim parentId As Integer

                                If parentIdObj IsNot Nothing Then
                                    parentId = Convert.ToInt32(parentIdObj)
                                Else
                                    ' Ordner existiert nicht, also neuen Ordner erstellen
                                    Dim insertFolderCmd As New SQLiteCommand("
                                        INSERT INTO T_Knowledge_Base_Filesystem (Name, ist_verzeichnis, parent_id)
                                        VALUES (@Name, 1, @parent_id);
                                        SELECT last_insert_rowid();", verbindung, transaction)

                                    insertFolderCmd.Parameters.AddWithValue("@Name", ordner)
                                    insertFolderCmd.Parameters.AddWithValue("@parent_id", 0) ' Root-Verzeichnis

                                    parentId = Convert.ToInt32(Await insertFolderCmd.ExecuteScalarAsync())
                                End If

                                If filesystemIdObj IsNot Nothing Then
                                    ' Eintrag aktualisieren
                                    Dim updateFilesystemCmd As New SQLiteCommand("
                                        UPDATE T_Knowledge_Base_Filesystem
                                        SET Name = @Name, parent_id = @parent_id
                                        WHERE Artikel_ID = @ArtikelId", verbindung, transaction)
                                    updateFilesystemCmd.Parameters.AddWithValue("@Name", artikeltitel)
                                    updateFilesystemCmd.Parameters.AddWithValue("@parent_id", parentId)
                                    updateFilesystemCmd.Parameters.AddWithValue("@ArtikelId", ArtikelId.Value)
                                    Await updateFilesystemCmd.ExecuteNonQueryAsync()
                                Else
                                    ' Eintrag erstellen
                                    Dim insertFilesystemCmd As New SQLiteCommand("
                                        INSERT INTO T_Knowledge_Base_Filesystem (Name, ist_verzeichnis, parent_id, Artikel_ID)
                                        VALUES (@Name, 0, @parent_id, @Artikel_ID)", verbindung, transaction)
                                    insertFilesystemCmd.Parameters.AddWithValue("@Name", artikeltitel)
                                    insertFilesystemCmd.Parameters.AddWithValue("@parent_id", parentId)
                                    insertFilesystemCmd.Parameters.AddWithValue("@Artikel_ID", ArtikelId.Value)
                                    Await insertFilesystemCmd.ExecuteNonQueryAsync()
                                End If

                                ' Erfolgsmeldung anzeigen
                                Dim erfolgsmeldung As New Snackbar(SnackbarPresenter) With {
                                    .Title = "Erfolg",
                                    .Appearance = ControlAppearance.Success,
                                    .Content = "Der Artikel wurde erfolgreich aktualisiert.",
                                    .Timeout = TimeSpan.FromSeconds(2)}
                                erfolgsmeldung.Show()

                                Await Task.Delay(2000)

                                ' Navigation zur Artikelansicht
                                Try
                                    Dim artikelSeite As New Artikel_anzeigen(BreadcrumbItems)
                                    NavigationService.Navigate(artikelSeite)
                                Catch ex As Exception
                                    ' Fehlerbehandlungscode hier einfügen
                                End Try
                            Else
                                ' Der Benutzer hat abgebrochen
                                Browser.Visibility = Visibility.Visible
                                Return
                            End If
                        Else
                            ' NEUEN ARTIKEL ERSTELLEN
                            ' Öffnen des ArtikelMetaDaten-Fensters, um Metadaten zu sammeln
                            Dim metaDatenFenster As New ArtikelMetaDaten()
                            Dim dialogResult As Boolean? = metaDatenFenster.ShowDialog()

                            If dialogResult = True Then
                                ' Metadaten wurden eingegeben und OK gedrückt
                                artikeltitel = metaDatenFenster.ArtikelTitel
                                ordner = metaDatenFenster.OrdnerName
                                tagsList = metaDatenFenster.TagsList

                                ' Artikel mit modifiziertem HTML-Inhalt speichern
                                Dim insertArtikelCmd As New SQLiteCommand("
                                    INSERT INTO T_Knowledge_Base_Artikelverwaltung (Titel, Artikel_Inhalt_HTML, Artikel_Inhalt, User_Id, Erstellt_am, Erstellt_von) 
                                    VALUES (@Artikel_Titel, @Artikel_Inhalt_HTML, @Artikel_Inhalt_ohne_HTML, @benutzer_id, @erstellt_am, @benutzername);
                                    SELECT last_insert_rowid();", verbindung, transaction)

                                insertArtikelCmd.Parameters.AddWithValue("@Artikel_Titel", artikeltitel)
                                insertArtikelCmd.Parameters.AddWithValue("@Artikel_Inhalt_HTML", modifiedHtmlContent)
                                insertArtikelCmd.Parameters.AddWithValue("@Artikel_Inhalt_ohne_HTML", editor_inhalt_ohne_HTML)
                                insertArtikelCmd.Parameters.AddWithValue("@benutzer_id", benutzer_id)
                                insertArtikelCmd.Parameters.AddWithValue("@erstellt_am", aktuelles_Datum)
                                insertArtikelCmd.Parameters.AddWithValue("@benutzername", benutzername)

                                ' Führt sowohl das INSERT als auch das SELECT aus
                                Dim artikelId As Integer = Convert.ToInt32(Await insertArtikelCmd.ExecuteScalarAsync())

                                ' Bilder in der Datenbank speichern
                                For Each imageDataItem In imageDataList
                                    Await StoreImageInDatabaseAsync(verbindung, transaction, artikelId, imageDataItem.Platzhalter, imageDataItem.ImageData)
                                Next

                                ' Tags speichern
                                For Each tagName In tagsList
                                    ' Überprüfen, ob der Tag bereits existiert
                                    Dim getTagIdCmd As New SQLiteCommand("SELECT TagId FROM T_Knowledge_Base_Tags WHERE TagName = @TagName", verbindung, transaction)
                                    getTagIdCmd.Parameters.AddWithValue("@TagName", tagName)
                                    Dim tagIdObj As Object = Await getTagIdCmd.ExecuteScalarAsync()
                                    Dim tagId As Integer

                                    If tagIdObj IsNot Nothing Then
                                        tagId = Convert.ToInt32(tagIdObj)
                                    Else
                                        ' Neuer Tag einfügen und die zuletzt eingefügte ID abrufen
                                        Dim insertTagCmd As New SQLiteCommand("
                                            INSERT INTO T_Knowledge_Base_Tags (TagName) 
                                            VALUES (@TagName);
                                            SELECT last_insert_rowid();", verbindung, transaction)

                                        insertTagCmd.Parameters.AddWithValue("@TagName", tagName)

                                        tagId = Convert.ToInt32(Await insertTagCmd.ExecuteScalarAsync())
                                    End If

                                    ' Verknüpfung zwischen Artikel und Tag erstellen
                                    Dim insertArtikelTagCmd As New SQLiteCommand("INSERT INTO T_Knowledge_Base_ArtikelTags (ArtikelId, TagId) VALUES (@ArtikelId, @TagId)", verbindung, transaction)
                                    insertArtikelTagCmd.Parameters.AddWithValue("@ArtikelId", artikelId)
                                    insertArtikelTagCmd.Parameters.AddWithValue("@TagId", tagId)
                                    Await insertArtikelTagCmd.ExecuteNonQueryAsync()
                                Next

                                ' Ordner aktualisieren
                                ' Abrufen der parent_id des ausgewählten Ordners
                                Dim getParentIdCmd As New SQLiteCommand("
                                    SELECT id FROM T_Knowledge_Base_Filesystem WHERE Name = @ordnername AND ist_verzeichnis = 1", verbindung, transaction)
                                getParentIdCmd.Parameters.AddWithValue("@ordnername", ordner)
                                Dim parentIdObj As Object = Await getParentIdCmd.ExecuteScalarAsync()
                                Dim parentId As Integer

                                If parentIdObj IsNot Nothing Then
                                    parentId = Convert.ToInt32(parentIdObj)

                                    ' Einfügen des neuen Artikels in die T_Knowledge_Base_Filesystem-Tabelle
                                    Dim insertFilesystemCmd As New SQLiteCommand("
                                        INSERT INTO T_Knowledge_Base_Filesystem (Name, ist_verzeichnis, parent_id, Artikel_ID)
                                        VALUES (@Name, 0, @parent_id, @Artikel_ID)", verbindung, transaction)
                                    insertFilesystemCmd.Parameters.AddWithValue("@Name", artikeltitel)
                                    insertFilesystemCmd.Parameters.AddWithValue("@parent_id", parentId)
                                    insertFilesystemCmd.Parameters.AddWithValue("@Artikel_ID", artikelId)
                                    Await insertFilesystemCmd.ExecuteNonQueryAsync()
                                Else
                                    ' Ordner existiert nicht, also neuen Ordner erstellen
                                    Dim insertFolderCmd As New SQLiteCommand("
                                        INSERT INTO T_Knowledge_Base_Filesystem (Name, ist_verzeichnis, parent_id)
                                        VALUES (@Name, 1, @parent_id);
                                        SELECT last_insert_rowid();", verbindung, transaction)
                                    insertFolderCmd.Parameters.AddWithValue("@Name", ordner)
                                    insertFolderCmd.Parameters.AddWithValue("@parent_id", 0) ' Root-Verzeichnis

                                    parentId = Convert.ToInt32(Await insertFolderCmd.ExecuteScalarAsync())

                                    ' Einfügen des neuen Artikels unter dem neu erstellten Ordner
                                    Dim insertFilesystemCmd As New SQLiteCommand("
                                        INSERT INTO T_Knowledge_Base_Filesystem (Name, ist_verzeichnis, parent_id, Artikel_ID)
                                        VALUES (@Name, 0, @parent_id, @Artikel_ID)", verbindung, transaction)
                                    insertFilesystemCmd.Parameters.AddWithValue("@Name", artikeltitel)
                                    insertFilesystemCmd.Parameters.AddWithValue("@parent_id", parentId)
                                    insertFilesystemCmd.Parameters.AddWithValue("@Artikel_ID", artikelId)
                                    Await insertFilesystemCmd.ExecuteNonQueryAsync()
                                End If

                                ' Erfolgsmeldung anzeigen
                                Dim erfolgsmeldung As New Snackbar(SnackbarPresenter) With {
                                    .Title = "Juhu!",
                                    .Appearance = ControlAppearance.Success,
                                    .Content = "Der Artikel wurde erfolgreich gespeichert.",
                                    .Timeout = TimeSpan.FromSeconds(2)}
                                erfolgsmeldung.Show()

                                Await Task.Delay(2000)

                                ' Navigation zur Startseite
                                Try
                                    NavigationService.Navigate(New LandingPage())
                                Catch ex As Exception
                                    ' Fehlerbehandlungscode hier einfügen
                                End Try
                            Else
                                ' Der Benutzer hat abgebrochen
                                Return
                            End If
                        End If

                        ' Transaktion abschließen
                        Await transaction.CommitAsync()
                    Catch ex As Exception
                        ' Bei einem Fehler die Transaktion zurückrollen
                        transaction.RollbackAsync()
                        ' Fehler anzeigen
                        Dim fehlermeldung As New Snackbar(SnackbarPresenter) With {
                            .Title = "Fehler!",
                            .Appearance = ControlAppearance.Danger,
                            .Content = $"Fehler beim Speichern des Artikels: {ex.Message}",
                            .Timeout = TimeSpan.FromSeconds(5)}
                        fehlermeldung.Show()
                    End Try
                End Using
                verbindung.Close()
            End Using
        Catch ex As Exception
            ' Allgemeine Fehlerbehandlung
            Dim fehlermeldung As New Snackbar(SnackbarPresenter) With {
                .Title = "Unbekannter Fehler!",
                .Appearance = ControlAppearance.Danger,
                .Content = $"Ein unerwarteter Fehler ist aufgetreten: {ex.Message}",
                .Timeout = TimeSpan.FromSeconds(5)}
            fehlermeldung.Show()
        End Try
    End Sub

    Private Async Function GetFolderNamesAsync(verbindung As SQLiteConnection, transaction As SQLiteTransaction) As Task(Of List(Of String))
        Dim folderNames As New List(Of String)
        Dim sql As String = "SELECT Name FROM T_Knowledge_Base_Filesystem WHERE ist_verzeichnis = 1"

        Using command As New SQLiteCommand(sql, verbindung, transaction)
            Using reader As SQLiteDataReader = Await command.ExecuteReaderAsync()
                While Await reader.ReadAsync()
                    folderNames.Add(reader("Name").ToString())
                End While
            End Using
        End Using

        Return folderNames
    End Function

    Private Async Function Editorinhalt_mit_HTML() As Task(Of String)
        Dim jsonContent As String = Await Browser.CoreWebView2.ExecuteScriptAsync("window.getEditorinhalt_mit_HTML();")
        ' Deserialisieren des JSON-Strings
        Dim content As String = JsonConvert.DeserializeObject(Of String)(jsonContent)
        Return content
    End Function

    Private Async Function Editorinhalt_ohne_HTML() As Task(Of String)
        Dim jsonResult As String = Await Browser.CoreWebView2.ExecuteScriptAsync("window.getEditorinhalt_ohne_HTML();")
        Dim result As String = JsonConvert.DeserializeObject(Of String)(jsonResult)
        Return result
    End Function

    Private Async Function LoadArticleContent() As Task
        Await Browser.CoreWebView2.ExecuteScriptAsync("CKEDITOR.instances.editor.setData('');")
        If ArtikelId.HasValue Then
            Using connection As New SQLiteConnection(ConnectionString)
                Await connection.OpenAsync()
                Dim command As New SQLiteCommand("SELECT Artikel_Inhalt_HTML FROM T_Knowledge_Base_Artikelverwaltung WHERE Id = @Id", connection)
                command.Parameters.AddWithValue("@Id", Me.ArtikelId)
                Dim reader As SQLiteDataReader = Await command.ExecuteReaderAsync()
                If Await reader.ReadAsync() Then
                    Dim articleContent As String = reader("Artikel_Inhalt_HTML").ToString()
                    ' Bilder im HTML-Inhalt wiederherstellen
                    articleContent = Await RestoreImagesInHtml(articleContent, Me.ArtikelId.Value)

                    ' Artikelinhalt in den CKEditor laden
                    Await InsertHtmlIntoEditor(articleContent)
                End If
                Await reader.CloseAsync()
                connection.Close()
            End Using
        Else
            ' Neuer Artikel, kein Inhalt zu laden
            ' Optional können Sie hier Standardwerte setzen oder den Editor leeren
            Await InsertHtmlIntoEditor("") ' Leeren Inhalt in den CKEditor laden
        End If
    End Function

    Private Function ProcessHtmlContent(htmlContent As String) As (ModifiedHtml As String, ImageDataList As List(Of (Platzhalter As String, ImageData As Byte())))
        ' HTML-Inhalt laden
        Dim doc As New HtmlDocument()
        doc.LoadHtml(htmlContent)

        ' Zähler für Platzhalter
        Dim imageCounter As Integer = 1

        ' Liste zum Speichern von Bilddaten und Platzhaltern
        Dim imageDataList As New List(Of (String, Byte()))()

        ' Alle <img>-Tags finden
        Dim imgNodes As HtmlNodeCollection = doc.DocumentNode.SelectNodes("//img")

        If imgNodes IsNot Nothing Then
            For Each imgNode As HtmlNode In imgNodes
                Dim srcValue As String = imgNode.GetAttributeValue("src", "")

                If Not String.IsNullOrEmpty(srcValue) Then
                    Dim imageData As Byte()

                    If srcValue.StartsWith("data:image") Then
                        ' Base64-Daten extrahieren
                        Dim base64Data As String = srcValue.Substring(srcValue.IndexOf("base64,") + 7)
                        imageData = Convert.FromBase64String(base64Data)
                    Else
                        ' Bilddaten von der Quelle abrufen
                        imageData = GetImageDataFromSrc(srcValue)
                    End If

                    If imageData IsNot Nothing Then
                        ' Platzhalter generieren
                        Dim platzhalter As String = $"{{Bild{imageCounter}}}"
                        imageCounter += 1

                        ' Bilddaten und Platzhalter speichern
                        imageDataList.Add((platzhalter, imageData))

                        ' src-Attribut durch Platzhalter ersetzen
                        imgNode.SetAttributeValue("src", platzhalter)
                    End If
                End If
            Next
        End If

        ' Modifizierten HTML-Inhalt zurückgeben
        Return (doc.DocumentNode.OuterHtml, imageDataList)
    End Function

    Private Async Function StoreImageInDatabaseAsync(verbindung As SQLiteConnection, transaction As SQLiteTransaction, artikelId As Integer, platzhalter As String, imageData As Byte()) As Task
        ' Bilddaten in Base64 konvertieren
        Dim base64Data As String = Convert.ToBase64String(imageData)

        ' Bild in der Datenbank speichern
        Dim cmd As New SQLiteCommand("INSERT INTO T_Knowledge_Base_Bilder (Artikel_Id, Platzhalter, Bilddaten) VALUES (@Artikel_Id, @Platzhalter, @Bilddaten)", verbindung, transaction)

        cmd.Parameters.AddWithValue("@Artikel_Id", artikelId)
        cmd.Parameters.AddWithValue("@Platzhalter", platzhalter)
        cmd.Parameters.AddWithValue("@Bilddaten", base64Data)

        Await cmd.ExecuteNonQueryAsync()
    End Function

    Private Function GetImageDataFromSrc(src As String) As Byte()
        Try
            If src.StartsWith("http") Then
                ' Bild von URL herunterladen
                Using client As New System.Net.WebClient()
                    Return client.DownloadData(src)
                End Using
            ElseIf src.StartsWith("file:///") Then
                ' Bild von lokalem Pfad lesen
                Dim filePath As String = New Uri(src).LocalPath
                Return System.IO.File.ReadAllBytes(filePath)
            Else
                ' Andere Fälle nicht unterstützt
                Return Nothing
            End If
        Catch ex As Exception
            ' Fehlerbehandlung
            Return Nothing
        End Try
    End Function

    ' Funktion zum Wiederherstellen der Bilder im HTML-Inhalt beim Laden des Artikels
    Private Async Function RestoreImagesInHtml(htmlContent As String, artikelId As Integer) As Task(Of String)
        Using connection As New SQLiteConnection(ConnectionString)
            Await connection.OpenAsync()

            ' Alle Platzhalter finden
            Dim pattern As String = "\{Bild\d+\}"
            Dim matches = Regex.Matches(htmlContent, pattern)

            For Each match As Match In matches
                Dim platzhalter As String = match.Value

                ' Bilddaten aus der Datenbank abrufen
                Dim cmd As New SQLiteCommand("SELECT Bilddaten FROM T_Knowledge_Base_Bilder WHERE Artikel_Id = @Artikel_Id AND Platzhalter = @Platzhalter", connection)
                cmd.Parameters.AddWithValue("@Artikel_Id", artikelId)
                cmd.Parameters.AddWithValue("@Platzhalter", platzhalter)

                Dim base64DataObj As Object = Await cmd.ExecuteScalarAsync()
                If base64DataObj IsNot Nothing Then
                    Dim base64Data As String = base64DataObj.ToString()
                    ' Bestimmen des MIME-Typs (optional, falls in der Datenbank gespeichert)
                    ' Hier wird standardmäßig "image/png" verwendet
                    Dim mimeType As String = "image/png"
                    ' Falls der MIME-Typ gespeichert ist, passen Sie den Code entsprechend an

                    ' Bilddaten als Data-URL einfügen
                    Dim dataUrl As String = $"data:{mimeType};base64,{base64Data}"
                    htmlContent = htmlContent.Replace(platzhalter, dataUrl)
                End If
            Next

            connection.Close()
        End Using

        Return htmlContent
    End Function
    Private Function ConvertWordToHtml(wordFilePath As String) As String
        If Not File.Exists(wordFilePath) Then
            Throw New FileNotFoundException("Die angegebene Word-Datei wurde nicht gefunden.", wordFilePath)
        End If

        ' Erstellen eines temporären Dateipfads
        Dim tempFilePath As String = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() & Path.GetExtension(wordFilePath))
        File.Copy(wordFilePath, tempFilePath, True)

        Try
            Using fileStream As New FileStream(tempFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
                Dim document As New Aspose.Words.Document(fileStream)
                Using memoryStream As New MemoryStream()
                    Dim saveOptions As New HtmlSaveOptions() With {
                    .ExportImagesAsBase64 = True,
                    .SaveFormat = SaveFormat.Html,
                    .PrettyFormat = True,
                    .CssStyleSheetType = CssStyleSheetType.Inline
                }
                    document.Save(memoryStream, saveOptions)
                    memoryStream.Position = 0
                    Using reader As New StreamReader(memoryStream)
                        Return reader.ReadToEnd()
                    End Using
                End Using
            End Using
        Catch ex As Exception
            Throw New ApplicationException("Fehler beim Konvertieren des Word-Dokuments in HTML.", ex)
        Finally
            ' Löschen der temporären Datei
            If File.Exists(tempFilePath) Then
                File.Delete(tempFilePath)
            End If
        End Try
    End Function

    Private Async Sub B_Word_Click(sender As Object, e As RoutedEventArgs) Handles B_Word.Click
        ' Open file dialog to select Word document
        Dim openFileDialog As New Microsoft.Win32.OpenFileDialog() With {
            .Filter = "Word Documents (*.docx)|*.docx|All Files (*.*)|*.*"
        }

        Dim result As Boolean? = openFileDialog.ShowDialog()

        If result = True Then
            Dim filePath As String = openFileDialog.FileName

            ' Convert Word document to HTML
            Dim htmlContent As String = ConvertWordToHtml(filePath)

            If Not String.IsNullOrEmpty(htmlContent) Then
                ' Insert HTML content into CKEditor
                Await InsertHtmlIntoEditor(htmlContent)
            End If
        End If
    End Sub

    Private Function GetMimeTypeFromImageFile(filePath As String) As String
        Dim extension As String = System.IO.Path.GetExtension(filePath).ToLower()
        Select Case extension
            Case ".jpg", ".jpeg"
                Return "image/jpeg"
            Case ".png"
                Return "image/png"
            Case ".gif"
                Return "image/gif"
            Case ".bmp"
                Return "image/bmp"
            Case Else
                Return "application/octet-stream"
        End Select
    End Function

    Private Async Function InsertHtmlIntoEditor(htmlContent As String) As Task
        ' HTML-Inhalt laden
        Dim htmlDoc As New HtmlDocument()
        htmlDoc.LoadHtml(htmlContent)

        ' 1. Entferne das erste <img>-Tag
        Dim firstImage As HtmlNode = htmlDoc.DocumentNode.SelectSingleNode("//img")
        If firstImage IsNot Nothing Then
            firstImage.ParentNode.RemoveChild(firstImage, True)
        End If

        ' 2. Entferne alle Elemente, die das Wort "Aspose" enthalten
        ' Dies kann in Textinhalt oder in Attributen vorkommen
        Dim nodesWithAspose As HtmlNodeCollection = htmlDoc.DocumentNode.SelectNodes("//*[contains(text(), 'Aspose') or contains(@*, 'Aspose')]")

        If nodesWithAspose IsNot Nothing Then
            ' ToList() erstellen, um eine Kopie der Sammlung zu verwenden
            For Each node In nodesWithAspose.ToList()
                If node.ParentNode IsNot Nothing Then
                    node.ParentNode.RemoveChild(node, True)
                End If
            Next
        End If

        ' 3. Entferne spezifische unerwünschte Textabschnitte
        ' Definiere die zu entfernenden Texte
        Dim unwantedTexts As String() = {
        "Evaluation Only. Created with Aspose.Words. Copyright 2003-2024 Aspose Pty Ltd.",
        "https://products.aspose.com/words/temporary-license/",
        "Created with an evaluation copy of Aspose.Words. To remove all limitations, you can use Free Temporary License "
    }

        ' Suche nach Textknoten, die diese unerwünschten Texte enthalten, und entferne sie
        Dim textNodes = htmlDoc.DocumentNode.SelectNodes("//text()")
        If textNodes IsNot Nothing Then
            For Each textNode As HtmlTextNode In textNodes.OfType(Of HtmlTextNode)()
                For Each unwantedText In unwantedTexts
                    If textNode.Text.Contains(unwantedText) Then
                        ' Entferne den unerwünschten Text aus dem Textknoten
                        textNode.Text = textNode.Text.Replace(unwantedText, "").Trim()
                    End If
                Next
            Next
        End If

        ' Optional: Entferne leere Textknoten, die nach dem Entfernen der unerwünschten Texte entstehen könnten
        If textNodes IsNot Nothing Then
            For Each textNode As HtmlTextNode In textNodes.OfType(Of HtmlTextNode)()
                If String.IsNullOrWhiteSpace(textNode.Text) Then
                    If textNode.ParentNode IsNot Nothing Then
                        textNode.ParentNode.RemoveChild(textNode, False)
                    End If
                End If
            Next
        End If

        ' Überarbeitetes HTML abrufen
        Dim modifiedHtmlContent As String = htmlDoc.DocumentNode.OuterHtml

        ' HTML-Inhalt in einen JSON-kompatiblen String konvertieren
        Dim jsonHtmlContent As String = JsonConvert.SerializeObject(modifiedHtmlContent)

        ' JavaScript-Code zum Einfügen des HTML-Inhalts
        ' Stellen Sie sicher, dass der CKEditor-Instanzname korrekt ist (z.B., 'editor')
        Dim script As String = $"CKEDITOR.instances.editor.setData({jsonHtmlContent});"

        Await Browser.CoreWebView2.ExecuteScriptAsync(script)
    End Function

    ' Funktion zum Konvertieren von PDF zu HTML (angepasst mit Aspose.Pdf)
    Private Function ConvertPdfToHtml(pdfPath As String) As String
        Try
            ' Laden des PDF-Dokuments
            Dim pdfDocument As New Document(pdfPath)

            ' Erstellen eines HTML-Speichers
            Using stringStream As New MemoryStream()
                ' Konfiguration des HtmlSaveOptions
                Dim saveOptions As New HtmlSaveOptions()
                saveOptions.ExportImagesAsBase64 = True ' Bilder als Base64 einbetten
                saveOptions.DocumentSplitCriteria = False ' Alle Inhalte in eine HTML-Datei

                ' Speichern des PDF als HTML in den MemoryStream
                pdfDocument.Save(stringStream, saveOptions)

                ' Konvertierung des MemoryStream in einen String
                stringStream.Position = 0
                Using reader As New StreamReader(stringStream)
                    Dim html As String = reader.ReadToEnd()
                    Return html
                End Using
            End Using
        Catch ex As Exception
            ' Fehlerbehandlung
            Dim snackbar As New Snackbar(SnackbarPresenter) With {
                .Title = "Fehler",
                .Appearance = ControlAppearance.Danger,
                .Content = $"Fehler bei der Konvertierung des PDF zu HTML: {ex.Message}",
                .Timeout = TimeSpan.FromSeconds(5)}
            snackbar.Show()
            Return String.Empty
        End Try
    End Function

    Private Async Sub B_PDF_Click(sender As Object, e As RoutedEventArgs) Handles B_PDF.Click
        Try
            ' Öffne einen Dateiauswahldialog für PDF-Dateien
            Dim openFileDialog As New OpenFileDialog() With {
                .Filter = "PDF Dateien (*.pdf)|*.pdf|Alle Dateien (*.*)|*.*",
                .Title = "PDF Datei auswählen"
            }

            Dim result As Boolean? = openFileDialog.ShowDialog()

            If result = True Then
                Dim pdfPath As String = openFileDialog.FileName

                ' Überprüfen, ob die ausgewählte Datei tatsächlich eine PDF ist
                If Path.GetExtension(pdfPath).ToLower() <> ".pdf" Then
                    MsgBox("Bitte wählen Sie eine gültige PDF-Datei aus.", MsgBoxStyle.Exclamation, "Ungültige Datei")
                    Return
                End If

                ' Sicherstellen, dass WebView2 initialisiert ist
                If Browser.CoreWebView2 Is Nothing Then
                    Await Browser.EnsureCoreWebView2Async()
                End If

                ' Überprüfen, ob die Datei existiert
                If Not File.Exists(pdfPath) Then
                    MsgBox("Die ausgewählte Datei existiert nicht.", MsgBoxStyle.Critical, "Datei nicht gefunden")
                    Return
                End If

                ' PDF in HTML konvertieren
                Dim htmlContent As String = ConvertPdfToHtml(pdfPath)

                ' Überprüfen, ob die Konvertierung erfolgreich war
                If String.IsNullOrEmpty(htmlContent) Then
                    MsgBox("Die PDF-Datei konnte nicht konvertiert werden.", MsgBoxStyle.Critical, "Konvertierungsfehler")
                    Return
                End If

                ' HTML-Inhalt in den CKEditor einfügen
                Await InsertHtmlIntoEditor(htmlContent)

                ' Erfolgsmeldung anzeigen
                Dim snackbar As New Snackbar(SnackbarPresenter) With {
                    .Title = "Erfolg",
                    .Appearance = ControlAppearance.Success,
                    .Content = "Das PDF wurde erfolgreich in den Editor eingefügt.",
                    .Timeout = TimeSpan.FromSeconds(2)}
                snackbar.Show()
            End If
        Catch ex As Exception
            ' Fehlerbehandlung
            Dim snackbar As New Snackbar(SnackbarPresenter) With {
                .Title = "Fehler",
                .Appearance = ControlAppearance.Danger,
                .Content = $"Ein Fehler ist aufgetreten: {ex.Message}",
                .Timeout = TimeSpan.FromSeconds(5)}
            snackbar.Show()
        End Try
    End Sub

End Class
