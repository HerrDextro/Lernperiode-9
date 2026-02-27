# Lernperiode-9
Containerised private fileserver using  ASP.NET and MongoDB as the Backend, and a Spectre.Console Frontend

## Zusammenfassung

Heute habe ich eine ASP.NET-API entwickelt, die Dateien in MongoDB über GridFS speichert und per GET abrufbar macht. Der Schwerpunkt liegt auf der Trennung von Metadaten und den eigentlichen Dateiinhalten innerhalb einer virtuellen Verzeichnisstruktur. Die Benutzeroberfläche erfolgt über ein Kommandozeilen-Interface (CLI), implementiert in C# unter Verwendung der Bibliothek Spectre.Console. Das gesamte System ist containerisiert mit Docker, um eine einfache Portierbarkeit und konsistente Laufzeitumgebungen zwischen Entwicklungs- und Zielsystemen zu gewährleisten. Auch habe ich zum erstes Mal mehrere Projekte im gleches Solution verwendet, und da ich mein Client auch ausversehen Containerized hatte (VS macht das automatisch) kam es zu Port konflikte. Ich habe auch gelernt, wie Containers zu debuggen in VS.

## Aktueller Projektstatus: Infrastruktur-Setup (20.02.2026)

Heute wurde das Grundgerüst der Anwendung erstellt und die Kommunikation zwischen den einzelnen Komponenten vorbereitet.

-   **Lösungsarchitektur:** Erstellung der Multi-Projekt-Struktur bestehend aus Cloud.Api und Cloud.Client.
    
-   **Containerisierung:** Konfiguration von Dockerfile und Docker Compose zur Orchestrierung von API und Datenbank.
    
-   **Datenbankschicht:** Integration des MongoDB-Treibers und Initialisierung der GridFS-Buckets für die Datenspeicherung.
    
-   **CLI-Frontend:** Grundgerüst der Spectre.Console-Anwendung zur interaktiven Darstellung der Dateilisten.

## Client UI
![Client Interface](images/clientV1.png)

----------

## Arbeitspakete 27.02.2026

Beschreibung: Implementierung einer Sicherheitsinstanz zur Identifizierung von Benutzern.  
Ziel: Nur autorisierte Benutzer können auf die API zugreifen; die Grundlage für private Datenbereiche ist geschaffen.  
- [ ] Integration von JWT (JSON Web Tokens)  
- [ ] Erstellung eines User-Modells und einer Anmelde-Logik  
- [ ] Vorbereitung der Datenbank-Metadaten auf ein Pflichtfeld `OwnerId`  
- [ ] Password hashing  


## Arbeitspakete 06.03.2026

Beschreibung: Hochperformante Verarbeitung von Dateitransfers und grundlegende CRUD-Operationen.  
Ziel: Dateien jeder Größe können stabil zwischen Client und Server übertragen werden.  
- [ ] Erstellung von Streaming-Endpunkten für Upload (POST) und Download (GET)  
- [ ] Nutzung von GridFS-Streams zur RAM-Schonung  
- [ ] Implementierung von Endpunkten zum Löschen von Dateien  
- [ ] Implementierung von Endpunkten zum Umbenennen von Dateien  


## Arbeitspakete 13.03.2026

Beschreibung: Abstraktion der flachen Dateiliste in eine hierarchische Ordnerstruktur.  
Ziel: Der Benutzer kann die Cloud wie ein klassisches Dateisystem bedienen.  
- [ ] Erweiterung der Metadaten um ein `Path`-Attribut  
- [ ] Anpassung der API-Abfragen zur Filterung nach Verzeichnissen  
- [ ] Implementierung einer Navigationslogik im CLI  


## Arbeitspakete 20.03.2026

Beschreibung: Vorbereitung des Systems für den Einsatz auf dem Raspberry Pi.  
Ziel: Die Anwendung ist sicher konfiguriert und bereit für das Deployment auf produktiver Hardware.  
- [ ] Auslagerung sensibler Daten in Environment Variables  
- [ ] Optimierung des Docker-Images für ARM-Architekturen  
