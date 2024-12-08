Imports System.Collections.ObjectModel
Imports System.ComponentModel
Imports System.Data.SQLite
Imports KnwoledgeBase.Artikel_anzeigen
Imports Wpf.Ui.Controls
Imports System.IO
Imports System.Windows.Data

Class Explorer

    Public Property cards As ObservableCollection(Of Explorer_Cards)
    Public Property BreadcrumbItems As ObservableCollection(Of BreadcrumbItem)
    Public Property currentParentId As Integer = 0
    Private startPoint As Point

    ' Hinzugefügt: CollectionView zur Unterstützung von Filtern
    Private cardsView As ICollectionView

    Private Sub B_Card_PreviewMouseLeftButtonDown(sender As Object, e As MouseButtonEventArgs)
        startPoint = e.GetPosition(Nothing)
    End Sub

    Private Sub B_Card_PreviewMouseMove(sender As Object, e As MouseEventArgs)
        Dim mousePos As Point = e.GetPosition(Nothing)
        Dim diff As Vector = startPoint - mousePos

        If e.LeftButton = MouseButtonState.Pressed AndAlso (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance OrElse Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance) Then
            ' Das gezogene Element ermitteln
            Dim cardAction As CardAction = CType(sender, CardAction)
            Dim card As Explorer_Cards = TryCast(cardAction.DataContext, Explorer_Cards)

            If card IsNot Nothing Then
                ' Drag-and-Drop-Operation initialisieren
                Dim dataObject As New DataObject("Explorer_Cards", card)
                DragDrop.DoDragDrop(cardAction, dataObject, DragDropEffects.Move)
            End If
        End If
    End Sub

    Private Sub BreadcrumbItem_DragOver(sender As Object, e As DragEventArgs)
        If e.Data.GetDataPresent("Explorer_Cards") Then
            e.Effects = DragDropEffects.Move
        Else
            e.Effects = DragDropEffects.None
        End If
        e.Handled = True
    End Sub

    Private Sub BreadcrumbItem_Drop(sender As Object, e As DragEventArgs)
        If e.Data.GetDataPresent("Explorer_Cards") Then
            Dim card As Explorer_Cards = CType(e.Data.GetData("Explorer_Cards"), Explorer_Cards)
            ' Cast sender to Wpf.Ui.Controls.TextBlock
            Dim textBlock As Wpf.Ui.Controls.TextBlock = CType(sender, Wpf.Ui.Controls.TextBlock)
            Dim breadcrumbItem As BreadcrumbItem = CType(textBlock.DataContext, BreadcrumbItem)

            ' Update des Parent-ID in der Datenbank
            Try
                Dim dbFilePath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "KnowledgeBase.db")
                Using connection As New SQLiteConnection($"Data Source={dbFilePath};Version=3;")
                    connection.Open()
                    Dim sql As New SQLiteCommand("UPDATE T_Knowledge_Base_Filesystem SET parent_id = @newParentId WHERE id = @id", connection)
                    sql.Parameters.AddWithValue("@newParentId", breadcrumbItem.Id)
                    sql.Parameters.AddWithValue("@id", card.ID)
                    sql.ExecuteNonQuery()
                End Using

                ' Entferne das Element aus der aktuellen Sammlung
                cards.Remove(card)

            Catch ex As Exception
                Dim fehlermeldung As New Snackbar(SnackbarPresenter) With {
                    .Title = "Fehler beim Verschieben des Elements",
                    .Appearance = ControlAppearance.Danger,
                    .Content = "Fehler beim Verschieben des Elements: " & ex.Message,
                    .Timeout = TimeSpan.FromSeconds(2)
                }
                fehlermeldung.Show()
            End Try
        End If
    End Sub

    ' Parameterloser Konstruktor, erforderlich für WPF
    Public Sub New()
        InitializeComponent()
        BreadcrumbItems = New ObservableCollection(Of BreadcrumbItem)
        Me.DataContext = Me
        Explorerinhalt_laden(0)
    End Sub

    ' Konstruktor mit parentId-Parameter
    Public Sub New(parentId As Integer)
        InitializeComponent()
        BreadcrumbItems = New ObservableCollection(Of BreadcrumbItem)
        Me.DataContext = Me
        Explorerinhalt_laden(parentId)
    End Sub

    Private Sub B_Card_Click(sender As Object, e As RoutedEventArgs)
        Dim cardAction As CardAction = CType(sender, CardAction)
        Dim card As Explorer_Cards = Nothing

        ' Suchen des DataContext im visuellen Baum
        Dim currentElement As DependencyObject = cardAction
        While currentElement IsNot Nothing AndAlso card Is Nothing
            card = TryCast((TryCast(currentElement, FrameworkElement))?.DataContext, Explorer_Cards)
            currentElement = VisualTreeHelper.GetParent(currentElement)
        End While

        If card IsNot Nothing Then
            If card.IstVerzeichnis Then
                ' Wenn es ein Ordner ist, Inhalte des Ordners laden
                Explorerinhalt_laden(card.ID)
            Else
                ' Wenn es eine Datei ist, Artikel öffnen
                If card.Artikel_ID.HasValue Then
                    Artikel.ID = card.Artikel_ID.Value
                    ' Artikel laden
                    NavigationService.Navigate(New Artikel_anzeigen(Me.BreadcrumbItems))
                Else
                    Dim fehlermeldung As New Snackbar(SnackbarPresenter) With {
                        .Title = "Artikel_ID ist nicht verfügbar",
                        .Appearance = ControlAppearance.Danger,
                        .Content = "Artikel_ID ist nicht verfügbar",
                        .Timeout = TimeSpan.FromSeconds(2)
                    }
                    fehlermeldung.Show()
                End If
            End If
        Else
            Dim fehlermeldung As New Snackbar(SnackbarPresenter) With {
                .Title = "Fehler beim Laden des Elements",
                .Appearance = ControlAppearance.Danger,
                .Content = "Fehler beim Laden des Elements",
                .Timeout = TimeSpan.FromSeconds(2)
            }
            fehlermeldung.Show()
        End If
    End Sub

    Private Sub Explorerinhalt_laden(Optional parentId As Integer = 0)
        currentParentId = parentId
        Dim dbFilePath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "KnowledgeBase.db")
        Dim verbindung As New SQLiteConnection($"Data Source={dbFilePath};Version=3;")
        Dim SQL As New SQLiteCommand("SELECT id, Name, ist_verzeichnis, Artikel_ID FROM T_Knowledge_Base_Filesystem WHERE parent_id = @parentId", verbindung)
        SQL.Parameters.AddWithValue("@parentId", parentId)
        verbindung.Open()
        Dim reader As SQLiteDataReader = SQL.ExecuteReader()
        cards = New ObservableCollection(Of Explorer_Cards)
        While reader.Read()
            Dim istVerzeichnis As Boolean = Convert.ToBoolean(reader("ist_verzeichnis"))
            Dim symbolPath As String
            If istVerzeichnis Then
                symbolPath = "/statics/Folder.png"
            Else
                symbolPath = "/statics/File.png"
            End If

            Dim artikelId As Integer? = Nothing
            If Not istVerzeichnis AndAlso Not IsDBNull(reader("Artikel_ID")) Then
                artikelId = Convert.ToInt32(reader("Artikel_ID"))
            End If

            cards.Add(New Explorer_Cards With {
                .Titel = reader("Name").ToString(),
                .ID = Convert.ToInt32(reader("id")),
                .IstVerzeichnis = istVerzeichnis,
                .Symbol = New BitmapImage(New Uri(symbolPath, UriKind.Relative)),
                .Artikel_ID = artikelId
            })
        End While
        reader.Close()
        verbindung.Close()

        ' Setze die ItemsSource auf die CollectionView
        cardsView = CollectionViewSource.GetDefaultView(cards)
        cardsView.Filter = AddressOf ApplyFilters
        Me.Dynamic_cards.ItemsSource = cardsView

        UpdateBreadcrumbs()
        LoadTags() ' Tags für den Tag-Filter laden
    End Sub

    Private Sub UpdateBreadcrumbs()
        UpdateBreadcrumbs(BreadcrumbItems)
    End Sub

    Private Sub UpdateBreadcrumbs(breadcrumbItems As ObservableCollection(Of BreadcrumbItem))
        ' Clear the current BreadcrumbItems
        breadcrumbItems.Clear()

        ' List to store the path
        Dim pathItems As New List(Of BreadcrumbItem)

        ' Temporary variable for the loop
        Dim tempParentId As Integer = currentParentId

        ' Set to track already visited IDs to avoid cycles
        Dim visitedIds As New HashSet(Of Integer)

        ' Establish connection to the database
        Dim dbFilePath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "KnowledgeBase.db")
        Dim verbindung As New SQLiteConnection($"Data Source={dbFilePath};Version=3;")
        verbindung.Open()

        ' Loop to get the full path
        While tempParentId <> 0
            ' Check if the ID has already been visited (cycle detection)
            If visitedIds.Contains(tempParentId) Then
                ' Cycle detected, break the loop to avoid infinite recursion
                Exit While
            End If
            visitedIds.Add(tempParentId)

            ' Adjust the SQL query to select ist_verzeichnis and Artikel_ID
            Dim SQL As New SQLiteCommand("SELECT id, Name, parent_id, ist_verzeichnis, Artikel_ID FROM T_Knowledge_Base_Filesystem WHERE id = @id", verbindung)
            SQL.Parameters.AddWithValue("@id", tempParentId)
            Dim reader As SQLiteDataReader = SQL.ExecuteReader()

            If reader.Read() Then
                Dim istVerzeichnis As Boolean = Convert.ToBoolean(reader("ist_verzeichnis"))
                Dim artikelId As Integer? = Nothing
                If Not istVerzeichnis AndAlso Not IsDBNull(reader("Artikel_ID")) Then
                    artikelId = Convert.ToInt32(reader("Artikel_ID"))
                End If

                Dim breadcrumbItem As New BreadcrumbItem With {
                    .DisplayName = reader("Name").ToString(),
                    .Id = Convert.ToInt32(reader("id")),
                    .ParentId = If(IsDBNull(reader("parent_id")), -1, Convert.ToInt32(reader("parent_id"))),
                    .IstVerzeichnis = istVerzeichnis,
                    .Artikel_ID = artikelId
                }
                pathItems.Insert(0, breadcrumbItem) ' Insert at the beginning of the list
                tempParentId = breadcrumbItem.ParentId
            Else
                ' Error handling if the folder is not found
                Exit While
            End If

            reader.Close()
        End While

        verbindung.Close()

        ' Add root element
        pathItems.Insert(0, New BreadcrumbItem With {
            .DisplayName = "Root",
            .Id = 0,
            .ParentId = -1,
            .IstVerzeichnis = True,
            .Artikel_ID = Nothing
        })

        ' Update the BreadcrumbItems
        For Each breadcrumbItem In pathItems
            breadcrumbItems.Add(breadcrumbItem)
        Next
    End Sub

    Private Sub BreadcrumbItem_Click(sender As Object, e As RoutedEventArgs)
        Dim hyperlink As Hyperlink = CType(sender, Hyperlink)
        Dim breadcrumbItem As BreadcrumbItem = CType(hyperlink.DataContext, BreadcrumbItem)
        Explorerinhalt_laden(breadcrumbItem.Id)
    End Sub

    Private Sub RenameMenuItem_Click(sender As Object, e As RoutedEventArgs)
        Dim menuItem As System.Windows.Controls.MenuItem = CType(sender, System.Windows.Controls.MenuItem)
        Dim card As Explorer_Cards = CType(menuItem.DataContext, Explorer_Cards)

        ' Eingabe eines neuen Namens
        Dim newName As String = InputBox("Geben Sie einen neuen Namen ein:", "Umbenennen", card.Titel)

        If Not String.IsNullOrEmpty(newName) AndAlso newName <> card.Titel Then
            ' Aktualisiere die Datenbank
            Try
                Dim dbFilePath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "KnowledgeBase.db")
                Using connection As New SQLiteConnection($"Data Source={dbFilePath};Version=3;")
                    connection.Open()
                    Dim sql As New SQLiteCommand("UPDATE T_Knowledge_Base_Filesystem SET Name = @Name WHERE id = @id", connection)
                    sql.Parameters.AddWithValue("@Name", newName)
                    sql.Parameters.AddWithValue("@id", card.ID)
                    sql.ExecuteNonQuery()
                End Using
                ' Aktualisiere die UI
                card.Titel = newName
                cardsView.Refresh()
            Catch ex As Exception
                Dim fehlermeldung As New Snackbar(SnackbarPresenter) With {
                    .Title = "Fehler beim Umbenennen des Elements",
                    .Appearance = ControlAppearance.Danger,
                    .Content = "Fehler beim Umbenennen des Elements",
                    .Timeout = TimeSpan.FromSeconds(2)
                }
                fehlermeldung.Show()
            End Try
        End If
    End Sub

    ' Hilfsfunktion, um zu überprüfen, ob targetId ein Unterelement von sourceId ist
    Private Function IsDescendant(sourceId As Integer, targetId As Integer) As Boolean
        ' Verbindungsaufbau zur Datenbank
        Dim dbFilePath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "KnowledgeBase.db")
        Using connection As New SQLiteConnection($"Data Source={dbFilePath};Version=3;")
            connection.Open()
            Dim sql As New SQLiteCommand("
WITH RecursiveCTE AS (
    SELECT id, parent_id FROM T_Knowledge_Base_Filesystem WHERE id = @sourceId
    UNION ALL
    SELECT t.id, t.parent_id FROM T_Knowledge_Base_Filesystem t
    INNER JOIN RecursiveCTE r ON t.parent_id = r.id
)
SELECT COUNT(*) FROM RecursiveCTE WHERE id = @targetId
", connection)
            sql.Parameters.AddWithValue("@sourceId", sourceId)
            sql.Parameters.AddWithValue("@targetId", targetId)
            Dim count As Integer = Convert.ToInt32(sql.ExecuteScalar())
            Return count > 0
        End Using
    End Function

    Private Sub DeleteMenuItem_Click(sender As Object, e As RoutedEventArgs)
        Dim menuItem As System.Windows.Controls.MenuItem = CType(sender, System.Windows.Controls.MenuItem)
        Dim card As Explorer_Cards = CType(menuItem.DataContext, Explorer_Cards)

        ' Bestätige das Löschen
        Dim result As System.Windows.MessageBoxResult = System.Windows.MessageBox.Show($"Möchten Sie '{card.Titel}' wirklich löschen?", "Löschen", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning)
        If result = System.Windows.MessageBoxResult.Yes Then
            Try
                Dim dbFilePath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "KnowledgeBase.db")
                Using connection As New SQLiteConnection($"Data Source={dbFilePath};Version=3;")
                    connection.Open()

                    ' Lösche das Element und seine Unterelemente
                    Dim sql As New SQLiteCommand("
WITH ItemsToDelete AS (
    SELECT id FROM T_Knowledge_Base_Filesystem WHERE id = @id
    UNION ALL
    SELECT child.id FROM T_Knowledge_Base_Filesystem child
    INNER JOIN ItemsToDelete parent ON child.parent_id = parent.id
)
DELETE FROM T_Knowledge_Base_Filesystem WHERE id IN (SELECT id FROM ItemsToDelete)
", connection)
                    sql.Parameters.AddWithValue("@id", card.ID)
                    sql.ExecuteNonQuery()
                End Using
                ' Entferne das Element aus der ObservableCollection
                cards.Remove(card)
                cardsView.Refresh()
            Catch ex As Exception
                Dim fehlermeldung As New Snackbar(SnackbarPresenter) With {
                    .Title = "Fehler beim Löschen des Elements",
                    .Appearance = ControlAppearance.Danger,
                    .Content = "Fehler beim Löschen des Elements",
                    .Timeout = TimeSpan.FromSeconds(2)
                }
                fehlermeldung.Show()
            End Try
        End If
    End Sub

    Private Sub Card_DragOver(sender As Object, e As DragEventArgs)
        Dim cardAction As CardAction = CType(sender, CardAction)
        Dim targetCard As Explorer_Cards = TryCast(cardAction.DataContext, Explorer_Cards)

        If e.Data.GetDataPresent("Explorer_Cards") AndAlso targetCard IsNot Nothing AndAlso targetCard.IstVerzeichnis Then
            e.Effects = DragDropEffects.Move
        Else
            e.Effects = DragDropEffects.None
        End If
        e.Handled = True
    End Sub

    Private Sub Card_Drop(sender As Object, e As DragEventArgs)
        If e.Data.GetDataPresent("Explorer_Cards") Then
            Dim sourceCard As Explorer_Cards = CType(e.Data.GetData("Explorer_Cards"), Explorer_Cards)
            Dim cardAction As CardAction = CType(sender, CardAction)
            Dim targetCard As Explorer_Cards = TryCast(cardAction.DataContext, Explorer_Cards)

            If sourceCard Is Nothing OrElse targetCard Is Nothing OrElse Not targetCard.IstVerzeichnis Then
                Return
            End If

            ' Überprüfen, ob das Verschieben einen Zyklus erzeugt
            If IsDescendant(sourceCard.ID, targetCard.ID) Then
                Dim fehlermeldung As New Snackbar(SnackbarPresenter) With {
                    .Title = "Verschieben nicht möglich",
                    .Appearance = ControlAppearance.Danger,
                    .Content = "Das Verschieben würde einen Zyklus erzeugen.",
                    .Timeout = TimeSpan.FromSeconds(2)
                }
                fehlermeldung.Show()
                Return
            End If

            ' Aktualisiere die parent_id in der Datenbank
            Try
                Dim dbFilePath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "KnowledgeBase.db")
                Using connection As New SQLiteConnection($"Data Source={dbFilePath};Version=3;")
                    connection.Open()
                    Dim sql As New SQLiteCommand("UPDATE T_Knowledge_Base_Filesystem SET parent_id = @newParentId WHERE id = @id", connection)
                    sql.Parameters.AddWithValue("@newParentId", targetCard.ID)
                    sql.Parameters.AddWithValue("@id", sourceCard.ID)
                    sql.ExecuteNonQuery()
                End Using

                ' Entferne das Element aus der aktuellen Sammlung
                cards.Remove(sourceCard)
                cardsView.Refresh()

                ' Optional: Aktuellen Ordnerinhalt neu laden
                ' Explorerinhalt_laden(currentParentId)

            Catch ex As Exception
                Dim fehler_beim_verschieben As New Snackbar(SnackbarPresenter) With {
                    .Title = "Fehler beim Verschieben des Elements",
                    .Appearance = ControlAppearance.Danger,
                    .Content = "Fehler beim Verschieben des Elements",
                    .Timeout = TimeSpan.FromSeconds(2)
                }
                fehler_beim_verschieben.Show()
            End Try
        End If
    End Sub

    Private Sub Grid_MouseRightButtonUp(sender As Object, e As MouseButtonEventArgs)
        ' Prüfen, ob der Rechtsklick auf eine leere Fläche erfolgt ist
        Dim clickedElement As DependencyObject = e.OriginalSource

        While clickedElement IsNot Nothing
            If TypeOf clickedElement Is CardAction Then
                ' Klick erfolgte auf eine Card, Kontextmenü nicht anzeigen
                Return
            ElseIf TypeOf clickedElement Is Grid Then
                ' Klick erfolgte auf das Grid selbst, Kontextmenü anzeigen
                Exit While
            End If
            clickedElement = VisualTreeHelper.GetParent(clickedElement)
        End While

        ' Kontextmenü erstellen
        Dim contextMenu As New ContextMenu()
        Dim menuItem As New MenuItem()
        menuItem.Header = "Neuen Ordner erstellen"
        AddHandler menuItem.Click, AddressOf CreateFolderMenuItem_Click
        contextMenu.Items.Add(menuItem)

        ' Kontextmenü anzeigen
        contextMenu.PlacementTarget = CType(sender, UIElement)
        contextMenu.IsOpen = True
    End Sub

    Private Sub CreateFolderMenuItem_Click(sender As Object, e As RoutedEventArgs)
        ' Eingabe des Ordnernamens
        Dim folderName As String = InputBox("Geben Sie den Namen des neuen Ordners ein:", "Neuen Ordner erstellen")
        If Not String.IsNullOrEmpty(folderName) Then
            ' Ordner in der Datenbank erstellen
            Try
                Dim dbFilePath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "KnowledgeBase.db")
                Using connection As New SQLiteConnection($"Data Source={dbFilePath};Version=3;")
                    connection.Open()
                    ' Neuer Ordner einfügen und die zuletzt eingefügte ID abrufen
                    Dim sql As New SQLiteCommand("
INSERT INTO T_Knowledge_Base_Filesystem (Name, ist_verzeichnis, parent_id) 
VALUES (@Name, 1, @ParentId);
SELECT last_insert_rowid();", connection)

                    sql.Parameters.AddWithValue("@Name", folderName)
                    sql.Parameters.AddWithValue("@ParentId", currentParentId)

                    Dim newId As Integer = Convert.ToInt32(sql.ExecuteScalar())

                    ' Ordner zur ObservableCollection hinzufügen
                    cards.Add(New Explorer_Cards With {
                        .Titel = folderName,
                        .ID = newId,
                        .IstVerzeichnis = True,
                        .Symbol = New BitmapImage(New Uri("/statics/Folder.png", UriKind.Relative)),
                        .Artikel_ID = Nothing
                    })
                    cardsView.Refresh()
                End Using
            Catch ex As Exception
                Dim Ordner_erstellen_Fehler As New Snackbar(SnackbarPresenter) With {
                    .Title = "Fehler beim Erstellen des neuen Ordners",
                    .Appearance = ControlAppearance.Danger,
                    .Content = "Fehler beim Erstellen des neuen Ordners",
                    .Timeout = TimeSpan.FromSeconds(2)
                }
                Ordner_erstellen_Fehler.Show()
            End Try
        End If
    End Sub

    Private Sub Grid_PreviewMouseRightButtonUp(sender As Object, e As MouseButtonEventArgs)
        ' Prüfen, ob der Rechtsklick auf eine leere Fläche erfolgt ist
        Dim clickedElement As DependencyObject = e.OriginalSource

        While clickedElement IsNot Nothing
            If TypeOf clickedElement Is Wpf.Ui.Controls.CardAction Then
                ' Klick erfolgte auf eine Card, Kontextmenü nicht anzeigen
                ' Ereignis nicht als behandelt markieren, damit die Card ihr Kontextmenü anzeigen kann
                Return
            ElseIf TypeOf clickedElement Is Grid Then
                ' Klick erfolgte auf das Grid selbst, Kontextmenü anzeigen
                Exit While
            End If
            clickedElement = VisualTreeHelper.GetParent(clickedElement)
        End While

        ' Kontextmenü erstellen
        Dim contextMenu As New ContextMenu()
        Dim menuItem As New MenuItem()
        menuItem.Header = "Neuen Ordner erstellen"
        menuItem.Foreground = New SolidColorBrush(Colors.White)
        AddHandler menuItem.Click, AddressOf CreateFolderMenuItem_Click
        contextMenu.Items.Add(menuItem)

        ' Kontextmenü anzeigen
        contextMenu.PlacementTarget = CType(sender, UIElement)
        contextMenu.IsOpen = True

        ' Ereignis als behandelt markieren, um zu verhindern, dass andere Elemente es erhalten
        e.Handled = True
    End Sub

    Private Sub T_Suchfeld_QuerySubmitted(sender As Object, e As AutoSuggestBoxQuerySubmittedEventArgs)
        Dim query As String = e.QueryText.Trim()
        If String.IsNullOrEmpty(query) Then
            ' Wenn das Suchfeld leer ist, lade den aktuellen Ordnerinhalt
            Explorerinhalt_laden(currentParentId)
        Else
            ' Suche durchführen
            ' Die Suchfunktion wird nun durch die Filterlogik abgedeckt
            cardsView.Refresh()
        End If
    End Sub


    ' Methode zur Anwendung der Filter
    Private Function ApplyFilters(obj As Object) As Boolean
        Dim card As Explorer_Cards = TryCast(obj, Explorer_Cards)
        If card Is Nothing Then Return False

        ' Typ-Filter anwenden
        Dim selectedType = CType(TypeFilterComboBox.SelectedItem, ComboBoxItem)
        If selectedType IsNot Nothing AndAlso selectedType.Content.ToString() <> "Alle" Then
            If selectedType.Content.ToString() = "Ordner" AndAlso Not card.IstVerzeichnis Then
                Return False
            ElseIf selectedType.Content.ToString() = "Datei" AndAlso card.IstVerzeichnis Then
                Return False
            End If
        End If

        ' Tag-Filter anwenden
        Dim selectedTag = CType(TagFilterComboBox.SelectedItem, ComboBoxItem)
        If selectedTag IsNot Nothing AndAlso selectedTag.Content.ToString() <> "Alle" Then
            If card.Tags Is Nothing OrElse Not card.Tags.Split(New String() {", "}, StringSplitOptions.None).Contains(selectedTag.Content.ToString()) Then
                Return False
            End If
        End If

        ' Suchfilter anwenden
        Dim query = T_Suchfeld.Text.Trim().ToLower()
        If Not String.IsNullOrEmpty(query) Then
            Dim titelMatch = card.Titel.ToLower().Contains(query)
            Dim authorMatch = If(card.Author, "").ToLower().Contains(query)
            Dim tagsMatch = If(card.Tags, "").ToLower().Contains(query)
            Return titelMatch OrElse authorMatch OrElse tagsMatch
        End If

        Return True
    End Function

    ' Ereignis-Handler für Änderungen der Filter
    Private Sub Filter_Changed(sender As Object, e As SelectionChangedEventArgs)
        If cardsView IsNot Nothing Then
            cardsView.Refresh()
        End If
    End Sub

    ' Laden der verfügbaren Tags für den Tag-Filter
    Private Sub LoadTags()
        Dim dbFilePath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "KnowledgeBase.db")
        Using verbindung As New SQLiteConnection($"Data Source={dbFilePath};Version=3;")
            verbindung.Open()

            Dim SQL As New SQLiteCommand("
                SELECT DISTINCT t.TagName
                FROM T_Knowledge_Base_Tags t
                INNER JOIN T_Knowledge_Base_ArtikelTags at ON t.TagId = at.TagId
                ", verbindung)
            Dim reader As SQLiteDataReader = SQL.ExecuteReader()

            Dim tags As New List(Of String)()
            While reader.Read()
                tags.Add(reader("TagName").ToString())
            End While
            reader.Close()
            verbindung.Close()

            ' Füge die Tags zur ComboBox hinzu
            TagFilterComboBox.Items.Clear()
            TagFilterComboBox.Items.Add(New ComboBoxItem With {.Content = "Alle", .IsSelected = True})
            For Each tagName As String In tags
                TagFilterComboBox.Items.Add(New ComboBoxItem With {.Content = tagName})
            Next
        End Using
    End Sub

    ' Aktualisieren der PerformSearch-Methode, um die CollectionView zu nutzen
    Private Sub PerformSearch(query As String)
        ' Da wir bereits die Suchfunktion in ApplyFilters integriert haben,
        ' müssen wir hier nur die CollectionView aktualisieren
        If cardsView IsNot Nothing Then
            cardsView.Refresh()
        End If
    End Sub

End Class