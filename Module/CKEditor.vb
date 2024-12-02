Imports Microsoft.Web.WebView2.Core
Imports Newtonsoft.Json
Imports System.Threading.Tasks
Imports System.IO
Imports HtmlAgilityPack
Imports System.Text.RegularExpressions

Module CKEditor
    ' Methode zum Laden des CKEditor
    Public Async Sub Ckeditor_laden(browser As Microsoft.Web.WebView2.Wpf.WebView2, messageHandler As EventHandler(Of CoreWebView2WebMessageReceivedEventArgs))
        Await browser.EnsureCoreWebView2Async()
        Dim html As String = $"<!DOCTYPE html>
        <html lang='de'>
        <head>
        <meta charset='UTF-8'>
        <title>CKEditor Integration</title>
        <script src='https://cdn.ckeditor.com/4.22.1/full-all/ckeditor.js'></script>

        <!-- Highlight.js Stylesheet -->
        <link rel='stylesheet' href='https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.8.0/styles/default.min.css'>

        <!-- Highlight.js Hauptskript -->
        <script src='https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.8.0/highlight.min.js'></script>

        <!-- Zusätzliche Sprachmodule für Highlight.js -->
        <script src='https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.8.0/languages/vbscript.min.js'></script>
        <script src='https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.8.0/languages/vbnet.min.js'></script>
        <script src='https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.8.0/languages/sql.min.js'></script>
        <script src='https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.8.0/languages/python.min.js'></script>

        <!-- Hinweis: Für VBA gibt es kein separates Modul, wir verwenden 'vbnet' als Ersatz -->

        <style>
        .cke_updatewarn {{ display: none !important; }}
        html, body {{
            height: 100%;
            margin: 0;
            padding: 0;
        }}
        pre code {{
            display: block;
            padding: 0.5em;
            background: #f5f5f5;
            border: 1px solid #ddd;
            border-radius: 4px;
        }}
        </style>
        </head>
        <body>
        <textarea name='editor' id='editor'></textarea>
        <script>
        CKEDITOR.replace('editor', {{
            height: 800,
            versionCheck: false,
            extraPlugins: 'maximize,codesnippet',
            codeSnippet_theme: 'default',
            codeSnippet_languages: {{
                vbnet: 'VB.NET',
                vbscript: 'VBS',
                sql: 'SQL',
                python: 'Python',
                vba: 'VBA'  // Wir verwenden 'vbnet' für VBA-Syntax
            }},
            on: {{
                instanceReady: function(evt) {{
                    this.execCommand('maximize');

                    // Funktionen definieren, nachdem der Editor bereit ist
                    window.getEditorinhalt_mit_HTML = function() {{
                        return CKEDITOR.instances.editor.getData();
                    }};

                    window.getEditorinhalt_ohne_HTML = function() {{
                        var editorData = CKEDITOR.instances.editor.getData();
                        var tempDiv = document.createElement('div');
                        tempDiv.innerHTML = editorData;
                        return tempDiv.textContent || tempDiv.innerText;
                    }};

                    // Highlight.js konfigurieren und initialisieren
                    hljs.configure({{languages: ['vbnet', 'vbscript', 'sql', 'python']}});
                    hljs.highlightAll();

                    // Signal an WPF senden, dass der Editor bereit ist
                    window.chrome.webview.postMessage('editorReady');
                }}
            }},
            toolbar: [
                {{ name: 'document', items: ['Preview','Templates'] }},
                {{ name: 'clipboard', items: ['Cut', 'Copy', '-', 'Undo', 'Redo'] }},
                {{ name: 'editing', items: ['Find', 'Replace', '-', 'SelectAll'] }},
                {{ name: 'basicstyles', items: ['Bold', 'Italic', 'Underline', 'Strike', '-', 'RemoveFormat'] }},
                {{ name: 'paragraph', items: ['NumberedList', 'BulletedList', '-', 'Outdent', 'Indent', '-', 'Blockquote'] }},
                {{ name: 'links', items: ['Link', 'Unlink'] }},
                {{ name: 'insert', items: ['Image', 'Flash', 'Table', 'HorizontalRule', 'SpecialChar', 'PageBreak', 'CodeSnippet'] }},
                '/','/',
                {{ name: 'styles', items: ['Styles', 'Format', 'Font', 'FontSize'] }},
                {{ name: 'colors', items: ['TextColor', 'BGColor'] }},
                {{ name: 'tools', items: ['Maximize', 'ShowBlocks'] }},
            ]
        }});
        </script>
        </body>
        </html>"

        browser.CoreWebView2.NavigateToString(html)

        ' Warten, bis der CKEditor bereit ist
        AddHandler browser.CoreWebView2.WebMessageReceived, messageHandler
    End Sub
End Module
