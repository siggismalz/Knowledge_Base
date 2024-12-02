﻿Imports System.Collections.ObjectModel
Imports System.ComponentModel
Imports System.Data.SQLite
Imports System.Windows.Input
Imports System.Windows.Media.Animation
Imports Wpf.Ui.Controls
Imports System.IO

Public Class Tagübersicht
    Inherits Page

    ' ObservableCollection zur Bindung der Tags an das ItemsControl
    Private TagsList As New ObservableCollection(Of Tag)()
    Private AllTags As New List(Of Tag)() ' Für die AutoSuggestBox
    Private TagsViewInternal As ICollectionView

    Private dbFilePath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "KnowledgeBase.db")
    Private connectionString As String = $"Data Source={dbFilePath};Version=3;"

    ' Variable zur Speicherung des ausgewählten Tags beim Umbenennen
    Private selectedTagForRename As Tag = Nothing

    Public Sub New()
        InitializeComponent()
        DataContext = Me ' Setzt den DataContext auf diese Klasse

        ' Initialisieren der CollectionView für die Filterung
        TagsViewInternal = CollectionViewSource.GetDefaultView(TagsList)

        LoadTags()

        ' Setzt die ItemsSource der AutoSuggestBox
        TagSearchBox.ItemsSource = AllTags.Select(Function(t) t.TagName).ToList()
    End Sub

    ' Öffentliche Eigenschaft zur Bindung an das ItemsControl
    Public ReadOnly Property TagsView As ICollectionView
        Get
            Return TagsViewInternal
        End Get
    End Property

    ' Methode zum Laden der Tags aus der Datenbank
    Private Sub LoadTags()
        TagsList.Clear()
        AllTags.Clear()
        Dim query As String = "SELECT TagId, TagName FROM T_Knowledge_Base_Tags ORDER BY TagName"

        Using verbindung As New SQLiteConnection(connectionString)
            Using cmd As New SQLiteCommand(query, verbindung)
                Try
                    verbindung.Open()
                    Using reader As SQLiteDataReader = cmd.ExecuteReader()

                        While reader.Read()
                            Dim tag As New Tag With {
                                    .TagId = reader.GetInt32(0),
                                    .TagName = reader.GetString(1)
                                }
                            TagsList.Add(tag)
                            AllTags.Add(tag)
                        End While
                    End Using
                    ' Aktualisiert die AutoSuggestBox
                    TagSearchBox.ItemsSource = AllTags.Select(Function(t) t.TagName).ToList()
                    TagsViewInternal.Refresh()
                Catch ex As Exception
                    ' Verwendung von Snackbar statt MessageBox
                    Dim fehlermeldung As New Snackbar(AppSnackbar) With {
                            .Title = "Fehler",
                            .Appearance = ControlAppearance.Danger,
                            .Content = $"Fehler beim Laden der Tags: {ex.Message}",
                            .Timeout = TimeSpan.FromSeconds(3)
                        }
                    fehlermeldung.Show()
                End Try
            End Using
        End Using
    End Sub

    ' Methode zum Anzeigen des Umbenennungsdialogs mit Animation
    Private Sub ShowRenameDialog()
        DialogOverlay.Visibility = Visibility.Visible
        NewTagNameTextBox.Focus()
        NewTagNameTextBox.SelectAll()

        ' Animation für das Einblenden
        Dim fadeIn As New DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.3))
        DialogOverlay.BeginAnimation(UIElement.OpacityProperty, fadeIn)
    End Sub

    ' Methode zum Verstecken des Umbenennungsdialogs mit Animation
    Private Sub HideRenameDialog()
        ' Animation für das Ausblenden
        Dim fadeOut As New DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.3))
        AddHandler fadeOut.Completed, Sub(sender As Object, e As EventArgs)
                                          DialogOverlay.Visibility = Visibility.Collapsed
                                          selectedTagForRename = Nothing
                                      End Sub
        DialogOverlay.BeginAnimation(UIElement.OpacityProperty, fadeOut)
    End Sub

    ' Event-Handler für die Abbrechen-Schaltfläche im Dialog
    Private Sub CancelRename_Click(sender As Object, e As RoutedEventArgs)
        HideRenameDialog()
    End Sub

    ' Event-Handler für die Speichern-Schaltfläche im Dialog
    Private Sub SaveRename_Click(sender As Object, e As RoutedEventArgs)
        If selectedTagForRename IsNot Nothing Then
            Dim newTagName As String = NewTagNameTextBox.Text.Trim()
            If String.IsNullOrEmpty(newTagName) Then
                Dim warningSnackbar As New Snackbar(AppSnackbar) With {
                        .Title = "Warnung",
                        .Appearance = ControlAppearance.Danger,
                        .Content = "Der Tag-Name darf nicht leer sein.",
                        .Timeout = TimeSpan.FromSeconds(2)
                    }
                warningSnackbar.Show()
                Return
            End If

            ' Überprüfen Sie auf Duplikate
            If AllTags.Any(Function(t) t.TagName.Equals(newTagName, StringComparison.OrdinalIgnoreCase) AndAlso t.TagId <> selectedTagForRename.TagId) Then
                Dim warningSnackbar As New Snackbar(AppSnackbar) With {
                        .Title = "Warnung",
                        .Appearance = ControlAppearance.Danger,
                        .Content = "Ein Tag mit diesem Namen existiert bereits.",
                        .Timeout = TimeSpan.FromSeconds(2)
                    }
                warningSnackbar.Show()
                Return
            End If

            UpdateTagName(selectedTagForRename.TagId, newTagName)
        End If
    End Sub

    ' Methode zum Aktualisieren des Tag-Namens in der Datenbank
    Private Sub UpdateTagName(tagId As Integer, newTagName As String)
        Dim query As String = "UPDATE T_Knowledge_Base_Tags SET TagName = @TagName WHERE TagId = @TagId"

        Using verbindung As New SQLiteConnection(connectionString)
            Using cmd As New SQLiteCommand(query, verbindung)
                cmd.Parameters.AddWithValue("@TagName", newTagName)
                cmd.Parameters.AddWithValue("@TagId", tagId)

                Try
                    verbindung.Open()
                    Dim rowsAffected As Integer = cmd.ExecuteNonQuery()
                    If rowsAffected > 0 Then
                        Dim successSnackbar As New Snackbar(AppSnackbar) With {
                                .Title = "Erfolg",
                                .Appearance = ControlAppearance.Success,
                                .Content = "Tag erfolgreich umbenannt.",
                                .Timeout = TimeSpan.FromSeconds(2)
                            }
                        successSnackbar.Show()
                        LoadTags()
                        HideRenameDialog()
                    Else
                        Dim errorSnackbar As New Snackbar(AppSnackbar) With {
                                .Title = "Fehler",
                                .Appearance = ControlAppearance.Danger,
                                .Content = "Tag konnte nicht gefunden oder aktualisiert werden.",
                                .Timeout = TimeSpan.FromSeconds(2)
                            }
                        errorSnackbar.Show()
                    End If
                Catch ex As Exception
                    Dim errorSnackbar As New Snackbar(AppSnackbar) With {
                            .Title = "Fehler",
                            .Appearance = ControlAppearance.Danger,
                            .Content = $"Fehler beim Umbenennen des Tags: {ex.Message}",
                            .Timeout = TimeSpan.FromSeconds(3)
                        }
                    errorSnackbar.Show()
                End Try
            End Using
        End Using
    End Sub

    ' Methode zum Löschen eines Tags aus der Datenbank
    Private Sub DeleteTag(tagId As Integer)
        ' SQL-Abfragen zum Löschen der Verknüpfungen und des Tags
        Dim deleteAssociationsQuery As String = "DELETE FROM T_Knowledge_Base_ArtikelTags WHERE TagId = @TagId"
        Dim deleteTagQuery As String = "DELETE FROM T_Knowledge_Base_Tags WHERE TagId = @TagId"

        Using verbindung As New SQLiteConnection(connectionString)
            verbindung.Open()

            ' Stellen Sie sicher, dass Fremdschlüssel unterstützt werden
            Using pragmaCmd As New SQLiteCommand("PRAGMA foreign_keys = ON;", verbindung)
                pragmaCmd.ExecuteNonQuery()
            End Using

            ' Starten einer Transaktion
            Dim transaction As SQLiteTransaction = verbindung.BeginTransaction()
            Try
                ' Erstellen des SQLiteCommand mit der Transaktion
                Using cmd As New SQLiteCommand()
                    cmd.Connection = verbindung
                    cmd.Transaction = transaction

                    ' Löschen der Verknüpfungen in T_Knowledge_Base_ArtikelTags
                    cmd.CommandText = deleteAssociationsQuery
                    cmd.Parameters.AddWithValue("@TagId", tagId)
                    cmd.ExecuteNonQuery()

                    ' Löschen des Tags in T_Knowledge_Base_Tags
                    cmd.CommandText = deleteTagQuery
                    ' Parameter muss nicht erneut hinzugefügt werden
                    Dim rowsAffected As Integer = cmd.ExecuteNonQuery()

                    If rowsAffected > 0 Then
                        ' Transaktion festschreiben
                        transaction.Commit()
                        Dim successSnackbar As New Snackbar(AppSnackbar) With {
                                .Title = "Erfolg",
                                .Appearance = ControlAppearance.Success,
                                .Content = "Tag erfolgreich gelöscht.",
                                .Timeout = TimeSpan.FromSeconds(2)
                            }
                        successSnackbar.Show()
                        LoadTags()
                    Else
                        ' Falls kein Tag gelöscht wurde, Transaktion zurückrollen
                        transaction.Rollback()
                        Dim errorSnackbar As New Snackbar(AppSnackbar) With {
                                .Title = "Fehler",
                                .Appearance = ControlAppearance.Danger,
                                .Content = "Tag konnte nicht gefunden oder gelöscht werden.",
                                .Timeout = TimeSpan.FromSeconds(2)
                            }
                        errorSnackbar.Show()
                    End If
                End Using
            Catch ex As Exception
                ' Bei Fehlern Transaktion zurückrollen
                transaction.Rollback()
                Dim errorSnackbar As New Snackbar(AppSnackbar) With {
                        .Title = "Fehler",
                        .Appearance = ControlAppearance.Danger,
                        .Content = $"Fehler beim Löschen des Tags: {ex.Message}",
                        .Timeout = TimeSpan.FromSeconds(3)
                    }
                errorSnackbar.Show()
            End Try
        End Using
    End Sub

    ' Event-Handler für die AutoSuggestBox Textänderung
    Private Sub TagSearchBox_TextChanged(sender As Object, e As AutoSuggestBoxTextChangedEventArgs)
        If e.Reason = AutoSuggestionBoxTextChangeReason.UserInput Then
            Dim query As String = TagSearchBox.Text.ToLower()
            Dim suggestions = AllTags.Where(Function(t) t.TagName.ToLower().Contains(query)).Select(Function(t) t.TagName).ToList()
            TagSearchBox.ItemsSource = suggestions
        End If
    End Sub

    ' Event-Handler für die AutoSuggestBox QuerySubmitted
    Private Sub TagSearchBox_QuerySubmitted(sender As Object, e As AutoSuggestBoxQuerySubmittedEventArgs)
        If Not String.IsNullOrWhiteSpace(e.QueryText) Then
            Dim queryText = e.QueryText.ToLower()
            TagsViewInternal.Filter = Function(item As Object)
                                          Dim tag = TryCast(item, Tag)
                                          Return tag IsNot Nothing AndAlso tag.TagName.ToLower().Contains(queryText)
                                      End Function
        Else
            ' Entfernt den Filter
            TagsViewInternal.Filter = Nothing
        End If
    End Sub

    ' Event-Handler für das Klicken auf eine Karte
    ' Event-Handler für das Klicken auf eine Karte
    Private Sub Card_Click(sender As Object, e As RoutedEventArgs)
        Dim element As FrameworkElement = TryCast(sender, FrameworkElement)
        If element IsNot Nothing AndAlso element.DataContext IsNot Nothing Then
            Dim tag As Tag = TryCast(element.DataContext, Tag)
            If tag IsNot Nothing Then
                ' Beispielaktion: Anzeigen einer Nachricht
                Dim infoSnackbar As New Snackbar(AppSnackbar) With {
                .Title = "Tag ausgewählt",
                .Appearance = ControlAppearance.Info,
                .Content = $"Tag '{tag.TagName}' wurde ausgewählt.",
                .Timeout = TimeSpan.FromSeconds(2)
            }
                infoSnackbar.Show()

                ' Hier kannst du weitere Aktionen implementieren, z.B. Details anzeigen
            End If
        End If
    End Sub


    ' Event-Handler für die Kontextmenü-Option "Umbenennen"
    Private Sub RenameButton_Click(sender As Object, e As RoutedEventArgs)
        Dim menuItem As MenuItem = TryCast(sender, MenuItem)
        If menuItem IsNot Nothing AndAlso menuItem.DataContext IsNot Nothing Then
            Dim selectedTag As Tag = TryCast(menuItem.DataContext, Tag)
            If selectedTag IsNot Nothing Then
                selectedTagForRename = selectedTag
                NewTagNameTextBox.Text = selectedTagForRename.TagName
                ShowRenameDialog()
            Else
                ' Snackbar-Benachrichtigung anzeigen
                Dim infoSnackbar As New Snackbar(AppSnackbar) With {
                    .Title = "Information",
                    .Appearance = ControlAppearance.Info,
                    .Content = "Tag konnte nicht gefunden werden.",
                    .Timeout = TimeSpan.FromSeconds(2)
                }
                infoSnackbar.Show()
            End If
        End If
    End Sub

    ' Event-Handler für die Kontextmenü-Option "Löschen"
    Private Sub DeleteButton_Click(sender As Object, e As RoutedEventArgs)
        Dim menuItem As MenuItem = TryCast(sender, MenuItem)
        If menuItem IsNot Nothing AndAlso menuItem.DataContext IsNot Nothing Then
            Dim selectedTag As Tag = TryCast(menuItem.DataContext, Tag)
            If selectedTag IsNot Nothing Then
                ' Bestätigungsdialog
                Dim result As System.Windows.MessageBoxResult = System.Windows.MessageBox.Show($"Möchten Sie den Tag '{selectedTag.TagName}' wirklich löschen?", "Tag löschen", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning)
                If result = System.Windows.MessageBoxResult.Yes Then
                    DeleteTag(selectedTag.TagId)
                End If
            Else
                ' Snackbar-Benachrichtigung anzeigen
                Dim infoSnackbar As New Snackbar(AppSnackbar) With {
                    .Title = "Information",
                    .Appearance = ControlAppearance.Info,
                    .Content = "Tag konnte nicht gefunden werden.",
                    .Timeout = TimeSpan.FromSeconds(2)
                }
                infoSnackbar.Show()
            End If
        End If
    End Sub

End Class