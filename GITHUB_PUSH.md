# GitHub Push Anleitung

## Authentifizierung erforderlich

Git benötigt Authentifizierung, um zu GitHub zu pushen. Hier sind die Optionen:

### Option 1: Personal Access Token (Empfohlen)

1. **Token erstellen:**
   - Gehe zu: https://github.com/settings/tokens
   - Klicke "Generate new token" → "Generate new token (classic)"
   - Name: z.B. "VRChat Avatar Optimizer"
   - Scopes: Aktiviere `repo` (voller Zugriff auf Repositories)
   - Klicke "Generate token"
   - **Kopiere den Token** (wird nur einmal angezeigt!)

2. **Mit Token pushen:**
   ```bash
   git push -u origin main
   ```
   - Username: `Sophey3dx`
   - Password: **Füge den Token hier ein** (nicht dein GitHub-Passwort!)

### Option 2: GitHub CLI

```bash
gh auth login
gh repo set-default Sophey3dx/VR-Chat-Avatar-Optimizer
git push -u origin main
```

### Option 3: SSH Key

1. SSH Key zu GitHub hinzufügen
2. Remote auf SSH ändern:
   ```bash
   git remote set-url origin git@github.com:Sophey3dx/VR-Chat-Avatar-Optimizer.git
   git push -u origin main
   ```

### Option 4: GitHub Desktop

- Öffne GitHub Desktop
- File → Add Local Repository
- Wähle diesen Ordner
- Klicke "Publish repository"

## Aktueller Status

✅ Repository initialisiert
✅ Alle Dateien committed (21 Dateien, 3623 Zeilen)
✅ Remote konfiguriert
⏳ Push wartet auf Authentifizierung

