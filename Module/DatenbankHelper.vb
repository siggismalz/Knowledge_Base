Imports System.Data.SQLite
Imports System.IO

Module DatenbankHelper
    Private dbDateipfad As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "KnowledgeBase.db")
    Private verbindungsString As String = $"Data Source={dbDateipfad};Version=3;"

    Public Sub CheckAndCreateDatabase()
        If Not File.Exists(dbDateipfad) Then
            CreateDatabaseAndTables()
        Else
            ' Überprüfen, ob die benötigten Spalten existieren, und ggf. hinzufügen
            VerwendeSpaltenCheck()
        End If
    End Sub

    Private Sub VerwendeSpaltenCheck()
        Using verbindung As New SQLiteConnection(verbindungsString)
            verbindung.Open()
            ' Überprüfen, ob die Spalte 'Bestätigungscode' existiert
            Dim checkBestaetigungscode As New SQLiteCommand("PRAGMA table_info(T_Knowledge_Base_Userverwaltung);", verbindung)
            Dim reader As SQLiteDataReader = checkBestaetigungscode.ExecuteReader()
            Dim bestaetigungscodeExistiert As Boolean = False
            Dim verifiziertExistiert As Boolean = False
            While reader.Read()
                If reader("name").ToString().Equals("Bestätigungscode", StringComparison.OrdinalIgnoreCase) Then
                    bestaetigungscodeExistiert = True
                End If
                If reader("name").ToString().Equals("Verifiziert", StringComparison.OrdinalIgnoreCase) Then
                    verifiziertExistiert = True
                End If
            End While
            reader.Close()

            Using transaction As SQLiteTransaction = verbindung.BeginTransaction()
                Try
                    Using command As New SQLiteCommand(verbindung)
                        If Not bestaetigungscodeExistiert Then
                            command.CommandText = "ALTER TABLE T_Knowledge_Base_Userverwaltung ADD COLUMN Bestätigungscode TEXT;"
                            command.ExecuteNonQuery()
                        End If
                        If Not verifiziertExistiert Then
                            command.CommandText = "ALTER TABLE T_Knowledge_Base_Userverwaltung ADD COLUMN Verifiziert INTEGER DEFAULT 0;"
                            command.ExecuteNonQuery()
                        End If
                    End Using
                    transaction.Commit()
                Catch ex As Exception
                    transaction.Rollback()
                    MsgBox($"Fehler beim Aktualisieren der Datenbank: {ex.Message}")
                End Try
            End Using
        End Using
    End Sub

    Private Sub CreateDatabaseAndTables()
        Try
            SQLiteConnection.CreateFile(dbDateipfad)
            Using verbindung As New SQLiteConnection(verbindungsString)
                verbindung.Open()
                Using transaction As SQLiteTransaction = verbindung.BeginTransaction()
                    Try
                        Using command As New SQLiteCommand(verbindung)
                            Dim script As String = GetTableCreationScript()
                            Dim commands As String() = script.Split(";"c, StringSplitOptions.RemoveEmptyEntries)
                            For Each cmdText As String In commands
                                Dim trimmedCmd As String = cmdText.Trim()
                                If Not String.IsNullOrWhiteSpace(trimmedCmd) Then
                                    command.CommandText = trimmedCmd & ";"
                                    command.ExecuteNonQuery()
                                End If
                            Next
                        End Using
                        transaction.Commit()
                    Catch ex As Exception
                        transaction.Rollback()
                        MsgBox($"Fehler beim Erstellen der Datenbank: {ex.Message}")
                    End Try
                End Using
            End Using
        Catch ex As Exception
            MsgBox($"Fehler beim Erstellen der Datenbankdatei: {ex.Message}")
        End Try
    End Sub

    Private Function GetTableCreationScript() As String
        Return "
    PRAGMA foreign_keys = ON;

    -- Tabelle T_Knowledge_Base_Tags erstellen
    CREATE TABLE IF NOT EXISTS T_Knowledge_Base_Tags (
        TagId INTEGER PRIMARY KEY AUTOINCREMENT,
        TagName TEXT NOT NULL UNIQUE
    );

    -- Tabelle T_Knowledge_Base_Userverwaltung erstellen
    CREATE TABLE IF NOT EXISTS T_Knowledge_Base_Userverwaltung (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        Vorname TEXT,
        Nachname TEXT,
        Email TEXT,
        Abteilung TEXT,
        Username TEXT,
        Passwort TEXT,
        Bestätigungscode TEXT,
        Verifiziert INTEGER DEFAULT 0
    );

    -- Tabelle T_Knowledge_Base_Artikelverwaltung erstellen
    CREATE TABLE IF NOT EXISTS T_Knowledge_Base_Artikelverwaltung (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        Titel TEXT,
        Artikel_Inhalt TEXT,
        Artikel_Inhalt_HTML TEXT,
        User_Id INTEGER,
        Erstellt_am DATETIME,
        Erstellt_von TEXT,
        Bearbeitet_am DATETIME,
        Bearbeitet_von TEXT,
        FOREIGN KEY(User_Id) REFERENCES T_Knowledge_Base_Userverwaltung(id)
    );

-- Tabelle T_Favoriten erstellen
CREATE TABLE IF NOT EXISTS T_Favoriten (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ArtikelId INTEGER NOT NULL,
    UserId INTEGER NOT NULL,
    Erstellungsdatum DATETIME DEFAULT CURRENT_TIMESTAMP,
    UNIQUE (ArtikelId, UserId)
);

    -- Tabelle T_Knowledge_Base_Bilder erstellen
    CREATE TABLE IF NOT EXISTS T_Knowledge_Base_Bilder (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        Artikel_Id INTEGER NOT NULL,
        Platzhalter TEXT NOT NULL,
        Bilddaten TEXT NOT NULL,
        FOREIGN KEY(Artikel_Id) REFERENCES T_Knowledge_Base_Artikelverwaltung(id)
    );

    -- Tabelle T_Knowledge_Base_Filesystem erstellen
    CREATE TABLE IF NOT EXISTS T_Knowledge_Base_Filesystem (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        Name TEXT NOT NULL,
        ist_verzeichnis INTEGER NOT NULL,
        parent_id INTEGER,
        Artikel_ID INTEGER,
        FOREIGN KEY(parent_id) REFERENCES T_Knowledge_Base_Filesystem(id),
        FOREIGN KEY(Artikel_ID) REFERENCES T_Knowledge_Base_Artikelverwaltung(id)
    );

    -- Tabelle T_Knowledge_Base_ArtikelTags erstellen
    CREATE TABLE IF NOT EXISTS T_Knowledge_Base_ArtikelTags (
        ArtikelId INTEGER NOT NULL,
        TagId INTEGER NOT NULL,
        PRIMARY KEY (ArtikelId, TagId),
        FOREIGN KEY(ArtikelId) REFERENCES T_Knowledge_Base_Artikelverwaltung(id),
        FOREIGN KEY(TagId) REFERENCES T_Knowledge_Base_Tags(TagId)
    );

    -- Tabelle T_Knowledge_Base_Artikelversionen erstellen
    CREATE TABLE IF NOT EXISTS T_Knowledge_Base_Artikelversionen (
        VersionId INTEGER PRIMARY KEY AUTOINCREMENT,
        ArtikelId INTEGER NOT NULL,
        Titel TEXT NOT NULL,
        Artikel_Inhalt_HTML TEXT NOT NULL,
        Artikel_Inhalt TEXT NOT NULL,
        User_Id INTEGER NOT NULL,
        Erstellt_am DATETIME NOT NULL,
        Versioniert_am DATETIME NOT NULL,
        Erstellt_von TEXT,
        FOREIGN KEY (ArtikelId) REFERENCES T_Knowledge_Base_Artikelverwaltung(id),
        FOREIGN KEY (User_Id) REFERENCES T_Knowledge_Base_Userverwaltung(id)
    );
    "
    End Function

    Public Function GetVerbindung() As SQLiteConnection
        Return New SQLiteConnection(verbindungsString)
    End Function
End Module
