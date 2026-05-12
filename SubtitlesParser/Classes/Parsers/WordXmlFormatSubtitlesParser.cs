using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;
using System.Xml;

namespace SubtitlesParser.Classes.Parsers;

public sealed class WordXmlFormatSubtitlesParser : IXmlFormatSubtitlesParser
{
    private const string Word2003Namespace = "http://schemas.microsoft.com/office/word/2003/wordml";
    // Old strict regex (kept for traceability):
    // private static readonly Regex TimeRangeRegex = new(@"\[(?<start>\d{2}:\d{2}:\d{2}\.\d{3})\s*-\s*(?<end>\d{2}:\d{2}:\d{2}\.\d{3})\]", RegexOptions.Compiled);
    // Word-exported files may contain fractional seconds with up to 7 digits.
    private static readonly Regex TimeRangeRegex = new(@"\[(?<start>\d{2}:\d{2}:\d{2}\.\d{3,7})\s*-\s*(?<end>\d{2}:\d{2}:\d{2}\.\d{3,7})\]", RegexOptions.Compiled);

    public List<SubtitleItem> ParseStream(Stream xmlStream, Encoding encoding)
    {
        var (xmlDoc, namespaceManager, paragraphNodes) = LoadParsingContext(xmlStream);

        var items = new List<SubtitleItem>();
        foreach (var paragraph in paragraphNodes.Cast<XmlNode>())
        {
            ParseParagraph(paragraph, namespaceManager, items);
        }

        return items.Any()
            ? items
            : throw new ArgumentException("Stream is not in a valid Word XML subtitles format");
    }

    private static (XmlDocument Document, XmlNamespaceManager NamespaceManager, XmlNodeList ParagraphNodes) LoadParsingContext(Stream xmlStream)
    {
        // Parse XML stream, letting XmlDocument use BOM/XML declaration to decode text.
        var xmlDoc = new XmlDocument();
        xmlDoc.Load(xmlStream);

        if (xmlDoc.DocumentElement == null)
        {
            throw new ArgumentException("Stream is not a valid XML document");
        }

        if (!string.Equals(xmlDoc.DocumentElement.NamespaceURI, Word2003Namespace, StringComparison.Ordinal))
        {
            throw new ArgumentException("Stream is not a WordprocessingML 2003 XML document");
        }

        var namespaceManager = new XmlNamespaceManager(xmlDoc.NameTable);
        namespaceManager.AddNamespace("w", Word2003Namespace);
        
        // WordprocessingML 2003 can use different root names (e.g. w:document, w:wordDocument).
        // We anchor on w:body/w:p so format detection is robust across those variants.
        var paragraphNodes = xmlDoc.DocumentElement!.SelectNodes("//w:body/w:p", namespaceManager);
        if (paragraphNodes == null || paragraphNodes.Count == 0)
        {
            throw new ArgumentException("Stream is not a valid Word XML subtitles structure");
        }

        return (xmlDoc, namespaceManager, paragraphNodes);
    }

    private static void ParseParagraph(XmlNode paragraph, XmlNamespaceManager namespaceManager, List<SubtitleItem> items)
    {
        var runNodes = paragraph.SelectNodes("./w:r", namespaceManager);
        if (runNodes == null)
        {
            return;
        }

        TimeSpan? currentStart = null;
        TimeSpan? currentEnd = null;
        var currentText = new StringBuilder();

        foreach (var run in runNodes.Cast<XmlNode>())
        {
            if (run.SelectSingleNode("./w:rPr/w:vanish", namespaceManager) != null)
            {
                AddCurrentItemIfReady(items, currentStart, currentEnd, currentText);
                ResetCurrentRangeFromRun(
                    GetRunText(run, namespaceManager),
                    ref currentStart,
                    ref currentEnd,
                    currentText);
                continue;
            }

            if (run.SelectSingleNode("./w:br", namespaceManager) != null)
            {
                currentText.AppendLine();
            }

            var runText = GetRunText(run, namespaceManager);
            if (!string.IsNullOrEmpty(runText))
            {
                currentText.Append(runText);
            }
        }

        AddCurrentItemIfReady(items, currentStart, currentEnd, currentText);
    }

    private static string GetRunText(XmlNode run, XmlNamespaceManager namespaceManager)
    {
        var runTextNodes = run.SelectNodes("./w:t", namespaceManager);
        return runTextNodes == null
            ? string.Empty
            : string.Concat(runTextNodes.Cast<XmlNode>().Select(textNode => textNode.InnerText));
    }

    private static void ResetCurrentRangeFromRun(
        string runText,
        ref TimeSpan? currentStart,
        ref TimeSpan? currentEnd,
        StringBuilder currentText)
    {
        var match = TimeRangeRegex.Match(runText);
        if (!match.Success)
        {
            currentStart = null;
            currentEnd = null;
            currentText.Clear();
            return;
        }

        currentStart = TimeSpan.Parse(match.Groups["start"].Value, CultureInfo.InvariantCulture);
        currentEnd = TimeSpan.Parse(match.Groups["end"].Value, CultureInfo.InvariantCulture);
        currentText.Clear();
    }

    private static void AddCurrentItemIfReady(List<SubtitleItem> items, TimeSpan? currentStart, TimeSpan? currentEnd, StringBuilder currentText)
    {
        if (currentStart == null || currentEnd == null)
        {
            return;
        }

        var text = currentText.ToString().Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        items.Add(new SubtitleItem()
        {
            StartTime = currentStart.Value,
            EndTime = currentEnd.Value,
            Lines = [text]
        });
    }
}