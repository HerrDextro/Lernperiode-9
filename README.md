# Lernperiode-9
Containerised private fileserver using  ASP.NET and MongoDB as the Backend, and a Spectre.Console Frontend

## Zusammenfassung

Heute habe ich eine ASP.NET-API entwickelt, die Dateien in MongoDB über GridFS speichert und per GET abrufbar macht. Der Schwerpunkt liegt auf der Trennung von Metadaten und den eigentlichen Dateiinhalten innerhalb einer virtuellen Verzeichnisstruktur. Die Benutzeroberfläche erfolgt über ein Kommandozeilen-Interface (CLI), implementiert in C# unter Verwendung der Bibliothek Spectre.Console. Das gesamte System ist containerisiert mit Docker, um eine einfache Portierbarkeit und konsistente Laufzeitumgebungen zwischen Entwicklungs- und Zielsystemen zu gewährleisten.

## Aktueller Projektstatus: Infrastruktur-Setup (20.02.2026)

Heute wurde das Grundgerüst der Anwendung erstellt und die Kommunikation zwischen den einzelnen Komponenten vorbereitet.

-   **Lösungsarchitektur:** Erstellung der Multi-Projekt-Struktur bestehend aus Cloud.Api und Cloud.Client.
    
-   **Containerisierung:** Konfiguration von Dockerfile und Docker Compose zur Orchestrierung von API und Datenbank.
    
-   **Datenbankschicht:** Integration des MongoDB-Treibers und Initialisierung der GridFS-Buckets für die Datenspeicherung.
    
-   **CLI-Frontend:** Grundgerüst der Spectre.Console-Anwendung zur interaktiven Darstellung der Dateilisten.
    

----------

## Arbeitspakete (Abgabetermin: 27.02.2026)

### AP-01: Virtuelle Verzeichnislogik

-   **Beschreibung:** Implementierung einer Ordnerstruktur innerhalb der Datenbank.
    
-   **Konkrete Aufgaben:** Erweiterung der Dateimetadaten in MongoDB um eine Pfad-Eigenschaft. Anpassung der API, um Inhalte basierend auf einem spezifischen Verzeichnis abzufragen (z.B. /dokumente).
    
-   **Ziel:** Der Benutzer kann im CLI durch Ordner navigieren.
    

### AP-02: Streaming-Upload-System

-   **Beschreibung:** Entwicklung einer Funktion zum Hochladen lokaler Dateien in den Cloud-Speicher.
    
-   **Konkrete Aufgaben:** Erstellung eines POST-Endpunkts in ASP.NET, der GridFSUploadStream nutzt, um Daten direkt in die Datenbank zu streamen. Implementierung eines Upload-Befehls im Client.
    
-   **Ziel:** Dateien können vom lokalen Rechner auf den Server übertragen werden.
    

### AP-03: Datenpersistenz und Volume-Mounting

-   **Beschreibung:** Sicherstellung der dauerhaften Speicherung auf der Festplatte.
    
-   **Konkrete Aufgaben:** Konfiguration von Docker-Volumes in der docker-compose.yml, um das MongoDB-Datenverzeichnis auf einen physischen Ordner des Host-Systems (bzw. der HDD) zu mappen.
    
-   **Ziel:** Daten bleiben auch nach einem Neustart oder Löschen der Container erhalten.
    

### AP-04: Dateiverwaltung und Metadaten

-   **Beschreibung:** Implementierung grundlegender Dateioperationen (CRUD).
    
-   **Konkrete Aufgaben:** Erstellung von Endpunkten zum Löschen und Umbenennen von Dateien. Anpassung der Benutzeroberfläche zur Ausführung dieser Aktionen.
    
-   **Ziel:** Vollständige Verwaltung der Dateien über das CLI-Frontend.
