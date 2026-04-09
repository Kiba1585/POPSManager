---

POPSManager – Plataforma Profesional para Gestión de Juegos PS1/PS2

POPSManager es una herramienta modular, escalable y profesional diseñada para automatizar y optimizar el manejo de juegos de PlayStation 1 y PlayStation 2.  
Incluye detección avanzada de IDs, limpieza inteligente de nombres, validación de integridad, soporte multidisco, integración con bases de datos locales/online y un flujo completo de procesamiento.

---

🚀 Características Principales

- Detección automática de GameID  
  - PS1: extracción desde ejecutables y patrones internos  
  - PS2: análisis avanzado mediante IOPRP.IMG

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

A continuación se muestran capturas conceptuales generadas para ilustrar el flujo completo del POPSManager.

---

1. Pantalla Principal
[!https://copilot.microsoft.com/shares/mWgDVYwS8XhcS48tG3bmo]

Vista general del POPSManager mostrando la lista de juegos, acciones principales y flujo de procesamiento.

---

2. Detección Automática de GameID
[![https://copilot.microsoft.com/shares/5R4D24GojanDNCi4dThpN])]

Interfaz de análisis y detección automática del GameID, región y coincidencia en la base de datos.

---

3. Gestión Multidisco
[!https://copilot.microsoft.com/shares/EAH2N16pHtvbGR2in3srW]

Vista de agrupación y renombrado de discos múltiples, con indicadores de estado y opciones de combinación.

---

4. Procesamiento Completo
[!https://copilot.microsoft.com/shares/CzvgcKELMFJyEMCqoqhn2]

Flujo final del procesamiento con validación, limpieza, descarga de carátulas y notificación de éxito.

---

🎨 Créditos Visuales

Las imágenes incluidas en este README fueron generadas conceptualmente para documentación del proyecto.  
No representan capturas reales del software, sino mockups técnicos diseñados para ilustrar el flujo de trabajo.

- Diseño conceptual: Raidel  
- Generación visual: Microsoft Copilot (IA)  
- Estilo: UI WPF moderna + elementos técnicos PlayStation  
- Proyecto: POPSManager – Gestión Profesional de Juegos PS1/PS2

---

📚 Notas de Documentación

Este README y sus recursos visuales forman parte de la documentación oficial del proyecto POPSManager.  
Pueden utilizarse en presentaciones, portafolios o demostraciones técnicas manteniendo la atribución correspondiente.

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
