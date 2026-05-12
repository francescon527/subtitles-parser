# Integrazione parser Word XML (WordprocessingML 2003)

Questo documento riassume le modifiche applicate per integrare il supporto ai file XML Word nel progetto `SubtitlesParser`.

## Obiettivo raggiunto

- Aggiunto supporto a un nuovo formato XML basato su WordprocessingML 2003.
- Parsing dei timecode nascosti con tag `w:vanish`.
- Gestione di segmenti multipli nello stesso paragrafo `w:p`.
- Integrazione nel flusso universale di `SubParser`.

---

## 1) Nuova classe parser XML Word

### File creato

- `SubtitlesParser/Classes/Parsers/WordXmlFormatSubtitlesParser.cs`

### Cosa fa

- Implementa l'interfaccia `IXmlFormatSubtitlesParser`.
- Legge XML tramite `XmlDocument.Load(stream)` (BOM/dichiarazione XML gestiti dal parser XML).
- Valida che il namespace root sia:
  - `http://schemas.microsoft.com/office/word/2003/wordml`
- Registra il namespace `w` in `XmlNamespaceManager`.
- Cerca i paragrafi con XPath:
  - `//w:body/w:p`
  - Questa scelta rende compatibile sia `w:document` sia `w:wordDocument`.

### Logica di estrazione

- Scorre i run `w:r` in ordine.
- Ogni run nascosto (`w:rPr/w:vanish`) con timecode avvia un nuovo blocco sottotitolo.
- I run visibili successivi (`w:t`) vengono concatenati come testo del blocco corrente.
- `w:br` viene trattato come newline.
- Alla chiusura del blocco viene creato `SubtitleItem`.

### Semplificazione applicata (refactor)

Il parser e' stato semplificato mantenendo invariato il comportamento:

- `ParseStream(...)` ora contiene solo il flusso principale ad alto livello.
- Setup e validazione sono stati accorpati in `LoadParsingContext(...)`, che restituisce:
  - `XmlDocument`
  - `XmlNamespaceManager`
  - `XmlNodeList` dei paragrafi
- La logica di parsing del singolo paragrafo e' concentrata in `ParseParagraph(...)`.
- Il reset del range temporale e' isolato in `ResetCurrentRangeFromRun(...)`.
- Helper ridondanti sono stati rimossi per rendere il flusso piu' lineare.

### Precisione timecode gestita

- Timecode supportati in formato:
  - `[hh:mm:ss.mmm - hh:mm:ss.mmm]`
  - e anche varianti Word con frazione più lunga (es. 7 cifre): `hh:mm:ss.fffffff`
- Regex aggiornata a `\d{3,7}` sulla parte frazionaria.

---

## 2) Aggiornamento dei formati supportati

### File modificato

- `SubtitlesParser/Classes/SubtitlesFormat.cs`

### Modifiche

- Aggiunto nuovo formato:
  - `WordXmlFormat`
  - `Name = "WordXml"`
  - `Extension = @"\.xml"`
- Inserito in `SupportedSubtitlesFormats`.

---

## 3) Refactor di SubParser per supporto XML

### File modificato

- `SubtitlesParser/Classes/Parsers/SubParser.cs`

### Modifiche principali

- Aggiunto dizionario parser XML:
  - `_xmlSubFormatToParser`
- Integrato flusso `ParseStream(...)` con doppio pass:
  1. parser XML (rilevamento per contenuto),
  2. parser testuali esistenti.
- Reset dello stream (`Position = 0`) prima di ogni tentativo parser.
- Aggiunto nuovo metodo:
  - `ParseFiles(IEnumerable<string> filePaths, Encoding encoding = null)`
  - Permette parsing batch di più file e merge dei `SubtitleItem`.
- In `ParseFiles`, su errore file singolo:
  - il file viene saltato,
  - si continua con i successivi,
  - viene stampato messaggio di skip.

---

## 4) Aggiornamento README

### File modificato

- `README.md`

### Modifiche

- Aggiornato conteggio formati parsing.
- Aggiunta voce per parser WordprocessingML 2003 XML.

---

## 5) File di esempio XML aggiunto

### File creato

- `Test/Content/word-xml-sample.xml`

### Scopo

- Esempio base Word XML con `w:vanish` + testo visibile per validazione parser.

---

## 6) Aggiornamento Program di test

### File modificato

- `Test/Program.cs`

### Modifiche

- Usa `ParseFiles(...)` invece di parsing singolo nel loop.
- Salva il testo dei sottotitoli (senza timecode) in:
  - `parsed-subtitles-text.txt`
- Stampa in console il path del file esportato.

---

## Note tecniche importanti

- Il riconoscimento XML Word ora è basato su contenuto (namespace/struttura), non solo su estensione.
- Questo riduce conflitti futuri con altri parser `.xml` (es. YouTube XML).
- Il namespace URI XML e' usato come identificatore semantico del vocabolario XML (non come URL da risolvere nel browser).

