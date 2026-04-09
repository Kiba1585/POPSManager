---

POPSManager – Plataforma Profesional para Gestión de Juegos PS1/PS2

POPSManager es una herramienta modular, escalable y profesional diseñada para automatizar y simplificar el manejo de juegos de PlayStation 1 y PlayStation 2.  
Incluye detección avanzada de IDs, limpieza inteligente de nombres, validación de integridad, soporte multidisco, integración con bases de datos locales/online y un flujo completo de procesamiento.

---

🚀 Características Principales

- Detección automática de GameID  
  - PS1: extracción desde ejecutables y patrones internos  
  - PS2: detección avanzada mediante análisis de IOPRP.IMG

- Limpieza profesional de nombres (Title Case)  
  - Convenciones: GameID.Name (CDX).VCD  
  - Corrección automática de mayúsculas, símbolos y formatos

- Soporte Multidisco  
  - Detección, agrupación y renombrado inteligente  
  - Compatibilidad con POPStarter y OPL

- Validación de integridad  
  - Verificación de estructura, archivos requeridos y consistencia  
  - Módulo IntegrityValidator modular y extensible

- Base de datos híbrida (local + online)  
  - ps1db.json y ps2db.json  
  - Expansión dinámica y validación automática

- Interfaz moderna y modular (WPF)  
  - Notificaciones animadas  
  - UI escalable y mantenible  
  - Integración de carátulas PS1/PS2

- Arquitectura profesional  
  - Módulos independientes  
  - Servicios desacoplados  
  - Código limpio, seguro y mantenible

---

📁 Estructura del Proyecto

`
/POPSManager
 ├── Core/
 │    ├── GameIdDetector/
 │    ├── NameCleaner/
 │    ├── MultiDiscManager/
 │    ├── IntegrityValidator/
 │    └── GameDatabase/
 ├── UI/
 │    ├── Views/
 │    ├── Controls/
 │    └── Notifications/
 ├── Services/
 │    ├── PathsService.cs
 │    ├── GameProcessor.cs
 │    └── CoverService.cs
 ├── Resources/
 │    ├── Icons/
 │    └── Themes/
 └── README.md
`

---

🧠 Flujo de Procesamiento

1. El usuario selecciona uno o varios juegos  
2. El sistema detecta automáticamente el GameID  
3. Se limpia y normaliza el nombre  
4. Se valida la integridad del contenido  
5. Se consulta la base de datos local/online  
6. Se genera la estructura final (incluyendo multidisco)  
7. Se descargan carátulas opcionales  
8. Se muestra notificación visual del resultado

---

🖼️ Capturas de Pantalla

Aquí irán las imágenes una vez generadas:

`
https://copilot.microsoft.com/shares/mWgDVYwS8XhcS48tG3bmo
https://copilot.microsoft.com/shares/5R4D24GojanDNCi4dThpN
!Captura 3
!Captura 4
`

---

🔧 Requisitos

- Windows 10/11  
- .NET 8  
- Permisos de lectura/escritura en las rutas configuradas  
- Conexión opcional para carátulas y base de datos online

---

📦 Build & Release Automation

- Pipeline GitHub Actions  
- Firma con certificados  
- Versionado automático  
- Generación de instalador profesional  
- Publicación automática de assets

---

📝 Licencia

Proyecto de uso personal y educativo.  
No incluye ni distribuye contenido con copyright.

---
