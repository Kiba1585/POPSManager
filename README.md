---

📘 README — POPSManager

🎮 POPSManager
POPSManager es una herramienta profesional diseñada para automatizar y simplificar el manejo de juegos de PlayStation 1 para POPStarter, OPL y entornos similares.  
Incluye conversión, multidisc, generación de VCD, creación de ELF, manejo de carpetas, detección automática y más.

---

✨ Características principales

- ✔ Conversión automática de BIN/CUE a VCD  
- ✔ Detección y manejo de multidisc (crea DISCS.TXT automáticamente)  
- ✔ Generación de ELF compatible con POPStarter  
- ✔ Estructura de carpetas profesional y configurable  
- ✔ Interfaz moderna y fácil de usar  
- ✔ Logs detallados para depuración  
- ✔ Totalmente modular y escalable  
- ✔ Instalador MSIX firmado digitalmente  

---

📦 Instalación

🔹 Opción 1 — Instalar directamente (recomendado)

1. Descarga el archivo POPSManager.msix desde GitHub Releases  
2. Haz doble clic para instalar  
3. Si Windows muestra SmartScreen, sigue las instrucciones de la sección siguiente

---

🛡️ Seguridad, firma digital y SmartScreen

POPSManager está firmado digitalmente con un certificado de desarrollador.  
Esto garantiza que:

- El archivo proviene del autor original  
- No ha sido modificado  
- Su integridad está verificada  

ℹ️ ¿Por qué aparece SmartScreen?

Windows SmartScreen puede mostrar advertencias cuando:

- El proyecto es nuevo  
- El certificado aún no tiene reputación  
- El archivo no ha sido descargado muchas veces  

Esto es normal en proyectos open‑source y certificados gratuitos.

✔ Cómo continuar la instalación

Si aparece el mensaje “Windows protegió tu PC”:

1. Haz clic en Más información  
2. Selecciona Ejecutar de todas formas

El instalador está firmado y es seguro.

---

🏷 Instalación opcional del certificado público
(Mejora la experiencia, elimina “Editor desconocido”, pero no elimina SmartScreen)

Si deseas que Windows reconozca a POPSManager como editor confiable:

1. Descarga la carpeta POPSManager-Certificate-Installer desde Releases  
2. Ejecuta InstallCert.bat  
3. Windows añadirá el certificado público a “Trusted People”

📁 Contenido del instalador

`
POPSManager-Certificate-Installer
│
├── POPSManager_Public.cer
├── InstallCert.ps1
└── InstallCert.bat
`

🧩 ¿Qué hace este instalador?

- Instala solo la parte pública del certificado  
- No requiere contraseña  
- No expone la clave privada  
- No compromete la seguridad del sistema  

---

🖼️ Capturas de pantalla

Vista principal
(Reemplaza la URL por tus imágenes)

`markdown
!POPSManager UI
`

Ejemplo de conversión

`markdown
!Conversión
`

---

🚀 Uso básico

1. Abre POPSManager  
2. Selecciona tus juegos en formato BIN/CUE  
3. Configura las opciones deseadas  
4. Haz clic en Procesar  
5. POPSManager generará automáticamente:
   - Carpeta del juego  
   - VCD  
   - ELF  
   - DISCS.TXT (si aplica)  
   - Estructura compatible con POPStarter  

---

🧩 Multidisc

POPSManager detecta automáticamente:

- Juegos multidisc  
- Orden correcto de los discos  
- Nombres compatibles  
- Generación de DISCS.TXT  
- Estructura POPStarter lista para copiar a tu dispositivo

---

🛠 Requisitos

- Windows 10/11  
- .NET 8 (si usas la versión portable)  
- 200 MB de espacio libre  

---

📄 Licencia

Este proyecto es open‑source.  
Puedes modificarlo, estudiarlo y contribuir.

---

🤝 Contribuciones

Las contribuciones son bienvenidas:

- Correcciones  
- Nuevas funciones  
- Mejoras de UI  
- Documentación  
- Traducciones  

---

📬 Contacto

Si deseas reportar un error o sugerir una mejora, abre un Issue en GitHub.

---
