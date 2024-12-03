Module HtmlContentLoader

    Public Async Sub LoadHtmlContent(browser As Microsoft.Web.WebView2.Wpf.WebView2, htmlContent As String)
        Await browser.EnsureCoreWebView2Async()
        browser.CoreWebView2.NavigateToString(htmlContent)
    End Sub

    Public Function GetWikiHowItWorksHtml() As String
        Return "
    <!DOCTYPE html>
    <html lang='de'>
    <head>
    <meta charset='UTF-8'>
    <title>Wie das Wiki funktioniert</title>
    <style>
        body {
            font-family: 'Roboto', Arial, sans-serif;
            background-color: #292e37;
            color: white;
            padding: 40px 20px;
            margin: 0;
        }
        h1 {
            color: #00aaff;
            margin-bottom: 20px;
            font-size: 2.5em;
        }
        p {
            line-height: 1.8;
            font-size: 1.2em;
            margin-bottom: 20px;
            text-align: justify;
        }
        ul {
            font-size: 1.1em;
            line-height: 1.8;
            margin-left: 20px;
        }
        a {
            color: #00ffaa;
            text-decoration: none;
        }
        a:hover {
            text-decoration: underline;
        }
    </style>
    </head>
    <body>
        <h1>Wie das Wiki funktioniert</h1>
        <p>Unser Wiki ist eine zentrale Plattform, auf der Wissen gesammelt, organisiert und geteilt wird. 
        Es ermöglicht unseren Teams, Informationen effizient zu speichern und miteinander zu teilen. 
        Beiträge können von allen Mitarbeitern erstellt, bearbeitet und kommentiert werden, was eine lebendige 
        und kollaborative Umgebung schafft.</p>
        
        <p>Das Wiki bietet eine Vielzahl von Funktionen, darunter:</p>
        <ul>
            <li>Eine leistungsstarke Suchfunktion, um Informationen schnell zu finden</li>
            <li>Kategorisierung und Verschlagwortung von Inhalten für bessere Organisation</li>
            <li>Versionierung, um Änderungen nachzuverfolgen und ältere Versionen wiederherzustellen</li>
            <li>Diskussionsforen, um offene Fragen zu klären</li>
        </ul>
        
        <p>Um das Wiki optimal zu nutzen, empfehlen wir:</p>
        <ul>
            <li>Regelmäßiges Aktualisieren und Überprüfen von Beiträgen</li>
            <li>Das Hinzufügen relevanter Tags, um Inhalte leicht auffindbar zu machen</li>
            <li>Die Teilnahme an Diskussionen, um die Zusammenarbeit zu fördern</li>
        </ul>
        
        <p>Das Wiki ist ein zentraler Bestandteil unserer Unternehmenskultur und unterstützt uns dabei, Wissen langfristig zu bewahren und die Zusammenarbeit zwischen Teams zu stärken. 
        Weitere Informationen finden Sie in der <a href='#'>Wiki-Dokumentation</a>.</p>
    </body>
    </html>"
    End Function

    Public Function GetLargeLanguageModelHtml() As String
        Return "
    <!DOCTYPE html>
    <html lang='de'>
    <head>
    <meta charset='UTF-8'>
    <title>Unser Large Language Model</title>
    <style>
        body {
            font-family: 'Roboto', Arial, sans-serif;
            background-color: #292e37;
            color: white;
            padding: 40px 20px;
            margin: 0;
        }
        h1 {
            color: #00aaff;
            margin-bottom: 20px;
            font-size: 2.5em;
        }
        p {
            line-height: 1.8;
            font-size: 1.2em;
            margin-bottom: 20px;
            text-align: justify;
        }
        ul {
            font-size: 1.1em;
            line-height: 1.8;
            margin-left: 20px;
        }
        a {
            color: #00ffaa;
            text-decoration: none;
        }
        a:hover {
            text-decoration: underline;
        }
    </style>
    </head>
    <body>
        <h1>Unser Large Language Model</h1>
        <p>Unser maßgeschneidertes Large Language Model (LLM) ist speziell für die Anforderungen unseres Unternehmens entwickelt worden. 
        Es verarbeitet und versteht natürliche Sprache, um Arbeitsprozesse zu optimieren und datengetriebene Entscheidungen zu unterstützen.</p>

        <p>Die Hauptmerkmale unseres LLM sind:</p>
        <ul>
            <li><strong>Textverständnis:</strong> Es analysiert und versteht komplexe Texte, um wertvolle Einblicke zu liefern.</li>
            <li><strong>Automatisierte Antworten:</strong> Bietet schnelle und präzise Antworten auf häufige Fragen.</li>
            <li><strong>Integration:</strong> Nahtlose Integration in unsere Systeme wie das Wiki, CRM und interne Chat-Plattformen.</li>
            <li><strong>Anpassbarkeit:</strong> Das LLM kann für spezifische Geschäftsprozesse trainiert werden.</li>
        </ul>

        <p>Einige Anwendungsbeispiele:</p>
        <ul>
            <li>Automatisierte Erstellung von Berichten und Zusammenfassungen</li>
            <li>Hilfe bei der Beantwortung von Kundenanfragen</li>
            <li>Analyse und Verarbeitung großer Mengen unstrukturierter Daten</li>
        </ul>

        <p>Unser LLM ist ein wichtiger Schritt in Richtung digitaler Transformation. Es hilft uns, repetitive Aufgaben zu automatisieren, die Effizienz zu steigern und gleichzeitig die Qualität der Ergebnisse zu verbessern. 
        Erfahren Sie mehr in unserer <a href='#'>LLM-Dokumentation</a>.</p>
    </body>
    </html>"
    End Function

    Public Function GetWikiImportanceHtml() As String
        Return "
    <!DOCTYPE html>
    <html lang='de'>
    <head>
    <meta charset='UTF-8'>
    <title>Warum das Wiki wichtig ist</title>
    <style>
        body {
            font-family: 'Roboto', Arial, sans-serif;
            background-color: #292e37;
            color: white;
            padding: 40px 20px;
            margin: 0;
        }
        h1 {
            color: #00aaff;
            margin-bottom: 20px;
            font-size: 2.5em;
        }
        p {
            line-height: 1.8;
            font-size: 1.2em;
            margin-bottom: 20px;
            text-align: justify;
        }
        ul {
            font-size: 1.1em;
            line-height: 1.8;
            margin-left: 20px;
        }
        li {
            margin-bottom: 10px;
        }
        a {
            color: #00ffaa;
            text-decoration: none;
        }
        a:hover {
            text-decoration: underline;
        }
    </style>
    </head>
    <body>
        <h1>Warum das Wiki wichtig ist</h1>
        <p>Das Wiki ist ein essenzielles Werkzeug, um unser Wissen effizient zu verwalten und zugänglich zu machen. 
        Es bietet eine Plattform, die es Teams ermöglicht, miteinander zu kommunizieren, Ideen auszutauschen und gemeinsam an Projekten zu arbeiten.</p>

        <p>Die Hauptvorteile unseres Wikis umfassen:</p>
        <ul>
            <li><strong>Effiziente Zusammenarbeit:</strong> Teams können nahtlos Informationen teilen und gemeinsam bearbeiten.</li>
            <li><strong>Bewahrung von Know-how:</strong> Wichtige Informationen bleiben langfristig erhalten und gehen nicht verloren.</li>
            <li><strong>Schneller Zugriff auf Informationen:</strong> Die Suchfunktion ermöglicht es, benötigtes Wissen in Sekunden zu finden.</li>
            <li><strong>Flexibilität:</strong> Inhalte können leicht aktualisiert und erweitert werden, um mit den sich ändernden Anforderungen Schritt zu halten.</li>
        </ul>

        <p>Ein gut gepflegtes Wiki schafft Transparenz und fördert die Innovation, indem es Wissen für alle Mitarbeiter zugänglich macht. 
        Es unterstützt uns dabei, Prozesse zu optimieren und fundierte Entscheidungen zu treffen.</p>

        <p>Das Wiki ist nicht nur ein Werkzeug, sondern auch ein Ausdruck unserer Unternehmenskultur. Es symbolisiert unser Engagement für offene Kommunikation und die gemeinsame Nutzung von Wissen. 
        Weitere Informationen finden Sie in der <a href='#'>Wiki-Richtlinie</a>.</p>
    </body>
    </html>"
    End Function


End Module
