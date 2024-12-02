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
        <script src='https://cdn.ckeditor.com/4.22.1/full/ckeditor.js'></script>
        <style>
        .cke_updatewarn {{ display: none !important; }}
        html, body {{
            height: 100%;
            margin: 0;
            padding: 0;
        }}
        </style>
        </head>
        <body>
        <textarea name='editor' id='editor'></textarea>
        <script>
        CKEDITOR.replace('editor', {{
            height: 800,
            versionCheck: false,
            extraPlugins: 'maximize',
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
                {{ name: 'insert', items: ['Image', 'Flash', 'Table', 'HorizontalRule', 'SpecialChar', 'PageBreak'] }},
                '/',
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
