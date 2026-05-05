# **Integrazione VTT Parser**

# Modifiche apportate al parser VTT

## 1) Filtrare eventuali formatting tags presenti nel testo

È stata introdotta una pulizia delle linee testuali dei cue per rimuovere tag di formattazione inline (ad esempio \`&lt;i&gt;\`, \`&lt;b&gt;\`, \`&lt;u&gt;\`, \`&lt;c...&gt;\`, ecc.), così da ottenere un testo più “pulito” lato consumo applicativo.

### Obiettivo

Evitare che i tag markup WebVTT finiscano nel testo mostrato o elaborato a valle.

### Impatto

\- Migliora la leggibilità del contenuto estratto.

\- Riduce il rischio di dover ripulire il testo in fasi successive del flusso.

\- Mantiene la compatibilità con cue che non contengono formattazione.

\---

## 2) Aggiungere la gestione dei cue settings

Il parser è stato esteso per riconoscere e gestire i cue settings presenti dopo il timecode finale (es. \`align:start position:0% line:90%\`).

### Problema originale

Il pattern \`GetNonStandardWebvttTimecodeRegex\` terminava con \`\$\` senza contemplare i settings opzionali dopo l’end time.

Di conseguenza, righe valide come:

\`00:00:10.000 --> 00:00:12.000 align:start position:0%\`

non venivano matchate correttamente, con possibile scarto dell’intera cue.

### Intervento

È stata resa opzionale la parte finale dei settings nel regex, permettendo il parsing di:

\- start/end timecode

\- eventuali coppie chiave:valore dei settings

### Impatto

\- Aumenta la conformità ai file VTT reali.

\- Riduce i falsi negativi di parsing.

\- Consente un comportamento più robusto con contenuti provenienti da player/editor diversi.

\---

## 3) Gestire ID cue alfanumerici non solo numerici

La logica di riconoscimento degli ID cue è stata ampliata: non solo ID interamente numerici, ma anche token alfanumerici (es. \`cue-A1\`, \`intro_scene_02\`, \`ABC123\`).

### Problema originale

L’ID era riconosciuto solo con check numerico, quindi cue con ID non numerico potevano essere interpretate in modo errato o innescare resync.

### Intervento

È stato introdotto un criterio più flessibile per l’identificazione dell’ID (incluso controllo aggiuntivo per evitare confusione con testo normale), mantenendo il flusso del parser invariato sulla struttura della cue:

1.  ID opzionale
    
2.  timestamp obbligatorio
    
3.  testo cue
    

### Impatto

\- Supporto a file WebVTT più eterogenei.

\- Migliore compatibilità con generatori che usano ID descrittivi.

\- Riduzione dei casi in cui cue valide non vengono acquisite.