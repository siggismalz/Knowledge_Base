Imports System.Data.SQLite
Imports System.Collections.ObjectModel
Imports System.IO
Imports Wpf.Ui.Controls

Class FavoritenAnzeigen
    Public Property FavoritenListe As ObservableCollection(Of Artikel)

    Public Sub New()
        InitializeComponent()
        FavoritenListe = New ObservableCollection(Of Artikel)()
        DataContext = Me ' Setze den DataContext für die Bindungen
        LadeFavoriten()
    End Sub

    ''' <summary>
    ''' Lädt die Favoriten aus der Datenbank und füllt die Liste.
    ''' </summary>
    Private Sub LadeFavoriten()
        Try
            Dim dbFilePath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "KnowledgeBase.db")
            Using verbindung As New SQLiteConnection($"Data Source={dbFilePath};Version=3;")
                verbindung.Open()

                Dim cmd As New SQLiteCommand("
                   SELECT a.Id, a.Titel, a.Erstellt_am
                   FROM T_Knowledge_Base_Artikelverwaltung a
                   INNER JOIN T_Favoriten f ON a.Id = f.ArtikelId
                   ORDER BY f.Erstellungsdatum DESC;", verbindung)

                Dim reader As SQLiteDataReader = cmd.ExecuteReader()
                While reader.Read()
                    Dim artikel As New Artikel()
                    Artikel.ID = Convert.ToInt32(reader("Id"))
                    Artikel.Titel = reader("Titel").ToString()
                    Artikel.Erstellt_am = Convert.ToDateTime(reader("Erstellt_am"))
                    artikel.SymbolPath = "/statics/File.png" ' Pfad zum Bild
                    FavoritenListe.Add(artikel)
                End While

            End Using
        Catch ex As Exception
            MsgBox($"Fehler beim Laden der Favoriten: {ex.Message}", MsgBoxStyle.Critical, "Fehler")
        End Try
    End Sub

    ''' <summary>
    ''' Event-Handler für das Klicken auf eine CardAction.
    ''' </summary>
    Private Sub CardAction_Click(sender As Object, e As RoutedEventArgs)
        Dim cardAction As CardAction = TryCast(sender, CardAction)
        If cardAction IsNot Nothing Then
            Dim artikel As Artikel = TryCast(cardAction.DataContext, Artikel)
            If artikel IsNot Nothing Then
                ' Navigieren Sie zur Artikelanzeige und übergeben Sie den Artikel
                Dim breadcrumbItems As New ObservableCollection(Of BreadcrumbItem)()
                Dim artikelAnzeigenPage As New Artikel_anzeigen(breadcrumbItems)
                artikelAnzeigenPage.Artikel.ID = Artikel.ID
                NavigationService.Navigate(artikelAnzeigenPage)
            End If
        End If
    End Sub
End Class
