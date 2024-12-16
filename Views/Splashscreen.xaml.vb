Imports System.Windows.Threading

Public Class Splashscreen
    Inherits Window

    Private loadingTimer As DispatcherTimer
    Private progressValue As Integer = 0

    Public Sub New()
        InitializeComponent()
        SetupLoadingSimulation()
    End Sub

    Private Sub SetupLoadingSimulation()
        loadingTimer = New DispatcherTimer()
        loadingTimer.Interval = TimeSpan.FromMilliseconds(100) ' Aktualisiere alle 100 ms
        AddHandler loadingTimer.Tick, AddressOf LoadingTimer_Tick
        loadingTimer.Start()
    End Sub

    Private Sub LoadingTimer_Tick(sender As Object, e As EventArgs)
        ' Simuliere das Laden mit einem zufälligen Inkrement
        Dim random As New Random()
        Dim increment As Integer = random.Next(1, 5) ' Zufälliges Inkrement zwischen 1 und 4

        progressValue += increment

        ' Stelle sicher, dass der Fortschritt nicht über 100 geht
        If progressValue > 100 Then
            progressValue = 100
            loadingTimer.Stop()
            OpenMainWindow()
        End If

        ' Aktualisiere die Fortschrittsleiste
        progressBar_.Value = progressValue
    End Sub

    Private Sub progressBar__ValueChanged(sender As Object, e As RoutedPropertyChangedEventArgs(Of Double))
        ' Optional: Zusätzliche Aktionen, wenn sich der Wert der Fortschrittsleiste ändert
    End Sub

    Private Sub OpenMainWindow()
        ' Öffne das Hauptfenster
        Dim mainWindow As New Startseite()
        mainWindow.Show()

        ' Schließe das Splash-Fenster
        Me.Close()
    End Sub
End Class
