POPSManager
Gestor profesional y modular para automatizar flujos de trabajo de juegos PlayStation 1 en POPStarter

POPSManager es una herramienta moderna, modular y totalmente automatizada diseñada para simplificar y profesionalizar la preparación de juegos de PlayStation 1 para POPStarter. El programa gestiona desde la validación de archivos hasta la creación de VCD, generación de ELF, renombrado profesional, soporte multidisco y creación automática de archivos auxiliares como DISCS.TXT y cheats.

Su arquitectura está pensada para ser escalable, mantenible y compatible con pipelines CI/CD como GitHub Actions.

---

✨ Características principales

🔧 Procesamiento automático de juegos
- Detección automática del Game ID.
- Renombrado profesional con formato:  
  GameID.Name (CDX).VCD
- Validación de integridad de archivos.
- Conversión y empaquetado automático.

💿 Soporte multidisco
- Detección automática de múltiples discos.
- Generación automática de DISCS.TXT con el orden correcto.
- Renombrado consistente entre discos.
- Integración completa con POPStarter.

📁 Generación de estructura POPStarter
- Creación automática de carpetas necesarias.
- Generación del ELF correspondiente con rutas internas correctas.
- Compatibilidad con PathsService para evitar rutas hardcodeadas.

🎮 Archivos auxiliares
- Generación automática de:
  - CHEATS.TXT
  - DISCS.TXT
  - Archivos de configuración POPStarter

🖥️ Interfaz moderna
- UI limpia, profesional y compatible con temas claros y oscuros.
- Progreso en tiempo real, notificaciones y logs detallados.

🔐 Preparado para CI/CD
- Arquitectura compatible con pipelines automatizados.
- Soporte para empaquetado MSIX y firma digital.
- Código modular, limpio y mantenible.

---

🧩 Arquitectura del proyecto

POPSManager está dividido en módulos independientes para facilitar mantenimiento y escalabilidad:

| Módulo | Descripción |
|-------|-------------|
| GameProcessor | Núcleo del procesamiento de juegos, validación, multidisco y generación de archivos. |
| PathsService | Descubrimiento dinámico de rutas y binarios, evitando riesgos legales. |
| Converter / VCD Builder | Conversión y empaquetado de imágenes PS1 a formato POPStarter. |
| UI (Views + Controls) | Interfaz moderna, modular y compatible con Dark Mode. |
| LoggingService | Registro detallado de cada operación. |

---

🚀 Cómo usar POPSManager

1. Ejecuta el programa.
2. Selecciona la carpeta o archivo del juego.
3. POPSManager detectará automáticamente:
   - Game ID  
   - Número de discos  
   - Nombre del juego  
4. El programa generará:
   - VCD renombrado profesionalmente  
   - Estructura POPStarter  
   - ELF correspondiente  
   - DISCS.TXT (si aplica)  
   - Cheats y archivos auxiliares  
5. El resultado final estará listo para copiar a tu dispositivo.

---

📂 Estructura generada

`
POPS/
 ├── XXGAMEID.ELF
 ├── XXGAMEID.VCD
 ├── CHEATS.TXT
 ├── DISCS.TXT   (solo multidisco)
 └── POPS.CFG
`

---

🛠️ Requisitos

- Windows 10/11  
- .NET 8  
- POPStarter compatible con PS2  
- Juegos en formato BIN/CUE o ISO  

---

🧪 Pipeline CI/CD (Opcional)

POPSManager está preparado para integrarse con GitHub Actions:

- Compilación automática  
- Firma digital con certificados  
- Empaquetado MSIX  
- Publicación de releases con assets generados  

---

🤝 Contribuciones

Las contribuciones son bienvenidas.  
El proyecto está diseñado para ser modular, limpio y fácil de extender.

---

📜 Licencia

Este proyecto no incluye binarios de POPStarter ni ningún archivo con copyright.  
El usuario debe proporcionar sus propios binarios legalmente obtenidos.

---
