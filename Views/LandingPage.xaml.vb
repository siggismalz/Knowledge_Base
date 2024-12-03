Imports Microsoft.Web.WebView2.Wpf

Class LandingPage
    Private Sub Card_WIKI_Click(sender As Object, e As RoutedEventArgs)
        WebViewControl.Visibility = Visibility.Visible
        HtmlContentLoader.LoadHtmlContent(WebViewControl, HtmlContentLoader.GetWikiImportanceHtml())
    End Sub

    Private Sub Card_EDEKAI_Click(sender As Object, e As RoutedEventArgs)
        WebViewControl.Visibility = Visibility.Visible
        HtmlContentLoader.LoadHtmlContent(WebViewControl, HtmlContentLoader.GetLargeLanguageModelHtml())
    End Sub

    Private Sub Card_Basiswissen_Click(sender As Object, e As RoutedEventArgs)
        WebViewControl.Visibility = Visibility.Visible
        HtmlContentLoader.LoadHtmlContent(WebViewControl, HtmlContentLoader.GetWikiHowItWorksHtml())
    End Sub
End Class
