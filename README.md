# POPSManager – Plataforma Profesional para Gestión de Juegos PS1/PS2

**POPSManager** es una herramienta modular, escalable y profesional diseñada para automatizar y optimizar el manejo de juegos de PlayStation 1 y PlayStation 2.  
Incluye detección avanzada de IDs, limpieza inteligente de nombres, validación de integridad, soporte multidisco, integración con bases de datos locales/online, un flujo completo de procesamiento y una interfaz moderna multi‑idioma.

---

## 🚀 Características Principales

### 🎯 Detección y Procesamiento
- **Detección automática de GameID**  
  - PS1: extracción desde ejecutables y patrones internos.  
  - PS2: análisis avanzado mediante IOPRP.IMG.  
- **Limpieza profesional de nombres (Title Case)**  
  - Convenciones: `GameID.Name (CDX).VCD`.  
  - Corrección automática de mayúsculas, símbolos y formatos.  
- **Soporte Multidisco**  
  - Detección, agrupación y renombrado inteligente.  
  - Compatibilidad total con POPStarter y OPL.  
- **Validación de integridad**  
  - Verificación de estructura, archivos requeridos y consistencia.  
  - Módulo `IntegrityValidator` modular y extensible.  
- **Base de datos híbrida (local + online)**  
  - Archivos `ps1db.json` y `ps2db.json` embebidos.  
  - Expansión dinámica y validación automática.  

### 🎨 Interfaz Moderna y Multi‑Idioma
- **Interfaz WPF moderna, responsiva y escalable**  
  - Se adapta a cualquier resolución (mínimo 800×600).  
  - Inicio maximizado para aprovechar toda la pantalla.  
- **Notificaciones visuales animadas**  
  - Toasts de éxito, error, advertencia e información.  
- **7 idiomas soportados** (cambiable en caliente desde la UI)  
  Español, English, Français, Deutsch, Italiano, Português, 日本語.  
- **Panel de progreso en tiempo real** para cada juego procesado.  

### 🧠 Automatización Inteligente
- **Motor de automatización configurable**  
  - Modos: Automático, Asistido (pregunta) o Manual.  
  - Reglas independientes para conversión, multidisco, carátulas, cheats, etc.  
- **Copia automática de recursos personalizados** (LNG y THM)  
  - Configura las carpetas de origen de archivos de idioma (`.lng`) y temas (`.thm`).  
  - Se copian a la raíz de OPL según el comportamiento elegido.  

### 📦 Build & Release Automatizado
- **Pipeline CI/CD en GitHub Actions**  
  - Compilación en Release con .NET 8.0.  
  - Generación de artefactos: Portable (ZIP), Self‑Contained (ZIP), Instalador Inno Setup (`.exe`) y Paquete MSIX firmado.  
  - Checksums SHA256 para verificar integridad.  
  - Creación automática de Releases en GitHub.  

---

## 📁 Estructura del Proyecto
