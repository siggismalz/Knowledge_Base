Imports HtmlAgilityPack
Imports Microsoft.Win32
Imports Newtonsoft.Json.Linq
Imports System.Collections.ObjectModel
Imports System.Data.SQLite
Imports System.Text.RegularExpressions
Imports System.IO
Imports Wpf.Ui.Controls
Partial Public Class Artikel_anzeigen
    Inherits Page

    Public Property BreadcrumbItems As ObservableCollection(Of BreadcrumbItem)
    Public Property Artikel As New Artikel ' Assuming you have a model class for the article
    Private isProgrammaticSelection As Boolean = False

    Public Sub SetArticleId(articleId As Long)
        Artikel.ID = articleId
        InitializeAsync()
    End Sub

    Public Sub New()
        InitializeComponent()
        ErstelleFavoritenTabelle()
    End Sub

    Public Sub New(breadcrumbItems As ObservableCollection(Of BreadcrumbItem))
        InitializeComponent()
        Me.BreadcrumbItems = New ObservableCollection(Of BreadcrumbItem)(breadcrumbItems)
        Me.DataContext = Me
        ErstelleFavoritenTabelle()
        InitializeAsync()
    End Sub

    Private Async Sub InitializeAsync()
        Await Browser_laden()
        Artikel_laden(Artikel.ID)
        Artikel_anzeigen()
    End Sub

    ''' <summary>
    ''' Erstellt die Favoriten-Tabelle, falls sie nicht existiert.
    ''' </summary>
    Private Sub ErstelleFavoritenTabelle()
        Dim dbFilePath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "KnowledgeBase.db")
        Using verbindung As New SQLiteConnection($"Data Source={dbFilePath};Version=3;")
            verbindung.Open()
            Dim createTableCmd As New SQLiteCommand("
                CREATE TABLE IF NOT EXISTS T_Favoriten (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ArtikelId INTEGER NOT NULL UNIQUE,
                    Erstellungsdatum DATETIME DEFAULT CURRENT_TIMESTAMP
                );", verbindung)
            createTableCmd.ExecuteNonQuery()
        End Using
    End Sub

    Public Async Function Browser_laden() As Task
        Await Browser.EnsureCoreWebView2Async()
        ' Optional: Navigieren Sie zu einer Standardseite
        Browser.CoreWebView2.Navigate("about:blank")
    End Function

    Private Sub Artikel_laden(Id As Integer)
        Dim dbFilePath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "KnowledgeBase.db")
        Dim verbindung As New SQLiteConnection($"Data Source={dbFilePath};Version=3;")
        verbindung.Open()

        ' Hauptartikel laden
        Dim SQL As New SQLiteCommand("SELECT DISTINCT Titel, Artikel_Inhalt_HTML, Erstellt_von, Erstellt_am FROM T_Knowledge_Base_Artikelverwaltung WHERE Id = @Id", verbindung)
        SQL.Parameters.AddWithValue("@Id", Id)
        Dim reader As SQLiteDataReader = SQL.ExecuteReader()
        If reader.Read() Then
            Artikel.Titel = reader("Titel").ToString()
            Dim artikelInhaltHtml As String = reader("Artikel_Inhalt_HTML").ToString()
            ' Bilder wiederherstellen
            Dim fullHtmlContent As String = RestoreImagesInHtml(artikelInhaltHtml, Id)

            ' Einbetten in ein HTML-Gerüst mit Druck-CSS
            Dim htmlContent As String = "<html>" & vbCrLf &
        "<head>" & vbCrLf &
        "    <style>" & vbCrLf &
        "        body { font-family: Calibri; color: white; background-color: #292e37; }" & vbCrLf &
        "        @media print {" & vbCrLf &
        "            body { color: black !important; background-color: white !important; }" & vbCrLf &
        "            h1, h2, h3, h4, h5, h6 { color: black !important; }" & vbCrLf &
        "            p { color: black !important; }" & vbCrLf &
        "            a { color: black !important; text-decoration: none !important; }" & vbCrLf &
        "        }" & vbCrLf &
        "    </style>" & vbCrLf &
        "</head>" & vbCrLf &
        "<body>" & fullHtmlContent & "</body>" & vbCrLf &
        "</html>"

            Artikel.Artikel_Inhalt_HTML = htmlContent
            Artikel.Autor = reader("Erstellt_von").ToString()
            Artikel.Erstellt_am = Convert.ToDateTime(reader("Erstellt_am")) ' Stelle sicher, dass du ein DateTime verwendest
        End If
        reader.Close()

        ' Tags für den Artikel abrufen
        Dim tagsCommand As New SQLiteCommand("SELECT t.TagName FROM T_Knowledge_Base_Tags t INNER JOIN T_Knowledge_Base_ArtikelTags at ON t.TagId = at.TagId WHERE at.ArtikelId = @Id", verbindung)
        tagsCommand.Parameters.AddWithValue("@Id", Id)
        Dim tagsReader As SQLiteDataReader = tagsCommand.ExecuteReader()
        Artikel.Tags = New List(Of String)()
        While tagsReader.Read()
            Artikel.Tags.Add(tagsReader("TagName").ToString())
        End While
        tagsReader.Close()

        ' Alte Versionen des Artikels abrufen
        ' Versionsliste initialisieren
        Dim versionsListData As New List(Of ArtikelVersion)()

        ' Aktuelle Version hinzufügen
        versionsListData.Add(New ArtikelVersion() With {
            .VersionId = 0, ' 0 kennzeichnet die aktuelle Version
            .Versioniert_am = Artikel.Erstellt_am,
            .Titel = Artikel.Titel
        })

        ' Alte Versionen des Artikels abrufen
        Dim versionsCommand As New SQLiteCommand("SELECT VersionId, Titel, Versioniert_am FROM T_Knowledge_Base_Artikelversionen WHERE ArtikelId = @Id ORDER BY Versioniert_am DESC", verbindung)
        versionsCommand.Parameters.AddWithValue("@Id", Id)
        Dim versionsReader As SQLiteDataReader = versionsCommand.ExecuteReader()

        While versionsReader.Read()
            versionsListData.Add(New ArtikelVersion() With {
                .VersionId = Convert.ToInt32(versionsReader("VersionId")),
                .Versioniert_am = Convert.ToDateTime(versionsReader("Versioniert_am")),
                .Titel = versionsReader("Titel").ToString()
            })
        End While
        versionsReader.Close()

        ' Versionsliste an das UI binden
        VersionsList.ItemsSource = versionsListData

        ' Programmatische Auswahl beginnen
        isProgrammaticSelection = True

        ' Aktuelle Version standardmäßig auswählen
        VersionsList.SelectedIndex = 0

        ' Programmatische Auswahl beendet
        isProgrammaticSelection = False

        verbindung.Close()
    End Sub

    Private Sub Artikel_anzeigen()
        Me.T_Erstellt_am.Text = Artikel.Erstellt_am.ToString("dd.MM.yyyy HH:mm")
        Me.T_Autor.Text = Artikel.Autor
        Me.T_Artikeltitel.Text = Artikel.Titel

        ' Build the TOC and update the HTML content
        BuildTOCFromHtml(Artikel.Artikel_Inhalt_HTML)

        ' Display the updated HTML content with heading IDs
        Browser.CoreWebView2.NavigateToString(Artikel.Artikel_Inhalt_HTML)

        ' Füge den aktuellen Artikel zum Breadcrumb hinzu
        Dim artikelBreadcrumbItem As New BreadcrumbItem With {
            .DisplayName = Artikel.Titel,
            .Id = 0,
            .ParentId = -1,
            .IstVerzeichnis = False,
            .Artikel_ID = Artikel.ID
        }
        BreadcrumbItems.Add(artikelBreadcrumbItem)

        ' Tags an das UI binden
        Me.TagsList.ItemsSource = Artikel.Tags

        ' Aktualisiere den Favoriten-Button
        SetzeFavoritenButtonStatus(IstFavorit(Artikel.ID))
    End Sub

    Private Sub BreadcrumbItem_Click(sender As Object, e As RoutedEventArgs)
        Dim hyperlink As Hyperlink = TryCast(sender, Hyperlink)
        If hyperlink Is Nothing Then
            Return
        End If

        Dim breadcrumbItem As BreadcrumbItem = TryCast(hyperlink.DataContext, BreadcrumbItem)
        If breadcrumbItem Is Nothing Then
            Return
        End If

        If breadcrumbItem.IstVerzeichnis Then
            ' Zurück zum Explorer und Inhalte des Ordners laden
            NavigationService.Navigate(New Explorer(breadcrumbItem.Id))
        ElseIf breadcrumbItem.Artikel_ID.HasValue Then
            ' Artikel laden
            Artikel.ID = breadcrumbItem.Artikel_ID.Value
            ' Erstellen einer neuen Breadcrumb-Liste bis zu diesem Artikel
            Dim index As Integer = BreadcrumbItems.IndexOf(breadcrumbItem)
            Dim newBreadcrumbItems As New ObservableCollection(Of BreadcrumbItem)(BreadcrumbItems.Take(index + 1))
            NavigationService.Navigate(New Artikel_anzeigen(newBreadcrumbItems))
        Else
            ' Fehlende Artikel_ID, möglicherweise ein Fehler in den Daten
            MsgBox("Dieser Artikel kann nicht geladen werden, da keine Artikel_ID verfügbar ist.")
        End If
    End Sub

    Private Function RestoreImagesInHtml(htmlContent As String, artikelId As Integer) As String
        Dim dbFilePath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "KnowledgeBase.db")
        Using connection As New SQLiteConnection($"Data Source={dbFilePath};Version=3;")
            connection.Open()

            ' Alle Platzhalter finden
            Dim pattern As String = "\{Bild\d+\}"
            Dim matches = Regex.Matches(htmlContent, pattern)

            For Each match As Match In matches
                Dim platzhalter As String = match.Value

                ' Bilddaten aus der Datenbank abrufen
                Dim cmd As New SQLiteCommand("SELECT Bilddaten FROM T_Knowledge_Base_Bilder WHERE Artikel_Id = @Artikel_Id AND Platzhalter = @Platzhalter", connection)
                cmd.Parameters.AddWithValue("@Artikel_Id", artikelId)
                cmd.Parameters.AddWithValue("@Platzhalter", platzhalter)

                Dim base64DataObj As Object = cmd.ExecuteScalar()
                If base64DataObj IsNot Nothing Then
                    Dim base64Data As String = base64DataObj.ToString()
                    ' Bilddaten als Data-URL einfügen
                    Dim dataUrl As String = $"data:image/png;base64,{base64Data}"
                    htmlContent = htmlContent.Replace(platzhalter, dataUrl)
                End If
            Next
        End Using

        Return htmlContent
    End Function

    Private Sub BuildTOCFromHtml(ByRef htmlContent As String)
        ' Parse the HTML
        Dim doc As New HtmlDocument()
        doc.LoadHtml(htmlContent)

        ' Select all heading elements
        Dim headingNodes = doc.DocumentNode.SelectNodes("//h1|//h2|//h3|//h4|//h5|//h6")
        If headingNodes Is Nothing Then
            Return
        End If

        ' Build the TOC tree
        Dim tocItems As New ObservableCollection(Of TOCItem)()
        Dim currentParents As New Stack(Of TOCItem)()
        Dim headingIndex As Integer = 1

        For Each headingNode In headingNodes
            Dim headingText As String = headingNode.InnerText
            Dim headingLevel As Integer = Integer.Parse(headingNode.Name.Substring(1))
            Dim headingId As String = $"heading{headingIndex}"

            ' Assign the ID to the heading node
            headingNode.SetAttributeValue("id", headingId)

            Dim tocItem As New TOCItem With {
                .Titel = headingText,
                .Level = headingLevel,
                .HeadingId = headingId
            }

            If currentParents.Count = 0 Then
                ' No parent, add to root
                tocItems.Add(tocItem)
                currentParents.Push(tocItem)
            Else
                While currentParents.Count > 0 AndAlso currentParents.Peek().Level >= headingLevel
                    currentParents.Pop()
                End While

                If currentParents.Count > 0 Then
                    currentParents.Peek().Children.Add(tocItem)
                Else
                    tocItems.Add(tocItem)
                End If

                currentParents.Push(tocItem)
            End If

            headingIndex += 1
        Next

        ' Get the modified HTML with IDs
        htmlContent = doc.DocumentNode.OuterHtml

        ' Bind the TOC to the TreeView
        Inhaltsverzeichnis.ItemsSource = tocItems
    End Sub

    Private Sub Inhaltsverzeichnis_SelectedItemChanged(sender As Object, e As RoutedPropertyChangedEventArgs(Of Object))
        Dim selectedItem As TOCItem = TryCast(Inhaltsverzeichnis.SelectedItem, TOCItem)
        If selectedItem IsNot Nothing Then
            ' Scroll to the heading in the WebView
            Dim script As String = $"document.getElementById('{selectedItem.HeadingId}').scrollIntoView();"
            Browser.CoreWebView2.ExecuteScriptAsync(script)
        End If
    End Sub

    ' Event-Handler für das PDF-Download
    Private Async Sub B_Artikel_als_PDF_speichern_Click(sender As Object, e As RoutedEventArgs) Handles B_Artikel_als_PDF_speichern.Click
        Try
            ' Öffnen Sie einen SaveFileDialog, um den Speicherort auszuwählen
            Dim saveFileDialog As New SaveFileDialog()
            saveFileDialog.Filter = "PDF-Dateien (*.pdf)|*.pdf"
            saveFileDialog.Title = "Artikel als PDF speichern"
            saveFileDialog.FileName = $"{Artikel.Titel}.pdf"

            If saveFileDialog.ShowDialog() = True Then
                Dim filePath As String = saveFileDialog.FileName

                ' Überprüfen Sie, ob CoreWebView2 initialisiert ist
                If Browser.CoreWebView2 Is Nothing Then
                    MsgBox("Der Browser ist noch nicht initialisiert.", MsgBoxStyle.Exclamation, "Fehler")
                    Return
                End If

                ' Definieren Sie die Optionen für den PDF-Export
                Dim printOptions As String = "{""landscape"":false,""displayHeaderFooter"":false,""printBackground"":true}"

                ' Führen Sie das DevTools-Protokoll-Kommando zum Drucken in PDF aus
                Dim pdfDataBase64 As String = Await Browser.CoreWebView2.CallDevToolsProtocolMethodAsync("Page.printToPDF", printOptions)

                ' Das Ergebnis ist eine JSON-Zeichenkette, die die Base64-PDF-Daten enthält
                ' Wir müssen die Base64-Daten extrahieren
                Dim pdfDataJson As JObject = JObject.Parse(pdfDataBase64)
                Dim pdfBase64 As String = pdfDataJson("data").ToString()

                ' Konvertieren Sie die Base64-Zeichenfolge in ein Byte-Array
                Dim pdfBytes As Byte() = Convert.FromBase64String(pdfBase64)

                ' Schreiben Sie die Bytes in die Datei
                System.IO.File.WriteAllBytes(filePath, pdfBytes)

                MsgBox("PDF wurde erfolgreich gespeichert.", MsgBoxStyle.Information, "Erfolg")
            End If
        Catch ex As Exception
            MsgBox($"Beim Speichern des PDF ist ein Fehler aufgetreten: {ex.Message}", MsgBoxStyle.Critical, "Fehler")
        End Try
    End Sub

    Private Sub B_Artikel_bearbeiten_Click(sender As Object, e As RoutedEventArgs)
        ' Navigieren Sie zur Editor-Seite und übergeben Sie die Artikel-ID
        Dim editorPage As New Artikel_erstellen(Artikel.ID, BreadcrumbItems)
        NavigationService.Navigate(editorPage)
    End Sub

    ' Event-Handler für die Auswahl einer alten Version
    Private Sub VersionsList_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)
        If isProgrammaticSelection Then
            ' Auswahl wurde programmgesteuert geändert, Event ignorieren
            Return
        End If

        Dim selectedVersion As ArtikelVersion = TryCast(VersionsList.SelectedItem, ArtikelVersion)
        If selectedVersion IsNot Nothing Then
            If selectedVersion.VersionId = 0 Then
                ' Aktuelle Version laden und anzeigen
                LadeUndZeigeAktuelleVersion()
            Else
                ' Alte Version laden und anzeigen
                LadeUndZeigeAlteVersion(selectedVersion.VersionId)
            End If
        End If
    End Sub

    Private Sub LadeUndZeigeAktuelleVersion()
        ' Artikel neu laden
        Artikel_laden(Artikel.ID)
        Artikel_anzeigen()
    End Sub

    Private Sub LadeUndZeigeAlteVersion(versionId As Integer)
        Dim dbFilePath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "KnowledgeBase.db")
        Using verbindung As New SQLiteConnection($"Data Source={dbFilePath};Version=3;")
            verbindung.Open()

            ' Alte Version abrufen
            Dim cmd As New SQLiteCommand("SELECT Titel, Artikel_Inhalt_HTML FROM T_Knowledge_Base_Artikelversionen WHERE VersionId = @VersionId", verbindung)
            cmd.Parameters.AddWithValue("@VersionId", versionId)
            Dim reader As SQLiteDataReader = cmd.ExecuteReader()
            If reader.Read() Then
                ' Temporär die alte Version anzeigen
                Dim versionTitel As String = reader("Titel").ToString()
                Dim versionContentHtml As String = reader("Artikel_Inhalt_HTML").ToString()
                ' Bilder wiederherstellen
                Dim fullHtmlContent As String = RestoreImagesInHtml(versionContentHtml, Artikel.ID)

                ' Einbetten in ein HTML-Gerüst
                Dim htmlContent As String = "<html>" & vbCrLf &
                "<head>" & vbCrLf &
                "    <style>" & vbCrLf &
                "        body { font-family: Calibri; color: white; background-color: #292e37; }" & vbCrLf &
                "        @media print {" & vbCrLf &
                "            body { color: black !important; background-color: white !important; }" & vbCrLf &
                "            h1, h2, h3, h4, h5, h6 { color: black !important; }" & vbCrLf &
                "            p { color: black !important; }" & vbCrLf &
                "            a { color: black !important; text-decoration: none !important; }" & vbCrLf &
                "        }" & vbCrLf &
                "    </style>" & vbCrLf &
                "</head>" & vbCrLf &
                "<body>" & fullHtmlContent & "</body>" & vbCrLf &
                "</html>"

                ' Anzeige aktualisieren
                Me.T_Artikeltitel.Text = versionTitel & " (Alte Version)"
                Browser.CoreWebView2.NavigateToString(htmlContent)
            End If
            reader.Close()
            verbindung.Close()
        End Using
    End Sub

    Private Function IstFavorit(artikelId As Integer) As Boolean
        Dim dbFilePath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "KnowledgeBase.db")
        Using verbindung As New SQLiteConnection($"Data Source={dbFilePath};Version=3;")
            verbindung.Open()
            Dim cmd As New SQLiteCommand("SELECT COUNT(1) FROM T_Favoriten WHERE ArtikelId = @ArtikelId AND UserId = @UserId", verbindung)
            cmd.Parameters.AddWithValue("@ArtikelId", artikelId)
            cmd.Parameters.AddWithValue("@UserId", User.UserID)
            Dim count As Integer = Convert.ToInt32(cmd.ExecuteScalar())
            Return count > 0
        End Using
    End Function

    Private Sub FügeFavoritHinzu(artikelId As Integer)
        Dim dbFilePath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "KnowledgeBase.db")
        Using verbindung As New SQLiteConnection($"Data Source={dbFilePath};Version=3;")
            verbindung.Open()
            Dim cmd As New SQLiteCommand("INSERT OR IGNORE INTO T_Favoriten (ArtikelId, UserId) VALUES (@ArtikelId, @UserId)", verbindung)
            cmd.Parameters.AddWithValue("@ArtikelId", artikelId)
            cmd.Parameters.AddWithValue("@UserId", User.UserID)
            cmd.ExecuteNonQuery()
        End Using
    End Sub

    Private Sub EntferneFavorit(artikelId As Integer)
        Dim dbFilePath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "KnowledgeBase.db")
        Using verbindung As New SQLiteConnection($"Data Source={dbFilePath};Version=3;")
            verbindung.Open()
            Dim cmd As New SQLiteCommand("DELETE FROM T_Favoriten WHERE ArtikelId = @ArtikelId AND UserId = @UserId", verbindung)
            cmd.Parameters.AddWithValue("@ArtikelId", artikelId)
            cmd.Parameters.AddWithValue("@UserId", User.UserID)
            cmd.ExecuteNonQuery()
        End Using
    End Sub

    Private Sub SetzeFavoritenButtonStatus(isFavorit As Boolean)
        If isFavorit Then
            B_Favoriten.Content = "Favorit"
            B_Favoriten.Icon = New Wpf.Ui.Controls.SymbolIcon() With {
                .Symbol = Wpf.Ui.Controls.SymbolRegular.Star24, ' Entsprechender Symbolwert aus der Enumeration
                .Filled = True ' Gefülltes Symbol
            }
        Else
            B_Favoriten.Content = "Favoritieren"
            B_Favoriten.Icon = New Wpf.Ui.Controls.SymbolIcon() With {
                .Symbol = Wpf.Ui.Controls.SymbolRegular.Star24, ' Entsprechender Symbolwert aus der Enumeration
                .Filled = False ' Leeres Symbol
            }
        End If
    End Sub

    Private Sub B_Favoriten_Click(sender As Object, e As RoutedEventArgs)
        ' Button deaktivieren, um Mehrfachklicks zu verhindern
        B_Favoriten.IsEnabled = False

        Try
            Dim artikelId As Integer = Artikel.ID ' Stellen Sie sicher, dass Artikel.ID korrekt gesetzt ist

            If IstFavorit(artikelId) Then
                EntferneFavorit(artikelId)
                SetzeFavoritenButtonStatus(False)
                MsgBox("Artikel aus den Favoriten entfernt.", MsgBoxStyle.Information, "Favorit")
            Else
                FügeFavoritHinzu(artikelId)
                SetzeFavoritenButtonStatus(True)
                MsgBox("Artikel zu den Favoriten hinzugefügt.", MsgBoxStyle.Information, "Favorit")
            End If
        Finally
            ' Button nach Abschluss wieder aktivieren
            B_Favoriten.IsEnabled = True
        End Try
    End Sub

    Private Sub Tag_Clicked(sender As Object, e As MouseButtonEventArgs)
        Dim stackPanel As StackPanel = TryCast(sender, StackPanel)
        If stackPanel IsNot Nothing AndAlso stackPanel.DataContext IsNot Nothing Then
            Dim tagName As String = stackPanel.DataContext.ToString()

            ' Navigiere zur Tagübersicht-Seite mit dem ausgewählten Tag
            Dim tagÜbersichtPage As New Tagübersicht(tagName)
            NavigationService.Navigate(tagÜbersichtPage)

        End If
    End Sub


End Class